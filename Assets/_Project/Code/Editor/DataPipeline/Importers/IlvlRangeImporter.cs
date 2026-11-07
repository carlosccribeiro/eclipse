using Eclipse.Core.DataTables;
using Eclipse.Core.Loot;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/ilvl_ranges.csv into IlvlRangeDefinition assets (05 sections 4.1, 4.2, R38).</summary>
    [TableImporter("IlvlRanges", Order = 60)]
    public sealed class IlvlRangeImporter : ItemTableImporter<IlvlRangeDefinition>
    {
        public const string DefaultCsvPath = "ilvl_ranges.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Loot/IlvlRanges";

        public IlvlRangeImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public IlvlRangeImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(IlvlRangeDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            int min = row.GetInt("ilvl_min");
            int max = row.GetInt("ilvl_max");
            if (min < 1 || max < min)
                throw new CsvFormatException("expected 1 <= ilvl_min <= ilvl_max", row.Line, "ilvl_min");
            definition.EditorSet(row.GetEnum<Difficulty>("difficulty"), row.GetString("region_id", string.Empty), min, max);
        }
    }
}
