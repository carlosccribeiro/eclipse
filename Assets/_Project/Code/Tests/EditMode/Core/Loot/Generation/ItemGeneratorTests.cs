using System;
using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Random;
using NUnit.Framework;

namespace Eclipse.Core.Loot.Generation.Tests
{
    [TestFixture]
    public class ItemGeneratorStructureTests
    {
        private GenFixture _f;
        private ItemGenerator _g;
        [SetUp] public void SetUp()
        {
            _f = GenFixture.Create();
            _f.Loot.SmartLootCurrentHeroLegendary = 1f; // keep the target hero fixed so forced kinds have candidates
            _g = _f.NewGenerator();
        }

        private ItemInstance Forced(ItemKind kind, ulong seed, string hero = "kael", string region = "ashen_wastes", int ilvl = 40)
        {
            var c = GenFixture.Ctx(LootSourceKind.Boss, Difficulty.Veteran, region, hero, ilvl);
            c.Guarantees.ForceLegendary = true; c.Guarantees.ForceKind = kind;
            var rng = GenFixture.Rng(seed);
            return _g.Generate(c, ref rng);
        }

        [TestCase(ItemKind.NamedLegendary)] [TestCase(ItemKind.SetPiece)]
        public void R25_LegendaryAndSet_4Fixed_1Open_2Modifiers(ItemKind kind)
        {
            for (ulong s = 1; s <= 200; s++)
            {
                var it = Forced(kind, s);
                var def = _f.Items.GetNamedItem(it.DefinitionId);
                Assert.AreEqual(kind, it.Kind); Assert.AreEqual(Rarity.Legendary, it.Rarity);
                Assert.AreEqual(def.BaseId, it.BaseId);
                Assert.AreEqual(5, it.Affixes.Count); Assert.AreEqual(2, it.Modifiers.Count);
                for (int i = 0; i < 4; i++)
                {
                    Assert.AreEqual(def.FixedAffixes[i].AffixId, it.Affixes[i].AffixId);
                    Assert.IsTrue(it.Affixes[i].Named); Assert.AreEqual(def.FixedAffixes[i].Band, it.Affixes[i].Band);
                }
                var open = it.Affixes[4];
                Assert.AreEqual(def.OpenOptions[it.OpenAffixChoice].AffixId, open.AffixId);
                Assert.IsTrue(open.Named); Assert.AreEqual(NamedBand.Default, open.Band);
                CollectionAssert.AreEqual(def.ModifierIds, new[] { it.Modifiers[0].ModifierId, it.Modifiers[1].ModifierId });
                Assert.AreEqual(def.HeroId, it.BoundHeroId);
            }
        }

        [Test]
        public void R25_Unique_5Fixed_NoOpen_2Modifiers()
        {
            for (ulong s = 1; s <= 200; s++)
            {
                var it = Forced(ItemKind.Unique, s);
                var def = _f.Items.GetNamedItem(it.DefinitionId);
                Assert.AreEqual(ItemKind.Unique, it.Kind); Assert.AreEqual(Rarity.Legendary, it.Rarity);
                Assert.AreEqual(5, it.Affixes.Count); Assert.AreEqual(2, it.Modifiers.Count);
                Assert.AreEqual(0, it.OpenAffixChoice);
                for (int i = 0; i < 5; i++) { Assert.AreEqual(def.FixedAffixes[i].AffixId, it.Affixes[i].AffixId); Assert.IsTrue(it.Affixes[i].Named); }
                Assert.IsNull(it.BoundHeroId);
            }
        }

        [Test]
        public void NamedAffixValue_UsesBandThroughItemMath()
        {
            var it = Forced(ItemKind.NamedLegendary, 3);
            var ember = it.Affixes[0]; // High band 95%-115% of the tier max; tier 2 -> max 0.25
            float v = ItemMath.AffixValue(_f.Items, it, ember, EquipSlot.MainHand, it.BaseId);
            Assert.GreaterOrEqual(v, 0.95f * 0.25f - 1e-5f); Assert.LessOrEqual(v, 1.15f * 0.25f + 1e-5f);
            Assert.AreEqual(2, ember.Tier);
        }

