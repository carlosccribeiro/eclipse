using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Eclipse.Core.DataTables;
using Eclipse.Data.Items;
using UnityEngine;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// Modifier references of named items are 03 codes (<c>KAE-CS-01</c>, <c>GEN-27</c>, <c>SIG-K16</c>), not StableIds. The format is
    /// always checked. Existence is checked against DataSource/modifiers.csv (column <c>code</c>, else <c>id</c>) once it exists;
    /// until then a single WARNING says the references are unverified (the table arrives with M10/M13).
    /// </summary>
    [ValidationRule]
    public sealed class ModifierReferenceRule : IValidationRule
    {
        private static readonly Regex CodePattern = new Regex("^[A-Z]{3}(-[A-Z0-9]+)+$", RegexOptions.Compiled);

        /// <summary>Path of the modifier table; tests may override it. Null = DataSource/modifiers.csv of the project.</summary>
        public static string ModifiersCsvPath { get; set; }

        public string Name => "ModifierReference";

        public static string DefaultModifiersCsvPath() =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "DataSource", "modifiers.csv");

        public void Validate(ValidationContext context)
        {
            var items = new List<NamedItemDefinition>();
            foreach (var d in context.Definitions)
                if (d is NamedItemDefinition item) items.Add(item);
            if (items.Count == 0) return;

            foreach (NamedItemDefinition item in items)
                foreach (string code in item.ModifierIds)
                    if (code == null || !CodePattern.IsMatch(code))
                        context.Report.Error(Name, item, "modifier code '" + code + "' is not a 03 code (e.g. KAE-CS-01, GEN-27, SIG-K16)");

            string path = ModifiersCsvPath ?? DefaultModifiersCsvPath();
            if (!File.Exists(path))
            {
                context.Report.Warning(Name, null, "modifiers.csv does not exist yet: modifier references of " + items.Count
                    + " named items are not verified");
                return;
            }

            CsvTable table = CsvTable.Parse(File.ReadAllText(path));
            string column = table.HasColumn("code") ? "code" : table.HasColumn("id") ? "id" : null;
            if (column == null)
            {
                context.Report.Warning(Name, null, "modifiers.csv has neither 'code' nor 'id' column: references are not verified");
                return;
            }

            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (CsvRow row in table.Rows) known.Add(row.GetString(column, string.Empty));
            foreach (NamedItemDefinition item in items)
                foreach (string code in item.ModifierIds)
                    if (code != null && CodePattern.IsMatch(code) && !known.Contains(code))
                        context.Report.Error(Name, item, "modifier '" + code + "' does not exist in modifiers.csv");
        }
    }
}
