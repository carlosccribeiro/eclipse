using Eclipse.Core.Items;
using Eclipse.Core.Loot.Generation;
using Eclipse.Core.Loot.Generation.Tests;
using NUnit.Framework;

namespace Eclipse.Core.Loot.Distribution.Tests
{
    /// <summary>Rarity floors per source (05 section 3.1), Legendary minDropILvl (5.2), no sets/Uniques in Normal and Unique iLvl (5.5), fixed seeds.</summary>
    [TestFixture]
    public class SourceRulesTests
    {
        private const int Rolls = 20000;

        // 05 section 3.1 "Piso de raridade" column (test fixture values).
        private static Rarity DocFloor(LootSourceKind s)
        {
            switch (s)
            {
                case LootSourceKind.Trash: return Rarity.Common;
                case LootSourceKind.Reliquary:
                case LootSourceKind.Challenge: return Rarity.Epic;
                default: return Rarity.Rare; // Elite, Guardian, Arena, Treasure, Event, Boss, Rift Cache
            }
        }

        private static readonly Difficulty[] AllDifficulties =
            { Difficulty.Normal, Difficulty.Veteran, Difficulty.Nightmare, Difficulty.Eclipse, Difficulty.Ascension };

        [Test]
        public void Rarity_floor_per_source_holds_in_every_difficulty()
        {
            var f = GenFixture.Create();
            var roller = new RarityRoller(f.Loot);
            ulong seed = 1;
            foreach (LootSourceKind source in System.Enum.GetValues(typeof(LootSourceKind)))
            foreach (var diff in AllDifficulties)
            {
                var src = f.Loot.GetSource(source);
                Assert.AreEqual(DocFloor(source), src.RarityFloor, source + " floor in catalog");
                var ctx = GenFixture.Ctx(source, diff, ascension: diff == Difficulty.Ascension ? 50 : 0);
                var rng = GenFixture.Rng(seed++);
                var min = Rarity.Legendary;
                for (int i = 0; i < Rolls; i++) { var r = roller.Roll(ctx, src, ref rng); if (r < min) min = r; }
                Assert.GreaterOrEqual(min, DocFloor(source), source + " " + diff);
            }
        }

        [Test]
        public void Generated_items_respect_the_source_floor()
        {
            var f = GenFixture.Create();
            var g = f.NewGenerator();
            ulong seed = 500;
            foreach (LootSourceKind source in System.Enum.GetValues(typeof(LootSourceKind)))
            foreach (var diff in AllDifficulties)
            {
                var ctx = GenFixture.Ctx(source, diff, ilvl: diff == Difficulty.Normal ? 5 : 60, ascension: diff == Difficulty.Ascension ? 30 : 0);
                var rng = GenFixture.Rng(seed++);
                for (int i = 0; i < 1500; i++)
                    Assert.GreaterOrEqual(g.Generate(ctx, ref rng).Rarity, DocFloor(source), source + " " + diff);
            }
        }

        [Test]
        public void Legendary_below_ilvl_10_becomes_Epic()
        {
            var f = GenFixture.Create();
            var g = f.NewGenerator();
            int below = 0, at10 = 0;
            // Challenge Normal: L = 5% with an Epic floor, so a failed Legendary can only land on Epic.
            for (int pass = 0; pass < 2; pass++)
            {
                var ctx = GenFixture.Ctx(LootSourceKind.Challenge, Difficulty.Normal, ilvl: pass == 0 ? 9 : 10);
                var rng = GenFixture.Rng(77UL + (ulong)pass);
                for (int i = 0; i < Rolls; i++)
                {
                    var it = g.Generate(ctx, ref rng);
                    if (it.Rarity == Rarity.Legendary) { if (pass == 0) below++; else at10++; }
                }
            }
            Assert.AreEqual(0, below, "no Legendary below iLvl 10");
            Assert.Greater(at10, 0, "Legendaries exist at iLvl 10");

            // Forced Legendary (hard pity style) also degrades below 10 unless the first-kill raise is requested.
            var c = GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Normal, ilvl: 4);
            c.Guarantees.ForceLegendary = true;
            var r2 = GenFixture.Rng(5);
            Assert.AreEqual(Rarity.Epic, g.Generate(c, ref r2).Rarity);
            c.Guarantees.RaiseILvlToLegendaryMin = true;
            var it2 = g.Generate(c, ref r2);
            Assert.AreEqual(Rarity.Legendary, it2.Rarity); Assert.AreEqual(10, it2.ItemLevel);
        }

