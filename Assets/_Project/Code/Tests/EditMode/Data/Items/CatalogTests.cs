using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Core.Items;
using Eclipse.Core.Loot;
using Eclipse.Core.Tags;
using Eclipse.Data.Loot;
using NUnit.Framework;

namespace Eclipse.Data.Items.Tests
{
    public class CatalogTests
    {
        private ItemFixtureBuilder _b;

        [SetUp]
        public void SetUp() => _b = new ItemFixtureBuilder();

        [TearDown]
        public void TearDown() => _b.Dispose();

        private void AddAllGlobals()
        {
            foreach (string key in LootGlobalKeys.AllRequired())
                _b.Global(key, key == LootGlobalKeys.UpgradePerLevel ? 0.03f : key.StartsWith(LootGlobalKeys.SlotWeightPrefix, StringComparison.Ordinal) ? 7f : 1.5f);
        }

        [Test]
        public void ItemCatalog_ConvertsDefinitionsToCoreData()
        {
            AddAllGlobals();
            AffixDefinition ember = _b.Affix("ember", "EMB", AffixCategory.Offensive, AffixGroup.Affinity);
            _b.Affix("armor", "ARM", AffixCategory.Defensive);
            _b.Base("chest", EquipSlot.Chest);
            _b.Band(NamedBand.High, 0.95f, 1.15f);
            _b.Add<RarityDefinition>("rarity.core.epic").EditorSet(Rarity.Epic, 3, 1, 1);
            var mult = _b.Add<SlotMultiplierDefinition>("slotmult.chest.damage");
            mult.EditorSet(EquipSlot.Chest, new List<string> { ember.StableIdString }, string.Empty, 1.25f, false);
            NamedItemDefinition item = _b.Item("ashfang", "K1", ItemKind.NamedLegendary, "chest",
                new[] { "ember:High", "armor*" }, new[] { "armor", "ember", "armor" }, ItemFixtureBuilder.TwoModifiers, 10);

            var catalog = new ItemCatalog(_b.Defs);

            Assert.AreEqual(1.5f, catalog.ArmorImplicitPerILvl);
            Assert.AreEqual(0.03f, catalog.UpgradePerLevel, 1e-6f);
            AffixData a = catalog.GetAffix("affix.core.ember");
            Assert.AreEqual(AffixGroup.Affinity, a.Group);
            Assert.AreEqual(0.01f, a.GetTier(1).Min, 1e-6f);
            Assert.AreEqual(0.06f, a.GetTier(5).Max, 1e-6f);
            Assert.AreEqual(2, catalog.Affixes.Count);
            Assert.AreEqual(2, catalog.Pool.Count);
            Assert.AreEqual("affix.core.armor", catalog.Pool[0].AffixId, "pool ordered by id");
            Assert.AreEqual(1.25f, catalog.SlotMultipliers.Single().Multiplier);
            Assert.AreEqual(EquipSlot.Chest, catalog.GetBase("base.test.chest").Slot);
            Assert.IsNull(catalog.GetBase("base.test.chest").HeroId);

            NamedItemData n = catalog.GetNamedItem(item.StableIdString);
            Assert.AreEqual(ItemKind.NamedLegendary, n.Kind);
            Assert.AreEqual(NamedBand.High, n.FixedAffixes[0].Band);
            Assert.IsTrue(n.FixedAffixes[1].OffPool);
            Assert.AreEqual(3, n.OpenOptions.Count);
            CollectionAssert.AreEqual(ItemFixtureBuilder.TwoModifiers, n.ModifierIds);
            Assert.IsNull(n.SetId);
            Assert.AreEqual(10, n.MinDropILvl);

            Assert.AreEqual(3, catalog.GetRarity(Rarity.Epic).RandomAffixCount);
            Assert.AreEqual(0.95f, catalog.GetBandRange(NamedBand.High).Lo);
            Assert.Throws<KeyNotFoundException>(() => catalog.GetRarity(Rarity.Rare));
            Assert.IsNull(catalog.GetAffix("affix.core.nope"));
        }

