using System.Linq;
using Eclipse.Core.Items;
using Eclipse.Core.Loot;
using Eclipse.Core.Tags;
using Eclipse.Data.Loot;
using Eclipse.Editor.DataPipeline;
using NUnit.Framework;

namespace Eclipse.Data.Items.Tests
{
    public class LootImporterTests : ItemImportTestBase
    {
        [Test]
        public void LootSource_ConvertsPercentAndParsesFlags()
        {
            Write("loot_sources.csv", Csv(
                "id,name_pt,name_en,source,drop_chance_pct,guaranteed_items,extra_item_chance_pct,extra_items_if_all_challenges,choose_of,guaranteed_epic_plus,rarity_floor,affected_by_quantity,ilvl_bonus,ilvl_from_range_top,consumes_hard_pity,source_doc",
                "lootsrc.core.elite,Elite,Elite,Elite,100,1,30,0,0,0,Common,true,1,false,true,05 §3.1",
                "lootsrc.core.bad,Bad,Bad,Boss,100,1,30,0,0,1,Mythic,true,1,false,true,05 §3.1"));
            ImportContext ctx = Run(new LootSourceImporter("loot_sources.csv", Sub("LootSources")));
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            LootSourceDefinition d = LoadAll<LootSourceDefinition>(Sub("LootSources")).Single();
            Assert.AreEqual(LootSourceKind.Elite, d.Source);
            Assert.AreEqual(1f, d.DropChance, 1e-6f);
            Assert.AreEqual(0.3f, d.ExtraItemChance, 1e-6f);
            Assert.AreEqual(Rarity.Common, d.RarityFloor);
            Assert.IsTrue(d.AffectedByQuantity);
            Assert.IsFalse(d.ILvlFromRangeTop);
            Assert.IsTrue(d.ConsumesHardPity);
            Assert.AreEqual(1, d.ILvlBonus);
        }

