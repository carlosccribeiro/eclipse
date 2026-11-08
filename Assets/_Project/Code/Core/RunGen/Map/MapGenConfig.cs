namespace Eclipse.Core.RunGen.Map
{
    /// <summary>How the types of a row are produced (09 §11.1 "Regras" column).</summary>
    public enum RowKind : byte
    {
        /// <summary>Types drawn from the stratum weights, restricted to <see cref="RowSpec.AllowedMask"/>.</summary>
        Weighted,
        /// <summary>Combat only (row 1).</summary>
        AllCombat,
        /// <summary>Clareira: at least one Rest, the others Shrine or Treasure (row 5).</summary>
        Clearing,
        /// <summary>Single Guardian Arena (row 9).</summary>
        Guardian,
        /// <summary>Single Rest flagged as Antechamber (row 13).</summary>
        Antechamber
    }

    /// <summary>Shape of one row of the map: node count range, kind and allowed types.</summary>
    public sealed class RowSpec
    {
        public int MinNodes;
        public int MaxNodes;
        public RowKind Kind;
        /// <summary>Bit mask over <see cref="RoomType"/> (<c>1 &lt;&lt; (int)type</c>).</summary>
        public int AllowedMask;

        public RowSpec(int minNodes, int maxNodes, RowKind kind, int allowedMask)
        {
            MinNodes = minNodes; MaxNodes = maxNodes; Kind = kind; AllowedMask = allowedMask;
        }

        public bool Allows(RoomType t) => (AllowedMask & (1 << (int)t)) != 0;
    }

    /// <summary>
    /// All numbers of the map generator. Defaults are copied from 09 §11 (and 00 §5 R33 for strata);
    /// they are overridden by data (M15 T5), never edited in code.
    /// </summary>
    public sealed class MapGenConfig
    {
        public const int RowCount = 13;          // 09 §11.1: 13 rows + Boss Room
        public const int BossRow = RowCount + 1; // Boss is stored as row 14
        public const int StratumCount = 3;       // 00 §5 R33 / 10 §3.1: I, II, III
        public const int WeightedTypeCount = 6;  // 09 §11.2 table columns, in order below

        /// <summary>Order of the weight columns: Combat, Elite, Treasure, Shrine, Event, Challenge.</summary>
        public static readonly RoomType[] WeightedTypes =
            { RoomType.Combat, RoomType.Elite, RoomType.Treasure, RoomType.Shrine, RoomType.Event, RoomType.Challenge };

        private static int Bit(RoomType t) => 1 << (int)t;

        /// <summary>Row shapes, index 0 = row 1 (<see cref="RowCount"/> entries).</summary>
        public RowSpec[] Rows { get; set; }

        /// <summary>
        /// Weights [stratum-1, column of <see cref="WeightedTypes"/>]. Elite "line 3+" and Treasure "from line 2"
        /// are expressed by <see cref="RowSpec.AllowedMask"/>, not by the weight.
        /// </summary>
        public float[,] Weights { get; set; }

        /// <summary>Door reward weights; order Equipment, RiftShards, AetherDust, BlessingLesser, Heal.</summary>
        public float[] DoorRewardWeights { get; set; }

        /// <summary>Row-local rejection tries before the whole attempt is abandoned (a "row rebuild" each).</summary>
        public int RowAttempts { get; set; } = 24;

        /// <summary>Edge layouts tried per row pair to avoid Elite-to-Elite edges (09 §11.3 rule 1).</summary>
        public int EdgeAttempts { get; set; } = 12;

        /// <summary>Reward redraws per attempt to satisfy 09 §11.3 rule 7 before abandoning the attempt.</summary>
        public int RewardAttempts { get; set; } = 24;

        /// <summary>Whole-map attempts before the deterministic template is used.</summary>
        public int MaxAttempts { get; set; } = 512;

        public static MapGenConfig CreateDefault()
        {
            int combat = Bit(RoomType.Combat), elite = Bit(RoomType.Elite), treasure = Bit(RoomType.Treasure),
                shrine = Bit(RoomType.Shrine), ev = Bit(RoomType.Event), challenge = Bit(RoomType.Challenge),
                rest = Bit(RoomType.Rest);
            int early = combat | shrine | ev | treasure;                        // 09 §11.1 rows 2-4 (Treasure from row 2)
            int late = combat | elite | treasure | shrine | ev | challenge;     // 09 §11.1 rows 6-8, 10-12: all but Rest

            var rows = new RowSpec[RowCount];
            rows[0] = new RowSpec(2, 2, RowKind.AllCombat, combat);             // row 1: 2 nodes, Combat only
            rows[1] = new RowSpec(2, 3, RowKind.Weighted, early);               // row 2: 2-3, no Elite
            rows[2] = new RowSpec(2, 3, RowKind.Weighted, early | elite);       // row 3: Elite from row 3
            rows[3] = new RowSpec(2, 3, RowKind.Weighted, early | elite);       // row 4
            rows[4] = new RowSpec(2, 3, RowKind.Clearing, rest | shrine | treasure); // row 5: Clareira
            rows[5] = new RowSpec(3, 4, RowKind.Weighted, late);                // rows 6-8: 3-4
            rows[6] = new RowSpec(3, 4, RowKind.Weighted, late);
            rows[7] = new RowSpec(3, 4, RowKind.Weighted, late);
            rows[8] = new RowSpec(1, 1, RowKind.Guardian, Bit(RoomType.Guardian)); // row 9: Guardian fixed
            rows[9] = new RowSpec(2, 4, RowKind.Weighted, late);                // rows 10-12: 2-4
            rows[10] = new RowSpec(2, 4, RowKind.Weighted, late);
            rows[11] = new RowSpec(2, 4, RowKind.Weighted, late);
            rows[12] = new RowSpec(1, 1, RowKind.Antechamber, rest);            // row 13: Antechamber

            // 09 §11.2. Columns: Combat, Elite, Treasure, Shrine, Event, Challenge.
            // TUNED (M15 risk policy; doc owner of 09 §11.2 please review): rejection sampling of the 09 §11.3
            // constraints skews the realized mix, so the raw 09 weights gave Shrine 1.41 / Elite 1.56 on a random path
            // (target 2 / 2-3). Original 09 §11.2 values are kept in the comments; tuned means are in the
            // test Mean_room_counts_on_random_path_match_09_11_2 (10,000 seeds): Combat 5.43, Elite 1.77, Treasure 0.73,
            // Shrine 1.73, Event 0.67, Challenge 0.27. Elite saturates near 1.8: higher Elite/Challenge weights make
            // the 09 §11.3 rejection exhaust attempts (fallback template), so they were not pushed further.
            var w = new float[StratumCount, WeightedTypeCount]
            {
                // 09 original: { 55, 10, 10, 15, 10, 0 }
                { 42, 12, 10, 26, 10, 0 },  // Stratum I (rows 1-4; also feeds Shrine/Treasure of the row-5 Clareira)
                // 09 original: { 40, 20, 8, 12, 12, 8 }
                { 28, 32, 8, 16, 10, 6 },   // Stratum II (rows 6-8)
                // 09 original: { 35, 25, 8, 10, 10, 12 }
                { 26, 34, 8, 12, 10, 8 }   // Stratum III (rows 10-12)
            };

            return new MapGenConfig
            {
                Rows = rows,
                Weights = w,
                // GAP: 09 §9 lists the five door rewards but gives no frequencies (quantities live in 05-loot).
                // Placeholder weights, to be replaced by data once 05/09 define them. Order:
                // Equipment, RiftShards, AetherDust, BlessingLesser, Heal.
                DoorRewardWeights = new float[] { 30, 20, 15, 25, 10 }
            };
        }

        /// <summary>Stratum of a row: I = 1-5, II = 6-9, III = 10-13 (00 §5 R33). Boss counts as III.</summary>
        public static int StratumOf(int row) => row <= 5 ? 1 : (row <= 9 ? 2 : 3);

        public float GetWeight(int stratum, RoomType type)
        {
            for (int i = 0; i < WeightedTypeCount; i++)
                if (WeightedTypes[i] == type) return Weights[stratum - 1, i];
            return 0f;
        }
    }
}
