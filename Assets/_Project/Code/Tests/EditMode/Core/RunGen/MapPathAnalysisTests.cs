using NUnit.Framework;

namespace Eclipse.Core.RunGen.Map.Tests
{
    [TestFixture]
    [Category("RunGen")]
    public class MapPathAnalysisTests
    {
        [Test]
        public void Extreme_paths_match_brute_force_and_rule_2()
        {
            for (ulong seed = 1; seed <= 60; seed++)
            {
                RunMap map = MapGenerator.Generate(seed);
                int min = int.MaxValue, max = -1;
                for (int c = 0; c < map.Rows[0].Length; c++) Walk(map, 0, c, 0, ref min, ref max);

                int[] pMin = MapPathAnalysis.FindEliteExtremePath(map, false);
                int[] pMax = MapPathAnalysis.FindEliteExtremePath(map, true);
                Assert.AreEqual(min, MapPathAnalysis.CountElites(map, pMin), "min seed " + seed);
                Assert.AreEqual(max, MapPathAnalysis.CountElites(map, pMax), "max seed " + seed);
                Assert.LessOrEqual(min, 2);
                Assert.GreaterOrEqual(max, 3);
                AssertConnected(map, pMin);
                AssertConnected(map, pMax);
            }
        }

        [Test]
        public void Null_map_returns_null()
        {
            Assert.IsNull(MapPathAnalysis.FindEliteExtremePath(null, true));
        }

        private static void AssertConnected(RunMap map, int[] path)
        {
            Assert.AreEqual(map.Rows.Length, path.Length);
            for (int r = 0; r < path.Length - 1; r++)
                CollectionAssert.Contains(map.Rows[r][path[r]].Next, path[r + 1]);
        }

        private static void Walk(RunMap map, int r, int c, int elites, ref int min, ref int max)
        {
            MapNode n = map.Rows[r][c];
            if (n.Type == RoomType.Elite) elites++;
            if (r == map.Rows.Length - 1)
            {
                if (elites < min) min = elites;
                if (elites > max) max = elites;
                return;
            }
            for (int k = 0; k < n.Next.Length; k++) Walk(map, r + 1, n.Next[k], elites, ref min, ref max);
        }
    }
}
