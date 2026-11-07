using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Eclipse.Editor.DataPipeline.Tests
{
    public class ImporterRegistryTests
    {
        private static ImporterRegistry.Entry E(string name, int order, params string[] deps) =>
            new ImporterRegistry.Entry
            {
                Name = name,
                Order = order,
                DependsOn = deps,
                Importer = new FakePipelineImporter(name, name + ".csv", "Assets/x"),
            };

        private static string[] Names(List<ITableImporter> list)
        {
            var r = new string[list.Count];
            for (int i = 0; i < r.Length; i++) r[i] = list[i].TableName;
            return r;
        }

        [Test]
        public void Sort_DependenciesFirst_ThenOrderThenName()
        {
            var sorted = ImporterRegistry.Sort(new[] { E("c", 0, "a"), E("b", 1), E("a", 5), E("d", 1) });
            CollectionAssert.AreEqual(new[] { "b", "d", "a", "c" }, Names(sorted));
        }

        [Test]
        public void Sort_Cycle_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ImporterRegistry.Sort(new[] { E("a", 0, "b"), E("b", 0, "a") }));
        }

        [Test]
        public void Sort_UnknownDependencyOrDuplicateName_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => ImporterRegistry.Sort(new[] { E("a", 0, "zz") }));
            Assert.Throws<InvalidOperationException>(() => ImporterRegistry.Sort(new[] { E("a", 0), E("a", 1) }));
        }

        [Test]
        public void Discover_WithoutAttributedImporters_ReturnsEmptyWithoutErrors()
        {
            var ctx = new ImportContext();
            // Test importers carry no attribute, so only real registered tables (none yet or valid ones) appear.
            ImporterRegistry.Discover(ctx);
            Assert.AreEqual(0, ctx.ErrorCount, string.Join("\n", ctx.Messages));
        }
    }
}
