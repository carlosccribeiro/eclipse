using System;
using System.Linq;
using Eclipse.Core.Tags;
using NUnit.Framework;

namespace Eclipse.Core.Tags.Tests
{
    public class HitFlagsTests
    {
        private static readonly string[] Expected =
        {
            "None", "Unavoidable", "Unblockable", "Annihilation", "Knockdown", "Execute", "IsEcho", "IsResonance", "IsTriggered",
        };

        [Test]
        public void Enum_MatchesExpectedNamesInOrder()
        {
            Assert.AreEqual(Expected, Enum.GetNames(typeof(HitFlags)));
            Assert.AreEqual(0, (int)HitFlags.None);
        }

        [Test]
        public void Bits_AreDistinctPowersOfTwo_0To7()
        {
            var values = ((HitFlags[])Enum.GetValues(typeof(HitFlags))).Where(f => f != HitFlags.None).ToArray();
            Assert.AreEqual(8, values.Length);
            ushort all = 0;
            for (int i = 0; i < values.Length; i++)
            {
                ushort v = (ushort)values[i];
                Assert.AreEqual(1 << i, v);
                Assert.AreEqual(0, v & (v - 1), "not a power of two");
                Assert.AreEqual(0, all & v, "overlapping bits");
                all |= v;
            }
            Assert.AreEqual(0xFF, all);
        }

        [Test]
        public void NoHitFlagNameIsAlsoATag()
        {
            var common = Enum.GetNames(typeof(HitFlags)).Intersect(Enum.GetNames(typeof(Tag))).ToArray();
            CollectionAssert.IsEmpty(common);
        }

        [Test]
        public void Has_BitTest()
        {
            var f = HitFlags.Unblockable | HitFlags.IsEcho;
            Assert.IsTrue(f.Has(HitFlags.Unblockable));
            Assert.IsTrue(f.Has(HitFlags.IsEcho));
            Assert.IsTrue(f.Has(HitFlags.Unblockable | HitFlags.IsEcho));
            Assert.IsFalse(f.Has(HitFlags.Execute));
            Assert.IsFalse(f.Has(HitFlags.Execute | HitFlags.IsEcho));
            Assert.IsFalse(HitFlags.None.Has(HitFlags.Knockdown));
        }
    }
}
