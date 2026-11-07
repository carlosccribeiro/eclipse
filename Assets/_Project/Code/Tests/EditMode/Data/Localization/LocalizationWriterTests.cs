using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Eclipse.Data.Affinities;
using Eclipse.Editor.DataPipeline;
using Eclipse.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Eclipse.Data.Localization.Tests
{
    public class LocalizationWriterTests
    {
        private static readonly string[] Ids =
        {
            "affinity.core.ember", "affinity.core.frost", "affinity.core.storm",
            "affinity.core.void", "affinity.core.echo", "affinity.core.ruin",
        };

        private static string RepoRoot => Directory.GetParent(Application.dataPath).FullName;

        [SetUp]
        public void SetUp()
        {
            LocalizationWriter.ResetCache();
            RunPipeline();
        }

        [Test]
        public void AffinityNames_ExistInBothLocales()
        {
            StringTableCollection c = LocalizationEditorSettings.GetStringTableCollection(LocalizationWriter.CollectionName);
            Assert.NotNull(c);
            var pt = (StringTable)c.GetTable("pt-BR");
            var en = (StringTable)c.GetTable("en-US");
            Assert.NotNull(pt);
            Assert.NotNull(en);
            foreach (string id in Ids)
            {
                Assert.IsFalse(string.IsNullOrEmpty(pt.GetEntry(id + ".name")?.Value), id + " pt name");
                Assert.IsFalse(string.IsNullOrEmpty(en.GetEntry(id + ".name")?.Value), id + " en name");
                Assert.IsFalse(string.IsNullOrEmpty(pt.GetEntry(id + ".desc")?.Value), id + " pt desc");
            }
            Assert.AreEqual("Ember", pt.GetEntry("affinity.core.ember.name").Value);
            Assert.AreEqual("Ember", en.GetEntry("affinity.core.ember.name").Value);
            Assert.AreEqual("Damage over time, explosions and spread.", en.GetEntry("affinity.core.ember.desc").Value);
        }

        [Test]
        public void Definitions_PointAtEntries()
        {
            var defs = AssetDatabase.FindAssets("t:AffinityDefinition", new[] { AffinityImporter.DefaultOutputFolder })
                .Select(g => AssetDatabase.LoadAssetAtPath<AffinityDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToList();
            Assert.AreEqual(6, defs.Count);
            foreach (AffinityDefinition d in defs)
            {
                Assert.IsFalse(d.DisplayName.IsEmpty, d.StableIdString);
                Assert.AreEqual(d.StableIdString + ".name", d.DisplayName.TableEntryReference.Key);
                Assert.AreEqual(LocalizationWriter.CollectionName, d.DisplayName.TableReference.TableCollectionName);
            }
        }

        [Test]
        public void Reimport_IsIdempotent()
        {
            Dictionary<string, string> before = HashFiles();
            Assert.Greater(before.Count, 0);
            RunPipeline();
            Dictionary<string, string> after = HashFiles();
            CollectionAssert.AreEquivalent(before, after);
        }

        [Test]
        public void ImportAndValidate_HasNoErrorsOrLocalizationWarnings()
        {
            ImportContext ctx = RunPipeline();
            Assert.AreEqual(0, ctx.ErrorCount, string.Join("\n", ctx.Messages.Select(m => m.ToString())));
            Assert.IsFalse(ctx.Messages.Any(m => m.Text.Contains("LocalizationKey")));
        }

        [Test]
        public void PartialLocalizationColumns_AreOptional()
        {
            const string folder = "Assets/_Project/Content/__LocPartialTests__";
            const string key = "fake.loc.partial";
            string root = Path.Combine(Path.GetTempPath(), "eclipse_loc_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                // Only name_en exists: name_pt/desc_pt/desc_en are missing columns and must read as empty.
                File.WriteAllText(Path.Combine(root, "fake.csv"), "id,power,label,partner,name_en\n" + key + ",1,L,,Only English\n");
                var importer = new Eclipse.Editor.DataPipeline.Tests.FakePipelineImporter("Fake", "fake.csv", folder);
                ImportContext ctx = DataPipeline.Run(new ITableImporter[] { importer }, root);
                AssetDatabase.SaveAssets();

                Assert.AreEqual(0, ctx.ErrorCount, string.Join("\n", ctx.Messages.Select(m => m.ToString())));
                var def = AssetDatabase.LoadAssetAtPath<Eclipse.Editor.DataPipeline.Tests.FakePipelineDef>(folder + "/partial.asset");
                Assert.NotNull(def);
                Assert.IsFalse(def.DisplayName.IsEmpty);
                Assert.IsTrue(def.Description.IsEmpty);
                StringTableCollection c = LocalizationEditorSettings.GetStringTableCollection(LocalizationWriter.CollectionName);
                Assert.AreEqual("Only English", ((StringTable)c.GetTable("en-US")).GetEntry(key + ".name").Value);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
                StringTableCollection c = LocalizationEditorSettings.GetStringTableCollection(LocalizationWriter.CollectionName);
                if (c != null)
                {
                    c.RemoveEntry(key + ".name");
                    EditorUtility.SetDirty(c.SharedData);
                    foreach (var t in c.StringTables) EditorUtility.SetDirty(t);
                    AssetDatabase.SaveAssets();
                }
                Directory.Delete(root, true);
            }
        }

        private static ImportContext RunPipeline()
        {
            var ctx = new ImportContext();
            ImportContext result = DataPipeline.Run(ImporterRegistry.Discover(ctx), null, DataValidator.PipelineValidator, ctx);
            AssetDatabase.SaveAssets();
            return result;
        }

        private static Dictionary<string, string> HashFiles()
        {
            var map = new Dictionary<string, string>();
            foreach (string folder in new[] { "Assets/_Project/Settings/Localization", "Assets/_Project/Content/Affinities" })
            {
                string abs = Path.Combine(RepoRoot, folder);
                foreach (string f in Directory.GetFiles(abs, "*", SearchOption.AllDirectories))
                {
                    using (var md5 = MD5.Create())
                        map[f.Substring(RepoRoot.Length)] = System.BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(f)));
                }
            }
            return map;
        }
    }
}
