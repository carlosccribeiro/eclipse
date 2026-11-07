using System;
using System.Collections.Generic;
using Eclipse.Core.Ids;
using NUnit.Framework;

namespace Eclipse.Core.Definitions.Tests
{
    public class InMemoryDefinitionRegistryTests
    {
        private class ItemDef : IDefinition
        {
            public ItemDef(string id) { Id = StableId.Parse(id); }
            public StableId Id { get; }
        }

        private class HeroDef : IDefinition
        {
            public HeroDef(string id) { Id = StableId.Parse(id); }
            public StableId Id { get; }
        }

        private class EmptyIdDef : IDefinition
        {
            public StableId Id => default;
        }

        private static StableId S(string s) => StableId.Parse(s);

        [Test]
        public void RegisterAndGet()
        {
            var r = new InMemoryDefinitionRegistry();
            var d = new ItemDef("item.sword");
            r.Register(d);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.Contains(S("item.sword")));
            Assert.IsTrue(r.TryGet(S("item.sword"), out ItemDef got));
            Assert.AreSame(d, got);
            Assert.AreSame(d, r.Get<ItemDef>(S("item.sword")));
            Assert.AreSame(d, r.Get<IDefinition>(S("item.sword")));
        }

        [Test]
        public void MissingId()
        {
            var r = new InMemoryDefinitionRegistry();
            Assert.IsFalse(r.TryGet(S("item.none"), out ItemDef got));
            Assert.IsNull(got);
            Assert.IsFalse(r.Contains(S("item.none")));
            var ex = Assert.Throws<KeyNotFoundException>(() => r.Get<ItemDef>(S("item.none")));
            StringAssert.Contains("item.none", ex.Message);
        }

        [Test]
        public void WrongType()
        {
            var r = new InMemoryDefinitionRegistry();
            r.Register(new ItemDef("item.sword"));
            Assert.IsFalse(r.TryGet(S("item.sword"), out HeroDef got));
            Assert.IsNull(got);
            Assert.IsTrue(r.Contains(S("item.sword")));
            var ex = Assert.Throws<KeyNotFoundException>(() => r.Get<HeroDef>(S("item.sword")));
            StringAssert.Contains("item.sword", ex.Message);
        }

        [Test]
        public void AllFiltersByTypeInRegistrationOrder()
        {
            var r = new InMemoryDefinitionRegistry();
            var a = new ItemDef("item.a");
            var h = new HeroDef("hero.h");
            var b = new ItemDef("item.b");
            r.Register(a); r.Register(h); r.Register(b);
            var items = r.All<ItemDef>();
            Assert.AreEqual(2, items.Count);
            Assert.AreSame(a, items[0]);
            Assert.AreSame(b, items[1]);
            Assert.AreEqual(1, r.All<HeroDef>().Count);
            Assert.AreEqual(3, r.All<IDefinition>().Count);
        }

        [Test]
        public void AllIsCachedAndRefreshedAfterRegister()
        {
            var r = new InMemoryDefinitionRegistry();
            r.Register(new ItemDef("item.a"));
            var first = r.All<ItemDef>();
            Assert.AreSame(first, r.All<ItemDef>());
            r.Register(new ItemDef("item.b"));
            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(2, r.All<ItemDef>().Count);
        }

        [Test]
        public void DuplicateThrowsWithId()
        {
            var r = new InMemoryDefinitionRegistry();
            r.Register(new ItemDef("item.a"));
            var ex = Assert.Throws<DuplicateDefinitionException>(() => r.Register(new HeroDef("item.a")));
            Assert.AreEqual(S("item.a"), ex.Id);
            StringAssert.Contains("item.a", ex.Message);
            Assert.AreEqual(1, r.Count);
        }

        [Test]
        public void RemappedIdResolvesToNewDefinition()
        {
            var remap = new IdRemapTable();
            remap.Add(S("item.old"), S("item.new"));
            var r = new InMemoryDefinitionRegistry(remap);
            var d = new ItemDef("item.new");
            r.Register(d);
            Assert.AreSame(d, r.Get<ItemDef>(S("item.old")));
            Assert.IsTrue(r.TryGet(S("item.old"), out ItemDef got));
            Assert.AreSame(d, got);
            Assert.IsTrue(r.Contains(S("item.old")));
            Assert.AreSame(remap, r.Remap);
        }

        [Test]
        public void RemapChain()
        {
            var remap = new IdRemapTable();
            remap.Add(S("item.a"), S("item.b"));
            remap.Add(S("item.b"), S("item.c"));
            var r = new InMemoryDefinitionRegistry(remap);
            var d = new ItemDef("item.c");
            r.Register(d);
            Assert.AreSame(d, r.Get<ItemDef>(S("item.a")));
            Assert.AreSame(d, r.Get<ItemDef>(S("item.b")));
        }

        [Test]
        public void RegisteringObsoleteIdThrows()
        {
            var remap = new IdRemapTable();
            remap.Add(S("item.old"), S("item.new"));
            var r = new InMemoryDefinitionRegistry(remap);
            Assert.Throws<ArgumentException>(() => r.Register(new ItemDef("item.old")));
            Assert.AreEqual(0, r.Count);
        }

        [Test]
        public void NullAndEmptyThrow()
        {
            var r = new InMemoryDefinitionRegistry();
            Assert.Throws<ArgumentNullException>(() => r.Register(null));
            Assert.Throws<ArgumentException>(() => r.Register(new EmptyIdDef()));
            Assert.Throws<ArgumentNullException>(() => r.RegisterRange(null));
            Assert.IsNotNull(r.Remap);
            Assert.AreEqual(0, r.Remap.Count);
        }

        [Test]
        public void RegisterRangeReportsFirstDuplicate()
        {
            var r = new InMemoryDefinitionRegistry();
            var list = new List<IDefinition> { new ItemDef("item.a"), new ItemDef("item.b"), new ItemDef("item.a") };
            var ex = Assert.Throws<DuplicateDefinitionException>(() => r.RegisterRange(list));
            Assert.AreEqual(S("item.a"), ex.Id);
            Assert.AreEqual(2, r.Count);
        }
    }
}