        [Test]
        public void OffPoolSpecs_AreCopiedWithoutPoolValidation()
        {
            var it = Forced(ItemKind.NamedLegendary, 5);
            CollectionAssert.Contains(new List<string>(Ids(it)), "armor"); // spec #4 has the OffPool flag; "armor" is not in the MH pool of the item base
        }

        private static IEnumerable<string> Ids(ItemInstance it) { foreach (var a in it.Affixes) yield return a.AffixId; }

        [Test]
        public void GenericRarities_StructureAndInvariants()
        {
            var counts = new Dictionary<Rarity, int>();
            var rng = GenFixture.Rng(99);
            var c = GenFixture.Ctx(LootSourceKind.Trash, Difficulty.Eclipse, "ashen_wastes", "kael", 50);
            for (int i = 0; i < 4000; i++)
            {
                var it = _g.Generate(c, ref rng);
                counts[it.Rarity] = counts.TryGetValue(it.Rarity, out var v) ? v + 1 : 1;
                if (it.Rarity == Rarity.Legendary) continue;
                var b = _f.Items.GetBase(it.BaseId);
                Assert.AreEqual(ItemKind.Generic, it.Kind); Assert.AreEqual(b.Id, it.DefinitionId);
                var r = _f.Items.GetRarity(it.Rarity);
                Assert.AreEqual(r.RandomAffixCount, it.Affixes.Count, it.Rarity.ToString());
                Assert.AreEqual(r.ModifierCount, it.Modifiers.Count);
                CheckGeneric(it, b);
            }
            Assert.IsTrue(counts.ContainsKey(Rarity.Common) && counts.ContainsKey(Rarity.Rare) && counts.ContainsKey(Rarity.Epic));
        }

        private static void CheckGeneric(ItemInstance it, ItemBaseData b)
        {
            var ids = new HashSet<string>(); int aff = 0, com = 0, res = 0, pri = 0;
            foreach (var a in it.Affixes)
            {
                Assert.IsTrue(ids.Add(a.AffixId), "duplicate " + a.AffixId);
                Assert.IsFalse(a.Named); Assert.AreEqual(NamedBand.None, a.Band); Assert.AreEqual(3, a.Tier);
                Assert.GreaterOrEqual(a.Roll01, 0f); Assert.Less(a.Roll01, 1f);
                switch (a.AffixId)
                {
                    case "ember": case "frost": case "storm": case "inf_ember": aff++; break;
                    case "pickup": case "xp": case "gold": com++; break;
                    case "phys_res": case "elem_res": case "void_res": res++; break;
                    case "str": case "dex": case "int": pri++; break;
                    case "move_speed": Assert.AreEqual(EquipSlot.Feet, b.Slot); break;
                }
            }
            Assert.LessOrEqual(aff, 2); Assert.LessOrEqual(com, 1);
            if (it.Rarity != Rarity.Epic) Assert.LessOrEqual(res, 1);
            int maxPri = b.Slot == EquipSlot.Amulet ? 2 : (b.Slot == EquipSlot.Ring || b.Slot == EquipSlot.OffHand || b.Slot == EquipSlot.Relic ? 0 : 1);
            Assert.LessOrEqual(pri, maxPri);
            if (string.IsNullOrEmpty(b.ImplicitAffixId)) Assert.AreEqual(0f, it.ImplicitRoll01); else Assert.Less(it.ImplicitRoll01, 1f);
        }

        [Test]
        public void LegendaryMinILvl_DemotesToEpicUnlessRaised()
        {
            var c = GenFixture.Ctx(LootSourceKind.Boss, ilvl: 5); c.Guarantees.ForceLegendary = true;
            var rng = GenFixture.Rng(1);
            var it = _g.Generate(c, ref rng);
            Assert.AreEqual(Rarity.Epic, it.Rarity); Assert.AreEqual(5, it.ItemLevel);
            c.Guarantees.RaiseILvlToLegendaryMin = true;
            it = _g.Generate(c, ref rng);
            Assert.AreEqual(Rarity.Legendary, it.Rarity); Assert.AreEqual(10, it.ItemLevel);
        }