        [Test]
        public void LootChance_EmptyEpicAndRareBecomeFloorMarker()
        {
            Write("loot_chances.csv", Csv(
                "id,name_pt,name_en,source,leg_normal,leg_veteran,leg_nightmare,leg_eclipse,leg_a1,leg_a100,epic_normal,epic_veteran,epic_nightmare,epic_eclipse,epic_a1,epic_a100,rare_normal,rare_veteran,rare_nightmare,rare_eclipse,rare_a1,rare_a100,source_doc",
                "lootchance.core.trash,Trash,Trash,Trash,0.1,0.2,0.3,0.4,0.5,0.6,2,3,4,5,6,7,25,25,25,25,25,25,05 §5.2",
                "lootchance.core.reliquary,Reliquary,Reliquary,Reliquary,1,2,3,4,5,6" + new string(',', 12) + ",05 §5.3"));
            ImportContext ctx = Run(new LootChanceImporter("loot_chances.csv", Sub("LootChances")));
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));
            var all = LoadAll<LootChanceDefinition>(Sub("LootChances")).ToDictionary(d => d.Source);
            Assert.AreEqual(0.001f, all[LootSourceKind.Trash].Legendary[0], 1e-6f);
            Assert.AreEqual(0.07f, all[LootSourceKind.Trash].Epic[5], 1e-6f);
            Assert.AreEqual(0.25f, all[LootSourceKind.Trash].Rare[2], 1e-6f);
            Assert.Less(all[LootSourceKind.Reliquary].Epic[0], 0f);
            Assert.Less(all[LootSourceKind.Reliquary].Rare[3], 0f);
            Assert.AreEqual(0.06f, all[LootSourceKind.Reliquary].Legendary[5], 1e-6f);
        }

        [Test]
        public void IlvlRange_ImportsAndRejectsInvertedRange()
        {
            Write("ilvl_ranges.csv", Csv(
                "id,name_pt,name_en,difficulty,region_id,ilvl_min,ilvl_max,source_doc",
                "ilvl.veteran.frozen_cathedral,V,V,Veteran,frozen_cathedral,30,45,05 §4.1",
                "ilvl.eclipse.all,E,E,Eclipse,,80,90,05 §4.1",
                "ilvl.normal.bad,N,N,Normal,x,20,10,05 §4.1"));
            ImportContext ctx = Run(new IlvlRangeImporter("ilvl_ranges.csv", Sub("IlvlRanges")));
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            var all = LoadAll<IlvlRangeDefinition>(Sub("IlvlRanges")).ToDictionary(d => d.StableIdString);
            Assert.AreEqual(2, all.Count);
            Assert.AreEqual(30, all["ilvl.veteran.frozen_cathedral"].ILvlMin);
            Assert.AreEqual("frozen_cathedral", all["ilvl.veteran.frozen_cathedral"].RegionId);
            Assert.IsTrue(string.IsNullOrEmpty(all["ilvl.eclipse.all"].RegionId));
        }

        private const string DifficultyHeader =
            "id,name_pt,name_en,difficulty,quantity_bonus_pct,quantity_bonus_per_ascension_pct,set_share_pct,standard_share_pct,unique_share_pct,roll_floor_pct,roll_floor_per_ascension_pct,ascension_base_ilvl,ascension_max_ilvl,ascension_ilvl_cap_level,ascension_primal_min_level,primal_base_chance_pct,primal_chance_per_level_pct,primal_roll_min_pct,source_doc";

        [Test]
        public void LootDifficulty_ConvertsPercents_AndSharesMustSumTo100()
        {
            Write("loot_difficulty.csv", Csv(DifficultyHeader,
                "lootdiff.core.nightmare,N,N,Nightmare,20,0,40,42,18,20,0,0,0,0,0,0,0,0,05 §5.5",
                "lootdiff.core.ascension,A,A,Ascension,25,0.4,40,40,40,25,0.4,85,100,90,50,2,0.16,85,05 §10"));
            ImportContext ctx = Run(new LootDifficultyImporter("loot_difficulty.csv", Sub("LootDifficulty")));
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            LootDifficultyDefinition d = LoadAll<LootDifficultyDefinition>(Sub("LootDifficulty")).Single();
            Assert.AreEqual(Difficulty.Nightmare, d.Difficulty);
            Assert.AreEqual(0.2f, d.QuantityBonus, 1e-6f);
            Assert.AreEqual(0.4f, d.SetShare, 1e-6f);
            Assert.AreEqual(0.18f, d.UniqueShare, 1e-6f);
            Assert.AreEqual(0.2f, d.RollFloor, 1e-6f);
        }

        [Test]
        public void Pity_ImportsAllColumns()
        {
            Write("pity.csv", Csv(
                "id,name_pt,name_en,difficulty,soft_start,hard,soft_step_pct,legendary_cap_mult,set_pity_after,dup_unowned_weight,boss_unique_chance_pct,boss_unique_chance_a100_pct,boss_unique_guarantee_kills,boss_echo_mult,first_kill_set_chance_pct,source_doc",
                "pity.core.veteran,V,V,Veteran,50,100,5,2,4,3,8,8,10,1.5,50,05 §8.1",
                "pity.core.bad,B,B,Normal,120,100,5,2,4,3,8,8,10,1.5,50,05 §8.1"));
            ImportContext ctx = Run(new PityImporter("pity.csv", Sub("Pity")));
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            PityDefinition d = LoadAll<PityDefinition>(Sub("Pity")).Single();
            Assert.AreEqual(50, d.SoftStart);
            Assert.AreEqual(100, d.Hard);
            Assert.AreEqual(0.05f, d.SoftStep, 1e-6f);
            Assert.AreEqual(0.08f, d.BossUniqueChance, 1e-6f);
            Assert.AreEqual(1.5f, d.BossEchoMultiplier);
            Assert.AreEqual(0.5f, d.FirstKillSetChance, 1e-6f);
        }

        [Test]
        public void HomeRegion_ImportsSetAndRegionRows()
        {
            Write("home_regions.csv", Csv(
                "id,name_pt,name_en,row_kind,set_id,hero_id,region_id,home_weight,home_boss_weight,attunement_mult,dominant_affinities,affinity_weight,unique_bonus_pp,source_doc",
                "homeregion.set.ember_vanguard,S,S,Set,ember_vanguard,kael,ashen_wastes,4,6,3,,,,05 §9.1",
                "homeregion.region.astral_citadel,R,R,Region,,,astral_citadel,,,,Void|Ruin,2,3,05 §9.2",
                "homeregion.region.bad,R,R,Region,,,,,,,Void,2,0,05 §9.2", // missing region_id
                "homeregion.region.none,R,R,Region,,,uniform,,,,,2,0,05 §9.2")); // no dominant affinity is valid
            ImportContext ctx = Run(new HomeRegionImporter("home_regions.csv", Sub("HomeRegions")));
            Assert.AreEqual(1, ctx.ErrorCount, Errors(ctx));
            var all = LoadAll<HomeRegionDefinition>(Sub("HomeRegions")).ToDictionary(d => d.StableIdString);
            HomeRegionDefinition set = all["homeregion.set.ember_vanguard"];
            Assert.AreEqual(HomeRegionRowKind.Set, set.RowKind);
            Assert.AreEqual("kael", set.HeroId);
            Assert.AreEqual(6f, set.HomeBossWeight);
            Assert.AreEqual(3f, set.AttunementMultiplier);
            HomeRegionDefinition region = all["homeregion.region.astral_citadel"];
            Assert.AreEqual(HomeRegionRowKind.Region, region.RowKind);
            Assert.AreEqual(TagMask.Of(Tag.Void) | TagMask.Of(Tag.Ruin), region.DominantAffinities);
            Assert.AreEqual(0.03f, region.UniqueBonus, 1e-6f);
            Assert.AreEqual(3, all.Count);
            Assert.IsTrue(all["homeregion.region.none"].DominantAffinities.IsEmpty);
        }

        [Test]
        public void LootGlobal_PercentKindIsDividedBy100()
        {
            Write("loot_global.csv", Csv(
                "id,name_pt,name_en,key,value,value_kind,source_doc",
                "lootglobal.core.quantity_cap,Q,Q,quantity_cap,2.0,Number,05 R45",
                "lootglobal.core.upgrade_per_level,U,U,upgrade_per_level,3,Percent,05 R31",
                "lootglobal.core.slot_weight.main_hand,W,W,slot_weight.main_hand,10,Number,05 §6.1"));
            ImportContext ctx = Run(new LootGlobalImporter("loot_global.csv", Sub("LootGlobal")));
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));
            var all = LoadAll<LootGlobalDefinition>(Sub("LootGlobal")).ToDictionary(d => d.Key);
            Assert.AreEqual(2f, all["quantity_cap"].Value);
            Assert.AreEqual(0.03f, all["upgrade_per_level"].Value, 1e-6f);
            Assert.AreEqual(10f, all["slot_weight.main_hand"].Value);
        }
    }
}
