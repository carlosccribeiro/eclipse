using System;
using System.Collections.Generic;
using Eclipse.Core.Random;

namespace Eclipse.Core.RunGen.Map
{
    /// <summary>
    /// Deterministic run-map generator (09 §11). Same seed + same context = same map.
    /// <para>
    /// Algorithm: whole-map attempts (<see cref="MapGenConfig.MaxAttempts"/>); each attempt builds row by row.
    /// Per row: node count (capped so the row stays coverable by the previous one), then types by rejection
    /// sampling (<see cref="MapGenConfig.RowAttempts"/> tries, each failure counts as a row rebuild), then
    /// monotone non-crossing edges (retrying layouts that link two Elites), then door rewards (redrawn until
    /// 09 §11.3 rule 7 holds) and event ids. The finished map goes through <see cref="MapConstraints"/>; only a
    /// valid map is returned. After all attempts fail, a deterministic valid template is returned
    /// (<see cref="RunMap.UsedFallback"/>).
    /// </para>
    /// <para>
    /// Node-count cap: each node links to 1-2 next nodes and every node must be reachable (09 §11.1), so a row can
    /// have at most (previous row size + 1) nodes. Hence row 10 after the single Guardian has 2 nodes (spec range 2-4)
    /// and row 6 has 3 when row 5 has 2. Reported as a design note on 09 §11.1.
    /// </para>
    /// </summary>
    public static class MapGenerator
    {
        public static RunMap Generate(ulong seed, MapGenContext ctx = null)
        {
            if (ctx == null) ctx = new MapGenContext();
            MapGenConfig cfg = ctx.Config ?? MapGenConfig.CreateDefault();

            var streams = new RngStreams(seed);
            ref Xoshiro128StarStar rng = ref streams.Get(RngStream.Map);

            var violations = new List<string>();
            int rowRebuilds = 0;
            for (int attempt = 1; attempt <= cfg.MaxAttempts; attempt++)
            {
                MapNode[][] rows = TryBuild(ref rng, ctx, cfg, ref rowRebuilds);
                if (rows == null) continue;
                violations.Clear();
                MapConstraints.ValidateRows(rows, cfg, ctx, violations);
                if (violations.Count != 0) continue;
                return new RunMap(seed, rows) { GenerationAttempts = attempt, RowRebuilds = rowRebuilds };
            }

            MapNode[][] fb = BuildTemplate(cfg);
            violations.Clear();
            MapConstraints.ValidateRows(fb, cfg, ctx, violations);
            if (violations.Count != 0)
                throw new InvalidOperationException("MapGenerator fallback template is invalid: " + violations[0]);
            return new RunMap(seed, fb) { GenerationAttempts = cfg.MaxAttempts + 1, RowRebuilds = rowRebuilds, UsedFallback = true };
        }

        // ---------------------------------------------------------------- sampling

        private static MapNode[][] TryBuild(ref Xoshiro128StarStar rng, MapGenContext ctx, MapGenConfig cfg, ref int rowRebuilds)
        {
            var rows = new MapNode[MapGenConfig.BossRow][];
            int prevCount = 0;
            for (int r = 0; r < MapGenConfig.RowCount; r++)
            {
                RowSpec spec = cfg.Rows[r];
                int max = spec.MaxNodes;
                if (r > 0 && max > prevCount + 1) max = prevCount + 1; // reachability cap, see class docs
                if (spec.MinNodes > max) return null;

                MapNode[] row = null;
                for (int t = 0; t < cfg.RowAttempts && row == null; t++)
                {
                    int count = max == spec.MinNodes ? max : rng.Range(spec.MinNodes, max + 1);
                    row = DrawRow(ref rng, cfg, spec, r + 1, count);
                    if (row == null) rowRebuilds++;
                }
                if (row == null) return null;
                rows[r] = row;
                prevCount = row.Length;

                if (r > 0 && !BuildEdges(ref rng, cfg, rows[r - 1], row)) return null;
            }

            rows[MapGenConfig.BossRow - 1] = new[]
            {
                new MapNode { Row = MapGenConfig.BossRow, Column = 0, Type = RoomType.Boss }
            };
            LinkToBoss(rows);

            if (!DrawRewards(ref rng, cfg, rows)) return null;
            AssignEvents(ref rng, ctx, rows);
            return rows;
        }

