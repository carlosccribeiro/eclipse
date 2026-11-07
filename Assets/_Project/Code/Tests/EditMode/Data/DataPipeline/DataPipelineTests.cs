using System;
using System.Collections.Generic;
using System.IO;
using Eclipse.Core.DataTables;
using NUnit.Framework;
using UnityEditor;

namespace Eclipse.Editor.DataPipeline.Tests
{
    public class DataPipelineTests
    {
        private const string Folder = "Assets/_Project/Content/__PipelineTests__";
        private string _root;

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(Folder);
            _root = Path.Combine(Path.GetTempPath(), "eclipse_pipeline_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.Refresh();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private void WriteCsv(string body) => File.WriteAllText(Path.Combine(_root, "fake.csv"), body);

        private ImportContext RunFake()
        {
            var importer = new FakePipelineImporter("Fake", "fake.csv", Folder);
            ImportContext ctx = DataPipeline.Run(new ITableImporter[] { importer }, _root);
            AssetDatabase.SaveAssets();
            return ctx;
        }

        private static string AbsPath(string assetPath) =>
            Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, assetPath);

        private const string TwoRows = "id,power,label,partner\nfake.t.alpha,5,A,fake.t.beta\nfake.t.beta,7,B,\n";

        [Test]
        public void Import_CreatesAssetsByIdAndResolvesReferences()
        {
            WriteCsv(TwoRows);
            ImportContext ctx = RunFake();
            Assert.AreEqual(0, ctx.ErrorCount, string.Join("\n", ctx.Messages));
            Assert.AreEqual(2, ctx.Created);
            Assert.AreEqual(1, ctx.TablesProcessed);
            var alpha = AssetDatabase.LoadAssetAtPath<FakePipelineDef>(Folder + "/alpha.asset");
            Assert.IsNotNull(alpha);
            Assert.AreEqual("fake.t.alpha", alpha.StableIdString);
            Assert.AreEqual(5, alpha.Power);
            Assert.AreEqual("fake.t.beta", alpha.Partner.StableIdString);
            Assert.IsFalse(string.IsNullOrEmpty(alpha.ImportedHash));
        }

        [Test]
        public void Reimport_SameCsv_KeepsGuidsAndDoesNotRewriteFiles()
        {
            WriteCsv(TwoRows);
            RunFake();
            string a = Folder + "/alpha.asset", b = Folder + "/beta.asset";
            string guidA = AssetDatabase.AssetPathToGUID(a), guidB = AssetDatabase.AssetPathToGUID(b);
            byte[] bytesA = File.ReadAllBytes(AbsPath(a)), bytesB = File.ReadAllBytes(AbsPath(b));
            DateTime timeA = File.GetLastWriteTimeUtc(AbsPath(a)), timeB = File.GetLastWriteTimeUtc(AbsPath(b));

            System.Threading.Thread.Sleep(1100);
            ImportContext ctx = RunFake();

            Assert.AreEqual(0, ctx.Created);
            Assert.AreEqual(0, ctx.Updated);
            Assert.AreEqual(2, ctx.Unchanged);
            Assert.AreEqual(0, ctx.WarningCount);
            Assert.AreEqual(guidA, AssetDatabase.AssetPathToGUID(a));
            Assert.AreEqual(guidB, AssetDatabase.AssetPathToGUID(b));
            CollectionAssert.AreEqual(bytesA, File.ReadAllBytes(AbsPath(a)));
            CollectionAssert.AreEqual(bytesB, File.ReadAllBytes(AbsPath(b)));
            Assert.AreEqual(timeA, File.GetLastWriteTimeUtc(AbsPath(a)));
            Assert.AreEqual(timeB, File.GetLastWriteTimeUtc(AbsPath(b)));
        }

        [Test]
        public void Reimport_ChangedRow_UpdatesSameAsset()
        {
            WriteCsv(TwoRows);
            RunFake();
            string guid = AssetDatabase.AssetPathToGUID(Folder + "/beta.asset");
            WriteCsv("id,power,label,partner\nfake.t.alpha,5,A,fake.t.beta\nfake.t.beta,9,B,\n");
            ImportContext ctx = RunFake();
            Assert.AreEqual(1, ctx.Updated);
            Assert.AreEqual(1, ctx.Unchanged);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(Folder + "/beta.asset"));
            Assert.AreEqual(9, AssetDatabase.LoadAssetAtPath<FakePipelineDef>(Folder + "/beta.asset").Power);
        }