        [Test]
        public void Normal_never_drops_set_pieces_or_Uniques()
        {
            var f = GenFixture.Create();
            var g = f.NewGenerator();
            int legendaries = 0;
            ulong seed = 900;
            foreach (var source in new[] { LootSourceKind.Boss, LootSourceKind.Challenge, LootSourceKind.RiftCache, LootSourceKind.Reliquary })
            foreach (var hero in new[] { "kael", "lyra" })
            {
                var ctx = GenFixture.Ctx(source, Difficulty.Normal, "frozen_cathedral", hero, 20);
                var rng = GenFixture.Rng(seed++);
                for (int i = 0; i < 5000; i++)
                {
                    var it = g.Generate(ctx, ref rng);
                    if (it.Rarity != Rarity.Legendary) { Assert.AreEqual(ItemKind.Generic, it.Kind); continue; }
                    legendaries++;
                    Assert.AreEqual(ItemKind.NamedLegendary, it.Kind);
                }
                var forced = GenFixture.Ctx(source, Difficulty.Normal, "frozen_cathedral", hero, 20);
                forced.Guarantees.ForceLegendary = true;
                for (int i = 0; i < 1000; i++) Assert.AreEqual(ItemKind.NamedLegendary, g.Generate(forced, ref rng).Kind);
            }
            Assert.Greater(legendaries, 1000);
        }

        [Test]
        public void Unique_requires_ilvl_35()
        {
            var f = GenFixture.Create();
            f.Loot.SmartLootCurrentHeroLegendary = 1f;
            var g = f.NewGenerator();
            int uniquesAt34 = 0, uniquesAt35 = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                var ctx = GenFixture.Ctx(LootSourceKind.Challenge, Difficulty.Veteran, "ashen_wastes", "kael", pass == 0 ? 34 : 35);
                ctx.Guarantees.ForceLegendary = true;
                var rng = GenFixture.Rng(31UL + (ulong)pass);
                for (int i = 0; i < Rolls; i++)
                {
                    var it = g.Generate(ctx, ref rng);
                    if (it.Kind != ItemKind.Unique) continue;
                    if (pass == 0) uniquesAt34++; else uniquesAt35++;
                }
            }
            TestContext.WriteLine("Challenge/Veteran forced Legendary: uniques@34=" + uniquesAt34 + " uniques@35=" + uniquesAt35);
            Assert.AreEqual(0, uniquesAt34);
            Assert.Greater(uniquesAt35, 0);
        }

#if !UNITY_5_3_OR_NEWER // heavy simulation: dotnet test only
        [Test]
        public void Free_running_mix_never_violates_ilvl_or_kind_rules()
        {
            var f = GenFixture.Create();
            var g = f.NewGenerator();
            var rng = GenFixture.Rng(123456);
            var sources = (LootSourceKind[])System.Enum.GetValues(typeof(LootSourceKind));
            for (int i = 0; i < 100000; i++)
            {
                var diff = AllDifficulties[(int)(rng.NextUInt() % 5)];
                var src = sources[(int)(rng.NextUInt() % (uint)sources.Length)];
                var ctx = GenFixture.Ctx(src, diff, rng.NextUInt() % 2 == 0 ? "ashen_wastes" : "frozen_cathedral",
                    rng.NextUInt() % 2 == 0 ? "kael" : "lyra", 1 + (int)(rng.NextUInt() % 100),
                    diff == Difficulty.Ascension ? 1 + (int)(rng.NextUInt() % 100) : 0);
                var it = g.Generate(ctx, ref rng);
                if (it.Rarity == Rarity.Legendary) Assert.GreaterOrEqual(it.ItemLevel, 10);
                if (it.Kind == ItemKind.Unique) Assert.GreaterOrEqual(it.ItemLevel, 35);
                if (diff == Difficulty.Normal) Assert.That(it.Kind == ItemKind.Generic || it.Kind == ItemKind.NamedLegendary);
                Assert.AreEqual(it.Rarity == Rarity.Legendary, it.Kind != ItemKind.Generic);
            }
        }
#endif
    }
}
