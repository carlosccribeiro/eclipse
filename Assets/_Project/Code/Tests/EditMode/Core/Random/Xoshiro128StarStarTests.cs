using System;
using NUnit.Framework;

namespace Eclipse.Core.Random.Tests
{
    public class Xoshiro128StarStarTests
    {
        [Test]
        public void Reference_vector_state_1_2_3_4()
        {
            var g = new Xoshiro128StarStar(1, 2, 3, 4);
            Assert.AreEqual(11520u, g.NextUInt());
            Assert.AreEqual(0u, g.NextUInt());
            Assert.AreEqual(5927040u, g.NextUInt());
        }

        [Test]
        public void Zero_state_throws()
        {
            Assert.Throws<ArgumentException>(() => new Xoshiro128StarStar(0, 0, 0, 0));
        }

        [Test]
        public void State_roundtrip_continues_sequence()
        {
            var a = Xoshiro128StarStar.FromSeed(5);
            for (int i = 0; i < 10; i++) a.NextUInt();
            var st = a.GetState();
            var b = new Xoshiro128StarStar(st.s0, st.s1, st.s2, st.s3);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void Same_seed_same_first_1000()
        {
            var a = Xoshiro128StarStar.FromSeed(99);
            var b = Xoshiro128StarStar.FromSeed(99);
            for (int i = 0; i < 1000; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void Different_seeds_differ()
        {
            var a = Xoshiro128StarStar.FromSeed(1);
            var b = Xoshiro128StarStar.FromSeed(2);
            int same = 0;
            for (int i = 0; i < 1000; i++) if (a.NextUInt() == b.NextUInt()) same++;
            Assert.Less(same, 5);
        }

        [Test]
        public void Float01_in_range()
        {
            var g = Xoshiro128StarStar.FromSeed(7);
            for (int i = 0; i < 100000; i++)
            {
                float f = g.NextFloat01();
                Assert.IsTrue(f >= 0f && f < 1f);
            }
            Assert.Less(Xoshiro128StarStar.ToFloat01(uint.MaxValue), 1f);
            Assert.AreEqual(0f, Xoshiro128StarStar.ToFloat01(0));
        }

        [Test]
        public void NextInt_is_non_negative()
        {
            var g = Xoshiro128StarStar.FromSeed(3);
            for (int i = 0; i < 10000; i++) Assert.GreaterOrEqual(g.NextInt(), 0);
        }

        [Test]
        public void Range_respects_bounds_and_throws()
        {
            var g = Xoshiro128StarStar.FromSeed(11);
            for (int i = 0; i < 10000; i++)
            {
                int v = g.Range(-5, 5);
                Assert.IsTrue(v >= -5 && v < 5);
            }
            Assert.Throws<ArgumentException>(() => g.Range(3, 3));
            Assert.Throws<ArgumentException>(() => g.Range(4, 3));
        }

        [Test]
        public void Range_single_element()
        {
            var g = Xoshiro128StarStar.FromSeed(11);
            for (int i = 0; i < 100; i++) Assert.AreEqual(7, g.Range(7, 8));
        }

        [Test]
        public void Range_full_int_span()
        {
            var g = Xoshiro128StarStar.FromSeed(13);
            bool neg = false, pos = false;
            for (int i = 0; i < 1000; i++)
            {
                int v = g.Range(int.MinValue, int.MaxValue);
                Assert.Less(v, int.MaxValue);
                if (v < 0) neg = true; else pos = true;
            }
            Assert.IsTrue(neg && pos);
        }

        [Test]
        public void Range_distribution_roughly_uniform()
        {
            var g = Xoshiro128StarStar.FromSeed(21);
            var counts = new int[6];
            for (int i = 0; i < 60000; i++) counts[g.Range(0, 6)]++;
            for (int b = 0; b < 6; b++) Assert.That(counts[b], Is.InRange(9000, 11000));
        }

        [Test]
        public void Chance_edges_and_consumption()
        {
            var g = Xoshiro128StarStar.FromSeed(5);
            var r = Xoshiro128StarStar.FromSeed(5);
            for (int i = 0; i < 1000; i++)
            {
                Assert.IsFalse(g.Chance(0f));
                Assert.IsTrue(g.Chance(1f));
                Assert.IsFalse(g.Chance(-1f));
                Assert.IsTrue(g.Chance(2f));
                Assert.IsFalse(g.Chance(float.NaN));
            }
            for (int i = 0; i < 5000; i++) r.NextUInt();
            Assert.AreEqual(r.NextUInt(), g.NextUInt()); // each call consumed exactly one output
        }

        [Test]
        public void Chance_half_is_about_half()
        {
            var g = Xoshiro128StarStar.FromSeed(8);
            int hits = 0;
            for (int i = 0; i < 20000; i++) if (g.Chance(0.5f)) hits++;
            Assert.That(hits, Is.InRange(9500, 10500));
        }
    }
}
