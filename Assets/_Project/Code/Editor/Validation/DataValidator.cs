using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Core.DataTables;
using Eclipse.Data.Definitions;
using Eclipse.Editor.DataPipeline;
using Eclipse.Editor.Definitions;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Validation
{
    /// <summary>Discovers <see cref="IValidationRule"/>s by attribute and runs them over definitions.</summary>
    [InitializeOnLoad]
    public static class DataValidator
    {
        public const string ReservedNamesFile = "ReservedNames.csv";

        static DataValidator()
        {
            Connect();
        }

        /// <summary>Hooks the validator into the pipeline validator hook (idempotent).</summary>
        public static void Connect()
        {
            Eclipse.Editor.DataPipeline.DataPipeline.Validator = PipelineValidator;
        }

        public static IReadOnlyList<IValidationRule> DiscoverRules()
        {
            var rules = new List<IValidationRule>();
            foreach (Type t in TypeCache.GetTypesWithAttribute<ValidationRuleAttribute>())
            {
                if (t.IsAbstract || !typeof(IValidationRule).IsAssignableFrom(t)) continue;
                rules.Add((IValidationRule)Activator.CreateInstance(t));
            }
            return rules.OrderBy(r => r.Name, StringComparer.Ordinal).ToList();
        }

        /// <summary>Runs all discovered rules over the given definitions. Testable: no asset database access needed.</summary>
        public static ValidationReport Validate(IEnumerable<Definition> definitions,
            IReadOnlyDictionary<string, string> reservedNames = null)
        {
            var context = new ValidationContext(definitions.Where(d => d != null).ToList(), reservedNames);
            foreach (IValidationRule rule in DiscoverRules())
            {
                try { rule.Validate(context); }
                catch (Exception ex) { context.Report.Error(rule.Name, null, "rule threw " + ex.GetType().Name + ": " + ex.Message); }
            }
            return context.Report;
        }

        /// <summary>Loads every Definition under Assets/_Project/Content plus DataSource/ReservedNames.csv and validates.</summary>
        public static ValidationReport ValidateProject()
        {
            var defs = new List<Definition>();
            foreach (string guid in AssetDatabase.FindAssets("t:Definition", new[] { RegistryBuilder.ContentFolder }))
            {
                var d = AssetDatabase.LoadAssetAtPath<Definition>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null) defs.Add(d);
            }
            return Validate(defs, LoadReservedNames(DefaultReservedNamesPath()));
        }

        public static string DefaultReservedNamesPath() =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "DataSource", ReservedNamesFile);

        /// <summary>Reads name,owner_doc; returns an empty map if the file is missing.</summary>
        public static Dictionary<string, string> LoadReservedNames(string path)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return map;
            CsvTable table = CsvTable.Parse(File.ReadAllText(path));
            foreach (CsvRow row in table.Rows)
                map[row.GetString("name")] = row.GetString("owner_doc");
            return map;
        }

        /// <summary>Pipeline hook: validates the project and copies the report into the import context.</summary>
        public static void PipelineValidator(ImportContext context)
        {
            CopyReport(ValidateProject(), context);
        }

        public static void CopyReport(ValidationReport report, ImportContext context)
        {
            foreach (ValidationMessage m in report.Messages)
            {
                string text = m.Rule + ": " + m.Text + (string.IsNullOrEmpty(m.AssetPath) ? "" : " (" + m.AssetPath + ")");
                if (m.Severity == ValidationSeverity.Error) context.Error("validation", 0, m.Id, text);
                else context.Warning("validation", 0, m.Id, text);
            }
        }
    }
}
