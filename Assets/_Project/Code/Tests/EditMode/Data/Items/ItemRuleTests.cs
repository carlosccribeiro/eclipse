using System;
using System.IO;
using System.Linq;
using Eclipse.Core.Items;
using Eclipse.Data.Loot;
using Eclipse.Editor.Validation;
using NUnit.Framework;

namespace Eclipse.Data.Items.Tests
{
    public class ItemRuleTests
    {
        private ItemFixtureBuilder _b;
        private string _tempCsv;

        [SetUp]
        public void SetUp()
        {
            _b = new ItemFixtureBuilder();
            _b.Affix("ember", "EMB", AffixCategory.Offensive, AffixGroup.Affinity);
            _b.Affix("frost", "FRO", AffixCategory.Offensive, AffixGroup.Affinity);
            _b.Affix("storm", "STO", AffixCategory.Offensive, AffixGroup.Affinity);
            _b.Affix("str", "STR", AffixCategory.Primary, AffixGroup.Primary);
            _b.Affix("dex", "DEX", AffixCategory.Primary, AffixGroup.Primary);
            _b.Affix("crit", "CRT");
            _b.Affix("armor", "ARM", AffixCategory.Defensive);
            _b.Affix("hp", "HP", AffixCategory.Defensive, inPool: false);
            _b.Affix("blk", "BLK", AffixCategory.Defensive, inPool: false);
            _b.Affix("dur", "DUR", AffixCategory.Utility, inPool: false);
            _b.Affix("hef", "HEF", AffixCategory.Utility, inPool: false);
            _b.Affix("xyz", "XYZ", inPool: false);
            _b.Base("chest", EquipSlot.Chest);
            _b.Base("ring", EquipSlot.Ring);
            _b.Base("amulet", EquipSlot.Amulet);
            _b.Band(NamedBand.Default, 0.8f, 1.0f);
            _b.Band(NamedBand.High, 0.95f, 1.15f);
            _b.Band(NamedBand.Low, 0.7f, 0.8f);
            _b.Global("max_primary_per_item", 1);
            _b.Global("max_primary_amulet", 2);
            _b.Global("max_affinity_per_item", 2);
            ModifierReferenceRule.ModifiersCsvPath = MissingModifiersPath();
        }

        private static string MissingModifiersPath() =>
            Path.Combine(Path.GetTempPath(), "eclipse_no_modifiers_" + Guid.NewGuid().ToString("N") + ".csv");

        [TearDown]
        public void TearDown()
        {
            _b.Dispose();
            ModifierReferenceRule.ModifiersCsvPath = null;
            if (_tempCsv != null && File.Exists(_tempCsv)) File.Delete(_tempCsv);
        }

        private static readonly string[] Open3 = { "crit", "armor", "ember" };

        private NamedItemDefinition Legendary(string slug = "leg", string catalogNo = "K1", string baseSlug = "chest",
            string[] fixedAffixes = null, string[] open = null, int minILvl = 10, ItemKind kind = ItemKind.NamedLegendary) =>
            _b.Item(slug, catalogNo, kind, baseSlug, fixedAffixes ?? new[] { "ember", "crit", "armor", "str" },
                open ?? Open3, ItemFixtureBuilder.TwoModifiers, minILvl);

        private int Count(string rule, ValidationSeverity severity)
        {
            ValidationReport r = DataValidator.Validate(_b.Defs);
            return r.Messages.Count(m => m.Rule == rule && m.Severity == severity);
        }

        private string Messages(string rule) =>
            string.Join("\n", DataValidator.Validate(_b.Defs).Messages.Where(m => m.Rule == rule));

        [Test]
        public void AllRulesAreDiscovered()
        {
            var names = DataValidator.DiscoverRules().Select(r => r.Name).ToList();
            foreach (string n in new[] { "ItemStructure", "AffixInPool", "OffPoolAffix", "NamedBand", "PrimaryLimit", "AffinityLimit",
                         "MinDropILvl", "ModifierReference", "LootGlobalKeys" })
                CollectionAssert.Contains(names, n);
        }

        [Test]
        public void ValidLegendary_PassesEveryItemRule()
        {
            Legendary();
            ValidationReport r = DataValidator.Validate(_b.Defs);
            // The fixture has only 3 loot_global keys on purpose (the limits); LootGlobalKeys is tested separately.
            var errors = r.Messages.Where(m => m.Severity == ValidationSeverity.Error && m.Rule != "LootGlobalKeys").ToList();
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
        }