        /// <summary>Draws the types of one row; null when the row-local rules reject it.</summary>
        private static MapNode[] DrawRow(ref Xoshiro128StarStar rng, MapGenConfig cfg, RowSpec spec, int row, int count)
        {
            var nodes = new MapNode[count];
            for (int c = 0; c < count; c++) nodes[c] = new MapNode { Row = row, Column = c };
            int stratum = MapGenConfig.StratumOf(row);

            switch (spec.Kind)
            {
                case RowKind.AllCombat:
                    for (int c = 0; c < count; c++) nodes[c].Type = RoomType.Combat;
                    return nodes;
                case RowKind.Guardian:
                    for (int c = 0; c < count; c++) nodes[c].Type = RoomType.Guardian;
                    return nodes;
                case RowKind.Antechamber:
                    for (int c = 0; c < count; c++) { nodes[c].Type = RoomType.Rest; nodes[c].IsAntechamber = true; }
                    return nodes;
                case RowKind.Clearing:
                    // 09 §11.1 row 5: Shrine or Treasure (stratum I weights) plus at least one Rest.
                    for (int c = 0; c < count; c++)
                        nodes[c].Type = DrawType(ref rng, cfg, spec, stratum, RoomType.Shrine, RoomType.Treasure);
                    nodes[rng.Range(0, count)].Type = RoomType.Rest;
                    return nodes;
                default:
                    for (int c = 0; c < count; c++)
                        nodes[c].Type = DrawType(ref rng, cfg, spec, stratum, null, null);
                    // 09 §11.3 rule 5: a Challenge row needs a node that is neither Challenge nor Elite.
                    if (MapConstraints.RowHasChallenge(nodes))
                    {
                        bool alt = false;
                        for (int c = 0; c < count; c++)
                            if (nodes[c].Type != RoomType.Challenge && nodes[c].Type != RoomType.Elite) { alt = true; break; }
                        if (!alt) return null;
                    }
                    return nodes;
            }
        }

        private static RoomType DrawType(ref Xoshiro128StarStar rng, MapGenConfig cfg, RowSpec spec, int stratum,
            RoomType? only1, RoomType? only2)
        {
            float total = 0f;
            for (int i = 0; i < MapGenConfig.WeightedTypeCount; i++)
                total += Candidate(cfg, spec, stratum, i, only1, only2);
            if (total <= 0f) return RoomType.Combat; // degenerate weights: Combat is allowed everywhere weighted
            float pick = rng.NextFloat01() * total;
            int last = 0;
            for (int i = 0; i < MapGenConfig.WeightedTypeCount; i++)
            {
                float w = Candidate(cfg, spec, stratum, i, only1, only2);
                if (w <= 0f) continue;
                last = i;
                if (pick < w) return MapGenConfig.WeightedTypes[i];
                pick -= w;
            }
            return MapGenConfig.WeightedTypes[last];
        }

        private static float Candidate(MapGenConfig cfg, RowSpec spec, int stratum, int col, RoomType? only1, RoomType? only2)
        {
            RoomType t = MapGenConfig.WeightedTypes[col];
            if (!spec.Allows(t)) return 0f;
            if (only1.HasValue && t != only1.Value && t != only2.Value) return 0f;
            return cfg.Weights[stratum - 1, col];
        }

        // ---------------------------------------------------------------- edges

