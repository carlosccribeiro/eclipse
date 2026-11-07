using Eclipse.Core.Items;
using Eclipse.Core.Loot.Generation;
using Eclipse.Core.Loot.Generation.Tests;
using Eclipse.Core.Loot.Pity;
using NUnit.Framework;

namespace Eclipse.Core.Loot.Distribution.Tests
{
    /// <summary>Long pity simulations (05 section 8.1: hard pity is never exceeded) and the Quantity cap (R45, 05 section 3.2).</summary>
    [TestFixture]
    public class PityAndQuantityTests
    {
#if !UNITY_5_3_OR_NEWER // heavy simulation: dotnet test only
        private const int Items = 100000;

        private static readonly object[] Pity =
        {
            new object[] { Difficulty.Normal, 0, 160 },
            new object[] { Difficulty.Veteran, 0, 100 },
            new object[] { Difficulty.Nightmare, 0, 60 },
            new object[] { Difficulty.Eclipse, 0, 35 },
            new object[] { Difficulty.Ascension, 1, 30 },
            new object[] { Difficulty.Ascension, 100, 30 },
        };

        [TestCaseSource(nameof(Pity))]
        public void Hard_pity_is_never_exceeded(Difficulty diff, int asc, int hard)
        {
            var f = GenFixture.Create();
            Assert.AreEqual(hard, f.Loot.GetPity(diff).Hard);
            var g = f.NewGenerator();
            var pity = new HeroPityState { HeroId = "kael" };
            // Every consuming source (05 section 8.1: Elite, Boss, Treasure, Challenge, Event, Rift Cache).
            var sources = new[] { LootSourceKind.Elite, LootSourceKind.Boss, LootSourceKind.TreasureDelivered,
                LootSourceKind.Challenge, LootSourceKind.Event, LootSourceKind.RiftCache };
            var rng = GenFixture.Rng(4242UL + (ulong)diff * 13UL + (ulong)asc);
            int peak = 0, forced = 0, legendaries = 0;
            for (int i = 0; i < Items; i++)
            {
                var ctx = GenFixture.Ctx(sources[i % sources.Length], diff, "ashen_wastes", "kael", 60, asc);
                ctx.Pity = pity;
                int before = pity.Drought;
                if (before > peak) peak = before;
                Assert.LessOrEqual(before, hard, "drought exceeded hard pity at item " + i);
                var it = g.Generate(ctx, ref rng);
                if (before >= hard) { forced++; Assert.AreEqual(Rarity.Legendary, it.Rarity, "hard pity must force a Legendary"); }
                if (it.Rarity == Rarity.Legendary) legendaries++;
            }
            TestContext.WriteLine(diff + " A" + asc + ": hard=" + hard + " peakDrought=" + peak + " forcedByHard=" + forced + " legendaries=" + legendaries);
        }

#endif

        [Test]
        public void Trash_does_not_consume_hard_pity_but_next_consuming_source_does()
        {
            var f = GenFixture.Create();
            var g = f.NewGenerator();
            var pity = new HeroPityState { HeroId = "kael", Drought = 500 };
            var rng = GenFixture.Rng(1);
            var trash = GenFixture.Ctx(LootSourceKind.Trash, Difficulty.Eclipse, ilvl: 60);
            trash.Pity = pity;
            // Soft pity multiplies L but the x2 cap holds: trash is never forced (base 1.8% -> at most 3.6%).
            int legendaries = 0;
            for (int i = 0; i < 2000; i++)
            {
                pity.Drought = 500;
                if (g.Generate(trash, ref rng).Rarity == Rarity.Legendary) legendaries++;
            }
            Assert.Less(legendaries, 2000 * 0.10, "trash is not forced by hard pity");

            pity.Drought = 500;
            var elite = GenFixture.Ctx(LootSourceKind.Elite, Difficulty.Eclipse, ilvl: 60);
            elite.Pity = pity;
            Assert.AreEqual(Rarity.Legendary, g.Generate(elite, ref rng).Rarity);
            Assert.AreEqual(0, pity.Drought);
        }

        [Test]
        public void Quantity_multiplier_never_exceeds_cap()
        {
            var f = GenFixture.Create();
            foreach (Difficulty d in System.Enum.GetValues(typeof(Difficulty)))
            foreach (float ext in new[] { 0f, 0.3f, 1f, 5f })
            foreach (int asc in new[] { 0, 1, 50, 100 })
            {
                if ((d == Difficulty.Ascension) != (asc > 0)) continue;
                var ctx = GenFixture.Ctx(LootSourceKind.Trash, d, ascension: asc);
                ctx.QuantityBonus = ext;
                Assert.LessOrEqual(LootMath.QuantityMultiplier(f.Loot, ctx), 2.0f);
            }
            var a100 = GenFixture.Ctx(LootSourceKind.Trash, Difficulty.Ascension, ascension: 100);
            Assert.AreEqual(1.7f, LootMath.QuantityMultiplier(f.Loot, a100), 1e-5f); // 05 section 3.2: A100 = +0.7
            a100.QuantityBonus = 0.3f;
            Assert.AreEqual(2.0f, LootMath.QuantityMultiplier(f.Loot, a100), 1e-5f);
            a100.QuantityBonus = 9f;
            Assert.AreEqual(2.0f, LootMath.QuantityMultiplier(f.Loot, a100), 1e-5f);
        }

        [Test]
        public void Item_counts_respect_the_quantity_cap()
        {
            var f = GenFixture.Create();
            var g = f.NewGenerator();
            var rng = GenFixture.Rng(99);
            const int n = 50000;
            // Boss: 4 guaranteed items x quantity; capped at x2.0 the count is at most 8.
            var boss = GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Ascension, ascension: 100);
            boss.QuantityBonus = 10f;
            long total = 0; int max = 0;
            for (int i = 0; i < n; i++) { int c = g.RollItemCount(boss, ref rng); total += c; if (c > max) max = c; }
            Assert.LessOrEqual(max, 8);
            Assert.AreEqual(8.0, (double)total / n, 0.02);

            // Trash: 3% x 2.0 = 6% at the cap.
            var trash = GenFixture.Ctx(LootSourceKind.Trash, Difficulty.Ascension, ascension: 100);
            trash.QuantityBonus = 10f;
            int drops = 0;
            for (int i = 0; i < 200000; i++) drops += g.RollItemCount(trash, ref rng);
            Assert.AreEqual(0.06, drops / 200000.0, 0.003);

            // Fixed chests ignore Quantity entirely.
            var cache = GenFixture.Ctx(LootSourceKind.RiftCache, Difficulty.Ascension, ascension: 100);
            cache.QuantityBonus = 10f;
            Assert.AreEqual(2, g.RollItemCount(cache, ref rng));
        }
    }
}
