using NUnit.Framework;

namespace Eclipse.Core.Items.Tests
{
    [TestFixture]
    public class ItemMathTests
    {
        private FakeItemCatalog _cat;

        [SetUp]
        public void SetUp()
        {
            _cat = new FakeItemCatalog();
            var ember = new AffixData { Id = "ember_damage", ValueKind = AffixValueKind.Percent };
            ember.Tiers[4] = new TierRange(0.20f, 0.30f);
            ember.Tiers[0] = new TierRange(0.02f, 0.04f);
            _cat.AffixList.Add(ember);
            var impl = new AffixData { Id = "impl_aff", ValueKind = AffixValueKind.Percent };
            impl.Tiers[4] = new TierRange(0.10f, 0.20f);
            _cat.AffixList.Add(impl);
            _cat.BandMap[NamedBand.Envelope] = new NamedBandRange(0.70f, 1.15f);
            _cat.BaseList.Add(new ItemBaseData { Id = "sword", Slot = EquipSlot.MainHand, ImplicitAffixId = "impl_aff", ImplicitFactor = 0.5f });
            _cat.SlotMultiplierList.Add(new SlotMultiplierData
            {
                Id = "mh", Slot = EquipSlot.MainHand, Multiplier = 1.5f,
                AffixIds = { "ember_damage" }
            });
        }

        private static ItemInstance Item(int upgrade = 0, int iLvl = 100, float implRoll = 0f)
            => new ItemInstance { ItemLevel = iLvl, UpgradeLevel = upgrade, ImplicitRoll01 = implRoll, BaseId = "sword" };

        [TestCase(1, 1)] [TestCase(20, 1)] [TestCase(21, 2)] [TestCase(100, 5)] [TestCase(500, 5)]
        public void Tier(int iLvl, int expected) => Assert.AreEqual(expected, ItemMath.Tier(iLvl));

        [TestCase(1, 1)] [TestCase(100, 50)] [TestCase(21, 11)]
        public void RequiredLevel(int iLvl, int expected) => Assert.AreEqual(expected, ItemMath.RequiredLevel(iLvl, 0.5f, 50));

        [TestCase(0f, 0.21f)] [TestCase(1f, 0.345f)]
        public void NamedEnvelope_Example01(float roll, float expected)
        {
            var a = new RolledAffix { AffixId = "ember_damage", Tier = 5, Roll01 = roll, Named = true, Band = NamedBand.Envelope };
            // Slot with no multiplier rule (OffHand) so only the band applies.
            Assert.AreEqual(expected, ItemMath.AffixValue(_cat, Item(), a, EquipSlot.OffHand, "sword"), 1e-5f);
        }

        [TestCase(0f, 0.20f)] [TestCase(1f, 0.30f)] [TestCase(0.5f, 0.25f)]
        public void RandomAffix_TierLimits(float roll, float expected)
        {
            var a = new RolledAffix { AffixId = "ember_damage", Tier = 5, Roll01 = roll };
            Assert.AreEqual(expected, ItemMath.AffixValue(_cat, Item(), a, EquipSlot.Head, "x"), 1e-5f);
        }

        [Test]
        public void SlotMultiplier_MainHand_1_5()
        {
            var a = new RolledAffix { AffixId = "ember_damage", Tier = 5, Roll01 = 1f };
            Assert.AreEqual(0.45f, ItemMath.AffixValue(_cat, Item(), a, EquipSlot.MainHand, "sword"), 1e-5f);
        }

        [Test]
        public void BaseSpecificMultiplierWinsOverSlotWide()
        {
            _cat.SlotMultiplierList.Add(new SlotMultiplierData
            {
                Id = "mh_axe", Slot = EquipSlot.MainHand, Multiplier = 2f, BaseId = "axe", AffixIds = { "ember_damage" }
            });
            Assert.AreEqual(2f, ItemMath.SlotMultiplier(_cat, EquipSlot.MainHand, "ember_damage", "axe"));
            Assert.AreEqual(1.5f, ItemMath.SlotMultiplier(_cat, EquipSlot.MainHand, "ember_damage", "sword"));
            Assert.AreEqual(1f, ItemMath.SlotMultiplier(_cat, EquipSlot.Head, "ember_damage", "sword"));
        }

        [Test]
        public void Upgrade10_Scales30Percent_OnAffixAndImplicit()
        {
            var a = new RolledAffix { AffixId = "ember_damage", Tier = 5, Roll01 = 1f };
            Assert.AreEqual(0.30f * 1.30f, ItemMath.AffixValue(_cat, Item(10), a, EquipSlot.Head, "x"), 1e-5f);
            var b = _cat.GetBase("sword");
            float baseImpl = ItemMath.ImplicitValue(_cat, Item(0, 100, 1f), b);
            Assert.AreEqual(0.10f, baseImpl, 1e-5f);                       // 0.20 x 0.5
            Assert.AreEqual(baseImpl * 1.30f, ItemMath.ImplicitValue(_cat, Item(10, 100, 1f), b), 1e-5f);
        }

        [Test]
        public void ImplicitValue_RollLimits_AndNoImplicit()
        {
            var b = _cat.GetBase("sword");
            Assert.AreEqual(0.05f, ItemMath.ImplicitValue(_cat, Item(0, 100, 0f), b), 1e-5f);
            Assert.AreEqual(0f, ItemMath.ImplicitValue(_cat, Item(), new ItemBaseData { Id = "none" }), 1e-6f);
        }

        [Test]
        public void ImplicitValue_SlotMultiplierOnlyWhenFlagged()
        {
            _cat.SlotMultiplierList.Add(new SlotMultiplierData
            {
                Id = "mh_impl", Slot = EquipSlot.MainHand, Multiplier = 2f, AppliesToImplicit = true, AffixIds = { "impl_aff" }
            });
            Assert.AreEqual(0.20f, ItemMath.ImplicitValue(_cat, Item(0, 100, 1f), _cat.GetBase("sword")), 1e-5f);
        }

        [Test]
        public void ArmorImplicit_Formula()
        {
            // 4 x 100 x 1.0 x 1.5 (Plate)
            Assert.AreEqual(600f, ItemMath.ArmorImplicit(100, 1f, 1.5f, _cat.ArmorImplicitPerILvl), 1e-3f);
            Assert.AreEqual(240f, ItemMath.ArmorImplicit(100, 1f, 0.6f, 4f), 1e-3f);
        }
    }
}
