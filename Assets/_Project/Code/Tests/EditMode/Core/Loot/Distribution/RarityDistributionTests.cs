using System;
using Eclipse.Core.Items;
using Eclipse.Core.Loot.Generation;
using Eclipse.Core.Loot.Generation.Tests;
using Eclipse.Core.Random;
using NUnit.Framework;

namespace Eclipse.Core.Loot.Distribution.Tests
{
    /// <summary>
    /// Statistical checks of 05 sections 5.2-5.4 (trash C/R/E/L per difficulty) against the generator, with fixed seeds.
    /// The catalog is the fixture with the 05 numbers (doc values live only in test fixtures). Heavy sampling: not compiled for Unity.
    /// </summary>
    [TestFixture]
    public class RarityDistributionTests
    {
        /// <summary>200,000 samples keep the standard error near 0.11 p.p. at 50%, well inside the +-0.5 p.p. tolerance.</summary>
        private const int Samples = 200000;
        private const double TolerancePp = 0.5;

        // Expected trash distribution C/R/E/L in %, derived from 05 sections 5.2-5.4 (Normal, Eclipse, A100 are quoted in 5.4).
        private static readonly object[] Cases =
        {
            new object[] { Difficulty.Normal, 0, 72.9, 25.0, 2.0, 0.1 },
            new object[] { Difficulty.Veteran, 0, 61.6, 32.0, 6.0, 0.4 },
            new object[] { Difficulty.Nightmare, 0, 49.1, 38.0, 12.0, 0.9 },
            new object[] { Difficulty.Eclipse, 0, 36.2, 42.0, 20.0, 1.8 },
            new object[] { Difficulty.Ascension, 1, 30.5, 42.0, 25.0, 2.5 },
            new object[] { Difficulty.Ascension, 100, 24.0, 40.0, 32.0, 4.0 },
        };

        [TestCaseSource(nameof(Cases))]
        public void Trash_distribution_matches_doc(Difficulty diff, int asc, double c, double r, double e, double l)
        {
            var f = GenFixture.Create();
            var roller = new RarityRoller(f.Loot);
            var ctx = GenFixture.Ctx(LootSourceKind.Trash, diff, ascension: asc);
            var src = f.Loot.GetSource(LootSourceKind.Trash);
            var rng = GenFixture.Rng(1000UL + (ulong)diff * 17UL + (ulong)asc);
            var n = new int[4];
            for (int i = 0; i < Samples; i++) n[(int)roller.Roll(ctx, src, ref rng)]++;

            double pc = 100.0 * n[(int)Rarity.Common] / Samples, pr = 100.0 * n[(int)Rarity.Rare] / Samples;
            double pe = 100.0 * n[(int)Rarity.Epic] / Samples, pl = 100.0 * n[(int)Rarity.Legendary] / Samples;
            TestContext.WriteLine($"{diff} A{asc}: C={pc:F2} R={pr:F2} E={pe:F2} L={pl:F2}");
            Assert.AreEqual(c, pc, TolerancePp, "Common"); Assert.AreEqual(r, pr, TolerancePp, "Rare");
            Assert.AreEqual(e, pe, TolerancePp, "Epic"); Assert.AreEqual(l, pl, TolerancePp, "Legendary");
        }

#if !UNITY_5_3_OR_NEWER
        [Test]
        public void Batch_rarity_sampling_does_not_allocate()
        {
            var f = GenFixture.Create();
            var roller = new RarityRoller(f.Loot);
            var ctx = GenFixture.Ctx(LootSourceKind.Trash, Difficulty.Eclipse);
            var src = f.Loot.GetSource(LootSourceKind.Trash);
            var rng = GenFixture.Rng(7);
            for (int i = 0; i < 1000; i++) roller.Roll(ctx, src, ref rng); // warm-up (JIT)
            long before = GC.GetAllocatedBytesForCurrentThread();
            int legendaries = 0;
            for (int i = 0; i < Samples; i++) if (roller.Roll(ctx, src, ref rng) == Rarity.Legendary) legendaries++;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Greater(legendaries, 0);
            Assert.AreEqual(0L, allocated, "RarityRoller.Roll must not allocate");
        }
#endif
    }
}
