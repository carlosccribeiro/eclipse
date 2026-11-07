using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Eclipse.Core.DataTables;
using Eclipse.Core.Ids;
using Eclipse.Core.Random;
using Eclipse.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>
    /// Base importer: matches rows to assets by StableId (read from the existing assets, never by file name),
    /// creates missing assets, updates only rows whose content hash changed, and reports orphans without deleting.
    /// </summary>
    public abstract class TableImporter<TDef> : ITableImporter where TDef : Definition
    {
        public virtual string TableName
        {
            get
            {
                var attr = GetType().GetCustomAttribute<TableImporterAttribute>();
                return attr != null ? attr.Name : GetType().Name;
            }
        }

        public abstract string CsvPath { get; }
        public abstract string OutputFolder { get; }

        /// <summary>Bump when <see cref="Apply"/> changes meaning so unchanged CSV rows are re-applied.</summary>
        protected virtual string HashSalt => string.Empty;

        /// <summary>
        /// When true (default) the columns name_pt/name_en/desc_pt/desc_en, if present, feed the
        /// "Definitions" String Table. Override to false for importers that write to scratch folders.
        /// </summary>
        protected virtual bool WritesLocalization => true;

        /// <summary>Pass 1: copy row values into the definition (id and ImportedHash are set by the base).</summary>
        protected abstract void Apply(TDef definition, CsvRow row, ImportContext context);

        /// <summary>Pass 2: resolve cross references by StableId. Return true if the asset was changed.</summary>
        protected virtual bool ResolveReferences(TDef definition, CsvRow row, ImportContext context) => false;

        public void Import(CsvTable table, ImportContext context)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (context == null) throw new ArgumentNullException(nameof(context));
            string tableName = TableName;

            if (!table.HasColumn("id"))
            {
                context.Error(tableName, 1, null, "missing mandatory column 'id'");
                return;
            }

            EnsureFolder(OutputFolder);
            Dictionary<string, TDef> existing = IndexExisting(context, tableName);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (CsvRow row in table.Rows)
            {
                string id = null;
                try
                {
                    id = row.GetString("id");
                    if (!seen.Add(id))
                    {
                        context.Error(tableName, row.Line, id, "duplicate id in CSV");
                        continue;
                    }
                    if (!StableId.TryParse(id, out _))
                    {
                        context.Error(tableName, row.Line, id, "id is not a valid StableId");
                        continue;
                    }

                    string hash = ComputeHash(table, row, HashSalt);
                    TDef def;
                    if (!existing.TryGetValue(id, out def))
                    {
                        def = ScriptableObject.CreateInstance<TDef>();
                        try
                        {
                            def.EditorSetStableId(id);
                            Apply(def, row, context);
                            def.ImportedHash = hash;
                            AssetDatabase.CreateAsset(def, NewAssetPath(id));
                        }
                        catch
                        {
                            UnityEngine.Object.DestroyImmediate(def);
                            throw;
                        }
                        context.Created++;
                    }
                    else if (def.ImportedHash != hash)
                    {
                        Apply(def, row, context);
                        def.ImportedHash = hash;
                        EditorUtility.SetDirty(def);
                        context.Updated++;
                    }
                    else
                    {
                        context.Unchanged++;
                    }

                    // Checked on every run (even with an unchanged hash) so a lost/edited table entry is restored.
                    if (WritesLocalization && LocalizationWriter.ApplyRow(def, id, table, row))
                        EditorUtility.SetDirty(def);

                    context.Register(def);
                    TDef captured = def;
                    CsvRow capturedRow = row;
                    context.Defer(() =>
                    {
                        if (ResolveReferences(captured, capturedRow, context))
                            EditorUtility.SetDirty(captured);
                    });
                }
                catch (CsvFormatException ex)
                {
                    context.Error(tableName, ex.Line, id, ex.Message);
                }
                catch (Exception ex)
                {
                    context.Error(tableName, row.Line, id, ex.GetType().Name + ": " + ex.Message);
                }
            }

            LocalizationWriter.Flush();

            foreach (var kv in existing)
            {
                if (seen.Contains(kv.Key)) continue;
                context.Orphans++;
                context.Warning(tableName, 0, kv.Key, "orphan: asset '" + AssetDatabase.GetAssetPath(kv.Value)
                    + "' has no row in " + CsvPath + " (not deleted)");
            }
        }

        /// <summary>Stable FNV-1a 64 hash (hex) of the row's column names and values.</summary>
        public static string ComputeHash(CsvTable table, CsvRow row, string salt)
        {
            var sb = new StringBuilder();
            sb.Append(salt).Append('\u001e');
            foreach (string col in table.Columns)
                sb.Append(col).Append('=').Append(row.GetString(col, string.Empty)).Append('\u001f');
            return RngStreams.Fnv1a64(sb.ToString()).ToString("x16");
        }

        private Dictionary<string, TDef> IndexExisting(ImportContext context, string tableName)
        {
            var map = new Dictionary<string, TDef>(StringComparer.Ordinal);
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(TDef).Name, new[] { OutputFolder }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);

            foreach (string path in paths)
            {
                var def = AssetDatabase.LoadAssetAtPath<TDef>(path);
                if (def == null || string.IsNullOrEmpty(def.StableIdString)) continue;
                if (map.ContainsKey(def.StableIdString))
                {
                    context.Warning(tableName, 0, def.StableIdString, "duplicate asset id at '" + path + "' ignored");
                    continue;
                }
                map.Add(def.StableIdString, def);
            }
            return map;
        }

        // Deterministic name: last id segment; falls back to the whole sanitized id on collision.
        private string NewAssetPath(string id)
        {
            string last = id.Substring(id.LastIndexOf('.') + 1);
            string path = OutputFolder + "/" + Sanitize(last) + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                path = OutputFolder + "/" + Sanitize(id) + ".asset";
            return path;
        }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            return sb.ToString();
        }

        internal static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
