using Eclipse.Core.DataTables;
using Eclipse.Data.Difficulties;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>
    /// Imports DataSource/Difficulty.csv into DifficultyDefinition assets (09 section 3.2 tokens, 06 sections 3.8.2 and
    /// 3.10). Percent columns (<c>*_pct</c>) are numbers in the CSV and fractions in the asset.
    /// </summary>
    [TableImporter("Difficulty", Order = 10)]
    public sealed class DifficultyImporter : TableImporter<DifficultyDefinition>
    {
        public override string CsvPath => "Difficulty.csv";
        public override string OutputFolder => "Assets/_Project/Content/Shared/Difficulty";

        protected override void Apply(DifficultyDefinition d, CsvRow row, ImportContext context)
        {
            row.GetString("name_pt");
            d.EditorSetNameEn(row.GetString("name_en"));
            d.EditorSetSourceDoc(row.GetString("source_doc"));

            int melee = row.GetInt("tokens_melee");
            int ranged = row.GetInt("tokens_ranged");
            float interval = row.GetFloat("min_token_interval_s");
            float reduction = row.GetFloat("max_windup_reduction_pct") / 100f;
            float stagger = row.GetFloat("stagger_threshold_mult");
            if (melee < 0 || ranged < 0) throw new CsvFormatException("token counts must be >= 0", row.Line, "tokens_melee");
            if (interval <= 0f) throw new CsvFormatException("min_token_interval_s must be greater than 0", row.Line, "min_token_interval_s");
            if (reduction < 0f || reduction >= 1f) throw new CsvFormatException("max_windup_reduction_pct must be in [0, 100)", row.Line, "max_windup_reduction_pct");
            if (stagger <= 0f) throw new CsvFormatException("stagger_threshold_mult must be greater than 0", row.Line, "stagger_threshold_mult");

            d.EditorSet(row.GetEnum<DifficultyTier>("tier"), melee, ranged, interval, row.GetInt("ambush_tokens"),
                reduction, stagger, row.GetFloat("hp_mult"), row.GetFloat("damage_mult"));
        }
    }
}
