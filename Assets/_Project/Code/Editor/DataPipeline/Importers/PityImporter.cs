using Eclipse.Core.DataTables;
using Eclipse.Core.Loot;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/pity.csv into PityDefinition assets (05 sections 8.1-8.5).</summary>
    [TableImporter("Pity", Order = 60)]
    public sealed class PityImporter : ItemTableImporter<PityDefinition>
    {
        public const string DefaultCsvPath = "pity.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Loot/Pity";

        public PityImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public PityImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(PityDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            int soft = row.GetInt("soft_start");
            int hard = row.GetInt("hard");
            if (soft < 0 || hard < soft)
                throw new CsvFormatException("expected 0 <= soft_start <= hard", row.Line, "soft_start");
            definition.EditorSet(
                row.GetEnum<Difficulty>("difficulty"),
                soft, hard,
                Pct(row, "soft_step_pct"),
                row.GetFloat("legendary_cap_mult"),
                row.GetInt("set_pity_after"),
                row.GetInt("dup_unowned_weight"),
                Pct(row, "boss_unique_chance_pct"),
                Pct(row, "boss_unique_chance_a100_pct"),
                row.GetInt("boss_unique_guarantee_kills"),
                row.GetFloat("boss_echo_mult"),
                Pct(row, "first_kill_set_chance_pct"));
        }
    }
}
