using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Core.DataTables;
using Eclipse.Core.Ids;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using Eclipse.Editor.DataPipeline;
using Eclipse.Editor.Definitions;
using Eclipse.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Data.Affinities.Tests
{
    public class AffinityPilotTests
    {
        private const string Folder = "Assets/_Project/Content/__AffinityTests__";
        private const string RealFolder = "Assets/_Project/Content/Affinities";
        private string _root;

        private static readonly string[] Ids =
        {
            "affinity.core.ember", "affinity.core.frost", "affinity.core.storm",
            "affinity.core.void", "affinity.core.echo", "affinity.core.ruin",
        };

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(Folder);
            _root = Path.Combine(Path.GetTempPath(), "eclipse_aff_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.Refresh();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private static string RepoRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string RealCsv => Path.Combine(RepoRoot, "DataSource", "Affinities.csv");
        private static string AbsPath(string assetPath) => Path.Combine(RepoRoot, assetPath);

        private ImportContext Run(string csvText)
        {
            File.WriteAllText(Path.Combine(_root, "Affinities.csv"), csvText);
            var importer = new AffinityImporter("Affinities.csv", Folder);
            ImportContext ctx = DataPipeline.Run(new ITableImporter[] { importer }, _root);
            AssetDatabase.SaveAssets();
            return ctx;
        }

        private static List<AffinityDefinition> LoadAll(string folder) =>
            AssetDatabase.FindAssets("t:AffinityDefinition", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<AffinityDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(d => d.StableIdString, StringComparer.Ordinal).ToList();

        private static Color Hex(string hex)
        {
            Assert.IsTrue(ColorUtility.TryParseHtmlString(hex, out Color c));
            return c;
        }

        [Test]
        public void RealCsv_HasSixRowsEachWithSourceDoc()
        {
            CsvTable table = CsvTable.Parse(File.ReadAllText(RealCsv));
            Assert.AreEqual(6, table.Rows.Count);
            var ids = new List<string>();
            foreach (CsvRow row in table.Rows)
            {
                ids.Add(row.GetString("id"));
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.GetString("source_doc", null)), row.GetString("id"));
            }
            CollectionAssert.AreEquivalent(Ids, ids);
        }

        [Test]
        public void Import_RealCsv_ProducesSixDefinitionsWithDocValues()
        {
            ImportContext ctx = Run(File.ReadAllText(RealCsv));
            Assert.AreEqual(0, ctx.ErrorCount, string.Join("\n", ctx.Messages));
            Assert.AreEqual(0, ctx.WarningCount, string.Join("\n", ctx.Messages));
            List<AffinityDefinition> defs = LoadAll(Folder);
            Assert.AreEqual(6, defs.Count);

            var expected = new Dictionary<string, (Tag tag, string p, string s, string status)>
            {
                ["affinity.core.ember"] = (Tag.Ember, "#FF6A1A", "#FFC23D", "Burning"),
                ["affinity.core.frost"] = (Tag.Frost, "#9EE7FF", "#F2FBFF", "Chill"),
                ["affinity.core.storm"] = (Tag.Storm, "#F5E663", "#B49CFF", "Shock"),
                ["affinity.core.void"] = (Tag.Void, "#5A2BBF", "#E040FB", "Corruption"),
                ["affinity.core.echo"] = (Tag.Echo, "#6FFFE0", "#D8E3E8", "Echo Mark"),
                ["affinity.core.ruin"] = (Tag.Ruin, "#8FBF3A", "#4A5A1E", "Decay"),
            };
            foreach (AffinityDefinition d in defs)
            {
                var e = expected[d.StableIdString];
                Assert.AreEqual(e.tag, d.AffinityTag, d.StableIdString);
                Assert.AreEqual(Hex(e.p), d.Primary, d.StableIdString);
                Assert.AreEqual(Hex(e.s), d.Secondary, d.StableIdString);
                Assert.AreEqual(e.status, d.PrimaryStatusId, d.StableIdString);
                Assert.AreEqual("07 §10", d.SourceDoc);
                Assert.IsFalse(string.IsNullOrEmpty(d.NameEn));
                Assert.IsNull(d.Icon);
            }
        }

        [Test]
        public void Reimport_KeepsGuidsAndContent()
        {
            string csv = File.ReadAllText(RealCsv);
            Run(csv);
            var before = LoadAll(Folder).ToDictionary(d => d.StableIdString,
                d => (guid: AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(d)),
                      bytes: File.ReadAllBytes(AbsPath(AssetDatabase.GetAssetPath(d)))));

            ImportContext ctx = Run(csv);
            Assert.AreEqual(0, ctx.Created);
            Assert.AreEqual(0, ctx.Updated);
            Assert.AreEqual(6, ctx.Unchanged);
            foreach (AffinityDefinition d in LoadAll(Folder))
            {
                string path = AssetDatabase.GetAssetPath(d);
                Assert.AreEqual(before[d.StableIdString].guid, AssetDatabase.AssetPathToGUID(path));
                CollectionAssert.AreEqual(before[d.StableIdString].bytes, File.ReadAllBytes(AbsPath(path)));
            }
        }

        [Test]
        public void RemovedRow_WarnsOrphanAndKeepsAsset()
        {
            string[] lines = File.ReadAllLines(RealCsv).Where(l => l.Length > 0).ToArray();
            Run(string.Join("\n", lines) + "\n");
            string trimmed = string.Join("\n", lines.Where(l => !l.StartsWith("affinity.core.ruin,"))) + "\n";
            ImportContext ctx = Run(trimmed);
            Assert.AreEqual(0, ctx.ErrorCount);
            Assert.AreEqual(1, ctx.Orphans);
            Assert.IsTrue(ctx.Messages.Any(m => m.Severity == ImportSeverity.Warning && m.Id == "affinity.core.ruin"));
            Assert.AreEqual(6, LoadAll(Folder).Count);
        }

        [Test]
        public void RealContent_PassesValidatorAndRegistryHasSixAffinities()
        {
            Assert.AreEqual(6, LoadAll(RealFolder).Count, "run Eclipse > Data > Import & Validate to generate the assets");

            ValidationReport report = DataValidator.ValidateProject();
            Assert.AreEqual(0, report.ErrorCount, string.Join("\n", report.Messages));

            DefinitionRegistry registry = RegistryBuilder.Rebuild();
            Assert.AreEqual(6, registry.All<AffinityDefinition>().Count);
            foreach (string id in Ids)
                Assert.IsTrue(registry.Contains(StableId.Parse(id)), id);
        }
    }
}