        [Test]
        public void Reimport_MatchesByIdEvenIfFileRenamed()
        {
            WriteCsv(TwoRows);
            RunFake();
            string guid = AssetDatabase.AssetPathToGUID(Folder + "/beta.asset");
            Assert.AreEqual(string.Empty, AssetDatabase.RenameAsset(Folder + "/beta.asset", "renamed"));
            ImportContext ctx = RunFake();
            Assert.AreEqual(0, ctx.Created);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(Folder + "/renamed.asset"));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<FakePipelineDef>(Folder + "/beta.asset"));
        }

        [Test]
        public void RemovedRow_ReportsOrphanWarning_AndKeepsAsset()
        {
            WriteCsv(TwoRows);
            RunFake();
            WriteCsv("id,power,label,partner\nfake.t.alpha,5,A,\n");
            ImportContext ctx = RunFake();
            Assert.AreEqual(0, ctx.ErrorCount);
            Assert.AreEqual(1, ctx.Orphans);
            Assert.AreEqual(1, ctx.WarningCount);
            StringAssert.Contains("orphan", ctx.Messages[0].Text);
            Assert.AreEqual("fake.t.beta", ctx.Messages[0].Id);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<FakePipelineDef>(Folder + "/beta.asset"));
        }

        [Test]
        public void BadCell_BecomesContextErrorWithLine_AndOtherRowsStillImport()
        {
            WriteCsv("id,power,label,partner\nfake.t.alpha,abc,A,\nfake.t.beta,7,B,\n");
            ImportContext ctx = RunFake();
            Assert.AreEqual(1, ctx.ErrorCount);
            ImportMessage m = ctx.Messages[0];
            Assert.AreEqual("Fake", m.Table);
            Assert.AreEqual(2, m.Line);
            Assert.AreEqual("fake.t.alpha", m.Id);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<FakePipelineDef>(Folder + "/alpha.asset"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<FakePipelineDef>(Folder + "/beta.asset"));
        }

        [Test]
        public void MalformedCsv_BecomesError()
        {
            WriteCsv("id,power\n\"unterminated,1\n");
            ImportContext ctx = RunFake();
            Assert.AreEqual(1, ctx.ErrorCount);
        }

        [Test]
        public void MissingCsvFile_BecomesError()
        {
            var importer = new FakePipelineImporter("Fake", "nope.csv", Folder);
            ImportContext ctx = DataPipeline.Run(new ITableImporter[] { importer }, _root);
            Assert.AreEqual(1, ctx.ErrorCount);
        }

        [Test]
        public void DuplicateAndInvalidIds_AreErrors()
        {
            WriteCsv("id,power,label,partner\nfake.t.alpha,1,A,\nfake.t.alpha,2,A,\nBad Id,3,C,\n");
            ImportContext ctx = RunFake();
            Assert.AreEqual(2, ctx.ErrorCount);
            Assert.AreEqual(1, ctx.Created);
        }

        [Test]
        public void UnresolvedReference_IsError()
        {
            WriteCsv("id,power,label,partner\nfake.t.alpha,1,A,fake.t.ghost\n");
            ImportContext ctx = RunFake();
            Assert.AreEqual(1, ctx.ErrorCount);
            StringAssert.Contains("fake.t.ghost", ctx.Messages[0].Text);
        }

        [Test]
        public void Validator_RunsAfterImport_AndCanAddErrors()
        {
            WriteCsv(TwoRows);
            var importer = new FakePipelineImporter("Fake", "fake.csv", Folder);
            ImportContext ctx = DataPipeline.Run(new ITableImporter[] { importer }, _root, c => c.Error("rule broken"));
            Assert.IsTrue(ctx.HasErrors);
            StringAssert.StartsWith("ImportAndValidate: 1 tables, 1 errors, 0 warnings", DataPipeline.Summary(ctx));
        }

        [Test]
        public void NoImporters_SummaryIsZero()
        {
            ImportContext ctx = DataPipeline.Run(new List<ITableImporter>(), _root);
            Assert.AreEqual("ImportAndValidate: 0 tables, 0 errors, 0 warnings", DataPipeline.Summary(ctx));
        }
    }
}
