using Eclipse.Core.Random;

namespace Eclipse.Core.RunGen.Map
{
    /// <summary>
    /// Generated run map: 13 rows plus the Boss row (last index, row number 14) as a layered DAG (09 §11).
    /// </summary>
    public sealed class RunMap
    {
        public RunMap(ulong seed, MapNode[][] rows)
        {
            Seed = seed;
            Rows = rows;
        }

        public ulong Seed { get; }

        /// <summary><c>Rows[r]</c> holds row <c>r + 1</c>; the last row has the single Boss node.</summary>
        public MapNode[][] Rows { get; }

        public MapNode Boss => Rows[Rows.Length - 1][0];

        /// <summary>Whole-map attempts used (1 = first try). Metric only; excluded from <see cref="Hash"/>.</summary>
        public int GenerationAttempts { get; internal set; }

        /// <summary>Row rejection-sampling failures (row rebuilds) summed over all attempts. Metric only.</summary>
        public int RowRebuilds { get; internal set; }

        /// <summary>True when the deterministic template replaced sampling. Metric only.</summary>
        public bool UsedFallback { get; internal set; }

        public MapNode GetNode(int row, int column) => Rows[row - 1][column];

        /// <summary>
        /// Stable FNV-1a 64 over the full content (types, rewards, ids, edges); platform independent.
        /// Excludes seed and generation metrics, so equal maps hash equal.
        /// </summary>
        public ulong Hash()
        {
            ulong h = 0xCBF29CE484222325UL;
            h = MixInt(h, Rows.Length);
            for (int r = 0; r < Rows.Length; r++)
            {
                MapNode[] row = Rows[r];
                h = MixInt(h, row.Length);
                for (int c = 0; c < row.Length; c++)
                {
                    MapNode n = row[c];
                    h = MixInt(h, n.Row);
                    h = MixInt(h, n.Column);
                    h = MixInt(h, (int)n.Type);
                    h = MixInt(h, n.IsAntechamber ? 1 : 0);
                    h = MixInt(h, (int)n.Reward);
                    h = MixU64(h, RngStreams.Fnv1a64(n.EventId ?? string.Empty));
                    h = MixU64(h, RngStreams.Fnv1a64(n.RoomModifierId ?? string.Empty));
                    h = MixInt(h, n.Next.Length);
                    for (int k = 0; k < n.Next.Length; k++) h = MixInt(h, n.Next[k]);
                }
            }
            return h;
        }

        private static ulong MixInt(ulong h, int v) => MixU64(h, unchecked((uint)v));

        private static ulong MixU64(ulong h, ulong v)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++)
                {
                    h = (h ^ (byte)(v >> (8 * i))) * 0x100000001B3UL;
                }
                return h;
            }
        }
    }
}
