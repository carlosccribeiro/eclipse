using System;
using System.Collections.Generic;

namespace Eclipse.Core.RunGen.Map
{
    /// <summary>
    /// Validation of a <see cref="RunMap"/> against 09 §11 (structure, 11.1; restrictions, 11.3).
    /// Violations are strings prefixed with "Struct:" or "Rule1:".."Rule7:" (09 §11.3 numbering).
    /// All path rules are exact (dynamic programming over the DAG, no sampling).
    /// </summary>
    public static class MapConstraints
    {
        private const int Inf = int.MaxValue / 2;

        /// <summary>Returns the violations of <paramref name="map"/>; an empty list means valid.</summary>
        public static List<string> Validate(RunMap map, MapGenContext ctx = null)
        {
            var v = new List<string>();
            if (map == null) { v.Add("Struct: map is null"); return v; }
            MapGenConfig cfg = ctx != null && ctx.Config != null ? ctx.Config : MapGenConfig.CreateDefault();
            ValidateRows(map.Rows, cfg, ctx, v);
            return v;
        }

        internal static void ValidateRows(MapNode[][] rows, MapGenConfig cfg, MapGenContext ctx, List<string> v)
        {
            if (!CheckStructure(rows, cfg, v)) return; // path rules need a sound graph

            int rc = rows.Length;
            CheckPathRules(rows, v, rc);
            CheckShrineRule(rows, v);
            CheckChallengeRule(rows, v);
            CheckEventRule(rows, ctx, v);
        }

        // ---------------------------------------------------------------- structure (09 §11.1)

        private static bool CheckStructure(MapNode[][] rows, MapGenConfig cfg, List<string> v)
        {
            int start = v.Count;
            if (rows == null || rows.Length != MapGenConfig.BossRow)
            {
                v.Add("Struct: expected " + MapGenConfig.BossRow + " rows (13 + Boss)");
                return false;
            }
            for (int r = 0; r < rows.Length; r++)
                if (rows[r] == null || rows[r].Length == 0) { v.Add("Struct: row " + (r + 1) + " is empty"); return false; }

            // Row shapes and fixed rows.
            for (int r = 0; r < MapGenConfig.RowCount; r++)
            {
                RowSpec spec = cfg.Rows[r];
                MapNode[] row = rows[r];
                if (row.Length < spec.MinNodes || row.Length > spec.MaxNodes)
                    v.Add("Struct: row " + (r + 1) + " has " + row.Length + " nodes, expected " + spec.MinNodes + ".." + spec.MaxNodes);
                int rests = 0;
                for (int c = 0; c < row.Length; c++)
                {
                    MapNode n = row[c];
                    if (n == null) { v.Add("Struct: null node at " + (r + 1) + "," + c); return false; }
                    if (n.Row != r + 1 || n.Column != c) v.Add("Struct: node coordinates mismatch at " + (r + 1) + "," + c);
                    if (!spec.Allows(n.Type)) v.Add("Struct: " + n.Type + " not allowed in row " + (r + 1));
                    if (n.Type == RoomType.Rest) rests++;
                    bool shouldBeAnte = spec.Kind == RowKind.Antechamber;
                    if (n.IsAntechamber != shouldBeAnte) v.Add("Struct: IsAntechamber wrong at " + (r + 1) + "," + c);
                    if (n.Type == RoomType.Combat != (n.Reward != DoorReward.None))
                        v.Add("Struct: door reward must exist exactly on Combat nodes at " + (r + 1) + "," + c);
                }
                if (spec.Kind == RowKind.Clearing && rests < 1) v.Add("Struct: Clareira (row " + (r + 1) + ") has no Rest");
            }
            if (rows[MapGenConfig.BossRow - 1].Length != 1 || rows[MapGenConfig.BossRow - 1][0].Type != RoomType.Boss)
                v.Add("Struct: last row must be a single Boss");

            // Edges: 1-2 consecutive ascending targets, in range, non-crossing.
            for (int r = 0; r < rows.Length - 1; r++)
            {
                MapNode[] a = rows[r];
                int m = rows[r + 1].Length;
                int prevHi = -1;
                for (int i = 0; i < a.Length; i++)
                {
                    int[] nx = a[i].Next;
                    if (nx == null || nx.Length < 1 || nx.Length > 2) { v.Add("Struct: node " + (r + 1) + "," + i + " must link to 1-2 nodes"); return false; }
                    if (nx[0] < 0 || nx[nx.Length - 1] >= m || (nx.Length == 2 && nx[1] != nx[0] + 1))
                    { v.Add("Struct: bad edge targets at " + (r + 1) + "," + i); return false; }
                    if (nx[0] < prevHi) v.Add("Struct: edges cross between row " + (r + 1) + " and " + (r + 2) + " at node " + i);
                    prevHi = nx[nx.Length - 1];
                }
            }
            if (rows[rows.Length - 1][0].Next != null && rows[rows.Length - 1][0].Next.Length != 0)
                v.Add("Struct: Boss must have no outgoing edges");

            // Reachability from row 1 and to the Boss.
            bool[][] fwd = NewFlags(rows);
            for (int c = 0; c < rows[0].Length; c++) fwd[0][c] = true;
            for (int r = 0; r < rows.Length - 1; r++)
                for (int c = 0; c < rows[r].Length; c++)
                    if (fwd[r][c]) for (int k = 0; k < rows[r][c].Next.Length; k++) fwd[r + 1][rows[r][c].Next[k]] = true;
            bool[][] bwd = NewFlags(rows);
            bwd[rows.Length - 1][0] = true;
            for (int r = rows.Length - 2; r >= 0; r--)
                for (int c = 0; c < rows[r].Length; c++)
                    for (int k = 0; k < rows[r][c].Next.Length; k++)
                        if (bwd[r + 1][rows[r][c].Next[k]]) bwd[r][c] = true;
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    if (!fwd[r][c]) v.Add("Struct: node " + (r + 1) + "," + c + " is unreachable from row 1");
                    if (!bwd[r][c]) v.Add("Struct: node " + (r + 1) + "," + c + " cannot reach the Boss");
                }
            return v.Count == start;
        }

