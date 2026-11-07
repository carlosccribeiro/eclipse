using System;
using Eclipse.Core.Tags;
using NUnit.Framework;

namespace Eclipse.Core.Tags.Tests
{
    public class TagMaskTests
    {
        private static readonly string[] Vocabulary =
        {
            "Attack", "Spell", "Melee", "Ranged", "Projectile", "Area", "DoT", "Summon", "Construct",
            "Movement", "Defensive", "Ultimate", "Channel", "Trap", "Clone", "Basic", "Heavy", "Ember",
            "Frost", "Storm", "Void", "Echo", "Ruin", "Physical",
        };

        [Test]
        public void Enum_MatchesVocabularyExactlyAndInOrder()
        {
            Assert.AreEqual(Vocabulary, Enum.GetNames(typeof(Tag)));
            Assert.AreEqual(24, TagMask.TagCount);
            Assert.AreEqual(TagMask.TagCount, Vocabulary.Length);
        }

        [Test]
        public void Enum_ValuesAreContiguousFromZero()
        {
            var values = (Tag[])Enum.GetValues(typeof(Tag));
            for (int i = 0; i < values.Length; i++) Assert.AreEqual(i, (int)values[i]);
        }

        [Test]
        public void NoneAndAll()
        {
            Assert.IsTrue(TagMask.None.IsEmpty);
            Assert.AreEqual(0, TagMask.None.Count);
            Assert.AreEqual(24, TagMask.All.Count);
            Assert.AreEqual((1UL << 24) - 1UL, TagMask.All.Bits);
            Assert.IsFalse(TagMask.All.IsEmpty);
            foreach (Tag t in Enum.GetValues(typeof(Tag)))
            {
                Assert.IsTrue(TagMask.All.Has(t));
                Assert.IsFalse(TagMask.None.Has(t));
            }
        }

        [Test]
        public void Of_SetsSingleBit()
        {
            foreach (Tag t in Enum.GetValues(typeof(Tag)))
            {
                var m = TagMask.Of(t);
                Assert.AreEqual(1UL << (int)t, m.Bits);
                Assert.IsTrue(m.Has(t));
                Assert.AreEqual(1, m.Count);
                Assert.AreEqual(m, new TagMask(t));
            }
        }

        [Test]
        public void ParamsConstructor()
        {
            var m = new TagMask(Tag.Attack, Tag.Melee, Tag.Attack);
            Assert.AreEqual(2, m.Count);
            Assert.IsTrue(m.Has(Tag.Attack));
            Assert.IsTrue(m.Has(Tag.Melee));
            Assert.IsFalse(m.Has(Tag.Spell));
            Assert.AreEqual(TagMask.None, new TagMask(new Tag[0]));
        }

        [Test]
        public void InvalidTagValue_IsIgnored()
        {
            var bad = (Tag)200;
            Assert.AreEqual(TagMask.None, new TagMask(bad));
            Assert.IsFalse(TagMask.All.Has(bad));
            Assert.AreEqual(TagMask.None, TagMask.None.With(bad));
        }

        [Test]
        public void HasAllHasAny()
        {
            var am = TagMask.Of(Tag.Attack) | TagMask.Of(Tag.Melee);
            Assert.IsTrue(am.HasAll(TagMask.Of(Tag.Attack)));
            Assert.IsTrue(am.HasAll(am));
            Assert.IsTrue(am.HasAll(TagMask.None));
            Assert.IsFalse(am.HasAll(new TagMask(Tag.Attack, Tag.Spell)));
            Assert.IsTrue(am.HasAny(new TagMask(Tag.Attack, Tag.Spell)));
            Assert.IsFalse(am.HasAny(new TagMask(Tag.Spell, Tag.Ranged)));
            Assert.IsFalse(am.HasAny(TagMask.None));
        }

        [Test]
        public void UnionExceptWithWithout()
        {
            var a = new TagMask(Tag.Attack, Tag.Melee);
            var b = new TagMask(Tag.Melee, Tag.Heavy);
            Assert.AreEqual(new TagMask(Tag.Attack, Tag.Melee, Tag.Heavy), a.Union(b));
            Assert.AreEqual(a.Union(b), a | b);
            Assert.AreEqual(TagMask.Of(Tag.Melee), a & b);
            Assert.AreEqual(TagMask.Of(Tag.Attack), a.Except(b));
            Assert.AreEqual(TagMask.Of(Tag.Attack), a.Without(Tag.Melee));
            Assert.AreEqual(a, a.Without(Tag.Heavy));
            Assert.AreEqual(a.With(Tag.Heavy), a | TagMask.Of(Tag.Heavy));
            Assert.AreEqual(a, a.With(Tag.Attack));
            Assert.AreEqual(TagMask.None, TagMask.All.Except(TagMask.All));
        }

