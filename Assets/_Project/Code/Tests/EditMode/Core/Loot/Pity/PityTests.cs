using System;
using Eclipse.Core.Items;
using Eclipse.Core.Loot.Generation.Tests;
using Eclipse.Core.Random;
using NUnit.Framework;

namespace Eclipse.Core.Loot.Pity.Tests
{
    [TestFixture]
    public class PityTests
    {
        private GenFixture _f;

        [SetUp] public void SetUp()
        {
            _f = GenFixture.Create();
            _f.Loot.SmartLootCurrentHeroLegendary = 1f;
            _f.Loot.SmartLootCurrentHeroGeneric = 1f;
            // Boss Unique parameters (test numbers mirroring 05 section 8.5 only where asserted).
            foreach (var kv in _f.Loot.PityMap)
            {
                var p = kv.Value;
                p.BossUniqueChance = kv.Key == Difficulty.Nightmare ? 0.08f : kv.Key == Difficulty.Eclipse ? 0.12f : kv.Key == Difficulty.Ascension ? 0.15f : 0f;
                p.BossUniqueChancePerAscension = kv.Key == Difficulty.Ascension ? 0.1f / 99f : 0f;
                p.BossUniqueGuaranteeKills = 10;
                p.BossEchoMultiplier = 1.5f;
                p.FirstKillSetChance = 0.5f;
            }
        }

        private PityConfigData Cfg(Difficulty d) => _f.Loot.GetPity(d);

        // ---------------- soft / hard
        [Test] public void Soft_MultiplierAndCap()
        {
            var c = Cfg(Difficulty.Veteran); // S=50, step 0.05
            Assert.AreEqual(1f, PityPoliciesSoft(c, 50), 1e-6);
            Assert.AreEqual(1.5f, PityPoliciesSoft(c, 60), 1e-5);
            Assert.AreEqual(0.02f * 2f, PityRules.LegendaryChance(c, 0.02f, 90), 1e-6); // 3.0x capped at 2x
            Assert.AreEqual(0.02f * 1.5f, PityRules.LegendaryChance(c, 0.02f, 60), 1e-6);
        }
        private static double PityPoliciesSoft(PityConfigData c, int d) => PityRules.SoftMultiplier(c, d);

        [TestCase(Difficulty.Normal, 160)] [TestCase(Difficulty.Veteran, 100)] [TestCase(Difficulty.Nightmare, 60)]
        [TestCase(Difficulty.Eclipse, 35)] [TestCase(Difficulty.Ascension, 30)]
        public void Hard_PerDifficulty_TriggersAtH(Difficulty d, int h)
        {
            var elite = _f.Loot.GetSource(LootSourceKind.Elite);
            Assert.IsFalse(PityRules.IsHardPity(Cfg(d), elite, h - 1));
            Assert.IsTrue(PityRules.IsHardPity(Cfg(d), elite, h));
        }

        [Test] public void Hard_TrashDoesNotConsume()
        {
            var trash = _f.Loot.GetSource(LootSourceKind.Trash);
            Assert.IsFalse(PityRules.IsHardPity(Cfg(Difficulty.Eclipse), trash, 1000));
            var st = new HeroPityState { Drought = 1000 };
            var ctx = GenFixture.Ctx(LootSourceKind.Trash, Difficulty.Eclipse);
            var rng = GenFixture.Rng(1);
            Assert.AreEqual(PityOutcome.None, PityApplier.Apply(ctx, _f.Loot, st, false, ref rng));
            Assert.IsFalse(ctx.Guarantees.ForceLegendary);
            Assert.AreSame(st, ctx.Pity);
        }

        [Test] public void Hard_ApplyForcesLegendaryOnElite()
        {
            var st = new HeroPityState { Drought = 35 };
            var ctx = GenFixture.Ctx(LootSourceKind.Elite, Difficulty.Eclipse);
            var rng = GenFixture.Rng(1);
            Assert.AreEqual(PityOutcome.HardPity, PityApplier.Apply(ctx, _f.Loot, st, false, ref rng));
            Assert.IsTrue(ctx.Guarantees.ForceLegendary);
        }

        // ---------------- state counters
        [Test] public void Reset_OnLegendary_AndCounterKeptAcrossDifficulty()
        {
            var st = new HeroPityState();
            for (int i = 0; i < 40; i++) st.OnItemGenerated(Rarity.Rare, ItemKind.Generic, true);
            Assert.AreEqual(40, st.Drought);
            // Veteran (hard 100) -> Eclipse (hard 35): same counter now exceeds the new hard threshold.
            Assert.IsFalse(PityRules.IsHardPity(Cfg(Difficulty.Veteran), _f.Loot.GetSource(LootSourceKind.Boss), st.Drought));
            Assert.IsTrue(PityRules.IsHardPity(Cfg(Difficulty.Eclipse), _f.Loot.GetSource(LootSourceKind.Boss), st.Drought));
            st.OnItemGenerated(Rarity.Legendary, ItemKind.NamedLegendary, true);
            Assert.AreEqual(0, st.Drought);
        }