        private static bool[][] NewFlags(MapNode[][] rows)
        {
            var f = new bool[rows.Length][];
            for (int r = 0; r < rows.Length; r++) f[r] = new bool[rows[r].Length];
            return f;
        }

        // ---------------------------------------------------------------- path rules (09 §11.3 rules 1, 2, 4, 7)

        private static void CheckPathRules(MapNode[][] rows, List<string> v, int rc)
        {
            // Rule 1 (second clause): never two consecutive Elites on a path = no Elite->Elite edge.
            // First clause (no Elite in rows 1-2) is enforced by the row masks and re-checked here.
            for (int r = 0; r < rc - 1; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    MapNode n = rows[r][c];
                    if (n.Type != RoomType.Elite) continue;
                    if (r < 2) v.Add("Rule1: Elite in row " + (r + 1));
                    for (int k = 0; k < n.Next.Length; k++)
                        if (rows[r + 1][n.Next[k]].Type == RoomType.Elite)
                            v.Add("Rule1: consecutive Elites " + (r + 1) + "," + c + " -> " + (r + 2) + "," + n.Next[k]);
                }

            int minE = Inf, maxE = -1, minCombat = Inf;
            int[][] eMin = NewInts(rows, Inf), eMax = NewInts(rows, -1), cMin = NewInts(rows, Inf);
            int[][] masks = NewInts(rows, 0);

            for (int c = 0; c < rows[0].Length; c++)
            {
                int e = rows[0][c].Type == RoomType.Elite ? 1 : 0;
                eMin[0][c] = e; eMax[0][c] = e;
                cMin[0][c] = rows[0][c].Type == RoomType.Combat ? 1 : 0;
                masks[0][c] = ApplyReward(1, rows[0][c]);
            }
            for (int r = 0; r < rc - 1; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    MapNode n = rows[r][c];
                    for (int k = 0; k < n.Next.Length; k++)
                    {
                        int t = n.Next[k];
                        MapNode d = rows[r + 1][t];
                        int e = d.Type == RoomType.Elite ? 1 : 0; // Guardian is a separate type: excluded from the count
                        int co = d.Type == RoomType.Combat ? 1 : 0;
                        if (eMin[r][c] + e < eMin[r + 1][t]) eMin[r + 1][t] = eMin[r][c] + e;
                        if (eMax[r][c] + e > eMax[r + 1][t]) eMax[r + 1][t] = eMax[r][c] + e;
                        if (cMin[r][c] + co < cMin[r + 1][t]) cMin[r + 1][t] = cMin[r][c] + co;
                        masks[r + 1][t] |= ApplyReward(masks[r][c], d);
                    }
                }
            // Aggregate over the Boss node (all paths end there).
            minE = eMin[rc - 1][0]; maxE = eMax[rc - 1][0]; minCombat = cMin[rc - 1][0];

            if (minE > 2) v.Add("Rule2: no path with <= 2 Elites (min is " + minE + ")");
            if (maxE < 3) v.Add("Rule2: no path with >= 3 Elites (max is " + maxE + ")");
            if (minCombat < 4) v.Add("Rule4: a path has only " + minCombat + " Combat Arenas (need >= 4)");
            if ((masks[rc - 1][0] & (1 << FullState)) == 0)
                v.Add("Rule7: no path has >= 2 Equipment and >= 2 BlessingLesser Combat doors");
        }

