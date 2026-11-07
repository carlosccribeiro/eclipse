using Eclipse.Core.DataTables;
using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/named_bands.csv into NamedBandDefinition assets (02 section 3.9.1, R24). The 70-115% envelope is checked by NamedBandRule.</summary>
    [TableImporter("NamedBands", Order = 30)]
    public sealed class NamedBandImporter : ItemTableImporter<NamedBandDefinition>
    {
        public const string DefaultCsvPath = "named_bands.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Items/NamedBands";

        public NamedBandImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public NamedBandImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(NamedBandDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            NamedBand band = row.GetEnum<NamedBand>("band");
            if (band == NamedBand.None)
                throw new CsvFormatException("band None has no row", row.Line, "band");
            float lo = Pct(row, "lo_pct");
            float hi = Pct(row, "hi_pct");
            if (lo > hi)
                throw new CsvFormatException("lo_pct is greater than hi_pct", row.Line, "lo_pct");
            definition.EditorSet(band, lo, hi);
        }
    }
}
