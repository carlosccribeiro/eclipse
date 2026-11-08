using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Eclipse.Core.RunGen.Map.Tests
{
    /// <summary>
    /// Property tests of <see cref="MapGenerator"/> over many seeds (M15 T3, criteria 1-4; 09 §11).
    /// Checks are written independently of <see cref="MapConstraints"/>.
    /// </summary>
    [TestFixture]
    [Category("RunGen")]
    public class MapGeneratorTests
    {
#if !UNITY_5_3_OR_NEWER
        private const int SeedCount = 10000;
#else
        private const int SeedCount = 500;
#endif

        private const int Rows = MapGenConfig.RowCount;

        // ------------------------------------------------------------ helpers

        private static string Fail(ulong seed, string msg) => "seed " + seed + ": " + msg;

        private static void AssertNoFailures(List<string> failures, string what)
        {
            if (failures.Count == 0) return;
            var sb = new StringBuilder();
            sb.Append(what).Append(": ").Append(failures.Count).Append(" failing seeds. First: ");
            for (int i = 0; i < failures.Count && i < 5; i++) sb.Append("[").Append(failures[i]).Append("] ");
            Assert.Fail(sb.ToString());
        }

        /// <summary>SplitMix64: deterministic test RNG, independent of the generator streams.</summary>
        private struct TestRng
        {
            private ulong _s;
            public TestRng(ulong seed) { _s = seed; }
            public int Next(int n)
            {
                unchecked
                {
                    _s += 0x9E3779B97F4A7C15UL;
                    ulong z = _s;
                    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                    z ^= z >> 31;
                    return (int)(z % (ulong)n);
                }
            }
        }

        // ------------------------------------------------------------ 1. structure

        [Test]
        public void Structure_has_13_rows_plus_boss_with_declared_row_shapes()
        {
            MapGenConfig cfg = MapGenConfig.CreateDefault();
            var failures = new List<string>();
            var seenCounts = new HashSet<int>[Rows];
            for (int r = 0; r < Rows; r++) seenCounts[r] = new HashSet<int>();

            for (ulong seed = 1; seed <= SeedCount; seed++)
            {
                RunMap map = MapGenerator.Generate(seed);
                if (map.Rows.Length != 14) { failures.Add(Fail(seed, "rows=" + map.Rows.Length)); continue; }
                if (map.Rows[13].Length != 1 || map.Rows[13][0].Type != RoomType.Boss) failures.Add(Fail(seed, "last row not single Boss"));
                if (map.Rows[0].Length != 2) failures.Add(Fail(seed, "row 1 size " + map.Rows[0].Length));
                if (map.Rows[8].Length != 1 || map.Rows[8][0].Type != RoomType.Guardian) failures.Add(Fail(seed, "row 9 not single Guardian"));
                if (map.Rows[12].Length != 1 || map.Rows[12][0].Type != RoomType.Rest || !map.Rows[12][0].IsAntechamber)
                    failures.Add(Fail(seed, "row 13 not single Antechamber Rest"));

                for (int r = 0; r < Rows; r++)
                {
                    RowSpec spec = cfg.Rows[r];
                    int len = map.Rows[r].Length;
                    seenCounts[r].Add(len);
                    if (len < spec.MinNodes || len > spec.MaxNodes) failures.Add(Fail(seed, "row " + (r + 1) + " size " + len));
                    if (r > 0 && len > map.Rows[r - 1].Length + 1) failures.Add(Fail(seed, "row " + (r + 1) + " exceeds prev+1"));
                    int rests = 0;
                    for (int c = 0; c < len; c++)
                    {
                        RoomType t = map.Rows[r][c].Type;
                        int row = r + 1;
                        if (row == 1 && t != RoomType.Combat) failures.Add(Fail(seed, "row 1 non-Combat"));
                        if (row <= 2 && t == RoomType.Elite) failures.Add(Fail(seed, "Elite in row " + row));
                        if (row == 2 && t == RoomType.Treasure) { /* allowed from row 2 */ }
                        if (row >= 2 && row != 13 && row != 5 && t == RoomType.Rest) failures.Add(Fail(seed, "Rest in row " + row));
                        if (row >= 6 && row <= 12 && row != 9 && t == RoomType.Guardian) failures.Add(Fail(seed, "Guardian in row " + row));
                        if (row == 5)
                        {
                            if (t == RoomType.Rest) rests++;
                            else if (t != RoomType.Shrine && t != RoomType.Treasure) failures.Add(Fail(seed, "Clareira has " + t));
                        }
                        if (row == 1 || row == 6) { /* sizes checked above */ }
                    }
                    if (r == 4 && rests < 1) failures.Add(Fail(seed, "Clareira without Rest"));
                }
            }
            AssertNoFailures(failures, "structure");

            // Observed ranges vs 09 §11.1: row 6 spec 3-4, row 10 spec 2-4. The 1-2 links rule caps a row at
            // prev+1, so row 6 reaches 4 only when row 5 has 3, and row 10 (after the single Guardian) is always 2.
            TestContext.WriteLine("Observed sizes: row6={" + string.Join(",", seenCounts[5]) + "} row10={" + string.Join(",", seenCounts[9]) + "}");
            CollectionAssert.AreEquivalent(new[] { 2 }, seenCounts[9], "row 10 observed sizes (09 says 2-4; capped by 1-2 links rule)");
        }

        // ------------------------------------------------------------ 2. constraints (independent checks)

        [Test]
        public void Every_map_satisfies_09_11_3_with_independent_checks()
        {
            var failures = new List<string>();
            for (ulong seed = 1; seed <= SeedCount; seed++)
            {
                RunMap map = MapGenerator.Generate(seed);
                var v = MapConstraints.Validate(map);
                if (v.Count != 0) { failures.Add(Fail(seed, "Validate: " + v[0])); continue; }
                string err = IndependentCheck(map);
                if (err != null) failures.Add(Fail(seed, err));
                if (map.UsedFallback) failures.Add(Fail(seed, "used fallback template"));
            }
            AssertNoFailures(failures, "constraints");
        }

        private static string IndependentCheck(RunMap map)
        {
            MapNode[][] rows = map.Rows;
            int n = rows.Length;

            // Edges: 1-2 targets, in range; no crossing (for a1<a2 in a row, every target of a1 <= every target of a2's... min).
            for (int r = 0; r < n - 1; r++)
            {
                int m = rows[r + 1].Length;
                for (int i = 0; i < rows[r].Length; i++)
                {
                    int[] a = rows[r][i].Next;
                    if (a.Length < 1 || a.Length > 2) return "node " + (r + 1) + "," + i + " has " + a.Length + " links";
                    foreach (int t in a) if (t < 0 || t >= m) return "edge out of range at " + (r + 1) + "," + i;
                    for (int j = i + 1; j < rows[r].Length; j++)
                        foreach (int t1 in a)
                            foreach (int t2 in rows[r][j].Next)
                                if (t1 > t2) return "crossing edges " + (r + 1) + "," + i + "->" + t1 + " and " + (r + 1) + "," + j + "->" + t2;
                }
            }

            // Reachability (forward from row 1 and backward from Boss) by flood fill.
            var reach = new bool[n][];
            for (int r = 0; r < n; r++) reach[r] = new bool[rows[r].Length];
            for (int c = 0; c < rows[0].Length; c++) reach[0][c] = true;
            for (int r = 0; r < n - 1; r++)
                for (int c = 0; c < rows[r].Length; c++)
                    if (reach[r][c]) foreach (int t in rows[r][c].Next) reach[r + 1][t] = true;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < reach[r].Length; c++)
                    if (!reach[r][c]) return "unreachable node " + (r + 1) + "," + c;

            // Elite and Combat counts per path via DP over (node) = set of achievable counts (small ints).
            var elites = new HashSet<int>[n][];
            var combats = new HashSet<int>[n][];
            for (int r = 0; r < n; r++)
            {
                elites[r] = new HashSet<int>[rows[r].Length];
                combats[r] = new HashSet<int>[rows[r].Length];
                for (int c = 0; c < rows[r].Length; c++) { elites[r][c] = new HashSet<int>(); combats[r][c] = new HashSet<int>(); }
            }
            for (int c = 0; c < rows[0].Length; c++)
            {
                elites[0][c].Add(rows[0][c].Type == RoomType.Elite ? 1 : 0);
                combats[0][c].Add(rows[0][c].Type == RoomType.Combat ? 1 : 0);
            }
            for (int r = 0; r < n - 1; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    if (rows[r][c].Type == RoomType.Elite)
                        foreach (int t in rows[r][c].Next)
                            if (rows[r + 1][t].Type == RoomType.Elite) return "consecutive Elites at row " + (r + 1);
                    foreach (int t in rows[r][c].Next)
                    {
                        int e = rows[r + 1][t].Type == RoomType.Elite ? 1 : 0;
                        int co = rows[r + 1][t].Type == RoomType.Combat ? 1 : 0;
                        foreach (int x in elites[r][c]) elites[r + 1][t].Add(x + e);
                        foreach (int x in combats[r][c]) combats[r + 1][t].Add(x + co);
                    }
                }
            HashSet<int> pe = elites[n - 1][0], pc = combats[n - 1][0];
            bool low = false, high = false;
            foreach (int x in pe) { if (x <= 2) low = true; if (x >= 3) high = true; }
            if (!low) return "no path with <= 2 Elites";
            if (!high) return "no path with >= 3 Elites";
            foreach (int x in pc) if (x < 4) return "a path has only " + x + " Combat Arenas";

            // Rule 1: no Elite in rows 1-2; Rule 3: Shrine in rows 2-4; Rule 5: Challenge rows have an alternative.
            bool shrine = false;
            for (int r = 0; r < Rows; r++)
            {
                bool challenge = false, alt = false;
                for (int c = 0; c < rows[r].Length; c++)
                {
                    RoomType t = rows[r][c].Type;
                    if (r < 2 && t == RoomType.Elite) return "Elite in row " + (r + 1);
                    if (r >= 1 && r <= 3 && t == RoomType.Shrine) shrine = true;
                    if (t == RoomType.Challenge) challenge = true;
                    if (t != RoomType.Challenge && t != RoomType.Elite) alt = true;
                }
                if (challenge && !alt) return "Challenge row " + (r + 1) + " without alternative";
            }
            if (!shrine) return "no Shrine in rows 2-4";
            return null;
        }

        // ------------------------------------------------------------ 3. mean counts per type (09 §11.2)

        [Test]
        public void Mean_room_counts_on_random_path_match_09_11_2()
        {
            var sum = new Dictionary<RoomType, long>();
            foreach (RoomType t in System.Enum.GetValues(typeof(RoomType))) sum[t] = 0;

            for (ulong seed = 1; seed <= SeedCount; seed++)
            {
                RunMap map = MapGenerator.Generate(seed);
                var rng = new TestRng(seed ^ 0xA5A5A5A5UL);
                int col = rng.Next(map.Rows[0].Length);
                for (int r = 0; r < map.Rows.Length; r++)
                {
                    MapNode node = map.Rows[r][col];
                    sum[node.Type]++;
                    if (r < map.Rows.Length - 1) col = node.Next[node.Next.Length == 1 ? 0 : rng.Next(node.Next.Length)];
                }
            }

            double Mean(RoomType t) => (double)sum[t] / SeedCount;
            // 09 §11.2 "Média esperada por run", widened by 0.5 on each side.
            var expected = new (RoomType type, double lo, double hi)[]
            {
                (RoomType.Combat, 5, 6), (RoomType.Elite, 2, 3), (RoomType.Treasure, 1, 2),
                (RoomType.Shrine, 2, 3 /* 2 + Clareira */), (RoomType.Event, 1, 2), (RoomType.Challenge, 0, 2),
            };
            var sb = new StringBuilder("means:");
            var bad = new StringBuilder();
            foreach (var e in expected)
            {
                double m = Mean(e.type);
                sb.Append(' ').Append(e.type).Append('=').Append(m.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                if (m < e.lo - 0.5 || m > e.hi + 0.5)
                    bad.Append(' ').Append(e.type).Append(" outside [").Append(e.lo - 0.5).Append(", ").Append(e.hi + 0.5).Append("]");
            }
            TestContext.WriteLine(sb.ToString());
            Assert.IsEmpty(bad.ToString(), sb + " |" + bad);
        }

        // ------------------------------------------------------------ 4. determinism, variety, events

        [Test]
        public void Same_seed_and_context_give_same_hash_and_different_seeds_vary()
        {
            var distinct = new HashSet<ulong>();
            var failures = new List<string>();
            int count = SeedCount < 1000 ? SeedCount : 1000;
            for (ulong seed = 1; seed <= (ulong)count; seed++)
            {
                ulong h1 = MapGenerator.Generate(seed, new MapGenContext()).Hash();
                ulong h2 = MapGenerator.Generate(seed, new MapGenContext()).Hash();
                if (h1 != h2) failures.Add(Fail(seed, "hash differs between runs"));
                distinct.Add(h1);
            }
            AssertNoFailures(failures, "determinism");
            Assert.Greater(distinct.Count, count * 9 / 10, "hashes should be varied across seeds");
        }

        [Test]
        public void Events_are_unique_in_run_and_avoid_last_run_of_the_region()
        {
            var pool = new List<string>();
            for (int i = 0; i < 20; i++) pool.Add("event_" + i);
            var history = new RunHistory();
            history.SetLastRunEventIds("region_a", new[] { "event_0", "event_1", "event_2", "event_3" });

            var failures = new List<string>();
            int eventsSeen = 0;
            int historySeeds = SeedCount < 1000 ? SeedCount : 1000; // generates twice per seed; 1000 keeps it fast
            for (ulong seed = 1; seed <= (ulong)historySeeds; seed++)
            {
                var ctx = new MapGenContext { RegionId = "region_a", EventPool = pool, History = history };
                RunMap map = MapGenerator.Generate(seed, ctx);
                var used = new HashSet<string>();
                foreach (MapNode[] row in map.Rows)
                    foreach (MapNode node in row)
                    {
                        if (node.Type != RoomType.Event) { if (!string.IsNullOrEmpty(node.EventId)) failures.Add(Fail(seed, "EventId on non-Event node")); continue; }
                        eventsSeen++;
                        if (string.IsNullOrEmpty(node.EventId)) { failures.Add(Fail(seed, "Event without id")); continue; }
                        if (!used.Add(node.EventId)) failures.Add(Fail(seed, "event repeated " + node.EventId));
                        if (history.GetLastRunEventIds("region_a") is IReadOnlyList<string> last && ((List<string>)new List<string>(last)).Contains(node.EventId))
                            failures.Add(Fail(seed, "event from last run " + node.EventId));
                    }
                if (!ReferenceEquals(map.Rows, map.Rows) || MapGenerator.Generate(seed, ctx).Hash() != map.Hash())
                    failures.Add(Fail(seed, "non-deterministic with history"));
            }
            AssertNoFailures(failures, "events");
            Assert.Greater(eventsSeen, 0);
        }
    }
}