        [Test]
        public void EqualityAndHash()
        {
            var a = new TagMask(Tag.Ember, Tag.Area);
            var b = TagMask.Of(Tag.Area) | TagMask.Of(Tag.Ember);
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.IsTrue(a.Equals((object)b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.IsTrue(a != TagMask.None);
        }

        [Test]
        public void FromBits_DiscardsBitsAboveVocabulary()
        {
            Assert.AreEqual(TagMask.All, TagMask.FromBits(ulong.MaxValue));
            Assert.AreEqual(TagMask.None, TagMask.FromBits(1UL << 40));
        }

        [Test]
        public void ToString_FormatAndOrder()
        {
            Assert.AreEqual("None", TagMask.None.ToString());
            Assert.AreEqual("Attack|Melee", (TagMask.Of(Tag.Melee) | TagMask.Of(Tag.Attack)).ToString());
            Assert.AreEqual(string.Join("|", Vocabulary), TagMask.All.ToString());
        }

        [Test]
        public void Parse_AllTagsIsolated_RoundTrip()
        {
            foreach (Tag t in Enum.GetValues(typeof(Tag)))
            {
                string name = t.ToString();
                Assert.AreEqual(TagMask.Of(t), TagMask.Parse(name));
                Assert.IsTrue(TagMask.TryParseTag(name, out Tag parsed));
                Assert.AreEqual(t, parsed);
                Assert.AreEqual(name, TagMask.Of(t).ToString());
            }
        }

        [Test]
        public void Parse_Combinations_AndSeparators()
        {
            var expected = new TagMask(Tag.Attack, Tag.Melee);
            Assert.AreEqual(expected, TagMask.Parse("Attack|Melee"));
            Assert.AreEqual(expected, TagMask.Parse("Melee|Attack"));
            Assert.AreEqual(expected, TagMask.Parse("Attack,Melee"));
            Assert.AreEqual(expected, TagMask.Parse("  Attack , Melee  "));
            Assert.AreEqual(expected, TagMask.Parse("Attack|Melee,Attack"));
            Assert.AreEqual(TagMask.All, TagMask.Parse(string.Join("|", Vocabulary)));
        }

        [Test]
        public void Parse_EmptyOrBlank_IsNone()
        {
            Assert.AreEqual(TagMask.None, TagMask.Parse(""));
            Assert.AreEqual(TagMask.None, TagMask.Parse("   "));
            Assert.AreEqual(TagMask.None, TagMask.Parse(null));
        }

        [Test]
        public void Parse_Invalid_Rejected()
        {
            // Unknown, numeric, wrong case, and empty tokens ("Attack||Melee", leading/trailing separators) are invalid.
            string[] invalid = { "Fire", "3", "attack", "ATTACK", "Attack||Melee", "Attack|", "|Attack", ",", "Attack Melee", "Attack|Fire", "None", "0" };
            foreach (string s in invalid)
            {
                Assert.IsFalse(TagMask.TryParse(s, out TagMask m), s);
                Assert.AreEqual(TagMask.None, m, s);
                Assert.Throws<FormatException>(() => TagMask.Parse(s), s);
            }
        }

        [Test]
        public void TryParseTag_Invalid_Rejected()
        {
            foreach (string s in new[] { null, "", "3", "attack", "Fire", " Attack", "Attack|Melee" })
                Assert.IsFalse(TagMask.TryParseTag(s, out _), s ?? "<null>");
        }

        [Test]
        public void RoundTrip_AllCombinationsOfSmallSubsets()
        {
            // Every pair plus a few larger sets.
            var tags = (Tag[])Enum.GetValues(typeof(Tag));
            for (int i = 0; i < tags.Length; i++)
                for (int j = i + 1; j < tags.Length; j++)
                {
                    var m = new TagMask(tags[i], tags[j]);
                    Assert.AreEqual(m, TagMask.Parse(m.ToString()));
                    Assert.AreEqual(2, m.Count);
                }
            foreach (var m in new[] { TagMask.None, TagMask.All, TagMask.FromBits(0xAAAAAAUL), TagMask.FromBits(0x555555UL) })
                Assert.AreEqual(m, TagMask.Parse(m.ToString().Replace("None", "")));
        }
    }
}
