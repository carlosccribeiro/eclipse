using System;

namespace Eclipse.Data.Loot
{
    /// <summary>
    /// Keys of loot_global.csv (schema doc docs/data-schemas/items.md, section 15). Names only; values live in the CSV.
    /// </summary>
    public static class LootGlobalKeys
    {
        public const string QuantityCap = "quantity_cap";
        public const string UndiscoveredUniqueWeight = "undiscovered_unique_weight";
        public const string RequiredLevelFactor = "required_level_factor";
        public const string RequiredLevelMax = "required_level_max";
        public const string SmartCurrentHeroGeneric = "smart_current_hero_generic";
        public const string SmartCurrentHeroLegendary = "smart_current_hero_legendary";
        public const string WeightIrrelevantAffix = "weight_irrelevant_affix";
        public const string WeightPrimaryCore = "weight_primary_core";
        public const string WeightPrimarySecondary = "weight_primary_secondary";
        public const string WeightPrimaryTertiary = "weight_primary_tertiary";
        public const string EpicHeroModifierShare = "epic_hero_modifier_share";
        public const string EquippedSkillMultiplier = "equipped_skill_multiplier";
        public const string ArmorImplicitPerILvl = "armor_implicit_per_ilvl";
        public const string UpgradePerLevel = "upgrade_per_level";
        public const string MaxPrimaryPerItem = "max_primary_per_item";
        public const string MaxPrimaryAmulet = "max_primary_amulet";
        public const string MaxAffinityPerItem = "max_affinity_per_item";
        public const string SlotWeightPrefix = "slot_weight.";

        /// <summary>Key of the slot weight for an <c>EquipSlot</c> name in snake_case (<c>main_hand</c>).</summary>
        public static readonly string[] SlotWeightSuffixes =
        {
            "main_hand", "off_hand", "head", "chest", "hands", "legs", "feet", "ring", "amulet", "relic",
        };

        /// <summary>Every key that must exist once the table has rows.</summary>
        public static string[] AllRequired()
        {
            string[] fixedKeys =
            {
                QuantityCap, UndiscoveredUniqueWeight, RequiredLevelFactor, RequiredLevelMax, SmartCurrentHeroGeneric,
                SmartCurrentHeroLegendary, WeightIrrelevantAffix, WeightPrimaryCore, WeightPrimarySecondary, WeightPrimaryTertiary,
                EpicHeroModifierShare, EquippedSkillMultiplier, ArmorImplicitPerILvl, UpgradePerLevel,
                MaxPrimaryPerItem, MaxPrimaryAmulet, MaxAffinityPerItem,
            };
            var all = new string[fixedKeys.Length + SlotWeightSuffixes.Length];
            Array.Copy(fixedKeys, all, fixedKeys.Length);
            for (int i = 0; i < SlotWeightSuffixes.Length; i++)
                all[fixedKeys.Length + i] = SlotWeightPrefix + SlotWeightSuffixes[i];
            return all;
        }
    }
}