        // ---- ItemStructure (R25) ----
        [Test]
        public void ItemStructure_LegendaryAndSetNeed4Plus3Plus2()
        {
            Legendary();
            Legendary("set", "S1", kind: ItemKind.SetPiece);
            Assert.AreEqual(0, Count("ItemStructure", ValidationSeverity.Error));

            Legendary("bad", "K9", fixedAffixes: new[] { "ember", "crit", "armor" });
            Assert.AreEqual(1, Count("ItemStructure", ValidationSeverity.Error));
        }

        [Test]
        public void ItemStructure_LegendaryNeedsThreeOpenOptions()
        {
            Legendary("bad", open: new[] { "crit", "armor" });
            Assert.AreEqual(1, Count("ItemStructure", ValidationSeverity.Error));
        }

        [Test]
        public void ItemStructure_UniqueNeeds5FixedNoOpenAndAtMostOneSignature()
        {
            var fixed5 = new[] { "ember", "crit", "armor", "str", "dex" };
            _b.Item("u_ok", "U9", ItemKind.Unique, "chest", fixed5, new string[0], new[] { "SIG-K16", "GEN-27" }, 35);
            Assert.AreEqual(0, Count("ItemStructure", ValidationSeverity.Error));

            _b.Item("u_sig", "U10", ItemKind.Unique, "chest", fixed5, new string[0], new[] { "SIG-K16", "SIG-K17" }, 35);
            Assert.AreEqual(1, Count("ItemStructure", ValidationSeverity.Error));
            StringAssert.Contains("Signature", Messages("ItemStructure"));

            _b.Item("u_open", "U11", ItemKind.Unique, "chest", fixed5, Open3, ItemFixtureBuilder.TwoModifiers, 35);
            Assert.AreEqual(2, Count("ItemStructure", ValidationSeverity.Error));
        }

        [Test]
        public void ItemStructure_NeedsExactlyTwoModifiers()
        {
            _b.Item("one_mod", "K8", ItemKind.NamedLegendary, "chest", new[] { "ember", "crit", "armor", "str" }, Open3,
                new[] { "KAE-CS-01" }, 10);
            Assert.AreEqual(1, Count("ItemStructure", ValidationSeverity.Error));
        }

        // ---- AffixInPool ----
        [Test]
        public void AffixInPool_RejectsAffixOutsidePoolWithoutStar()
        {
            Legendary();
            Assert.AreEqual(0, Count("AffixInPool", ValidationSeverity.Error));

            Legendary("bad", "K8", fixedAffixes: new[] { "ember", "crit", "armor", "xyz" });
            Assert.AreEqual(1, Count("AffixInPool", ValidationSeverity.Error));
        }

        [Test]
        public void AffixInPool_StarOutsidePoolPasses_StarInsidePoolWarns()
        {
            Legendary("star", "K8", fixedAffixes: new[] { "ember", "crit", "armor", "xyz*" });
            Assert.AreEqual(0, Count("AffixInPool", ValidationSeverity.Error));
            Assert.AreEqual(0, Count("AffixInPool", ValidationSeverity.Warning));

            Legendary("star2", "K7", fixedAffixes: new[] { "ember", "crit", "armor", "frost*" });
            Assert.AreEqual(1, Count("AffixInPool", ValidationSeverity.Warning));
        }

        [Test]
        public void AffixInPool_DocumentedUnmarkedOpenOptionExceptions()
        {
            // U1 (DUR) and U3 (HEF): open option outside the pool without a star is tolerated; other items are not.
            Legendary("u1", "U1", open: new[] { "crit", "armor", "dur" });
            Legendary("u3", "U3", open: new[] { "crit", "armor", "hef" });
            Assert.AreEqual(0, Count("AffixInPool", ValidationSeverity.Error));

            Legendary("other", "U2", open: new[] { "crit", "armor", "dur" });
            Assert.AreEqual(1, Count("AffixInPool", ValidationSeverity.Error));
        }

        [Test]
        public void AffixInPool_MissingBaseOrPoolRowIsAnError()
        {
            _b.Defs.RemoveAll(d => d.StableIdString == "pool.core.crit");
            Legendary(); // crit appears in the fixed list and in the open options
            Assert.AreEqual(2, Count("AffixInPool", ValidationSeverity.Error));
            StringAssert.Contains("affix_pools", Messages("AffixInPool"));
        }

        // ---- OffPoolAffix (R23 x 02 2.14) ----
        [Test]
        public void OffPool_SetPieceMayHaveOneStar()
        {
            Legendary("set", "S1", kind: ItemKind.SetPiece, fixedAffixes: new[] { "ember", "crit", "armor", "xyz*" });
            Assert.AreEqual(0, Count("OffPoolAffix", ValidationSeverity.Error));
        }

