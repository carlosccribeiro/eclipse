using System;
using Eclipse.Core.Ids;
using NUnit.Framework;

namespace Eclipse.Core.Ids.Tests
{
    public class IdRemapTableTests
    {
        private static StableId Id(string s) => new StableId(s);

        [Test]
        public void Unmapped_id_resolves_to_itself()
        {
            var t = new IdRemapTable();
            Assert.AreEqual(Id("a.x"), t.Resolve(Id("a.x")));
            Assert.IsFalse(t.TryResolve(Id("a.x"), out var r));
            Assert.AreEqual(Id("a.x"), r);
            Assert.AreEqual(0, t.Count);
        }

        [Test]
        public void Single_and_chain_resolve()
        {
            var t = new IdRemapTable();
            t.Add(Id("a.one"), Id("a.two"));
            t.Add(Id("a.two"), Id("a.three"));
            Assert.AreEqual(2, t.Count);
            Assert.AreEqual(Id("a.three"), t.Resolve(Id("a.one")));
            Assert.AreEqual(Id("a.three"), t.Resolve(Id("a.two")));
            Assert.IsTrue(t.TryResolve(Id("a.one"), out var r));
            Assert.AreEqual(Id("a.three"), r);
            Assert.IsTrue(t.Contains(Id("a.one")));
            Assert.IsFalse(t.Contains(Id("a.three")));
        }

        [Test]
        public void Chain_added_in_reverse_order_resolves()
        {
            var t = new IdRemapTable();
            t.Add(Id("a.two"), Id("a.three"));
            t.Add(Id("a.one"), Id("a.two"));
            Assert.AreEqual(Id("a.three"), t.Resolve(Id("a.one")));
        }

        [Test]
        public void Self_map_rejected()
        {
            var t = new IdRemapTable();
            Assert.Throws<ArgumentException>(() => t.Add(Id("a.one"), Id("a.one")));
            Assert.AreEqual(0, t.Count);
        }

        [Test]
        public void Duplicate_source_rejected()
        {
            var t = new IdRemapTable();
            t.Add(Id("a.one"), Id("a.two"));
            Assert.Throws<ArgumentException>(() => t.Add(Id("a.one"), Id("a.three")));
            Assert.AreEqual(1, t.Count);
        }

        [Test]
        public void Empty_ids_rejected()
        {
            var t = new IdRemapTable();
            Assert.Throws<ArgumentException>(() => t.Add(default, Id("a.one")));
            Assert.Throws<ArgumentException>(() => t.Add(Id("a.one"), default));
        }

        [Test]
        public void Cycle_of_two_rejected()
        {
            var t = new IdRemapTable();
            t.Add(Id("a.one"), Id("a.two"));
            Assert.Throws<InvalidOperationException>(() => t.Add(Id("a.two"), Id("a.one")));
            Assert.AreEqual(1, t.Count);
        }

        [Test]
        public void Cycle_of_three_rejected()
        {
            var t = new IdRemapTable();
            t.Add(Id("a.one"), Id("a.two"));
            t.Add(Id("a.two"), Id("a.three"));
            Assert.Throws<InvalidOperationException>(() => t.Add(Id("a.three"), Id("a.one")));
            Assert.AreEqual(2, t.Count);
            Assert.AreEqual(Id("a.three"), t.Resolve(Id("a.one")));
        }
    }
}
