using System;
using System.Collections.Generic;
using Eclipse.Core.RunGen.Map;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Run
{
    /// <summary>Which table a <see cref="MapGenConfigDefinition"/> row came from.</summary>
    public enum MapGenSection
    {
        RoomWeights,
        Row,
        DoorReward
    }

    /// <summary>
    /// One row of the map generator data (09 §11.1 map_rows.csv, §11.2 room_weights.csv, §9 door_rewards.csv).
    /// The full <see cref="MapGenConfig"/> is the union of all rows: use <see cref="ToCore"/>.
    /// </summary>
    public sealed class MapGenConfigDefinition : Definition
    {
        [SerializeField] private MapGenSection section;
        // RoomWeights
        [SerializeField] private int stratum;
        [SerializeField] private float[] weights = new float[MapGenConfig.WeightedTypeCount];
        // Row
        [SerializeField] private int row;
        [SerializeField] private int minNodes;
        [SerializeField] private int maxNodes;
        [SerializeField] private RowKind kind;
        [SerializeField] private int allowedMask;
        // DoorReward
        [SerializeField] private DoorReward reward;
        [SerializeField] private float rewardWeight;

        public MapGenSection Section => section;
        public int Stratum => stratum;
        /// <summary>Order of <see cref="MapGenConfig.WeightedTypes"/>.</summary>
        public IReadOnlyList<float> Weights => weights;
        public int Row => row;
        public int MinNodes => minNodes;
        public int MaxNodes => maxNodes;
        public RowKind Kind => kind;
        public int AllowedMask => allowedMask;
        public DoorReward Reward => reward;
        public float RewardWeight => rewardWeight;

        /// <summary>
        /// Builds the Core config from every row definition. Throws when rows are missing or repeated
        /// (the validation rule reports the same problems with asset context).
        /// </summary>
        public static MapGenConfig ToCore(IEnumerable<MapGenConfigDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var rows = new RowSpec[MapGenConfig.RowCount];
            var w = new float[MapGenConfig.StratumCount, MapGenConfig.WeightedTypeCount];
            bool[] strataSeen = new bool[MapGenConfig.StratumCount];
            var door = new float[Enum.GetValues(typeof(DoorReward)).Length - 1];
            bool[] doorSeen = new bool[door.Length];

            foreach (MapGenConfigDefinition d in definitions)
            {
                if (d == null) continue;
                switch (d.section)
                {
                    case MapGenSection.RoomWeights:
                        if (d.stratum < 1 || d.stratum > MapGenConfig.StratumCount || strataSeen[d.stratum - 1])
                            throw new InvalidOperationException("invalid or repeated stratum " + d.stratum + " in " + d.StableIdString);
                        strataSeen[d.stratum - 1] = true;
                        for (int i = 0; i < MapGenConfig.WeightedTypeCount; i++) w[d.stratum - 1, i] = d.weights[i];
                        break;
                    case MapGenSection.Row:
                        if (d.row < 1 || d.row > MapGenConfig.RowCount || rows[d.row - 1] != null)
                            throw new InvalidOperationException("invalid or repeated row " + d.row + " in " + d.StableIdString);
                        rows[d.row - 1] = new RowSpec(d.minNodes, d.maxNodes, d.kind, d.allowedMask);
                        break;
                    case MapGenSection.DoorReward:
                        int idx = (int)d.reward - 1;
                        if (idx < 0 || idx >= door.Length || doorSeen[idx])
                            throw new InvalidOperationException("invalid or repeated door reward " + d.reward + " in " + d.StableIdString);
                        doorSeen[idx] = true;
                        door[idx] = d.rewardWeight;
                        break;
                }
            }

            foreach (RowSpec r in rows) if (r == null) throw new InvalidOperationException("map_rows.csv must define all " + MapGenConfig.RowCount + " rows");
            foreach (bool s in strataSeen) if (!s) throw new InvalidOperationException("room_weights.csv must define all strata");
            foreach (bool s in doorSeen) if (!s) throw new InvalidOperationException("door_rewards.csv must define all rewards");

            // Attempt counters keep the Core defaults (algorithm internals, not design numbers).
            return new MapGenConfig { Rows = rows, Weights = w, DoorRewardWeights = door };
        }

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSetWeights(int stratumNumber, float[] typeWeights)
        {
            section = MapGenSection.RoomWeights;
            stratum = stratumNumber;
            weights = (float[])typeWeights.Clone();
        }

        /// <summary>Editor/importer only.</summary>
        public void EditorSetRow(int rowNumber, int min, int max, RowKind rowKind, int mask)
        {
            section = MapGenSection.Row;
            row = rowNumber; minNodes = min; maxNodes = max; kind = rowKind; allowedMask = mask;
        }

        /// <summary>Editor/importer only.</summary>
        public void EditorSetDoorReward(DoorReward doorReward, float weight)
        {
            section = MapGenSection.DoorReward;
            reward = doorReward; rewardWeight = weight;
        }
#endif
    }
}
