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
