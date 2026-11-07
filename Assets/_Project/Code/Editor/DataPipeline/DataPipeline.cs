using System;
using System.Collections.Generic;
using System.IO;
using Eclipse.Core.DataTables;
using Eclipse.Editor.Definitions;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Batchmode entry point and orchestration: import all tables, validate, rebuild the registry.</summary>
    public static class DataPipeline
    {
        public const string DataSourceFolderName = "DataSource";

        /// <summary>
        /// Validation hook. The validation module assigns it (it reports into the context via Error/Warning).
        /// Kept as a delegate so the pipeline has no dependency on the validator.
        /// </summary>
        public static Action<ImportContext> Validator { get; set; }

        [MenuItem("Eclipse/Data/Import & Validate")]
        public static void ImportAndValidate()
        {
            var context = new ImportContext();
            List<ITableImporter> importers = ImporterRegistry.Discover(context);
            Run(importers, null, Validator, context);

            try
            {
                RegistryBuilder.Rebuild();
            }
            catch (Exception ex)
            {
                context.Error("registry rebuild failed: " + ex.Message);
            }
            AssetDatabase.SaveAssets();

            foreach (ImportMessage m in context.Messages)
            {
                if (m.Severity == ImportSeverity.Error) Debug.LogError(m.ToString());
                else Debug.LogWarning(m.ToString());
            }
            Debug.Log(Summary(context));

            if (Application.isBatchMode)
                EditorApplication.Exit(context.HasErrors ? 1 : 0);
        }

        public static string Summary(ImportContext c) =>
            "ImportAndValidate: " + c.TablesProcessed + " tables, " + c.ErrorCount + " errors, " + c.WarningCount + " warnings";

        /// <summary>
        /// Runs the importers in the given order, then pass 2 (references), then the validator. Never exits the editor.
        /// <paramref name="dataSourceRoot"/> defaults to the project's DataSource folder.
        /// </summary>
        public static ImportContext Run(IEnumerable<ITableImporter> importers, string dataSourceRoot = null,
            Action<ImportContext> validator = null, ImportContext context = null)
        {
            context = context ?? new ImportContext();
            dataSourceRoot = dataSourceRoot ?? DefaultDataSourceRoot();

            foreach (ITableImporter importer in importers)
            {
                string name = importer.TableName;
                context.CurrentTable = name;
                context.TablesProcessed++;
                string path = Path.Combine(dataSourceRoot, importer.CsvPath);
                if (!File.Exists(path))
                {
                    context.Error(name, 0, null, "CSV file not found: " + importer.CsvPath);
                    continue;
                }
                try
                {
                    CsvTable table = CsvTable.Parse(File.ReadAllText(path));
                    importer.Import(table, context);
                }
                catch (CsvFormatException ex)
                {
                    context.Error(name, ex.Line, null, importer.CsvPath + ": " + ex.Message);
                }
                catch (Exception ex)
                {
                    context.Error(name, 0, null, ex.GetType().Name + ": " + ex.Message);
                }
            }

            context.CurrentTable = null;
            context.RunDeferred();

            if (validator != null)
            {
                try { validator(context); }
                catch (Exception ex) { context.Error("validator failed: " + ex.Message); }
            }
            return context;
        }

        private static string DefaultDataSourceRoot() =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, DataSourceFolderName);
    }
}
