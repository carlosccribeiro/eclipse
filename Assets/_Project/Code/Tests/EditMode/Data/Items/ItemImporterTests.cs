using System.IO;
using System.Linq;
using Eclipse.Core.Items;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Editor.DataPipeline;
using NUnit.Framework;
using UnityEditor;

namespace Eclipse.Data.Items.Tests
{
    public class ItemImporterTests : ItemImportTestBase
    {
        private AffixImporter Affixes() => new AffixImporter("affixes.csv", Sub("Affixes"));
        private ItemBaseImporter Bases() => new ItemBaseImporter("item_bases.csv", Sub("Bases"));

        [Test]
        public void RealHeaders_WithNoRows_ImportWithoutErrorsOrAssets()
        {
            var specs = new (string file, ITableImporter importer)[]
            {
                ("affixes.csv", Affixes()),
                ("slot_multipliers.csv", new SlotMultiplierImporter("slot_multipliers.csv", Sub("SlotMultipliers"))),
                ("affix_pools.csv", new AffixPoolImporter("affix_pools.csv", Sub("AffixPools"))),
                ("infusions.csv", new InfusionImporter("infusions.csv", Sub("Infusions"))),
                ("rarities.csv", new RarityImporter("rarities.csv", Sub("Rarities"))),
                ("named_bands.csv", new NamedBandImporter("named_bands.csv", Sub("NamedBands"))),
                ("item_bases.csv", Bases()),
                ("items_named.csv", new NamedItemImporter("items_named.csv", Sub("Named"))),
                ("loot_sources.csv", new LootSourceImporter("loot_sources.csv", Sub("LootSources"))),
                ("loot_chances.csv", new LootChanceImporter("loot_chances.csv", Sub("LootChances"))),
                ("ilvl_ranges.csv", new IlvlRangeImporter("ilvl_ranges.csv", Sub("IlvlRanges"))),
                ("loot_difficulty.csv", new LootDifficultyImporter("loot_difficulty.csv", Sub("LootDifficulty"))),
                ("pity.csv", new PityImporter("pity.csv", Sub("Pity"))),
                ("home_regions.csv", new HomeRegionImporter("home_regions.csv", Sub("HomeRegions"))),
                ("loot_global.csv", new LootGlobalImporter("loot_global.csv", Sub("LootGlobal"))),
            };
            foreach (var (file, _) in specs)
                Write(file, File.ReadLines(RealCsv(file)).First() + "\n");

            ImportContext ctx = Run(specs.Select(s => s.importer).ToArray());
            Assert.AreEqual(15, ctx.TablesProcessed);
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));
            Assert.AreEqual(0, ctx.Created);
        }

        [Test]
        public void Importers_AreRegisteredWithDependenciesThatExist()
        {
            var context = new ImportContext();
            var importers = ImporterRegistry.Discover(context);
            Assert.AreEqual(0, context.ErrorCount, Errors(context));
            var names = importers.Select(i => i.TableName).ToList();
            foreach (string n in new[] { "Affixes", "SlotMultipliers", "AffixPools", "Infusions", "Rarities", "NamedBands", "ItemBases",
                         "NamedItems", "LootSources", "LootChances", "IlvlRanges", "LootDifficulty", "Pity", "HomeRegions", "LootGlobal" })
                CollectionAssert.Contains(names, n);
        }

        [Test]
        public void Affix_ConvertsPercentToFractionAndKeepsFlat()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixEmberRow, AffixStrengthRow, AffixInfusionRow));
            ImportContext ctx = Run(Affixes());
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));

            var all = LoadAll<AffixDefinition>(Sub("Affixes")).ToDictionary(d => d.StableIdString);
            AffixDefinition ember = all["affix.core.ember_damage"];
            Assert.AreEqual(StatId.EmberDamage, ember.Stat);
            Assert.IsTrue(ember.HasStat);
            Assert.AreEqual(AffixValueKind.Percent, ember.ValueKind);
            Assert.AreEqual(AffixGroup.Affinity, ember.Group);
            Assert.AreEqual(TagMask.Of(Tag.Ember), ember.Affinity);
            Assert.AreEqual(0.01f, ember.Tiers[0], 1e-6f);
            Assert.AreEqual(0.125f, ember.Tiers[9], 1e-6f);
            Assert.AreEqual("01 §3.9.2", ember.SourceDoc);
            Assert.AreEqual("Ember Damage", ember.NameEn);

            Assert.AreEqual(22f, all["affix.core.strength"].Tiers[8], 1e-6f);
            AffixDefinition infusion = all["affix.core.infusion_ember_added"];
            Assert.IsFalse(infusion.HasStat);
            Assert.AreEqual(AffixCategory.Infusion, infusion.Category);
        }

        [Test]
        public void Affix_MinAboveMax_IsAnError()
        {
            string bad = AffixStrengthRow.Replace(",3,6,", ",9,6,");
            Write("affixes.csv", Csv(AffixHeader, bad));
            ImportContext ctx = Run(Affixes());
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            Assert.AreEqual(0, LoadAll<AffixDefinition>(Sub("Affixes")).Count);
        }

        [Test]
        public void Affix_MissingNamePtOrSourceDoc_IsAnError()
        {
            string noName = AffixStrengthRow.Replace("Força", "");
            string noDoc = AffixEmberRow.Replace(",01 §3.9.2", ",");
            Write("affixes.csv", Csv(AffixHeader, noName, noDoc));
            ImportContext ctx = Run(Affixes());
            Assert.AreEqual(2, ctx.ErrorCount, Errors(ctx));
        }

        [Test]
        public void Affix_UnknownStat_IsAnError()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixEmberRow.Replace("EmberDamage", "NotAStat")));
            Assert.AreEqual(1, Run(Affixes()).ErrorCount);
        }

        [Test]
        public void Affix_Reimport_ChangesNothing()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixEmberRow, AffixStrengthRow));
            Run(Affixes());
            var before = LoadAll<AffixDefinition>(Sub("Affixes"))
                .ToDictionary(d => d.StableIdString, d => File.ReadAllBytes(AbsPath(AssetDatabase.GetAssetPath(d))));

            ImportContext ctx = Run(Affixes());
            Assert.AreEqual(0, ctx.Created);
            Assert.AreEqual(0, ctx.Updated);
            Assert.AreEqual(2, ctx.Unchanged);
            foreach (var d in LoadAll<AffixDefinition>(Sub("Affixes")))
                CollectionAssert.AreEqual(before[d.StableIdString], File.ReadAllBytes(AbsPath(AssetDatabase.GetAssetPath(d))));
        }

        [Test]
        public void SlotMultiplier_ParsesListAndResolvesReferences()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixEmberRow, AffixStrengthRow));
            Write("item_bases.csv", Csv(BaseHeader, BaseGreatswordRow));
            Write("slot_multipliers.csv", Csv(
                "id,name_pt,name_en,slot,affix_ids,base_id,multiplier,applies_to_implicit,source_doc",
                "slotmult.main_hand.damage,Dano em Main Hand,Main Hand damage,MainHand,affix.core.ember_damage|affix.core.strength,base.kael.greatsword,1.5,false,01 §3.9.3"));
            ImportContext ctx = Run(Affixes(), Bases(), new SlotMultiplierImporter("slot_multipliers.csv", Sub("SlotMultipliers")));
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));
            SlotMultiplierDefinition d = LoadAll<SlotMultiplierDefinition>(Sub("SlotMultipliers")).Single();
            Assert.AreEqual(EquipSlot.MainHand, d.Slot);
            Assert.AreEqual(2, d.AffixIds.Count);
            Assert.AreEqual(1.5f, d.Multiplier);
            Assert.AreEqual("base.kael.greatsword", d.BaseId);
            Assert.IsFalse(d.AppliesToImplicit);
        }

        [Test]
        public void SlotMultiplier_UnknownAffix_IsAnError()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixEmberRow));
            Write("slot_multipliers.csv", Csv(
                "id,name_pt,name_en,slot,affix_ids,base_id,multiplier,applies_to_implicit,source_doc",
                "slotmult.head.x,X,X,Head,affix.core.missing,,1.25,false,01 §3.9.3"));
            ImportContext ctx = Run(Affixes(), new SlotMultiplierImporter("slot_multipliers.csv", Sub("SlotMultipliers")));
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            StringAssert.Contains("affix.core.missing", Errors(ctx));
        }

        [Test]
        public void AffixPool_ReadsTenWeightsInSlotOrder()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixEmberRow));
            Write("affix_pools.csv", Csv(
                "id,name_pt,name_en,affix_id,w_main_hand,w_off_hand,w_head,w_chest,w_hands,w_legs,w_feet,w_ring,w_amulet,w_relic,restriction,source_doc",
                "pool.core.ember_damage,Pool,Pool,affix.core.ember_damage,20,10,0,0,0,0,0,10,10,20,weapon_tag:Attack,02 §3.9.3"));
            ImportContext ctx = Run(Affixes(), new AffixPoolImporter("affix_pools.csv", Sub("AffixPools")));
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));
            AffixPoolDefinition d = LoadAll<AffixPoolDefinition>(Sub("AffixPools")).Single();
            CollectionAssert.AreEqual(new float[] { 20, 10, 0, 0, 0, 0, 0, 10, 10, 20 }, d.SlotWeights);
            Assert.AreEqual("weapon_tag:Attack", d.Restriction);
        }

        [Test]
        public void AffixPool_NegativeWeight_IsAnError()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixEmberRow));
            Write("affix_pools.csv", Csv(
                "id,name_pt,name_en,affix_id,w_main_hand,w_off_hand,w_head,w_chest,w_hands,w_legs,w_feet,w_ring,w_amulet,w_relic,restriction,source_doc",
                "pool.core.ember_damage,Pool,Pool,affix.core.ember_damage,-1,10,0,0,0,0,0,10,10,20,,02 §3.9.3"));
            Assert.AreEqual(1, Run(Affixes(), new AffixPoolImporter("affix_pools.csv", Sub("AffixPools"))).ErrorCount);
        }

        [Test]
        public void Infusion_ImportsMetadataAndChecksCategory()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixInfusionRow, AffixEmberRow));
            Write("infusions.csv", Csv(
                "id,name_pt,name_en,affix_id,affinity,form,hit_filter,counts_as_affinity,pool_rule,source_doc",
                "infusion.ember.added,Inf,Inf,affix.core.infusion_ember_added,Ember,Added,Basic,true,any,02 §3.9.4",
                "infusion.ember.bad,Inf,Inf,affix.core.ember_damage,Ember,Strikes,,true,any,02 §3.9.4"));
            ImportContext ctx = Run(Affixes(), new InfusionImporter("infusions.csv", Sub("Infusions")));
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            InfusionDefinition d = LoadAll<InfusionDefinition>(Sub("Infusions")).First(x => x.StableIdString == "infusion.ember.added");
            Assert.AreEqual(InfusionForm.Added, d.Form);
            Assert.AreEqual(InfusionHitFilter.Basic, d.HitFilter);
            Assert.IsTrue(d.CountsAsAffinity);
            Assert.AreEqual(InfusionHitFilter.None, LoadAll<InfusionDefinition>(Sub("Infusions")).First(x => x.StableIdString == "infusion.ember.bad").HitFilter);
        }

        [Test]
        public void Rarity_ImportsCounts_AndRejectsZeroILvl()
        {
            Write("rarities.csv", Csv(
                "id,name_pt,name_en,rarity,random_affix_count,modifier_count,min_drop_ilvl,source_doc",
                "rarity.core.epic,Epic,Epic,Epic,3,1,1,02 §3.2",
                "rarity.core.bad,Bad,Bad,Rare,2,0,0,02 §3.2"));
            ImportContext ctx = Run(new RarityImporter("rarities.csv", Sub("Rarities")));
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            RarityDefinition d = LoadAll<RarityDefinition>(Sub("Rarities")).Single();
            Assert.AreEqual(Rarity.Epic, d.Rarity);
            Assert.AreEqual(3, d.RandomAffixCount);
            Assert.AreEqual(1, d.ModifierCount);
        }

        [Test]
        public void NamedBand_ConvertsPercentToFraction_AndRejectsNone()
        {
            Write("named_bands.csv", Csv(
                "id,name_pt,name_en,band,lo_pct,hi_pct,source_doc",
                "band.core.high,Alta,High,High,95,115,02 §3.9.1",
                "band.core.none,Nenhuma,None,None,80,100,02 §3.9.1"));
            ImportContext ctx = Run(new NamedBandImporter("named_bands.csv", Sub("NamedBands")));
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            NamedBandDefinition d = LoadAll<NamedBandDefinition>(Sub("NamedBands")).Single();
            Assert.AreEqual(NamedBand.High, d.Band);
            Assert.AreEqual(0.95f, d.Lo, 1e-6f);
            Assert.AreEqual(1.15f, d.Hi, 1e-6f);
        }

        [Test]
        public void ItemBase_ImportsCoreAndExtraColumns()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixEmberRow, AffixStrengthRow));
            Write("item_bases.csv", Csv(BaseHeader, BaseGreatswordRow, BaseCuirassRow));
            ImportContext ctx = Run(Affixes(), Bases());
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));
            var all = LoadAll<ItemBaseDefinition>(Sub("Bases")).ToDictionary(d => d.StableIdString);
            ItemBaseDefinition sword = all["base.kael.greatsword"];
            Assert.AreEqual(EquipSlot.MainHand, sword.Slot);
            Assert.AreEqual("kael", sword.HeroId);
            Assert.IsTrue(sword.Tags.Has(Tag.Attack) && sword.Tags.Has(Tag.Melee));
            Assert.AreEqual(1.05f, sword.DpsFactor);
            Assert.AreEqual("DodgeChance", sword.SecondaryImplicitStat);
            Assert.AreEqual(ArmorFamily.Plate, all["base.armor.cuirass"].Family);
            Assert.AreEqual(1.5f, all["base.armor.cuirass"].ArmorFactor);
            Assert.IsTrue(string.IsNullOrEmpty(all["base.armor.cuirass"].HeroId));
        }

        [Test]
        public void ItemBase_UnknownImplicitAffix_IsAnError()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixStrengthRow));
            Write("item_bases.csv", Csv(BaseHeader, BaseGreatswordRow));
            ImportContext ctx = Run(Affixes(), Bases());
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            StringAssert.Contains("affix.core.ember_damage", Errors(ctx));
        }

        private const string NamedHeader =
            "id,name_pt,name_en,catalog_no,item_kind,base_id,hero_id,effect_id,fixed_affixes,open_options,modifier_ids,affinity,set_id,unique_source_kind,unique_source_ref,min_drop_ilvl,source_doc";

        private ImportContext RunNamed(string namedRow)
        {
            Write("affixes.csv", Csv(AffixHeader, AffixEmberRow, AffixStrengthRow));
            Write("item_bases.csv", Csv(BaseHeader, BaseGreatswordRow, BaseCuirassRow));
            Write("items_named.csv", Csv(NamedHeader, namedRow));
            return Run(Affixes(), Bases(), new NamedItemImporter("items_named.csv", Sub("Named")));
        }

        [Test]
        public void NamedItem_ParsesAffixListsBandsStarAndModifierCodes()
        {
            ImportContext ctx = RunNamed(
                "item.kael.ashfang,Presa,Ashfang,K1,NamedLegendary,base.kael.greatsword,kael,effect.ashfang,"
                + "affix.core.ember_damage:High|affix.core.strength:Default*,affix.core.strength:Low|affix.core.ember_damage:Default|affix.core.strength:Default,"
                + "KAE-CS-01|GEN-27,Ember,,,,10,02 §3.12.1");
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));
            NamedItemDefinition d = LoadAll<NamedItemDefinition>(Sub("Named")).Single();
            Assert.AreEqual(ItemKind.NamedLegendary, d.ItemKind);
            Assert.AreEqual(2, d.FixedAffixes.Count);
            Assert.AreEqual(NamedBand.High, d.FixedAffixes[0].band);
            Assert.IsFalse(d.FixedAffixes[0].offPool);
            Assert.IsTrue(d.FixedAffixes[1].offPool);
            Assert.AreEqual(NamedBand.Low, d.OpenOptions[0].band);
            CollectionAssert.AreEqual(new[] { "KAE-CS-01", "GEN-27" }, d.ModifierIds);
            Assert.AreEqual(TagMask.Of(Tag.Ember), d.Affinity);
            Assert.AreEqual(10, d.MinDropILvl);
            Assert.AreEqual(UniqueSourceKind.None, d.UniqueSourceKind);
        }

        [Test]
        public void NamedItem_UnknownBand_UnknownAffix_AndGenericKind_AreErrors()
        {
            Write("affixes.csv", Csv(AffixHeader, AffixEmberRow));
            Write("item_bases.csv", Csv(BaseHeader, BaseGreatswordRow));
            Write("items_named.csv", Csv(NamedHeader,
                "item.kael.a,A,A,K1,NamedLegendary,base.kael.greatsword,kael,,affix.core.ember_damage:Top,,KAE-CS-01|GEN-27,,,,,10,02",
                "item.kael.b,B,B,K2,NamedLegendary,base.kael.greatsword,kael,,affix.core.missing:Default,,KAE-CS-01|GEN-27,,,,,10,02",
                "item.kael.c,C,C,K3,Generic,base.kael.greatsword,kael,,affix.core.ember_damage:Default,,KAE-CS-01|GEN-27,,,,,10,02"));
            ImportContext ctx = Run(Affixes(), Bases(), new NamedItemImporter("items_named.csv", Sub("Named")));
            Assert.AreEqual(3, ctx.ErrorCount, Errors(ctx));
        }

        [Test]
        public void NamedItem_UniqueNeedsSource_SetPieceNeedsSetId()
        {
            ImportContext ctx = RunNamed(
                "item.universal.u1,U,U,U1,Unique,base.kael.greatsword,,effect.u1,affix.core.ember_damage:Default,,KAE-CS-01|SIG-K16,,,None,,35,02 §3.12.6");
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
        }

        [Test]
        public void NamedItem_ChallengeUniqueMayHaveEmptySourceRef()
        {
            ImportContext ctx = RunNamed(
                "item.kael.ordeal,U,U,K15,Unique,base.kael.greatsword,kael,effect.u,affix.core.ember_damage:Default,,KAE-AG-04|KAE-AG-02,,,Challenge,,35,02 §3.12.1");
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));
            Assert.AreEqual(UniqueSourceKind.Challenge, LoadAll<NamedItemDefinition>(Sub("Named")).Single().UniqueSourceKind);
        }

        [Test]
        public void NamedItem_Reimport_ChangesNothing()
        {
            string row = "item.kael.ashfang,Presa,Ashfang,K1,NamedLegendary,base.kael.greatsword,kael,effect.ashfang,"
                         + "affix.core.ember_damage:High,affix.core.strength:Low,KAE-CS-01|GEN-27,Ember,,,,10,02 §3.12.1";
            Assert.AreEqual(0, RunNamed(row).ErrorCount);
            ImportContext ctx = Run(Affixes(), Bases(), new NamedItemImporter("items_named.csv", Sub("Named")));
            Assert.AreEqual(0, ctx.Created);
            Assert.AreEqual(0, ctx.Updated);
        }
    }
}