        [Test]
        public void ItemCatalog_MissingGlobalKeyThrowsNamingTheKey()
        {
            _b.Global(LootGlobalKeys.ArmorImplicitPerILvl, 4f);
            var ex = Assert.Throws<InvalidOperationException>(() => new ItemCatalog(_b.Defs));
            StringAssert.Contains(LootGlobalKeys.UpgradePerLevel, ex.Message);
        }

        [Test]
        public void LootCatalog_ReadsConstantsFromGlobalsAndConvertsTables()
        {
            AddAllGlobals();

            _b.Add<LootSourceDefinition>("lootsrc.core.elite").EditorSet(LootSourceKind.Elite, 1f, 1, 0.3f, 0, 0, 0, Rarity.Common, true, 1, false, true);
            _b.Add<LootChanceDefinition>("lootchance.core.elite").EditorSet(LootSourceKind.Elite,
                new float[6], new float[] { -1, -1, -1, -1, -1, -1 }, new float[6]);
            _b.Add<IlvlRangeDefinition>("ilvl.veteran.all").EditorSet(Difficulty.Veteran, string.Empty, 30, 45);
            _b.Add<LootDifficultyDefinition>("lootdiff.core.veteran").EditorSet(Difficulty.Veteran, 0.1f, 0f, 0.4f, 0.4f, 0.2f,
                0.1f, 0f, 0, 0, 0, 0, 0f, 0f, 0f);
            _b.Add<PityDefinition>("pity.core.veteran").EditorSet(Difficulty.Veteran, 50, 100, 0.05f, 2f, 4, 3, 0.08f, 0.2f, 10, 1.5f, 0.5f);
            _b.Add<HomeRegionDefinition>("homeregion.set.a").EditorSet(HomeRegionRowKind.Set, "set_a", "kael", "ashen_wastes", 4f, 6f, 3f,
                TagMask.None, 0f, 0f);
            _b.Add<HomeRegionDefinition>("homeregion.region.b").EditorSet(HomeRegionRowKind.Region, "", "", "astral_citadel", 0f, 0f, 0f,
                TagMask.Of(Tag.Void), 2f, 0.03f);

            var catalog = new LootCatalog(_b.Defs);

            Assert.AreEqual(1.5f, catalog.QuantityCap);
            Assert.AreEqual(2, catalog.RequiredLevelMax, "1.5 rounds to 2");
            Assert.AreEqual(1.5f, catalog.SmartWeights.PrimaryCore);
            Assert.AreEqual(7f, catalog.SlotWeights.Weights[(int)EquipSlot.Relic]);
            Assert.AreEqual(10, catalog.SlotWeights.Weights.Length);
            Assert.AreEqual(0.3f, catalog.GetSource(LootSourceKind.Elite).ExtraItemChance, 1e-6f);
            Assert.Less(catalog.GetChances(LootSourceKind.Elite).Epic[0], 0f);
            Assert.AreEqual(1, catalog.IlvlRanges.Count);
            Assert.IsNull(catalog.IlvlRanges[0].RegionId);
            Assert.AreEqual(0.4f, catalog.GetDifficulty(Difficulty.Veteran).SetPieceShare);
            PityConfigData pity = catalog.GetPity(Difficulty.Veteran);
            Assert.AreEqual((0.2f - 0.08f) / 99f, pity.BossUniqueChancePerAscension, 1e-7f);
            Assert.AreEqual(1, catalog.HomeRegions.Count);
            Assert.AreEqual("kael", catalog.HomeRegions[0].HeroId);
            Assert.AreEqual(1, catalog.RegionAffinities.Count);
            Assert.AreEqual(0.03f, catalog.RegionAffinities[0].UniqueBonus, 1e-6f);
            Assert.Throws<KeyNotFoundException>(() => catalog.GetSource(LootSourceKind.Boss));
        }

        [Test]
        public void LootCatalog_MissingGlobalKeyThrowsNamingTheKey()
        {
            AddAllGlobals();
            _b.Defs.RemoveAll(d => d is LootGlobalDefinition g && g.Key == LootGlobalKeys.QuantityCap);
            var ex = Assert.Throws<InvalidOperationException>(() => new LootCatalog(_b.Defs));
            StringAssert.Contains(LootGlobalKeys.QuantityCap, ex.Message);
        }
    }
}