        [Test] public void SetPity_AfterFourLegendariesWithoutSet_ResetsOnSetPiece()
        {
            var st = new HeroPityState();
            var rng = GenFixture.Rng(1);
            for (int i = 0; i < 3; i++) st.OnItemGenerated(Rarity.Legendary, i == 0 ? ItemKind.Unique : ItemKind.NamedLegendary, true);
            var ctx = GenFixture.Ctx(LootSourceKind.Elite, Difficulty.Veteran);
            PityApplier.Apply(ctx, _f.Loot, st, false, ref rng);
            Assert.IsFalse(ctx.Guarantees.ForceKind.HasValue);
            st.OnItemGenerated(Rarity.Legendary, ItemKind.NamedLegendary, true);
            ctx = GenFixture.Ctx(LootSourceKind.Elite, Difficulty.Veteran);
            Assert.AreEqual(PityOutcome.SetPity, PityApplier.Apply(ctx, _f.Loot, st, false, ref rng));
            Assert.AreEqual(ItemKind.SetPiece, ctx.Guarantees.ForceKind);
            st.OnItemGenerated(Rarity.Legendary, ItemKind.SetPiece, true);
            Assert.AreEqual(0, st.LegendariesWithoutSet);
        }

        [Test] public void SetPity_DisabledOnNormal()
        {
            var st = new HeroPityState { LegendariesWithoutSet = 10 };
            var ctx = GenFixture.Ctx(LootSourceKind.Elite, Difficulty.Normal);
            var rng = GenFixture.Rng(1);
            PityApplier.Apply(ctx, _f.Loot, st, false, ref rng);
            Assert.IsFalse(ctx.Guarantees.ForceKind.HasValue);
        }

        // ---------------- boss Unique
        private LootContext BossCtx(Difficulty d, int asc = 0)
        {
            var c = GenFixture.Ctx(LootSourceKind.Boss, d, ascension: asc);
            c.Guarantees.BossId = "boss.colossus";
            return c;
        }

        [Test] public void BossUnique_NotOnNormalOrVeteran()
        {
            var st = new HeroPityState();
            st.BossKills.Add(new BossKillCounter { BossId = "boss.colossus", Kills = 50 });
            st.RegisterBossKill("boss.x", Difficulty.Veteran, false);
            foreach (var d in new[] { Difficulty.Normal, Difficulty.Veteran })
            {
                var c = BossCtx(d); var rng = GenFixture.Rng(1);
                PityApplier.Apply(c, _f.Loot, st, false, ref rng);
                Assert.AreNotEqual(ItemKind.Unique, c.Guarantees.ForceKind);
            }
        }

        [Test] public void BossUnique_GuaranteedOnTenth()
        {
            var st = new HeroPityState();
            for (int k = 1; k <= 9; k++) st.RegisterBossKill("boss.colossus", Difficulty.Nightmare, false);
            Assert.AreEqual(9, st.KillsWithoutUnique("boss.colossus"));
            var c = BossCtx(Difficulty.Nightmare); var rng = GenFixture.Rng(1);
            var o = PityApplier.Apply(c, _f.Loot, st, false, ref rng);
            Assert.IsTrue((o & PityOutcome.BossUnique) != 0);
            Assert.AreEqual(ItemKind.Unique, c.Guarantees.ForceKind);
            Assert.AreEqual("boss.colossus", c.Guarantees.BossId);
            st.RegisterBossKill("boss.colossus", Difficulty.Nightmare, true);
            Assert.AreEqual(0, st.KillsWithoutUnique("boss.colossus"));
        }

        [Test] public void BossUnique_NinthKillNotGuaranteed()
        {
            var cfg = Cfg(Difficulty.Nightmare);
            Assert.IsFalse(PityRules.IsBossUniqueGuaranteed(cfg, Difficulty.Nightmare, 8));
            Assert.IsTrue(PityRules.IsBossUniqueGuaranteed(cfg, Difficulty.Nightmare, 9));
        }

