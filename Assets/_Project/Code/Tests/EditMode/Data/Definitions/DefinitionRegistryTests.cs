using System.Collections.Generic;
using Eclipse.Core.Definitions;
using Eclipse.Core.Ids;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Data.Definitions.Tests
{
    public class DefinitionRegistryTests
    {
        private readonly List<Object> _created = new List<Object>();

        private T Make<T>(string id) where T : Definition
        {
            var d = ScriptableObject.CreateInstance<T>();
            d.EditorSetStableId(id);
            _created.Add(d);
            return d;
        }

        private DefinitionRegistry NewRegistry()
        {
            var r = ScriptableObject.CreateInstance<DefinitionRegistry>();
            _created.Add(r);
            return r;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) if (o != null) Object.DestroyImmediate(o);
            _created.Clear();
        }

        [Test]
        public void Id_ParsesAndCaches()
        {
            var a = Make<FakeDefA>("aff.fire");
            Assert.AreEqual(new StableId("aff.fire"), a.Id);
            a.EditorSetStableId("aff.ice");
            Assert.AreEqual(new StableId("aff.ice"), a.Id);
        }

        [Test]
        public void ResolvesById()
        {
            var a = Make<FakeDefA>("aff.fire");
            var r = NewRegistry();
            r.SetDefinitions(new Definition[] { a });
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.Contains(new StableId("aff.fire")));
            Assert.AreSame(a, r.Get<FakeDefA>(new StableId("aff.fire")));
            Assert.IsFalse(r.TryGet<FakeDefB>(new StableId("aff.fire"), out _));
        }

        [Test]
        public void ResolvesByRemap()
        {
            var a = Make<FakeDefA>("aff.new");
            var remap = ScriptableObject.CreateInstance<IdRemapAsset>();
            _created.Add(remap);
            remap.AddPair("aff.old", "aff.new");
            var r = NewRegistry();
            r.SetDefinitions(new Definition[] { a }, remap);
            Assert.AreSame(a, r.Get<FakeDefA>(new StableId("aff.old")));
            Assert.AreEqual(1, r.Remap.Count);
        }

        [Test]
        public void Duplicate_Throws_AndKeepsPreviousState()
        {
            var a = Make<FakeDefA>("aff.fire");
            var b = Make<FakeDefB>("aff.fire");
            var r = NewRegistry();
            r.SetDefinitions(new Definition[] { a });
            Assert.Throws<DuplicateDefinitionException>(() => r.SetDefinitions(new Definition[] { a, b }));
            Assert.AreEqual(1, r.Count);
        }

        [Test]
        public void All_IsTypedAndCached()
        {
            var a1 = Make<FakeDefA>("a.one");
            var b1 = Make<FakeDefB>("b.one");
            var a2 = Make<FakeDefA>("a.two");
            var r = NewRegistry();
            r.SetDefinitions(new Definition[] { a1, b1, a2 });
            IReadOnlyList<FakeDefA> list = r.All<FakeDefA>();
            Assert.AreEqual(2, list.Count);
            Assert.AreSame(a1, list[0]);
            Assert.AreSame(a2, list[1]);
            Assert.AreSame(list, r.All<FakeDefA>());
            Assert.AreEqual(3, r.All<Definition>().Count);
        }

        [Test]
        public void IdRemapAsset_RejectsCycle()
        {
            var remap = ScriptableObject.CreateInstance<IdRemapAsset>();
            _created.Add(remap);
            remap.AddPair("a.x", "a.y");
            Assert.Throws<System.InvalidOperationException>(() => remap.AddPair("a.y", "a.x"));
            Assert.AreEqual(1, remap.Pairs.Count);
        }
    }
}
