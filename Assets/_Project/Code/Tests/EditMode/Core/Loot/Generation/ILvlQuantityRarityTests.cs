using Eclipse.Core.Items;
using Eclipse.Core.Random;
using NUnit.Framework;

namespace Eclipse.Core.Loot.Generation.Tests
{
    [TestFixture]
    public class ILvlTests
    {
        private GenFixture _f;
        [SetUp] public void SetUp() => _f = GenFixture.Create();

        private int ILvl(LootContext c) => ILvlCalculator.Compute(_f.Loot, c, _f.Loot.GetSource(c.Source));

        [Test]
        public void RoomFormula_NormalAshen()
        {
            var c = GenFixture.Ctx(); c.RoomIndex = 3; c.TotalRooms = 6; // 1 + floor(5*3/6) = 3
            Assert.AreEqual(3, ILvl(c));
        }

        [Test]
        public void SourceBonusAndCapOverRangeTop()
        {
            var c = GenFixture.Ctx(LootSourceKind.Elite); c.RoomIndex = 3; c.TotalRooms = 6;
            Assert.AreEqual(4, ILvl(c));
            c.RoomIndex = 6; // 6 + 1 = 7, cap = max + 2 = 8
            Assert.AreEqual(7, ILvl(c));
            c.Source = LootSourceKind.Challenge; // +2 -> 8 (cap)
            Assert.AreEqual(8, ILvl(c));
        }

        [Test]
        public void BossAndRiftCache_UseRangeTopPlus2()
        {
            Assert.AreEqual(8, ILvl(GenFixture.Ctx(LootSourceKind.Boss)));
            Assert.AreEqual(86, ILvl(GenFixture.Ctx(LootSourceKind.RiftCache, Difficulty.Eclipse)));
        }

        [TestCase(1, 85)] [TestCase(45, 92)] [TestCase(90, 100)] [TestCase(100, 100)]
        public void Ascension_BaseFormula(int level, int expected)
            => Assert.AreEqual(expected, ILvl(GenFixture.Ctx(LootSourceKind.Trash, Difficulty.Ascension, null, "kael", null, level)));

        [Test]
        public void Ascension_AddsSourceBonusAndCaps()
        {
            Assert.AreEqual(86, ILvl(GenFixture.Ctx(LootSourceKind.Elite, Difficulty.Ascension, null, "kael", null, 1)));
            Assert.AreEqual(100, ILvl(GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Ascension, null, "kael", null, 100)));
        }

        [Test]
        public void ForcedILvl_SkipsComputation()
            => Assert.AreEqual(42, ILvl(GenFixture.Ctx(ilvl: 42)));

