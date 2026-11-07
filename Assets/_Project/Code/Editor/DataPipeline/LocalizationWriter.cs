using System.Collections.Generic;
using Eclipse.Core.DataTables;
using Eclipse.Data.Definitions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>
    /// Writes the "Definitions" String Table Collection (pt-BR, en-US) from the CSV columns
    /// name_pt/name_en/desc_pt/desc_en and points Definition.DisplayName/Description at the entries
    /// (keys "&lt;StableId&gt;.name" and "&lt;StableId&gt;.desc"). Idempotent: nothing is touched when already equal.
    /// </summary>
    public static class LocalizationWriter
    {
        public const string CollectionName = "Definitions";
        public const string Folder = "Assets/_Project/Settings/Localization";
        public const string LocalesFolder = Folder + "/Locales";
        public const string TablesFolder = Folder + "/Tables";
        public const string SettingsPath = Folder + "/Localization Settings.asset";
        public const string PtBr = "pt-BR";
        public const string EnUs = "en-US";

        private static StringTableCollection _collection;
        private static bool _dirty;

        /// <summary>Creates (if missing) the Locales, the Localization Settings asset and the collection.</summary>
        public static StringTableCollection EnsureInfrastructure()
        {
            if (_collection != null) return _collection;

            TableImporter<Definition>.EnsureFolder(LocalesFolder);
            TableImporter<Definition>.EnsureFolder(TablesFolder);

            Locale pt = EnsureLocale(PtBr);
            Locale en = EnsureLocale(EnUs);

            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                }
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(CollectionName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(CollectionName, TablesFolder,
                    new List<Locale> { pt, en });
                AssetDatabase.SaveAssets();
            }
            else
            {
                // Restore a missing table if someone removed it by hand.
                if (collection.GetTable(PtBr) == null) collection.AddNewTable(pt.Identifier);
                if (collection.GetTable(EnUs) == null) collection.AddNewTable(en.Identifier);
            }

            _collection = collection;
            return collection;
        }

        private static Locale EnsureLocale(string code)
        {
            var id = new LocaleIdentifier(code);
            Locale locale = LocalizationEditorSettings.GetLocale(id);
            if (locale != null) return locale;

            string path = LocalesFolder + "/Locale_" + code + ".asset";
            locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
            if (locale == null)
            {
                locale = Locale.CreateLocale(id);
                AssetDatabase.CreateAsset(locale, path);
            }
            LocalizationEditorSettings.AddLocale(locale, false);
            return locale;
        }

        /// <summary>
        /// Applies the localization columns of one row. Returns true if the definition asset changed
        /// (table changes are tracked internally and saved by <see cref="Flush"/>).
        /// </summary>
        public static bool ApplyRow(Definition definition, string id, CsvTable table, CsvRow row)
        {
            if (!table.HasColumn("name_pt") && !table.HasColumn("name_en")
                && !table.HasColumn("desc_pt") && !table.HasColumn("desc_en"))
                return false;

            StringTableCollection collection = EnsureInfrastructure();
            bool changed = false;

            string nameKey = id + ".name";
            string descKey = id + ".desc";
            bool hasName = SetEntry(collection, nameKey, Optional(table, row, "name_pt"), Optional(table, row, "name_en"));
            bool hasDesc = SetEntry(collection, descKey, Optional(table, row, "desc_pt"), Optional(table, row, "desc_en"));

            LocalizedString name = hasName ? new LocalizedString(CollectionName, nameKey) : null;
            LocalizedString desc = hasDesc ? new LocalizedString(CollectionName, descKey) : null;

            if (!SameRef(definition.DisplayName, hasName ? nameKey : null)
                || !SameRef(definition.Description, hasDesc ? descKey : null))
            {
                definition.EditorSetLocalized(name, desc);
                changed = true;
            }
            return changed;
        }

        // Localization columns are optional individually: a missing column reads as empty.
        private static string Optional(CsvTable table, CsvRow row, string column) =>
            table.HasColumn(column) ? row.GetString(column, string.Empty) : string.Empty;

        // Returns true if the key has any text. Writes only differing values.
        private static bool SetEntry(StringTableCollection collection, string key, string pt, string en)
        {
            bool any = false;
            any |= SetValue(collection, PtBr, key, pt);
            any |= SetValue(collection, EnUs, key, en);
            return any;
        }

        private static bool SetValue(StringTableCollection collection, string locale, string key, string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            var table = collection.GetTable(locale) as StringTable;
            if (table == null) return false;

            StringTableEntry entry = table.GetEntry(key);
            if (entry == null)
            {
                table.AddEntry(key, value);
                MarkDirty(collection, table);
            }
            else if (entry.Value != value)
            {
                entry.Value = value;
                MarkDirty(collection, table);
            }
            return true;
        }

        private static void MarkDirty(StringTableCollection collection, StringTable table)
        {
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection.SharedData);
            _dirty = true;
        }

        private static bool SameRef(LocalizedString current, string key)
        {
            if (key == null) return current == null || current.IsEmpty;
            if (current == null || current.IsEmpty) return false;
            return current.TableReference.ReferenceType == TableReference.Type.Name
                && current.TableReference.TableCollectionName == CollectionName
                && current.TableEntryReference.ReferenceType == TableEntryReference.Type.Name
                && current.TableEntryReference.Key == key;
        }

        /// <summary>Saves the collection assets if anything changed since the last call.</summary>
        public static void Flush()
        {
            if (!_dirty) return;
            _dirty = false;
            AssetDatabase.SaveAssets();
        }

        /// <summary>Drops cached references (tests / domain-independent reset).</summary>
        public static void ResetCache()
        {
            _collection = null;
            _dirty = false;
        }
    }
}
