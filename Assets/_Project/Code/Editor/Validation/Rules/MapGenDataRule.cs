using System.Collections.Generic;
using Eclipse.Core.RunGen.Map;
using Eclipse.Data.Run;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// Map generator tables (M15): once any row exists, strata weights (09 §11.2) must be 3 non-negative sets with
    /// a positive sum; map_rows must be exactly the 13 rows of 09 §11.1 with coherent counts and shapes; door reward
    /// weights (09 §9) must cover the 5 rewards with a positive sum.
    /// </summary>
    [ValidationRule]
    public sealed class MapGenDataRule : IValidationRule
    {
        public string Name => "MapGenData";

        public void Validate(ValidationContext context)
        {
            var strata = new Dictionary<int, MapGenConfigDefinition>();
            var rows = new Dictionary<int, MapGenConfigDefinition>();
            var doors = new Dictionary<DoorReward, MapGenConfigDefinition>();
            int total = 0;

            foreach (var def in context.Definitions)
            {
                if (!(def is MapGenConfigDefinition d)) continue;
                total++;
                switch (d.Section)
                {
                    case MapGenSection.RoomWeights: CheckWeights(context, d, strata); break;
                    case MapGenSection.Row: CheckRow(context, d, rows); break;
                    case MapGenSection.DoorReward: CheckDoor(context, d, doors); break;
                }
            }
            if (total == 0) return;

            for (int s = 1; s <= MapGenConfig.StratumCount; s++)
                if (!strata.ContainsKey(s)) context.Report.Error(Name, null, "room_weights.csv is missing stratum " + s);
            for (int r = 1; r <= MapGenConfig.RowCount; r++)
                if (!rows.ContainsKey(r)) context.Report.Error(Name, null, "map_rows.csv is missing row " + r + " (09 §11.1 has " + MapGenConfig.RowCount + " rows)");
            for (DoorReward k = DoorReward.Equipment; k <= DoorReward.Heal; k++)
                if (!doors.ContainsKey(k)) context.Report.Error(Name, null, "door_rewards.csv is missing reward " + k);

            float doorSum = 0f;
            foreach (var kv in doors) doorSum += kv.Value.RewardWeight;
            if (doors.Count > 0 && doorSum <= 0f)
                context.Report.Error(Name, null, "door reward weights must sum to more than 0");
        }

        private void CheckWeights(ValidationContext c, MapGenConfigDefinition d, Dictionary<int, MapGenConfigDefinition> strata)
        {
            if (d.Stratum < 1 || d.Stratum > MapGenConfig.StratumCount)
            {
                c.Report.Error(Name, d, "stratum must be 1.." + MapGenConfig.StratumCount + " (got " + d.Stratum + ")");
                return;
            }
            if (strata.ContainsKey(d.Stratum)) c.Report.Error(Name, d, "duplicate stratum " + d.Stratum);
            else strata[d.Stratum] = d;

            float sum = 0f;
            foreach (float w in d.Weights)
            {
                if (w < 0f) c.Report.Error(Name, d, "negative room weight");
                sum += w;
            }
            if (d.Weights.Count != MapGenConfig.WeightedTypeCount) c.Report.Error(Name, d, "expected " + MapGenConfig.WeightedTypeCount + " weights");
            if (sum <= 0f) c.Report.Error(Name, d, "room weights must sum to more than 0");
        }

        private void CheckRow(ValidationContext c, MapGenConfigDefinition d, Dictionary<int, MapGenConfigDefinition> rows)
        {
            if (d.Row < 1 || d.Row > MapGenConfig.RowCount)
            {
                c.Report.Error(Name, d, "row must be 1.." + MapGenConfig.RowCount + " (got " + d.Row + ")");
                return;
            }
            if (rows.ContainsKey(d.Row)) c.Report.Error(Name, d, "duplicate row " + d.Row);
            else rows[d.Row] = d;

            if (d.MinNodes < 1 || d.MaxNodes < d.MinNodes)
                c.Report.Error(Name, d, "node count must satisfy 1 <= min <= max (got " + d.MinNodes + ".." + d.MaxNodes + ")");
            if (d.AllowedMask == 0)
                c.Report.Error(Name, d, "allowed room types is empty");

            // Fixed rows of 09 §11.1: kind is tied to the row number.
            RowKind expected = d.Row == 1 ? RowKind.AllCombat
                : d.Row == 5 ? RowKind.Clearing
                : d.Row == 9 ? RowKind.Guardian
                : d.Row == 13 ? RowKind.Antechamber
                : RowKind.Weighted;
            if (d.Kind != expected)
                c.Report.Error(Name, d, "row " + d.Row + " must be " + expected + " (09 §11.1), got " + d.Kind);
            if ((d.Kind == RowKind.Guardian || d.Kind == RowKind.Antechamber) && (d.MinNodes != 1 || d.MaxNodes != 1))
                c.Report.Error(Name, d, d.Kind + " row must have exactly 1 node");
        }

        private void CheckDoor(ValidationContext c, MapGenConfigDefinition d, Dictionary<DoorReward, MapGenConfigDefinition> doors)
        {
            if (d.Reward == DoorReward.None)
            {
                c.Report.Error(Name, d, "reward None is not allowed");
                return;
            }
            if (doors.ContainsKey(d.Reward)) c.Report.Error(Name, d, "duplicate reward " + d.Reward);
            else doors[d.Reward] = d;
            if (d.RewardWeight < 0f) c.Report.Error(Name, d, "negative door reward weight");
        }
    }
}
