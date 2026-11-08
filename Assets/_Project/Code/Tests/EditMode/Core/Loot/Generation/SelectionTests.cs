using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Random;
using NUnit.Framework;

namespace Eclipse.Core.Loot.Generation.Tests
{
    [TestFixture]
    public class HeroAndBaseSelectionTests
    {
        private GenFixture _f;
        [SetUp] public void SetUp() => _f = GenFixture.Create();

        [Test]
        public void HeroList_IsDistinctSortedAndIncludesContextHero()
        {
            var sel = new HeroSelector(_f.Items, _f.Loot);
            CollectionAssert.AreEqual(new[] { "kael", "lyra", "nyx", "orun" }, sel.Collect("kael"));
            CollectionAssert.AreEqual(new[] { "extra", "kael", "lyra", "nyx", "orun" }, sel.Collect("extra"));
        }

        [TestCase(false, 0.75)] [TestCase(true, 0.85)]
        public void SmartLoot_CurrentHeroShare(bool legendary, double p)
        {
            var sel = new HeroSelector(_f.Items, _f.Loot); var rng = GenFixture.Rng(2);
            int cur = 0; const int n = 20000; var c = GenFixture.Ctx(hero: "lyra");
            for (int i = 0; i < n; i++) if (sel.Pick(c, legendary, ref rng) == "lyra") cur++;
            // "Any hero" is uniform among the 4, so it also lands on the current hero 1/4 of the time.
            Assert.AreEqual(p + (1 - p) / 4, cur / (double)n, 0.01);
        }

        [Test]
        public void GenericPick_SkipsHeroesWithoutCompatibleBase()
        {
            // "ghost" only appears in a home region: no base can drop for it.
            _f.Loot.HomeList.Add(new HomeRegionData { SetId = "ghost_set", HeroId = "ghost", RegionId = "ashen_wastes" });
            foreach (var b in _f.Items.Bases) if (string.IsNullOrEmpty(b.HeroId)) b.Droppable = false; // only hero-specific bases remain
            var sel = new HeroSelector(_f.Items, _f.Loot); var rng = GenFixture.Rng(5);
            var c = GenFixture.Ctx(hero: "kael");
            for (int i = 0; i < 2000; i++) Assert.AreNotEqual("ghost", sel.Pick(c, false, ref rng));
        }

        [Test]
        public void Base_RespectsHeroDroppableAndSlotWeights()
        {
            var sel = new BaseSelector(_f.Items, _f.Loot); var rng = GenFixture.Rng(9);
            var perSlot = new int[10]; const int n = 30000;
            for (int i = 0; i < n; i++)
            {
                var b = sel.Pick("kael", ref rng);
                Assert.IsTrue(b.HeroId == null || b.HeroId == "kael", b.Id);
                Assert.AreNotEqual("keystone", b.Id);
                perSlot[(int)b.Slot]++;
            }
            // weights 10,7,8,8,8,8,8,9,6,7 = 79 total
            Assert.AreEqual(9 / 79.0, perSlot[(int)EquipSlot.Ring] / (double)n, 0.01);
            Assert.AreEqual(7 / 79.0, perSlot[(int)EquipSlot.OffHand] / (double)n, 0.01);
            Assert.AreEqual(7 / 79.0, perSlot[(int)EquipSlot.Relic] / (double)n, 0.01);
        }

        [Test]
        public void Base_SlotWithoutCompatibleBaseGetsNoWeight()
        {
            _f.Items.BaseList.RemoveAll(b => b.Slot == EquipSlot.Feet);
            var sel = new BaseSelector(_f.Items, _f.Loot); var rng = GenFixture.Rng(9);
            for (int i = 0; i < 3000; i++) Assert.AreNotEqual(EquipSlot.Feet, sel.Pick("lyra", ref rng).Slot);
        }
    }

    [TestFixture]
    public class LegendaryKindTests
    {
        private GenFixture _f;
        [SetUp] public void SetUp() => _f = GenFixture.Create();

        private Dictionary<ItemKind, int> Kinds(LootContext c, int n, string hero = "kael", int ilvl = 40)
        {
            var sel = new LegendaryKindSelector(_f.Loot, new NamedItemSelector(_f.Items, _f.Loot));
            var rng = GenFixture.Rng(4); var d = new Dictionary<ItemKind, int>();
            for (int i = 0; i < n; i++)
            {
                var k = sel.Pick(c, hero, ilvl, ref rng);
                d[k] = d.TryGetValue(k, out var v) ? v + 1 : 1;
            }
            return d;
        }

        private static int Get(Dictionary<ItemKind, int> d, ItemKind k) => d.TryGetValue(k, out var v) ? v : 0;

