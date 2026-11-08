using System;
using Eclipse.Core.DataTables;
using Eclipse.Core.Loot;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/loot_difficulty.csv into LootDifficultyDefinition assets (05 sections 3.2, 4.1, 5.5, 10.1, 10.3).</summary>
    [TableImporter("LootDifficulty", Order = 60)]
    public sealed class LootDifficultyImporter : ItemTableImporter<LootDifficultyDefinition>
    {
        public const string DefaultCsvPath = "loot_difficulty.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Loot/Difficulty";

        /// <summary>Tolerance of the "shares sum to 100%" check (fractions).</summary>
        private const float ShareTolerance = 0.0001f;

        public LootDifficultyImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public LootDifficultyImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(LootDifficultyDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            float set = Pct(row, "set_share_pct");
            float standard = Pct(row, "standard_share_pct");
            float unique = Pct(row, "unique_share_pct");
            if (Math.Abs(set + standard + unique - 1f) > ShareTolerance)
                throw new CsvFormatException("set/standard/unique shares must sum to 100", row.Line, "set_share_pct");

            definition.EditorSet(
                row.GetEnum<Difficulty>("difficulty"),
                Pct(row, "quantity_bonus_pct"),
                Pct(row, "quantity_bonus_per_ascension_pct"),
                set, standard, unique,
                Pct(row, "roll_floor_pct"),
                Pct(row, "roll_floor_per_ascension_pct"),
                row.GetInt("ascension_base_ilvl"),
                row.GetInt("ascension_max_ilvl"),
                row.GetInt("ascension_ilvl_cap_level"),
                row.GetInt("ascension_primal_min_level"),
                Pct(row, "primal_base_chance_pct"),
                Pct(row, "primal_chance_per_level_pct"),
                Pct(row, "primal_roll_min_pct"));
        }
    }
}
