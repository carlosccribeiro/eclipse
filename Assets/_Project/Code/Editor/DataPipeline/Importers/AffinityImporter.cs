using Eclipse.Core.DataTables;
using Eclipse.Core.Tags;
using Eclipse.Data.Affinities;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/Affinities.csv into AffinityDefinition assets (07 section 10.1).</summary>
    [TableImporter("Affinities", Order = 10)]
    public sealed class AffinityImporter : TableImporter<AffinityDefinition>
    {
        public const string DefaultCsvPath = "Affinities.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Affinities";

        private readonly string _csvPath;
        private readonly string _outputFolder;

        /// <summary>Used by the importer registry.</summary>
        public AffinityImporter() : this(DefaultCsvPath, DefaultOutputFolder)
        {
        }

        /// <summary>Alternative CSV (relative to the data source root) and output folder, for tests.</summary>
        public AffinityImporter(string csvPath, string outputFolder)
        {
            _csvPath = csvPath;
            _outputFolder = outputFolder;
        }

        public override string CsvPath => _csvPath;
        public override string OutputFolder => _outputFolder;

        protected override void Apply(AffinityDefinition definition, CsvRow row, ImportContext context)
        {
            Tag tag = row.GetEnum<Tag>("affinity_tag");
            Color primary = ParseColor(row, "color_primary");
            Color secondary = ParseColor(row, "color_secondary");
            string status = row.GetString("primary_status");
            string shape = row.GetString("shape", string.Empty);
            string iconPath = row.GetString("icon", string.Empty);
            Sprite icon = iconPath.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            definition.EditorSet(tag, primary, secondary, status, icon, shape);
            definition.EditorSetSourceDoc(row.GetString("source_doc"));
            definition.EditorSetNameEn(row.GetString("name_en"));
        }

        private static Color ParseColor(CsvRow row, string column)
        {
            string hex = row.GetString(column);
            if (!ColorUtility.TryParseHtmlString(hex, out Color c))
                throw new CsvFormatException("invalid color '" + hex + "' (expected #RRGGBB)", row.Line, column);
            return c;
        }
    }
}