        [Test]
        public void NoNamedAvailable_DemotesToEpic()
        {
            _f.Items.NamedList.Clear();
            var c = GenFixture.Ctx(LootSourceKind.Boss, ilvl: 40); c.Guarantees.ForceLegendary = true;
            var rng = GenFixture.Rng(1);
            Assert.AreEqual(Rarity.Epic, _g.Generate(c, ref rng).Rarity);
        }

        [Test]
        public void Metadata_SourceSeedTierGuidAndPityCallback()
        {
            var pity = new FakePity { Drought = 3 };
            var c = GenFixture.Ctx(LootSourceKind.Elite, ilvl: 45); c.Pity = pity;
            var rng = GenFixture.Rng(12); var before = rng.GetState();
            var it = _g.Generate(c, ref rng);
            Assert.AreEqual(LootSourceKind.Elite, it.Source);
            Assert.AreEqual(45, it.ItemLevel); Assert.AreNotEqual(Guid.Empty, it.InstanceId);
            Assert.AreEqual(1, pity.Calls); Assert.AreEqual(it.Rarity, pity.LastRarity); Assert.AreEqual(it.Kind, pity.LastKind);
            Assert.IsTrue(pity.LastConsumes); // Elite consumes hard pity
            Assert.AreNotEqual(before, rng.GetState());
            var rng2 = GenFixture.Rng(12);
            Assert.AreEqual(it.DropSeed, _g.Generate(c, ref rng2).DropSeed); // derived from the state before generating
            var rng3 = GenFixture.Rng(13);
            Assert.AreNotEqual(it.DropSeed, _g.Generate(c, ref rng3).DropSeed);
        }

        [Test]
        public void Constructor_RejectsNulls()
        {
            Assert.Throws<ArgumentNullException>(() => new ItemGenerator(null, _f.Loot, _f.Mods));
            Assert.Throws<ArgumentNullException>(() => new ItemGenerator(_f.Items, null, _f.Mods));
            Assert.Throws<ArgumentNullException>(() => new ItemGenerator(_f.Items, _f.Loot, null));
        }
    }

    [TestFixture]
    public class ItemGeneratorDeterminismTests
    {
        private GenFixture _f;
        [SetUp] public void SetUp() => _f = GenFixture.Create();

        private static LootContext ContextFor(int i)
        {
            var sources = (LootSourceKind[])Enum.GetValues(typeof(LootSourceKind));
            var diffs = new[] { Difficulty.Normal, Difficulty.Veteran, Difficulty.Nightmare, Difficulty.Eclipse, Difficulty.Ascension };
            var heroes = new[] { "kael", "lyra", "nyx", "orun" };
            var regions = new[] { "ashen_wastes", "frozen_cathedral", "shattered_expanse", "astral_citadel" };
            var d = diffs[(i / 3) % diffs.Length];
            var c = GenFixture.Ctx(sources[i % sources.Length], d, regions[(i / 7) % regions.Length], heroes[(i / 5) % heroes.Length],
                null, d == Difficulty.Ascension ? 1 + (i * 7) % 100 : 0);
            c.Guarantees.ForceEpicPlus = i % 4 == 0;
            c.Guarantees.ForceLegendary = i % 6 == 0;
            c.Pity = null;
            return c;
        }

