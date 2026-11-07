using System.Text;
using Eclipse.Core.Ids;
using NUnit.Framework;

namespace Eclipse.Core.Random.Tests
{
    public class RngStreamsTests
    {
        private static readonly RngStream[] All =
            { RngStream.Map, RngStream.Encounter, RngStream.Loot, RngStream.Combat, RngStream.Cosmetic };

        [Test]
        public void Enum_order_is_frozen()
        {
            Assert.AreEqual(0, (int)RngStream.Map);
            Assert.AreEqual(1, (int)RngStream.Encounter);
            Assert.AreEqual(2, (int)RngStream.Loot);
            Assert.AreEqual(3, (int)RngStream.Combat);
            Assert.AreEqual(4, (int)RngStream.Cosmetic);
        }

        [Test]
        public void Streams_are_distinct()
        {
            var s = new RngStreams(42);
            Assert.AreEqual(42UL, s.RunSeed);
            var first = new uint[5];
            for (int i = 0; i < 5; i++) first[i] = s.Get(All[i]).NextUInt();
            for (int i = 0; i < 5; i++)
                for (int j = i + 1; j < 5; j++) Assert.AreNotEqual(first[i], first[j]);
        }

        [Test]
        public void Consuming_combat_does_not_affect_loot()
        {
            var a = new RngStreams(1234);
            var b = new RngStreams(1234);
            for (int i = 0; i < 1000; i++) a.Get(RngStream.Combat).NextUInt();
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(b.Get(RngStream.Loot).NextUInt(), a.Get(RngStream.Loot).NextUInt());
        }

        [Test]
        public void Fork_is_deterministic_distinct_and_non_consuming()
        {
            var a = new RngStreams(77);
            var b = new RngStreams(77);
            var k1 = new StableId("room.crypt.01");
            var k2 = new StableId("room.crypt.02");
            var f1 = a.Fork(RngStream.Loot, k1);
            var f1b = b.Fork(RngStream.Loot, k1);
            var f2 = a.Fork(RngStream.Loot, k2);
            Assert.AreEqual(f1.NextUInt(), f1b.NextUInt());
            Assert.AreNotEqual(f1.NextUInt(), f2.NextUInt());
            var fl = a.Fork(RngStream.Loot, k1);
            var fm = a.Fork(RngStream.Map, k1);
            Assert.AreNotEqual(fl.NextUInt(), fm.NextUInt());
            for (int i = 0; i < 50; i++)
                Assert.AreEqual(b.Get(RngStream.Loot).NextUInt(), a.Get(RngStream.Loot).NextUInt());
        }

        [Test]
        public void Fnv1a64_empty_is_offset_basis()
        {
            Assert.AreEqual(0xCBF29CE484222325UL, RngStreams.Fnv1a64(""));
        }

        [Test]
        public void Golden_values_runSeed_42()
        {
            var s = new RngStreams(42);
            var sb = new StringBuilder();
            foreach (var st in All)
            {
                sb.Append(st).Append(':');
                for (int i = 0; i < 3; i++) sb.Append(s.Get(st).NextUInt()).Append(i < 2 ? "," : ";");
            }
            Assert.AreEqual(
                "Map:2094977898,3540636818,1020874294;Encounter:1680906763,3387814439,443965197;"
                + "Loot:3689283648,3178807754,227784487;Combat:1559807665,509639421,2601844753;"
                + "Cosmetic:3722333714,2201559271,640565689;", sb.ToString());
        }

        [Test]
        public void Fork_rejects_empty_key()
        {
            var s = new RngStreams(7);
            Assert.Throws<System.ArgumentException>(() => s.Fork(RngStream.Loot, default(StableId)));
            Assert.Throws<System.ArgumentException>(() => s.Fork(RngStream.Loot, default(StableId), 3UL));
        }

        [Test]
        public void Fork_with_salt_distinguishes_same_key_and_salt_zero_matches_unsalted()
        {
            var s = new RngStreams(7);
            var key = StableId.Parse("item.ashfang");
            var plain = s.Fork(RngStream.Loot, key);
            var zero = s.Fork(RngStream.Loot, key, 0UL);
            var one = s.Fork(RngStream.Loot, key, 1UL);
            var two = s.Fork(RngStream.Loot, key, 2UL);
            var oneAgain = new RngStreams(7).Fork(RngStream.Loot, key, 1UL);
            uint p = plain.NextUInt(), z = zero.NextUInt(), a = one.NextUInt(), b = two.NextUInt();
            Assert.AreEqual(p, z);
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(p, a);
            Assert.AreEqual(a, oneAgain.NextUInt());
        }
    }
}
