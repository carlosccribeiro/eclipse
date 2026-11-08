namespace Eclipse.Core.RunGen.Map
{
    /// <summary>
    /// Read-only path queries over a <see cref="RunMap"/> (exact DP over the DAG). Used by tooling to show the
    /// paths that satisfy 09 §11.3 rule 2 (a path with at most 2 Elites and a path with at least 3).
    /// Ties resolve to the lowest column, so results are deterministic.
    /// </summary>
    public static class MapPathAnalysis
    {
        /// <summary>
        /// Path from row 1 to the Boss with the fewest (or most) Elite rooms. Returns the column taken in each
        /// row (<c>result[r]</c> is the column in <c>Rows[r]</c>); null for a null or empty map.
        /// </summary>
        public static int[] FindEliteExtremePath(RunMap map, bool maximizeElites)
        {
            if (map == null || map.Rows == null || map.Rows.Length == 0) return null;
            MapNode[][] rows = map.Rows;
            int rc = rows.Length;
            var best = new int[rc][];
            var from = new int[rc][];
            for (int r = 0; r < rc; r++)
            {
                best[r] = new int[rows[r].Length];
                from[r] = new int[rows[r].Length];
                for (int c = 0; c < best[r].Length; c++) { best[r][c] = -1; from[r][c] = -1; }
            }
            for (int c = 0; c < rows[0].Length; c++) best[0][c] = IsElite(rows[0][c]);

            for (int r = 0; r < rc - 1; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    if (best[r][c] < 0) continue;
                    int[] next = rows[r][c].Next;
                    for (int k = 0; k < next.Length; k++)
                    {
                        int t = next[k];
                        int v = best[r][c] + IsElite(rows[r + 1][t]);
                        bool better = best[r + 1][t] < 0 || (maximizeElites ? v > best[r + 1][t] : v < best[r + 1][t]);
                        if (better) { best[r + 1][t] = v; from[r + 1][t] = c; }
                    }
                }

            // The Boss row has a single node; walk the parents back.
            var path = new int[rc];
            int col = 0;
            for (int r = rc - 1; r >= 0; r--)
            {
                path[r] = col;
                if (r > 0) col = from[r][col];
                if (col < 0) return null; // unreachable node: malformed map
            }
            return path;
        }

        /// <summary>Elite rooms on <paramref name="path"/> (one column per row).</summary>
        public static int CountElites(RunMap map, int[] path)
        {
            int n = 0;
            for (int r = 0; r < path.Length; r++) n += IsElite(map.Rows[r][path[r]]);
            return n;
        }

        private static int IsElite(MapNode n) => n.Type == RoomType.Elite ? 1 : 0;
    }
}
