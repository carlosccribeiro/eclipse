using System;

namespace Eclipse.Core.Items
{
    /// <summary>
    /// Derived item values (01 section 3.9; 02 section 3.9.1; 00 R24, R31). Values are computed at read time and never saved.
    /// Allocation-free. All numbers come from the catalog/arguments (data); only structural formulas live here.
    /// </summary>
    public static class ItemMath
    {
        /// <summary>Number of iLvl per tier (01 section 3.9.1: Tier = ceil(iLvl / 20)).</summary>
        private const int ILvlPerTier = 20;
        private const int MinTier = 1;
        private const int MaxTier = 5;

        /// <summary>Tier = ceil(iLvl / 20) clamped to 1..5 (01 section 3.9.1).</summary>
        public static int Tier(int itemLevel)
        {
            int t = (itemLevel + ILvlPerTier - 1) / ILvlPerTier;
            return t < MinTier ? MinTier : (t > MaxTier ? MaxTier : t);
        }

        /// <summary>Required level = clamp(ceil(iLvl * factor), 1, max) (05 section 4.3). Factor and max are data.</summary>
        public static int RequiredLevel(int itemLevel, float factor, int max)
        {
            int r = (int)Math.Ceiling(itemLevel * factor);
            return r < 1 ? 1 : (r > max ? max : r);
        }

        /// <summary>Slot multiplier for an affix on a slot/base (1 when no rule matches). A base-specific rule wins over a slot-wide one.</summary>
        public static float SlotMultiplier(IItemCatalog cat, EquipSlot slot, string affixId, string baseId)
        {
            var list = cat.SlotMultipliers;
            float generic = 1f;
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m.Slot != slot || !m.AffixIds.Contains(affixId)) continue;
                if (string.IsNullOrEmpty(m.BaseId)) generic = m.Multiplier;
                else if (m.BaseId == baseId) return m.Multiplier;
            }
            return generic;
        }

        /// <summary>Upgrade factor 1 + perLevel * level (R31). Applies to attributes and implicit only, never to modifiers.</summary>
        public static float UpgradeFactor(IItemCatalog cat, int upgradeLevel) => 1f + cat.UpgradePerLevel * upgradeLevel;

        /// <summary>Effective value of a rolled attribute (fraction for percent affixes).</summary>
        public static float AffixValue(IItemCatalog cat, ItemInstance item, RolledAffix a, EquipSlot slot, string baseId)
        {
            var tier = cat.GetAffix(a.AffixId).GetTier(a.Tier);
            float slotMult = SlotMultiplier(cat, slot, a.AffixId, baseId);
            float v;
            if (a.Named)
            {
                var band = cat.GetBandRange(a.Band);
                v = Lerp(band.Lo * tier.Max, band.Hi * tier.Max, a.Roll01);
            }
            else
            {
                v = Lerp(tier.Min, tier.Max, a.Roll01);
            }
            return v * slotMult * UpgradeFactor(cat, item.UpgradeLevel);
        }

        /// <summary>
        /// Base implicit (02 section 3.9 rule 11): lerp over the tier range of the implicit affix x ImplicitFactor,
        /// x slot multiplier only when the rule has AppliesToImplicit, x upgrade. Returns 0 when the base has no implicit affix.
        /// </summary>
        public static float ImplicitValue(IItemCatalog cat, ItemInstance item, ItemBaseData baseData)
        {
            if (baseData == null || string.IsNullOrEmpty(baseData.ImplicitAffixId)) return 0f;
            var tier = cat.GetAffix(baseData.ImplicitAffixId).GetTier(Tier(item.ItemLevel));
            float v = Lerp(tier.Min, tier.Max, item.ImplicitRoll01) * baseData.ImplicitFactor;
            var list = cat.SlotMultipliers;
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (!m.AppliesToImplicit || m.Slot != baseData.Slot || !m.AffixIds.Contains(baseData.ImplicitAffixId)) continue;
                if (string.IsNullOrEmpty(m.BaseId) || m.BaseId == baseData.Id) { v *= m.Multiplier; break; }
            }
            return v * UpgradeFactor(cat, item.UpgradeLevel);
        }

        /// <summary>Armor implicit = perILvl (4) x iLvl x slot mult x family factor (01 section 3.9.3, R30). Not rolled.</summary>
        public static float ArmorImplicit(int itemLevel, float slotMult, float familyFactor, float perILvl)
            => perILvl * itemLevel * slotMult * familyFactor;

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