        /// <summary>
        /// Monotone interval edges: node i links to columns [lo_i, hi_i] (1-2 values), lo_0 = 0, hi_last = m-1,
        /// lo_(i+1) in {hi_i, hi_i+1}. Intervals only share endpoints, so edges never cross, and every next-row
        /// node is covered. Retries layouts that join two Elites; if none is found the last one is kept and the
        /// final validation decides.
        /// </summary>
        private static bool BuildEdges(ref Xoshiro128StarStar rng, MapGenConfig cfg, MapNode[] a, MapNode[] b)
        {
            if (b.Length > a.Length + 1) return false;
            for (int t = 0; t < cfg.EdgeAttempts; t++)
            {
                AssignEdges(ref rng, a, b.Length, false);
                if (!HasEliteEdge(a, b)) return true;
            }
            return true;
        }

        private static bool HasEliteEdge(MapNode[] a, MapNode[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].Type != RoomType.Elite) continue;
                for (int k = 0; k < a[i].Next.Length; k++)
                    if (b[a[i].Next[k]].Type == RoomType.Elite) return true;
            }
            return false;
        }

        private static void AssignEdges(ref Xoshiro128StarStar rng, MapNode[] a, int m, bool deterministic)
        {
            int n = a.Length;
            int lo = 0;
            for (int i = 0; i < n; i++)
            {
                int r = n - 1 - i; // nodes still to place after this one
                bool ok0 = HiOk(lo, r, m), ok1 = HiOk(lo + 1, r, m);
                int hi;
                if (ok0 && ok1) hi = deterministic ? lo + 1 : (rng.Range(0, 2) == 0 ? lo : lo + 1);
                else hi = ok1 ? lo + 1 : lo;

                a[i].Next = hi == lo ? new[] { lo } : new[] { lo, hi };

                if (r > 0)
                {
                    bool n0 = hi + r >= m - 1;                  // next lo = hi
                    bool n1 = hi + 1 <= m - 1 && hi + 1 + r >= m - 1; // next lo = hi + 1
                    if (n0 && n1) lo = deterministic ? hi : (rng.Range(0, 2) == 0 ? hi : hi + 1);
                    else lo = n1 ? hi + 1 : hi;
                }
            }
        }

        // Is hi a feasible upper target for a node followed by r more nodes, for a next row of m nodes?
        private static bool HiOk(int hi, int r, int m)
        {
            if (hi > m - 1) return false;
            if (r == 0) return hi == m - 1;
            return hi == m - 1 || hi + 1 + r >= m - 1;
        }

        private static void LinkToBoss(MapNode[][] rows)
        {
            MapNode[] last = rows[MapGenConfig.RowCount - 1];
            for (int c = 0; c < last.Length; c++) last[c].Next = new[] { 0 };
        }

        // ---------------------------------------------------------------- rewards and events

        private static bool DrawRewards(ref Xoshiro128StarStar rng, MapGenConfig cfg, MapNode[][] rows)
        {
            for (int t = 0; t < cfg.RewardAttempts; t++)
            {
                for (int r = 0; r < rows.Length; r++)
                    for (int c = 0; c < rows[r].Length; c++)
                    {
                        MapNode n = rows[r][c];
                        n.Reward = n.Type == RoomType.Combat ? DrawReward(ref rng, cfg) : DoorReward.None;
                    }
                if (MapConstraints.HasBalancedRewardPath(rows)) return true; // 09 §11.3 rule 7
            }
            return false;
        }

        private static DoorReward DrawReward(ref Xoshiro128StarStar rng, MapGenConfig cfg)
        {
            float[] w = cfg.DoorRewardWeights;
            float total = 0f;
            for (int i = 0; i < w.Length; i++) total += w[i];
            if (total <= 0f) return DoorReward.Equipment;
            float pick = rng.NextFloat01() * total;
            for (int i = 0; i < w.Length; i++)
            {
                if (w[i] <= 0f) continue;
                if (pick < w[i]) return (DoorReward)(i + 1);
                pick -= w[i];
            }
            return (DoorReward)w.Length;
        }

        /// <summary>
        /// 09 §11.3 rule 6. Preference order for each Event node: an id unused in this run and absent from the last
        /// run of the region; else (pool too small) an id unused in this run; else (pool exhausted) an empty id.
        /// </summary>
        private static void AssignEvents(ref Xoshiro128StarStar rng, MapGenContext ctx, MapNode[][] rows)
        {
            IReadOnlyList<string> pool = ctx.EventPool;
            if (pool == null || pool.Count == 0) return;
            IReadOnlyList<string> last = ctx.History != null ? ctx.History.GetLastRunEventIds(ctx.RegionId) : null;
            var used = new HashSet<string>(StringComparer.Ordinal);
            var cand = new List<string>(pool.Count);

            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    MapNode n = rows[r][c];
                    if (n.Type != RoomType.Event) continue;
                    cand.Clear();
                    for (int i = 0; i < pool.Count; i++)
                        if (!used.Contains(pool[i]) && !InList(last, pool[i])) cand.Add(pool[i]);
                    if (cand.Count == 0)
                        for (int i = 0; i < pool.Count; i++)
                            if (!used.Contains(pool[i])) cand.Add(pool[i]);
                    if (cand.Count == 0) continue;
                    string id = cand[cand.Count == 1 ? 0 : rng.Range(0, cand.Count)];
                    used.Add(id);
                    n.EventId = id;
                }
        }

        private static bool InList(IReadOnlyList<string> list, string id)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++) if (string.Equals(list[i], id, StringComparison.Ordinal)) return true;
            return false;
        }

        // ---------------------------------------------------------------- deterministic fallback

        /// <summary>
        /// Fixed valid map used when sampling fails. Column 0 is the high-risk lane (Elites in rows 3, 7, 10, 12,
        /// never adjacent); column 1 avoids Elites. Stratum I rows 1-5, II rows 6-9, III rows 10-13 keep the row
        /// counts and types of 09 §11.1. Edges use the deterministic "ladder" layout. Door rewards alternate by row
        /// parity (Equipment on odd rows, BlessingLesser on even rows, Heal on the third lane). No events.
        /// Uses the default row shapes, so it ignores custom row specs; callers with custom specs get a thrown
        /// InvalidOperationException instead of an invalid map.
        /// </summary>
        private static MapNode[][] BuildTemplate(MapGenConfig cfg)
        {
            const RoomType C = RoomType.Combat, E = RoomType.Elite, S = RoomType.Shrine, R = RoomType.Rest;
            RoomType[][] types =
            {
                new[] { C, C },          // 1
                new[] { C, S },          // 2
                new[] { E, C },          // 3
                new[] { C, C },          // 4
                new[] { R, S },          // 5 Clareira
                new[] { C, C, C },       // 6
                new[] { E, C, C },       // 7
                new[] { C, C, C },       // 8
                new[] { RoomType.Guardian }, // 9
                new[] { E, C },          // 10
                new[] { C, C },          // 11
                new[] { E, C },          // 12
                new[] { R },             // 13 Antechamber
            };

            var rows = new MapNode[MapGenConfig.BossRow][];
            for (int r = 0; r < types.Length; r++)
            {
                rows[r] = new MapNode[types[r].Length];
                for (int c = 0; c < types[r].Length; c++)
                {
                    var n = new MapNode { Row = r + 1, Column = c, Type = types[r][c] };
                    if (n.Type == RoomType.Combat)
                        n.Reward = c == 2 ? DoorReward.Heal : ((r + 1) % 2 == 1 ? DoorReward.Equipment : DoorReward.BlessingLesser);
                    if (r == 12) n.IsAntechamber = true;
                    rows[r][c] = n;
                }
            }
            rows[MapGenConfig.BossRow - 1] = new[] { new MapNode { Row = MapGenConfig.BossRow, Column = 0, Type = RoomType.Boss } };

            Xoshiro128StarStar unused = default;
            for (int r = 1; r < MapGenConfig.RowCount; r++)
                AssignEdges(ref unused, rows[r - 1], rows[r].Length, true);
            LinkToBoss(rows);
            return rows;
        }
    }
}