        [Test]
        public void Normal_NeverSetOrUnique()
        {
            var d = Kinds(GenFixture.Ctx(LootSourceKind.Boss), 2000);
            Assert.AreEqual(2000, Get(d, ItemKind.NamedLegendary));
        }

        [Test]
        public void Veteran_SharesFollowDifficultyData()
        {
            var d = Kinds(GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Veteran), 20000);
            Assert.AreEqual(0.40, Get(d, ItemKind.SetPiece) / 20000.0, 0.012);
            Assert.AreEqual(0.15, Get(d, ItemKind.Unique) / 20000.0, 0.012);
            Assert.AreEqual(0.45, Get(d, ItemKind.NamedLegendary) / 20000.0, 0.012);
        }

        [Test]
        public void Unique_BelowMinDropILvl_RedistributesToStandard()
        {
            var d = Kinds(GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Veteran), 20000, "kael", 34);
            Assert.AreEqual(0, Get(d, ItemKind.Unique));
            Assert.AreEqual(0.60, Get(d, ItemKind.NamedLegendary) / 20000.0, 0.012);
        }

        [Test]
        public void NoSetPieceForHero_RedistributesToStandard()
        {
            var d = Kinds(GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Veteran), 10000, "nyx", 40);
            Assert.AreEqual(0, Get(d, ItemKind.SetPiece));
            Assert.AreEqual(0.85, Get(d, ItemKind.NamedLegendary) / 10000.0, 0.012);
        }

        [Test]
        public void AstralCitadel_AddsUniqueBonus()
        {
            var u = new NamedItemData { Id = "item.u_astral", Kind = ItemKind.Unique, BaseId = "band", MinDropILvl = 35,
                UniqueSource = UniqueSourceKind.Region, UniqueSourceRef = "astral_citadel" };
            _f.Items.NamedList.Add(u);
            var c = GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Veteran, "astral_citadel");
            var d = Kinds(c, 20000);
            Assert.AreEqual(0.18, Get(d, ItemKind.Unique) / 20000.0, 0.012);
            // no bonus in Normal (no Uniques there)
            var n = Kinds(GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Normal, "astral_citadel"), 500);
            Assert.AreEqual(0, Get(n, ItemKind.Unique));
        }

        [Test]
        public void SetPity_ForcesSetPieceAfterConfiguredLegendaries()
        {
            var c = GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Veteran);
            c.Pity = new FakePity { LegendariesWithoutSet = 3 };
            Assert.Greater(Get(Kinds(c, 500), ItemKind.NamedLegendary), 0);
            c.Pity = new FakePity { LegendariesWithoutSet = 4 };
            Assert.AreEqual(500, Get(Kinds(c, 500), ItemKind.SetPiece));
            // never in Normal
            c = GenFixture.Ctx(LootSourceKind.Boss); c.Pity = new FakePity { LegendariesWithoutSet = 99 };
            Assert.AreEqual(500, Get(Kinds(c, 500), ItemKind.NamedLegendary));
        }

        [Test]
        public void SetPieceChanceOverride_ReplacesShares()
        {
            var c = GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Veteran); c.Guarantees.SetPieceChanceOverride = 0.5f;
            var d = Kinds(c, 10000);
            Assert.AreEqual(0.5, Get(d, ItemKind.SetPiece) / 10000.0, 0.015);
            Assert.AreEqual(0, Get(d, ItemKind.Unique));
        }

        [Test]
        public void ForcedKind_FallsBackWhenUnavailable()
        {
            var c = GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Veteran); c.Guarantees.ForceKind = ItemKind.Unique;
            Assert.AreEqual(300, Get(Kinds(c, 300), ItemKind.Unique));
            Assert.AreEqual(0, Get(Kinds(c, 300, "kael", 20), ItemKind.Unique)); // iLvl 20 < 35
        }
    }

    [TestFixture]
    public class NamedSelectionTests
    {
        private GenFixture _f;
        private NamedItemSelector _s;
        [SetUp] public void SetUp() { _f = GenFixture.Create(); _s = new NamedItemSelector(_f.Items, _f.Loot); }

        private double Share(ItemKind kind, LootContext c, System.Func<NamedItemData, bool> pred, string hero = "kael", int ilvl = 40, int n = 20000)
        {
            var rng = GenFixture.Rng(6); int hit = 0;
            for (int i = 0; i < n; i++) if (pred(_s.Pick(kind, c, hero, ilvl, ref rng))) hit++;
            return hit / (double)n;
        }

        private static LootContext Veteran(LootSourceKind s = LootSourceKind.Elite, string region = "ashen_wastes")
            => GenFixture.Ctx(s, Difficulty.Veteran, region);

        [Test] public void Set_HomeRegionWeightX4()
            => Assert.AreEqual(0.8, Share(ItemKind.SetPiece, Veteran(), p => p.SetId == "ashen"), 0.012);

        [Test] public void Set_HomeRegionBossWeightX6()
            => Assert.AreEqual(6 / 7.0, Share(ItemKind.SetPiece, Veteran(LootSourceKind.Boss), p => p.SetId == "ashen"), 0.012);

        [Test] public void Set_NoHomeRegionIsUniform()
            => Assert.AreEqual(0.5, Share(ItemKind.SetPiece, Veteran(region: "shattered_expanse"), p => p.SetId == "ashen"), 0.012);

        [Test] public void Set_AttunementMultipliesCumulatively()
        {
            var c = Veteran(); c.Attunement = new AttunementTarget(AttunementKind.Set, "eternal");
            Assert.AreEqual(4 / 7.0, Share(ItemKind.SetPiece, c, p => p.SetId == "ashen"), 0.012);
        }

        [Test] public void SetPiece_UnownedWeightX3()
        {
            var col = new FakeCollection(); col.Owned.Add("piece.ashen.helm");
            var c = Veteran(); c.Collection = col;
            // inside "ashen": helm owned (1) vs chest unowned (3)
            double chest = Share(ItemKind.SetPiece, c, p => p.Id == "piece.ashen.chest", n: 40000) / 0.8;
            Assert.AreEqual(0.75, chest, 0.02);
        }

        [Test] public void Standard_RegionAffinityWeightX2()
            => Assert.AreEqual(0.5, Share(ItemKind.NamedLegendary, GenFixture.Ctx(region: "ashen_wastes"), p => p.Id == "item.ashfang"), 0.012);

        [Test] public void Standard_NoAffinityRegionIsUniform()
            => Assert.AreEqual(1 / 3.0, Share(ItemKind.NamedLegendary, GenFixture.Ctx(region: "astral_citadel"), p => p.Id == "item.ashfang"), 0.012);

        [Test] public void Standard_RespectsHeroBinding()
        {
            var rng = GenFixture.Rng(1);
            for (int i = 0; i < 500; i++)
            {
                var p = _s.Pick(ItemKind.NamedLegendary, GenFixture.Ctx(), "lyra", 40, ref rng);
                Assert.IsTrue(p.HeroId == null || p.HeroId == "lyra", p.Id);
            }
        }

        [Test] public void Unique_UndiscoveredWeightX2()
        {
            var col = new FakeCollection(); col.Owned.Add("item.u_region_a");
            var c = Veteran(); c.Collection = col;
            Assert.AreEqual(2 / 3.0, Share(ItemKind.Unique, c, p => p.Id == "item.u_region_b"), 0.012);
        }

        [Test] public void Unique_AttunementX3()
        {
            var c = Veteran(); c.Attunement = new AttunementTarget(AttunementKind.Unique, "item.u_region_a");
            Assert.AreEqual(0.75, Share(ItemKind.Unique, c, p => p.Id == "item.u_region_a"), 0.012);
        }

        [Test] public void Unique_SourceRules()
        {
            var rng = GenFixture.Rng(2);
            // natural roll: region Uniques only (Boss/Pinnacle never, Challenge only in Challenge rooms on Eclipse+)
            for (int i = 0; i < 300; i++)
            {
                var id = _s.Pick(ItemKind.Unique, Veteran(), "kael", 40, ref rng).Id;
                Assert.IsTrue(id == "item.u_region_a" || id == "item.u_region_b", id);
            }
            Assert.IsNull(_s.Pick(ItemKind.Unique, Veteran(region: "frozen_cathedral"), "kael", 40, ref rng));
            var ch = GenFixture.Ctx(LootSourceKind.Challenge, Difficulty.Eclipse, "frozen_cathedral");
            Assert.AreEqual("item.u_challenge", _s.Pick(ItemKind.Unique, ch, "kael", 80, ref rng).Id);
            ch.Difficulty = Difficulty.Nightmare;
            Assert.IsNull(_s.Pick(ItemKind.Unique, ch, "kael", 60, ref rng));
            // forced boss Unique restricted to that boss pool
            var boss = Veteran(LootSourceKind.Boss); boss.Guarantees.ForceKind = ItemKind.Unique; boss.Guarantees.BossId = "boss.colossus";
            for (int i = 0; i < 50; i++) Assert.AreEqual("item.u_boss", _s.Pick(ItemKind.Unique, boss, "kael", 60, ref rng).Id);
        }

        [Test] public void MinDropILvl_Filters()
        {
            var rng = GenFixture.Rng(1);
            Assert.IsNull(_s.Pick(ItemKind.Unique, Veteran(), "kael", 34, ref rng));
            Assert.IsNull(_s.Pick(ItemKind.NamedLegendary, GenFixture.Ctx(), "kael", 9, ref rng));
        }
    }
}