        [Test]
        public void OffPool_SetPieceWithTwoStarsFails()
        {
            Legendary("set", "S1", kind: ItemKind.SetPiece, fixedAffixes: new[] { "ember", "crit", "xyz*", "hp*" });
            Assert.AreEqual(1, Count("OffPoolAffix", ValidationSeverity.Error));
        }

        [Test]
        public void OffPool_StarOnNonSetFailsExceptDocumentedK3AndK4()
        {
            Legendary("bad", "K9", fixedAffixes: new[] { "ember", "crit", "armor", "xyz*" });
            Assert.AreEqual(1, Count("OffPoolAffix", ValidationSeverity.Error));

            Legendary("k3", "K3", fixedAffixes: new[] { "ember", "crit", "armor", "hp*" });
            Legendary("k4", "K4", fixedAffixes: new[] { "ember", "crit", "armor", "blk*" });
            Assert.AreEqual(1, Count("OffPoolAffix", ValidationSeverity.Error));
        }

        [Test]
        public void OffPool_ExceptionIsKeyedByCatalogNoAndAffix()
        {
            Assert.IsTrue(OffPoolAffixRule.IsException("K3", "HP", OffPoolAffixRule.ExceptionKind.MarkedOffPool));
            Assert.IsFalse(OffPoolAffixRule.IsException("K3", "BLK", OffPoolAffixRule.ExceptionKind.MarkedOffPool));
            Assert.IsTrue(OffPoolAffixRule.IsException("U1", "DUR", OffPoolAffixRule.ExceptionKind.UnmarkedOpenOption));
            Assert.IsFalse(OffPoolAffixRule.IsException("U1", "DUR", OffPoolAffixRule.ExceptionKind.MarkedOffPool));
        }

        // ---- NamedBand (R24) ----
        [Test]
        public void NamedBand_EnvelopeIs70To115()
        {
            Legendary();
            Assert.AreEqual(0, Count("NamedBand", ValidationSeverity.Error));

            _b.Band(NamedBand.Envelope, 0.6f, 1.2f);
            Assert.AreEqual(1, Count("NamedBand", ValidationSeverity.Error));
        }

        [Test]
        public void NamedBand_ItemBandWithoutRowFails()
        {
            _b.Defs.RemoveAll(d => d is NamedBandDefinition n && n.Band == NamedBand.High);
            Legendary("hi", fixedAffixes: new[] { "ember:High", "crit", "armor", "str" });
            Assert.AreEqual(1, Count("NamedBand", ValidationSeverity.Error));
        }

        // ---- PrimaryLimit ----
        [Test]
        public void PrimaryLimit_OnePerItem_TwoOnAmulet_NoneOnRing()
        {
            Legendary(); // 1 primary in fixed, none in open
            Assert.AreEqual(0, Count("PrimaryLimit", ValidationSeverity.Error));

            Legendary("two", "K2", fixedAffixes: new[] { "str", "dex", "crit", "armor" });
            Assert.AreEqual(1, Count("PrimaryLimit", ValidationSeverity.Error));

            Legendary("amu", "K3", baseSlug: "amulet", fixedAffixes: new[] { "str", "dex", "crit", "armor" }); // 2 allowed
            Assert.AreEqual(1, Count("PrimaryLimit", ValidationSeverity.Error));

            Legendary("ring", "K4", baseSlug: "ring"); // has STR on a ring
            Assert.AreEqual(2, Count("PrimaryLimit", ValidationSeverity.Error));
        }

        [Test]
        public void PrimaryLimit_OpenOptionPrimaryCountsAgainstFixedOnes()
        {
            Legendary("open", open: new[] { "crit", "armor", "dex" });
            Assert.AreEqual(1, Count("PrimaryLimit", ValidationSeverity.Error));
        }

        [Test]
        public void PrimaryLimit_MissingGlobalKeysIsAnError()
        {
            _b.Defs.RemoveAll(d => d is LootGlobalDefinition g && g.Key == "max_primary_amulet");
            Legendary();
            Assert.AreEqual(1, Count("PrimaryLimit", ValidationSeverity.Error));
        }

        // ---- AffinityLimit ----
        [Test]
        public void AffinityLimit_MaxTwoAffinityAffixes()
        {
            var open = new[] { "crit", "armor", "dex" };
            Legendary("two", "K2", fixedAffixes: new[] { "ember", "frost", "crit", "armor" }, open: open);
            Assert.AreEqual(0, Count("AffinityLimit", ValidationSeverity.Error));

            Legendary("three", "K3", fixedAffixes: new[] { "ember", "frost", "storm", "armor" }, open: open);
            Assert.AreEqual(1, Count("AffinityLimit", ValidationSeverity.Error));
        }