        [Test]
        public void UnknownRegion_FallsBackToFirstRangeOfDifficulty()
            => Assert.AreEqual(1, ILvl(GenFixture.Ctx(region: "nowhere")));
    }

    [TestFixture]
    public class QuantityTests
    {
        private GenFixture _f;
        [SetUp] public void SetUp() => _f = GenFixture.Create();

        [Test]
        public void Multiplier_IsAdditiveAndCapped()
        {
            Assert.AreEqual(1f, LootMath.QuantityMultiplier(_f.Loot, GenFixture.Ctx()), 1e-5f);
            Assert.AreEqual(1.3f, LootMath.QuantityMultiplier(_f.Loot, GenFixture.Ctx(diff: Difficulty.Eclipse)), 1e-5f);
            // A100: +0.3 + 0.004 x 100 = +0.7
            Assert.AreEqual(1.7f, LootMath.QuantityMultiplier(_f.Loot, GenFixture.Ctx(diff: Difficulty.Ascension, ascension: 100)), 1e-5f);
            var c = GenFixture.Ctx(diff: Difficulty.Ascension, ascension: 100); c.QuantityBonus = 0.8f;
            Assert.AreEqual(2f, LootMath.QuantityMultiplier(_f.Loot, c), 1e-5f);
        }

        private double Mean(LootContext c, int n)
        {
            var g = _f.NewGenerator(); var rng = GenFixture.Rng(11);
            long sum = 0;
            for (int i = 0; i < n; i++) sum += g.RollItemCount(c, ref rng);
            return sum / (double)n;
        }

        [Test] public void Trash_DropChanceTimesQuantity() => Assert.AreEqual(0.039, Mean(GenFixture.Ctx(diff: Difficulty.Eclipse), 40000), 0.004);
        [Test] public void Elite_GuaranteedPlusExtraChance() => Assert.AreEqual(1.3 + 0.3 * 1.3, Mean(GenFixture.Ctx(LootSourceKind.Elite, Difficulty.Eclipse), 20000), 0.05);
        [Test] public void Boss_GuaranteedStochasticallyRounded() => Assert.AreEqual(4 * 1.3, Mean(GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Eclipse), 20000), 0.05);

        [Test]
        public void FixedSources_IgnoreQuantity()
        {
            var g = _f.NewGenerator(); var rng = GenFixture.Rng(1);
            var c = GenFixture.Ctx(LootSourceKind.Reliquary, Difficulty.Eclipse);
            Assert.AreEqual(3, g.RollItemCount(c, ref rng)); // choose 1 of 3: all 3 generated
            c.Source = LootSourceKind.Challenge;
            Assert.AreEqual(2, g.RollItemCount(c, ref rng));
        }

        [Test]
        public void RiftCache_ExtraItemWhenAllChallengesCleared()
        {
            var g = _f.NewGenerator(); var rng = GenFixture.Rng(1);
            var c = GenFixture.Ctx(LootSourceKind.RiftCache);
            Assert.AreEqual(2, g.RollItemCount(c, ref rng));
            c.AllChallengesCleared = true;
            Assert.AreEqual(3, g.RollItemCount(c, ref rng));
        }
    }

    [TestFixture]
    public class RarityTests
    {
        private GenFixture _f;
        private RarityRoller _r;
        [SetUp] public void SetUp() { _f = GenFixture.Create(); _r = new RarityRoller(_f.Loot); }

        private double[] Dist(LootContext c, int n, ulong seed = 5)
        {
            var rng = GenFixture.Rng(seed);
            var src = _f.Loot.GetSource(c.Source);
            var d = new double[4];
            for (int i = 0; i < n; i++) d[(int)_r.Roll(c, src, ref rng)]++;
            for (int i = 0; i < 4; i++) d[i] /= n;
            return d;
        }

        [Test]
        public void TrashNormal_AbsoluteChances_Matches05_5_4()
        {
            var d = Dist(GenFixture.Ctx(), 60000); // C, R, E, L
            Assert.AreEqual(0.729, d[0], 0.01); Assert.AreEqual(0.25, d[1], 0.01);
            Assert.AreEqual(0.02, d[2], 0.005); Assert.AreEqual(0.001, d[3], 0.001);
        }

        [Test]
        public void TrashEclipse_Matches05_5_4()
        {
            var d = Dist(GenFixture.Ctx(diff: Difficulty.Eclipse), 60000);
            Assert.AreEqual(0.362, d[0], 0.01); Assert.AreEqual(0.42, d[1], 0.01);
            Assert.AreEqual(0.20, d[2], 0.01); Assert.AreEqual(0.018, d[3], 0.004);
        }

        [Test]
        public void Ascension_InterpolatesLinearly()
        {
            var lA1 = _r.LegendaryChance(GenFixture.Ctx(diff: Difficulty.Ascension, ascension: 1));
            var lA100 = _r.LegendaryChance(GenFixture.Ctx(diff: Difficulty.Ascension, ascension: 100));
            var lA50 = _r.LegendaryChance(GenFixture.Ctx(diff: Difficulty.Ascension, ascension: 50));
            Assert.AreEqual(0.025f, lA1, 1e-6f); Assert.AreEqual(0.04f, lA100, 1e-6f);
            Assert.AreEqual(0.025f + 0.015f * 49f / 99f, lA50, 1e-6f);
        }

        [Test]
        public void SourceFloor_RaisesRarity()
        {
            foreach (var r in new[] { Rarity.Epic })
            {
                var d = Dist(GenFixture.Ctx(LootSourceKind.Reliquary), 2000);
                Assert.AreEqual(0.0, d[0] + d[1]); // never Common/Rare
                Assert.Greater(d[2], 0.9);
            }
            var elite = Dist(GenFixture.Ctx(LootSourceKind.Elite), 2000);
            Assert.AreEqual(0.0, elite[0]); // Rare floor
        }

        [Test]
        public void Guarantees_EpicPlusAndLegendary()
        {
            var c = GenFixture.Ctx(); c.Guarantees.ForceEpicPlus = true;
            var d = Dist(c, 2000);
            Assert.AreEqual(0.0, d[0] + d[1]);
            c.Guarantees.ForceLegendary = true;
            Assert.AreEqual(1.0, Dist(c, 500)[3]);
        }

        [Test]
        public void SoftPity_MultipliesAndCapsAtTwiceBase()
        {
            var c = GenFixture.Ctx(LootSourceKind.Elite); // base L = 0.5%, Normal soft start = 80
            c.Pity = new FakePity { Drought = 80 };
            Assert.AreEqual(0.005f, _r.LegendaryChance(c), 1e-6f);
            c.Pity = new FakePity { Drought = 90 }; // x(1 + 0.05 x 10)
            Assert.AreEqual(0.0075f, _r.LegendaryChance(c), 1e-6f);
            c.Pity = new FakePity { Drought = 150 }; // x4.5 would exceed the x2 cap
            Assert.AreEqual(0.01f, _r.LegendaryChance(c), 1e-6f);
        }

        [Test]
        public void HardPity_ForcesLegendary_ExceptTrash()
        {
            var elite = GenFixture.Ctx(LootSourceKind.Elite); elite.Pity = new FakePity { Drought = 160 };
            Assert.AreEqual(1.0, Dist(elite, 300)[3]);
            var trash = GenFixture.Ctx(); trash.Pity = new FakePity { Drought = 160 };
            Assert.Less(Dist(trash, 300)[3], 0.5); // trash does not consume hard pity
        }

        [Test]
        public void SingleDrawPerRoll()
        {
            var rng = GenFixture.Rng(3); var rng2 = GenFixture.Rng(3);
            var c = GenFixture.Ctx(LootSourceKind.Reliquary); c.Guarantees.ForceLegendary = true;
            _r.Roll(c, _f.Loot.GetSource(c.Source), ref rng);
            rng2.NextUInt();
            Assert.AreEqual(rng2.GetState(), rng.GetState());
        }
    }
}
