using System.IO;
using Eclipse.Core.Definitions;
using Eclipse.Core.Ids;
using Eclipse.Editor.Definitions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Data.Definitions.Tests
{
    public class RegistryBuilderTests
    {
        private const string Folder = "Assets/_Project/Content/__Tests__";
        private const string Registry = Folder + "/Registry.asset";
        private const string Remap = Folder + "/Remap.asset";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.CreateFolder("Assets/_Project/Content", "__Tests__");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.Refresh();
        }

        private static T Create<T>(string file, string id) where T : Definition
        {
            var d = ScriptableObject.CreateInstance<T>();
            d.EditorSetStableId(id);
            AssetDatabase.CreateAsset(d, Folder + "/" + file);
            return d;
        }

        [Test]
        public void Rebuild_CollectsDefinitionsInPathOrder()
        {
            var b = Create<FakeDefB>("b.asset", "t.b");
            var a = Create<FakeDefA>("a.asset", "t.a");
            DefinitionRegistry reg = RegistryBuilder.Rebuild(Folder, Registry, Remap);
            Assert.AreEqual(2, reg.Count);
            Assert.AreSame(a, reg.Definitions[0]);
            Assert.AreSame(b, reg.Definitions[1]);
            Assert.IsNotNull(reg.RemapAsset);
            Assert.AreSame(a, reg.Get<FakeDefA>(new StableId("t.a")));
        }

        [Test]
        public void Rebuild_DuplicateThrows()
        {
            Create<FakeDefA>("a.asset", "t.same");
            Create<FakeDefB>("b.asset", "t.same");
            Assert.Throws<DuplicateDefinitionException>(() => RegistryBuilder.Rebuild(Folder, Registry, Remap));
        }

        [Test]
        public void Rebuild_UnchangedDoesNotTouchFile()
        {
            Create<FakeDefA>("a.asset", "t.a");
            RegistryBuilder.Rebuild(Folder, Registry, Remap);
            string full = Path.GetFullPath(Registry);
            File.SetLastWriteTimeUtc(full, new System.DateTime(2020, 1, 1, 0, 0, 0, System.DateTimeKind.Utc));
            System.DateTime before = File.GetLastWriteTimeUtc(full);
            RegistryBuilder.Rebuild(Folder, Registry, Remap);
            Assert.AreEqual(before, File.GetLastWriteTimeUtc(full));
        }
    }
}