        /// <summary>
        /// Rule 7 (09 §11.3): "pelo menos 2 portas de Equipamento e 2 de Blessing alcancaveis no caminho mais curto".
        /// Every Row1 -> Boss path has the same length (14 rooms), so "shortest path" cannot discriminate.
        /// Interpretation: there exists a path from row 1 to the Boss that crosses at least 2 Equipment doors and at
        /// least 2 BlessingLesser doors, i.e. a player can walk a route that collects both. Exact DP over
        /// (min(equipment,2), min(blessing,2)) states; this is the weakest reading that still constrains the draw.
        /// </summary>
        internal static bool HasBalancedRewardPath(MapNode[][] rows)
        {
            int[][] masks = NewInts(rows, 0);
            for (int c = 0; c < rows[0].Length; c++) masks[0][c] = ApplyReward(1, rows[0][c]);
            for (int r = 0; r < rows.Length - 1; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    MapNode n = rows[r][c];
                    for (int k = 0; k < n.Next.Length; k++)
                        masks[r + 1][n.Next[k]] |= ApplyReward(masks[r][c], rows[r + 1][n.Next[k]]);
                }
            return (masks[rows.Length - 1][0] & (1 << FullState)) != 0;
        }

        private const int FullState = 2 * 3 + 2; // blessing = 2, equipment = 2

        private static int ApplyReward(int mask, MapNode node)
        {
            if (node.Type != RoomType.Combat) return mask;
            int eq = node.Reward == DoorReward.Equipment ? 1 : 0;
            int bl = node.Reward == DoorReward.BlessingLesser ? 1 : 0;
            int result = 0;
            for (int s = 0; s < 9; s++)
            {
                if ((mask & (1 << s)) == 0) continue;
                int e = Math.Min(2, s % 3 + eq), b = Math.Min(2, s / 3 + bl);
                result |= 1 << (b * 3 + e);
            }
            return result;
        }

        private static int[][] NewInts(MapNode[][] rows, int fill)
        {
            var a = new int[rows.Length][];
            for (int r = 0; r < rows.Length; r++)
            {
                a[r] = new int[rows[r].Length];
                if (fill != 0) for (int c = 0; c < a[r].Length; c++) a[r][c] = fill;
            }
            return a;
        }

        // ---------------------------------------------------------------- rule 3, 5, 6

        private static void CheckShrineRule(MapNode[][] rows, List<string> v)
        {
            // Rule 3: all nodes are reachable (structure check), so any Shrine in rows 2-4 is reachable.
            for (int r = 1; r <= 3; r++)
                for (int c = 0; c < rows[r].Length; c++)
                    if (rows[r][c].Type == RoomType.Shrine) return;
            v.Add("Rule3: no Shrine in rows 2-4");
        }

        private static void CheckChallengeRule(MapNode[][] rows, List<string> v)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                if (!RowHasChallenge(rows[r])) continue;
                bool alt = false;
                for (int c = 0; c < rows[r].Length; c++)
                {
                    RoomType t = rows[r][c].Type;
                    if (t != RoomType.Challenge && t != RoomType.Elite) { alt = true; break; }
                }
                if (!alt) v.Add("Rule5: row " + (r + 1) + " has a Challenge but no node that is neither Challenge nor Elite");
            }
        }

        internal static bool RowHasChallenge(MapNode[] row)
        {
            for (int c = 0; c < row.Length; c++) if (row[c].Type == RoomType.Challenge) return true;
            return false;
        }

        private static void CheckEventRule(MapNode[][] rows, MapGenContext ctx, List<string> v)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int eventNodes = 0;
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    MapNode n = rows[r][c];
                    if (n.Type != RoomType.Event) continue;
                    eventNodes++;
                    if (string.IsNullOrEmpty(n.EventId)) continue; // pool exhausted: documented degradation
                    if (!seen.Add(n.EventId)) v.Add("Rule6: event '" + n.EventId + "' repeats in the run");
                }
            if (ctx == null || ctx.History == null || ctx.EventPool == null) return;

            // Second clause only binds when the pool can honour it for every Event node.
            IReadOnlyList<string> last = ctx.History.GetLastRunEventIds(ctx.RegionId);
            int fresh = 0;
            for (int i = 0; i < ctx.EventPool.Count; i++)
                if (!Contains(last, ctx.EventPool[i])) fresh++;
            if (fresh < eventNodes) return;
            foreach (string id in seen)
                if (Contains(last, id)) v.Add("Rule6: event '" + id + "' also appeared in the last run of this region");
        }

        private static bool Contains(IReadOnlyList<string> list, string id)
        {
            for (int i = 0; i < list.Count; i++) if (string.Equals(list[i], id, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