        private static void AssertSame(ItemInstance a, ItemInstance b, string tag)
        {
            Assert.AreEqual(a.InstanceId, b.InstanceId, tag); Assert.AreEqual(a.DefinitionId, b.DefinitionId, tag);
            Assert.AreEqual(a.BaseId, b.BaseId, tag); Assert.AreEqual(a.Rarity, b.Rarity, tag); Assert.AreEqual(a.Kind, b.Kind, tag);
            Assert.AreEqual(a.ItemLevel, b.ItemLevel, tag); Assert.AreEqual(a.BoundHeroId, b.BoundHeroId, tag);
            Assert.AreEqual(a.ImplicitRoll01, b.ImplicitRoll01, tag); Assert.AreEqual(a.OpenAffixChoice, b.OpenAffixChoice, tag);
            Assert.AreEqual(a.UpgradeLevel, b.UpgradeLevel, tag); Assert.AreEqual(a.Flags, b.Flags, tag);
            Assert.AreEqual(a.EnchantedAffixIndex, b.EnchantedAffixIndex, tag); Assert.AreEqual(a.DropSeed, b.DropSeed, tag);
            Assert.AreEqual(a.Source, b.Source, tag);
            Assert.AreEqual(a.Affixes.Count, b.Affixes.Count, tag);
            for (int i = 0; i < a.Affixes.Count; i++)
            {
                Assert.AreEqual(a.Affixes[i].AffixId, b.Affixes[i].AffixId, tag); Assert.AreEqual(a.Affixes[i].Tier, b.Affixes[i].Tier, tag);
                Assert.AreEqual(a.Affixes[i].Roll01, b.Affixes[i].Roll01, tag); Assert.AreEqual(a.Affixes[i].Named, b.Affixes[i].Named, tag);
                Assert.AreEqual(a.Affixes[i].Band, b.Affixes[i].Band, tag);
            }
            Assert.AreEqual(a.Modifiers.Count, b.Modifiers.Count, tag);
            for (int i = 0; i < a.Modifiers.Count; i++)
            {
                Assert.AreEqual(a.Modifiers[i].ModifierId, b.Modifiers[i].ModifierId, tag);
                Assert.AreEqual(a.Modifiers[i].Roll01, b.Modifiers[i].Roll01, tag);
            }
        }

        [Test]
        public void SameContextAndSeed_SameItem_1000Seeds_AndBufferReuseDoesNotLeak()
        {
            var shared = _f.NewGenerator(); // reused across all seeds: buffers must not leak state between calls
            for (int s = 1; s <= 1000; s++)
            {
                var ctx = ContextFor(s);
                var rngA = GenFixture.Rng((ulong)s); var rngB = GenFixture.Rng((ulong)s); var rngC = GenFixture.Rng((ulong)s);
                var a = _f.NewGenerator().Generate(ctx, ref rngA);
                var b = _f.NewGenerator().Generate(ctx, ref rngB);
                var c = shared.Generate(ctx, ref rngC);
                AssertSame(a, b, "fresh vs fresh seed " + s);
                AssertSame(a, c, "fresh vs shared seed " + s);
                Assert.AreEqual(rngA.GetState(), rngB.GetState()); Assert.AreEqual(rngA.GetState(), rngC.GetState());
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentInstances()
        {
            var g = _f.NewGenerator(); var ids = new HashSet<Guid>();
            var c = GenFixture.Ctx(LootSourceKind.Boss, ilvl: 40);
            for (int s = 1; s <= 200; s++) { var rng = GenFixture.Rng((ulong)s); ids.Add(g.Generate(c, ref rng).InstanceId); }
            Assert.AreEqual(200, ids.Count);
        }

        [Test]
        public void ContextVariety_CoversAllKindsAndRarities()
        {
            var g = _f.NewGenerator(); var seen = new HashSet<string>();
            for (int s = 1; s <= 1000; s++)
            {
                var rng = GenFixture.Rng((ulong)s);
                var it = g.Generate(ContextFor(s), ref rng);
                seen.Add(it.Rarity + "/" + it.Kind);
            }
            foreach (var k in new[] { "Common/Generic", "Rare/Generic", "Epic/Generic", "Legendary/NamedLegendary", "Legendary/SetPiece", "Legendary/Unique" })
                CollectionAssert.Contains(seen, k);
        }
    }
}
