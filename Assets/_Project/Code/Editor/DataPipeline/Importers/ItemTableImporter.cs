using System;
using System.Collections.Generic;
using Eclipse.Core.DataTables;
using Eclipse.Core.Items;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using Eclipse.Data.Items;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>
    /// Shared base of the M12 item/loot importers: configurable CSV path and output folder (tests use scratch folders),
    /// common columns (name_pt, name_en, source_doc) and the parsing helpers of docs/data-schemas/items.md.
    /// </summary>
    public abstract class ItemTableImporter<TDef> : TableImporter<TDef> where TDef : Definition
    {
        private readonly string _csvPath;
        private readonly string _outputFolder;
        private readonly string _defaultOutputFolder;

        protected ItemTableImporter(string csvPath, string outputFolder, string defaultOutputFolder)
        {
            _csvPath = csvPath;
            _outputFolder = outputFolder;
            _defaultOutputFolder = defaultOutputFolder;
        }

        public override string CsvPath => _csvPath;
        public override string OutputFolder => _outputFolder;

        // Test importers write to scratch folders and must not touch the real String Table.
        protected override bool WritesLocalization => _outputFolder == _defaultOutputFolder;

        /// <summary>Validates and copies the mandatory common columns (name_pt, name_en, source_doc).</summary>
        protected static void ApplyCommon(TDef definition, CsvRow row)
        {
            row.GetString("name_pt");
            definition.EditorSetNameEn(row.GetString("name_en"));
            definition.EditorSetSourceDoc(row.GetString("source_doc"));
        }

        /// <summary>Percent number in the CSV (12.5) to a fraction (0.125).</summary>
        protected static float Pct(CsvRow row, string column) => row.GetFloat(column) / 100f;

        protected static float Pct(CsvRow row, string column, float defaultFraction) =>
            row.IsEmpty(column) ? defaultFraction : row.GetFloat(column) / 100f;

        protected static TagMask Mask(CsvRow row, string column)
        {
            string text = row.HasColumn(column) ? row.GetString(column, string.Empty) : string.Empty;
            if (!TagMask.TryParse(text, out TagMask mask))
                throw new CsvFormatException("invalid tag mask '" + text + "'", row.Line, column);
            return mask;
        }

        protected static string OptString(CsvRow row, string column) =>
            row.HasColumn(column) ? row.GetString(column, string.Empty) : string.Empty;

        protected static float OptFloat(CsvRow row, string column) =>
            row.HasColumn(column) ? row.GetFloat(column, 0f) : 0f;

        protected static void RequireColumns(CsvRow row, params string[] columns)
        {
            foreach (string c in columns)
                if (!row.HasColumn(c))
                    throw new CsvFormatException("column '" + c + "' does not exist", row.Line, c);
        }

        /// <summary>Parses a list of <c>affix_id:Band[*]</c> entries (band = Default|High|Low; <c>*</c> = outside the slot pool).</summary>
        protected static List<NamedAffixEntry> NamedAffixList(CsvRow row, string column)
        {
            var list = new List<NamedAffixEntry>();
            foreach (string item in row.GetList(column))
            {
                string text = item;
                bool off = false;
                if (text.EndsWith("*", StringComparison.Ordinal))
                {
                    off = true;
                    text = text.Substring(0, text.Length - 1).TrimEnd();
                }
                int colon = text.IndexOf(':');
                if (colon <= 0 || colon == text.Length - 1)
                    throw new CsvFormatException("invalid affix entry '" + item + "' (expected affix_id:Band[*])", row.Line, column);
                string id = text.Substring(0, colon).Trim();
                string bandName = text.Substring(colon + 1).Trim();
                if (!Enum.TryParse(bandName, false, out NamedBand band) || !Enum.IsDefined(typeof(NamedBand), band)
                    || band == NamedBand.None || band == NamedBand.Envelope || char.IsDigit(bandName[0]))
                    throw new CsvFormatException("unknown band '" + bandName + "' in '" + item + "' (valid: Default, High, Low)", row.Line, column);
                list.Add(new NamedAffixEntry(id, band, off));
            }
            return list;
        }
    }
}