        [Test]
        public void AffinityLimit_InfusionsCountWhenFlagged()
        {
            AffixDefinition inf = _b.Affix("inf_ember", "INF-EMB-ADD", AffixCategory.Infusion, AffixGroup.None);
            var infusion = _b.Add<InfusionDefinition>("infusion.ember.added");
            infusion.EditorSet(inf.StableIdString, Eclipse.Core.Tags.TagMask.Of(Eclipse.Core.Tags.Tag.Ember), InfusionForm.Added,
                InfusionHitFilter.Basic, true, "any");
            Legendary("inf", "K5", fixedAffixes: new[] { "ember", "frost", "inf_ember", "armor" });
            Assert.AreEqual(1, Count("AffinityLimit", ValidationSeverity.Error));
        }

        // ---- MinDropILvl ----
        [Test]
        public void MinDropILvl_Legendary10_Unique35()
        {
            Legendary("ok", minILvl: 10);
            _b.Item("u_ok", "U9", ItemKind.Unique, "chest", new[] { "ember", "crit", "armor", "str", "dex" }, new string[0],
                new[] { "SIG-K16", "GEN-27" }, 35);
            Assert.AreEqual(0, Count("MinDropILvl", ValidationSeverity.Error));

            Legendary("low", "K8", minILvl: 9);
            _b.Item("u_low", "U8", ItemKind.Unique, "chest", new[] { "ember", "crit", "armor", "str", "dex" }, new string[0],
                new[] { "SIG-K16", "GEN-27" }, 20);
            Assert.AreEqual(2, Count("MinDropILvl", ValidationSeverity.Error));
        }

        // ---- ModifierReference ----
        [Test]
        public void ModifierReference_WarnsOnceWhileModifiersCsvIsMissing()
        {
            ModifierReferenceRule.ModifiersCsvPath = MissingModifiersPath();
            Legendary();
            Legendary("b", "K2");
            Assert.AreEqual(0, Count("ModifierReference", ValidationSeverity.Error));
            Assert.AreEqual(1, Count("ModifierReference", ValidationSeverity.Warning));
        }

        [Test]
        public void ModifierReference_BadFormatIsAnError()
        {
            ModifierReferenceRule.ModifiersCsvPath = MissingModifiersPath();
            _b.Item("bad", "K8", ItemKind.NamedLegendary, "chest", new[] { "ember", "crit", "armor", "str" }, Open3,
                new[] { "modifier.kael.x", "gen-27" }, 10);
            Assert.AreEqual(2, Count("ModifierReference", ValidationSeverity.Error));
        }

        [Test]
        public void ModifierReference_ChecksExistenceOnceCsvExists()
        {
            _tempCsv = Path.Combine(Path.GetTempPath(), "eclipse_modifiers_" + Guid.NewGuid().ToString("N") + ".csv");
            File.WriteAllText(_tempCsv, "id,code\nmod.a,KAE-CS-01\nmod.b,GEN-27\n");
            ModifierReferenceRule.ModifiersCsvPath = _tempCsv;
            Legendary();
            Assert.AreEqual(0, Count("ModifierReference", ValidationSeverity.Error));
            Assert.AreEqual(0, Count("ModifierReference", ValidationSeverity.Warning));

            _b.Item("miss", "K8", ItemKind.NamedLegendary, "chest", new[] { "ember", "crit", "armor", "str" }, Open3,
                new[] { "KAE-CS-01", "GEN-99" }, 10);
            Assert.AreEqual(1, Count("ModifierReference", ValidationSeverity.Error));
        }

        // ---- LootGlobalKeys ----
        [Test]
        public void LootGlobalKeys_PartialTableFails_FullTablePasses_DuplicatesFail()
        {
            Assert.AreEqual(Count("LootGlobalKeys", ValidationSeverity.Error), LootGlobalKeys.AllRequired().Length - 3,
                "3 of the required keys are present in the fixture");

            foreach (string key in LootGlobalKeys.AllRequired())
                if (!_b.Defs.OfType<LootGlobalDefinition>().Any(g => g.Key == key)) _b.Global(key, 1f);
            Assert.AreEqual(0, Count("LootGlobalKeys", ValidationSeverity.Error));

            _b.Global("quantity_cap", 5f); // second row with the same key
            Assert.AreEqual(1, Count("LootGlobalKeys", ValidationSeverity.Error));
        }

        [Test]
        public void LootGlobalKeys_EmptyTableIsIgnored()
        {
            _b.Defs.RemoveAll(d => d is LootGlobalDefinition);
            Assert.AreEqual(0, Count("LootGlobalKeys", ValidationSeverity.Error));
        }
    }
}
