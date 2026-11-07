using Eclipse.Core.DataTables;
using Eclipse.Core.Items;
using Eclipse.Core.Stats;
using Eclipse.Data.Items;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/affixes.csv into AffixDefinition assets (01 section 3.9.2).</summary>
    [TableImporter("Affixes", Order = 20)]
    public sealed class AffixImporter : ItemTableImporter<AffixDefinition>
    {
        public const string DefaultCsvPath = "affixes.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Items/Affixes";

        public AffixImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public AffixImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(AffixDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            AffixValueKind kind = row.GetEnum<AffixValueKind>("value_kind");
            bool hasStat = !row.IsEmpty("stat");
            StatId stat = hasStat ? row.GetEnum<StatId>("stat") : default;
            float scale = kind == AffixValueKind.Percent ? 0.01f : 1f;

            var tiers = new float[10];
            for (int t = 1; t <= 5; t++)
            {
                float min = row.GetFloat("t" + t + "_min");
                float max = row.GetFloat("t" + t + "_max");
                if (min > max)
                    throw new CsvFormatException("t" + t + "_min is greater than t" + t + "_max", row.Line, "t" + t + "_min");
                tiers[(t - 1) * 2] = min * scale;
                tiers[(t - 1) * 2 + 1] = max * scale;
            }

            definition.EditorSet(row.GetString("abbrev"), hasStat, stat, kind, row.GetEnum<AffixCategory>("category"),
                row.GetEnum<AffixGroup>("group"), Mask(row, "affinity"), Mask(row, "relevance_tags"), tiers);
        }
    }
}
