using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eclipse.Core.Ids;
using NUnit.Framework;

namespace Eclipse.Core.Ids.Tests
{
    public class StableIdTests
    {
        private static readonly Regex Pattern = new Regex(@"^[a-z][a-z0-9_]*(\.[a-z0-9][a-z0-9_]*){1,3}$");

        [TestCase("item.ashfang")]
        [TestCase("mod.cleaving_strike.echo_slash")]
        [TestCase("a.b.c.d")]
        [TestCase("item.1abc")]
        [TestCase("a1_b.2")]
        public void Valid_ids_parse(string s)
        {
            var id = StableId.Parse(s);
            Assert.AreEqual(s, id.Value);
            Assert.AreEqual(s, id.ToString());
            Assert.IsFalse(id.IsEmpty);
            Assert.AreEqual(new StableId(s), id);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Item.ashfang")]
        [TestCase("item.Ashfang")]
        [TestCase("item. ashfang")]
        [TestCase("item ashfang")]
        [TestCase("item.ashfang.")]
        [TestCase(".item.ashfang")]
        [TestCase("item..ashfang")]
        [TestCase("item")]
        [TestCase("a.b.c.d.e")]
        [TestCase("1item.ashfang")]
        [TestCase("_item.ashfang")]
        [TestCase("item.ash-fang")]
        [TestCase("item.ashfang\n")]
        public void Invalid_ids_throw_and_TryParse_fails(string s)
        {
            Assert.Throws<ArgumentException>(() => new StableId(s));
            Assert.Throws<ArgumentException>(() => StableId.Parse(s));
            Assert.IsFalse(StableId.TryParse(s, out var id));
            Assert.IsTrue(id.IsEmpty);
        }

        [Test]
        public void TryParse_valid()
        {
            Assert.IsTrue(StableId.TryParse("item.ashfang", out var id));
            Assert.AreEqual("item.ashfang", id.Value);
        }

        [Test]
        public void Manual_validation_matches_regex()
        {
            string[] samples =
            {
                "item.ashfang", "a.b", "a", "", ".", "a.", ".a", "a..b", "a.b.c.d", "a.b.c.d.e", "a.1", "1.a", "a.1b_",
                "a_.b_", "A.b", "a.B", "a b.c","a._b", "a.b-c", "é.a", "a.é", "a0.0a.0_.z9"
            };
            foreach (var s in samples)
                Assert.AreEqual(Pattern.IsMatch(s), StableId.IsValid(s), "sample: '" + s + "'");
        }

        [Test]
        public void Default_is_empty_and_safe()
        {
            var d = default(StableId);
            Assert.IsTrue(d.IsEmpty);
            Assert.AreEqual(string.Empty, d.Value);
            Assert.AreEqual(string.Empty, d.ToString());
            Assert.AreEqual(0, d.GetHashCode());
            Assert.IsTrue(d.Equals(default(StableId)));
            Assert.IsFalse(d.Equals(new StableId("a.b")));
        }

        [Test]
        public void Equality_hash_and_operators()
        {
            var a = new StableId("item.ashfang");
            var b = StableId.Parse(new string("item.ashfang".ToCharArray()));
            var c = new StableId("item.other");
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsFalse(a == c);
            Assert.IsTrue(a.Equals((object)b));
            Assert.IsFalse(a.Equals((object)"item.ashfang"));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            var set = new HashSet<StableId> { a, b, c };
            Assert.AreEqual(2, set.Count);
        }

        [Test]
        public void CompareTo_is_ordinal()
        {
            var a = new StableId("a.b");
            var b = new StableId("a.c");
            Assert.Less(a.CompareTo(b), 0);
            Assert.Greater(b.CompareTo(a), 0);
            Assert.AreEqual(0, a.CompareTo(new StableId("a.b")));
        }
    }
}