        [Test] public void BossUnique_ChanceByDifficultyAscensionAndEcho()
        {
            Assert.AreEqual(0f, PityRules.BossUniqueChance(Cfg(Difficulty.Veteran), Difficulty.Veteran, 0, false));
            Assert.AreEqual(0.08f, PityRules.BossUniqueChance(Cfg(Difficulty.Nightmare), Difficulty.Nightmare, 0, false), 1e-6);
            Assert.AreEqual(0.12f, PityRules.BossUniqueChance(Cfg(Difficulty.Eclipse), Difficulty.Eclipse, 0, false), 1e-6);
            var a = Cfg(Difficulty.Ascension);
            Assert.AreEqual(0.15f, PityRules.BossUniqueChance(a, Difficulty.Ascension, 1, false), 1e-6);
            Assert.AreEqual(0.25f, PityRules.BossUniqueChance(a, Difficulty.Ascension, 100, false), 1e-5);
            Assert.AreEqual(0.12f, PityRules.BossUniqueChance(Cfg(Difficulty.Nightmare), Difficulty.Nightmare, 0, true), 1e-6); // 0.08 x 1.5
        }

        [Test] public void BossUnique_RollFrequencyMatchesChance()
        {
            var rng = GenFixture.Rng(7);
            int hits = 0, n = 20000;
            for (int i = 0; i < n; i++)
            {
                var st = new HeroPityState(); st.RegisterBossKill("boss.colossus", Difficulty.Nightmare, false);
                st.KillsWithoutUnique("boss.colossus");
                var c = BossCtx(Difficulty.Nightmare);
                if ((PityApplier.Apply(c, _f.Loot, st, false, ref rng) & PityOutcome.BossUnique) != 0) hits++;
            }
            Assert.AreEqual(0.08, hits / (double)n, 0.01);
        }

        // ---------------- first kill / first clear
        [Test] public void FirstKill_Normal_GuaranteedStandardLegendary()
        {
            var st = new HeroPityState();
            var c = BossCtx(Difficulty.Normal); var rng = GenFixture.Rng(1);
            var o = PityApplier.Apply(c, _f.Loot, st, false, ref rng);
            Assert.AreEqual(PityOutcome.FirstKill, o);
            Assert.IsTrue(c.Guarantees.ForceLegendary);
            Assert.IsTrue(c.Guarantees.RaiseILvlToLegendaryMin);
            Assert.AreEqual(ItemKind.NamedLegendary, c.Guarantees.ForceKind);
            Assert.Less(c.Guarantees.SetPieceChanceOverride, 0f);
        }

        [Test] public void FirstKill_Veteran_SetChanceOverride_AndPerDifficulty()
        {
            var st = new HeroPityState();
            st.RegisterBossKill("boss.colossus", Difficulty.Normal, false);
            Assert.IsTrue(st.HasFirstKill("boss.colossus", Difficulty.Normal));
            Assert.IsFalse(st.HasFirstKill("boss.colossus", Difficulty.Veteran));
            var c = BossCtx(Difficulty.Veteran); var rng = GenFixture.Rng(1);
            PityApplier.Apply(c, _f.Loot, st, false, ref rng);
            Assert.IsTrue(c.Guarantees.ForceLegendary);
            Assert.AreEqual(0.5f, c.Guarantees.SetPieceChanceOverride);
            st.RegisterBossKill("boss.colossus", Difficulty.Veteran, false);
            c = BossCtx(Difficulty.Veteran);
            Assert.AreEqual(PityOutcome.None, PityApplier.Apply(c, _f.Loot, st, false, ref rng));
            Assert.IsFalse(c.Guarantees.ForceLegendary);
        }

        [Test] public void FirstRegionClear_Normal_RiftCacheEpicGuaranteed()
        {
            var st = new HeroPityState(); var rng = GenFixture.Rng(1);
            var c = GenFixture.Ctx(LootSourceKind.RiftCache, Difficulty.Normal);
            Assert.AreEqual(PityOutcome.FirstRegionClear, PityApplier.Apply(c, _f.Loot, st, false, ref rng));
            Assert.IsTrue(c.Guarantees.ForceEpicPlus);
            st.MarkRegionClearedOnNormal("ashen_wastes");
            c = GenFixture.Ctx(LootSourceKind.RiftCache, Difficulty.Normal);
            Assert.AreEqual(PityOutcome.None, PityApplier.Apply(c, _f.Loot, st, false, ref rng));
            c = GenFixture.Ctx(LootSourceKind.RiftCache, Difficulty.Veteran);
            Assert.AreEqual(PityOutcome.None, PityApplier.Apply(c, _f.Loot, new HeroPityState(), false, ref rng));
        }

        [Test] public void FirstRegionClear_ProducesEpicOrBetterItem()
        {
            var g = _f.NewGenerator();
            for (ulong s = 1; s <= 200; s++)
            {
                var st = new HeroPityState(); var rng = GenFixture.Rng(s);
                var c = GenFixture.Ctx(LootSourceKind.RiftCache, Difficulty.Normal, ilvl: 8);
                PityApplier.Apply(c, _f.Loot, st, false, ref rng);
                Assert.GreaterOrEqual(g.Generate(c, ref rng).Rarity, Rarity.Epic);
            }
        }

