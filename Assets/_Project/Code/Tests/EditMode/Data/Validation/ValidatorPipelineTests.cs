using System.Collections.Generic;
using System.IO;
using Eclipse.Data.Definitions;
using Eclipse.Editor.DataPipeline;
using Eclipse.Editor.Validation;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Data.Validation.Tests
{
    public class ValidatorPipelineTests
    {
        [Test]
        public void ValidatorIsConnectedToPipeline()
        {
            DataValidator.Connect();
            Assert.IsNotNull(Eclipse.Editor.DataPipeline.DataPipeline.Validator);
        }

        private static ImportContext RunWith(string id, out string root, out FakeValidationDef def)
        {
            def = FakeValidationDef.Make<FakeValidationDef>(id);
            root = Path.Combine(Path.GetTempPath(), "eclipse_t4_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            FakeValidationDef captured = def;
            return Eclipse.Editor.DataPipeline.DataPipeline.Run(new List<ITableImporter>(), root,
                c => DataValidator.CopyReport(DataValidator.Validate(new Definition[] { captured }), c));
        }

        [Test]
        public void Run_WithInvalidId_HasErrors()
        {
            ImportContext ctx = RunWith("Not Valid", out string root, out FakeValidationDef def);
            try { Assert.IsTrue(ctx.HasErrors); }
            finally { Object.DestroyImmediate(def); Directory.Delete(root, true); }
        }

        [Test]
        public void Run_WithValidId_HasNoErrors()
        {
            ImportContext ctx = RunWith("good.id", out string root, out FakeValidationDef def);
            try { Assert.IsFalse(ctx.HasErrors); }
            finally { Object.DestroyImmediate(def); Directory.Delete(root, true); }
        }
    }
}
