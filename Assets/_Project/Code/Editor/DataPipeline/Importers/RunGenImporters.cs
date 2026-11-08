using System;
using System.Collections.Generic;
using System.Globalization;
using Eclipse.Core.DataTables;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Core.RunGen.Map;
using Eclipse.Data.Definitions;
using Eclipse.Data.Run;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>
    /// Shared base of the run generation importers (M15 T5). CSVs live in <c>DataSource/run/</c> (CsvPath is relative
    /// to DataSource, so a subfolder works); each file has its own output folder under Content/RunGen.
    /// Columns name_pt/name_en/source_doc are mandatory, as in the other tables.
    /// </summary>
    public abstract class RunGenTableImporter<TDef> : TableImporter<TDef> where TDef : Definition
    {
        public const string OutputRoot = "Assets/_Project/Content/RunGen";

        private readonly string _root;

        /// <param name="outputRoot">Root of the output folders; tests pass a scratch folder (no String Table writes then).</param>
        protected RunGenTableImporter(string outputRoot) { _root = outputRoot ?? OutputRoot; }

        protected abstract string SubFolder { get; }
        public sealed override string OutputFolder => _root + "/" + SubFolder;

        protected override bool WritesLocalization => _root == OutputRoot;

        protected static void ApplyCommon(TDef definition, CsvRow row)
        {
            row.GetString("name_pt");
            definition.EditorSetNameEn(row.GetString("name_en"));
            definition.EditorSetSourceDoc(row.GetString("source_doc"));
        }

        protected static float ParseFloat(string text, CsvRow row, string column)
        {
            if (!float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                throw new CsvFormatException("invalid number '" + text + "'", row.Line, column);
            return v;
        }

        protected static int ParseInt(string text, CsvRow row, string column)
        {
            if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                throw new CsvFormatException("invalid integer '" + text + "'", row.Line, column);
            return v;
        }
    }

    /// <summary>room_weights.csv (09 §11.2): weights per stratum.</summary>
    [TableImporter("RunRoomWeights", Order = 70)]
    public sealed class RoomWeightsImporter : RunGenTableImporter<MapGenConfigDefinition>
    {
        public RoomWeightsImporter() : base(null) { }
        public RoomWeightsImporter(string outputRoot) : base(outputRoot) { }

        public override string CsvPath => "run/room_weights.csv";
        protected override string SubFolder => "RoomWeights";

        protected override void Apply(MapGenConfigDefinition d, CsvRow row, ImportContext context)
        {
            ApplyCommon(d, row);
            var w = new float[MapGenConfig.WeightedTypeCount];
            for (int i = 0; i < w.Length; i++)
                w[i] = row.GetFloat(MapGenConfig.WeightedTypes[i].ToString().ToLowerInvariant());
            d.EditorSetWeights(row.GetInt("stratum"), w);
        }
    }

    /// <summary>map_rows.csv (09 §11.1): the 13 row shapes.</summary>
    [TableImporter("RunMapRows", Order = 70)]
    public sealed class MapRowsImporter : RunGenTableImporter<MapGenConfigDefinition>
    {
        public MapRowsImporter() : base(null) { }
        public MapRowsImporter(string outputRoot) : base(outputRoot) { }

        public override string CsvPath => "run/map_rows.csv";
        protected override string SubFolder => "MapRows";

        protected override void Apply(MapGenConfigDefinition d, CsvRow row, ImportContext context)
        {
            ApplyCommon(d, row);
            int mask = 0;
            foreach (string item in row.GetList("allowed"))
            {
                if (!Enum.TryParse(item, false, out RoomType t) || !Enum.IsDefined(typeof(RoomType), t))
                    throw new CsvFormatException("unknown room type '" + item + "'", row.Line, "allowed");
                mask |= 1 << (int)t;
            }
            d.EditorSetRow(row.GetInt("row"), row.GetInt("min_nodes"), row.GetInt("max_nodes"),
                row.GetEnum<RowKind>("kind"), mask);
        }
    }

    /// <summary>door_rewards.csv (09 §9): door reward weights.</summary>
    [TableImporter("RunDoorRewards", Order = 70)]
    public sealed class DoorRewardsImporter : RunGenTableImporter<MapGenConfigDefinition>
    {
        public DoorRewardsImporter() : base(null) { }
        public DoorRewardsImporter(string outputRoot) : base(outputRoot) { }

        public override string CsvPath => "run/door_rewards.csv";
        protected override string SubFolder => "DoorRewards";

        protected override void Apply(MapGenConfigDefinition d, CsvRow row, ImportContext context)
        {
            ApplyCommon(d, row);
            DoorReward reward = row.GetEnum<DoorReward>("reward");
            if (reward == DoorReward.None)
                throw new CsvFormatException("reward None is not allowed", row.Line, "reward");
            d.EditorSetDoorReward(reward, row.GetFloat("weight"));
        }
    }

    /// <summary>Key/value importer base shared by density.csv and encounter_limits.csv. Percent values become fractions.</summary>
    public abstract class EncounterKeyImporter : RunGenTableImporter<EncounterConfigDefinition>
    {
        protected EncounterKeyImporter(string outputRoot) : base(outputRoot) { }

        protected abstract EncounterSection Section { get; }

        protected override void Apply(EncounterConfigDefinition d, CsvRow row, ImportContext context)
        {
            ApplyCommon(d, row);
            string kind = row.GetString("value_kind");
            if (kind != "Number" && kind != "Percent")
                throw new CsvFormatException("value_kind must be Number or Percent", row.Line, "value_kind");
            List<string> items = row.GetList("values");
            if (items.Count == 0) throw new CsvFormatException("values is empty", row.Line, "values");
            var values = new float[items.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ParseFloat(items[i], row, "values");
                if (kind == "Percent") values[i] /= 100f;
            }
            d.EditorSet(Section, row.GetString("key"), values);
        }
    }

    /// <summary>density.csv (09 §10.1, §10.2): budget, density, waves and runtime numbers.</summary>
    [TableImporter("RunDensity", Order = 70)]
    public sealed class DensityImporter : EncounterKeyImporter
    {
        public DensityImporter() : base(null) { }
        public DensityImporter(string outputRoot) : base(outputRoot) { }

        public override string CsvPath => "run/density.csv";
        protected override string SubFolder => "Density";
        protected override EncounterSection Section => EncounterSection.Density;
    }

    /// <summary>encounter_limits.csv (09 §10.3): readability limits.</summary>
    [TableImporter("RunEncounterLimits", Order = 70)]
    public sealed class EncounterLimitsImporter : EncounterKeyImporter
    {
        public EncounterLimitsImporter() : base(null) { }
        public EncounterLimitsImporter(string outputRoot) : base(outputRoot) { }

        public override string CsvPath => "run/encounter_limits.csv";
        protected override string SubFolder => "Limits";
        protected override EncounterSection Section => EncounterSection.Limits;
    }

    /// <summary>
    /// encounter_recipes.csv (09 §10.4). Cells: <c>slots</c> = <c>Archetype:min:max:sharePct</c> joined by <c>|</c>;
    /// <c>filler</c> = 8 weights (Fodder, Standard, Ranged, Caster, Tank, Summoner, Assassin, Support);
    /// <c>weights</c> = <c>region:minRow:maxRow:weight</c> joined by <c>|</c> (empty region = any).
    /// </summary>
    [TableImporter("RunEncounterRecipes", Order = 70)]
    public sealed class EncounterRecipesImporter : RunGenTableImporter<EncounterRecipeDefinition>
    {
        public EncounterRecipesImporter() : base(null) { }
        public EncounterRecipesImporter(string outputRoot) : base(outputRoot) { }

        public override string CsvPath => "run/encounter_recipes.csv";
        protected override string SubFolder => "Recipes";

        protected override void Apply(EncounterRecipeDefinition d, CsvRow row, ImportContext context)
        {
            ApplyCommon(d, row);

            var slots = new List<RecipeSlotData>();
            foreach (string item in row.GetList("slots"))
            {
                string[] p = item.Split(':');
                if (p.Length != 4) throw new CsvFormatException("slot '" + item + "' must be Archetype:min:max:sharePct", row.Line, "slots");
                if (!Enum.TryParse(p[0].Trim(), false, out EnemyArchetype a) || !Enum.IsDefined(typeof(EnemyArchetype), a))
                    throw new CsvFormatException("unknown archetype '" + p[0] + "'", row.Line, "slots");
                slots.Add(new RecipeSlotData
                {
                    archetype = a,
                    minCount = ParseInt(p[1], row, "slots"),
                    maxCount = ParseInt(p[2], row, "slots"),
                    targetShare = ParseFloat(p[3], row, "slots") / 100f
                });
            }

            List<string> fillerItems = row.GetList("filler");
            var filler = new float[fillerItems.Count];
            for (int i = 0; i < filler.Length; i++) filler[i] = ParseFloat(fillerItems[i], row, "filler");

            var weights = new List<RecipeWeightData>();
            foreach (string item in row.GetList("weights"))
            {
                string[] p = item.Split(':');
                if (p.Length != 4) throw new CsvFormatException("weight '" + item + "' must be region:minRow:maxRow:weight", row.Line, "weights");
                weights.Add(new RecipeWeightData
                {
                    regionId = p[0].Trim(),
                    minRow = ParseInt(p[1], row, "weights"),
                    maxRow = ParseInt(p[2], row, "weights"),
                    weight = ParseFloat(p[3], row, "weights")
                });
            }

            d.EditorSet(slots.ToArray(), filler, weights.ToArray(),
                row.GetInt("max_casters_override", 0), row.GetInt("max_summoners_override", 0));
        }
    }
}