        [Test] public void FirstKill_ProducesLegendary_EvenBelowMinILvl()
        {
            var g = _f.NewGenerator();
            for (ulong s = 1; s <= 100; s++)
            {
                var st = new HeroPityState(); var rng = GenFixture.Rng(s);
                var c = BossCtx(Difficulty.Normal); c.ForcedILvl = 5;
                PityApplier.Apply(c, _f.Loot, st, false, ref rng);
                var it = g.Generate(c, ref rng);
                Assert.AreEqual(Rarity.Legendary, it.Rarity);
                Assert.GreaterOrEqual(it.ItemLevel, 10);
                Assert.AreEqual(0, st.Drought);
            }
        }

        // ---------------- integration with the generator
        [Test] public void Integration_HardPityNeverExceeded_In1000Items()
        {
            var g = _f.NewGenerator();
            var st = new HeroPityState { HeroId = "kael" };
            var rng = GenFixture.Rng(2024);
            var sources = new[] { LootSourceKind.Trash, LootSourceKind.Elite, LootSourceKind.Trash, LootSourceKind.Event, LootSourceKind.Challenge };
            int h = Cfg(Difficulty.Eclipse).Hard, legendaries = 0;
            for (int i = 0; i < 1000; i++)
            {
                var src = sources[i % sources.Length];
                var c = GenFixture.Ctx(src, Difficulty.Eclipse, hero: "kael", ilvl: 80);
                int before = st.Drought;
                PityApplier.Apply(c, _f.Loot, st, false, ref rng);
                var it = g.Generate(c, ref rng);
                bool consumes = _f.Loot.GetSource(src).ConsumesHardPity;
                if (consumes && before >= h) Assert.AreEqual(Rarity.Legendary, it.Rarity, "hard pity not honored at item " + i);
                if (it.Rarity == Rarity.Legendary) legendaries++;
                if (consumes) Assert.LessOrEqual(st.Drought, h, "drought above H after a consuming source");
            }
            Assert.Greater(legendaries, 0);
        }

        [Test] public void Integration_TrashDoesNotConsumeHard()
        {
            var g = _f.NewGenerator();
            var st = new HeroPityState { Drought = 500 };
            var rng = GenFixture.Rng(5);
            var c = GenFixture.Ctx(LootSourceKind.Trash, Difficulty.Eclipse, ilvl: 80);
            PityApplier.Apply(c, _f.Loot, st, false, ref rng);
            var it = g.Generate(c, ref rng); // soft pity capped x2 can still roll Legendary by chance; hard must not force it
            Assert.IsFalse(c.Guarantees.ForceLegendary);
            if (it.Rarity != Rarity.Legendary) Assert.AreEqual(501, st.Drought);
        }

        [Test] public void Integration_SetPityProducesSetPieceAfterFour()
        {
            var g = _f.NewGenerator();
            var st = new HeroPityState { LegendariesWithoutSet = 4 };
            var rng = GenFixture.Rng(11);
            var c = GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Veteran, hero: "kael", ilvl: 40);
            c.Guarantees.ForceLegendary = true;
            PityApplier.Apply(c, _f.Loot, st, false, ref rng);
            var it = g.Generate(c, ref rng);
            Assert.AreEqual(ItemKind.SetPiece, it.Kind);
            Assert.AreEqual(0, st.LegendariesWithoutSet);
        }

        // ---------------- serialization
#if !UNITY_5_3_OR_NEWER
        [Test] public void State_JsonRoundTrip()
        {
            var st = new HeroPityState { HeroId = "kael", Drought = 12, LegendariesWithoutSet = 3 };
            st.RegisterBossKill("boss.colossus", Difficulty.Nightmare, false);
            st.RegisterBossKill("boss.colossus", Difficulty.Eclipse, false);
            st.MarkRegionClearedOnNormal("ashen_wastes");
            string json = System.Text.Json.JsonSerializer.Serialize(st);
            var back = System.Text.Json.JsonSerializer.Deserialize<HeroPityState>(json);
            Assert.AreEqual("kael", back.HeroId);
            Assert.AreEqual(12, back.Drought);
            Assert.AreEqual(3, back.LegendariesWithoutSet);
            Assert.AreEqual(2, back.KillsWithoutUnique("boss.colossus"));
            Assert.IsTrue(back.HasFirstKill("boss.colossus", Difficulty.Nightmare));
            Assert.IsTrue(back.HasFirstKill("boss.colossus", Difficulty.Eclipse));
            Assert.IsFalse(back.HasFirstKill("boss.colossus", Difficulty.Normal));
            Assert.IsTrue(back.HasClearedRegionOnNormal("ashen_wastes"));
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(back));
        }
#endif
    }
}
