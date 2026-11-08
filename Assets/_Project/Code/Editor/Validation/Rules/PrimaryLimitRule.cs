using Eclipse.Core.Items;
using Eclipse.Data.Items;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// Primary attribute limits per named item (02 section 3.9.3): at most <c>max_primary_per_item</c>, Amulet <c>max_primary_amulet</c>,
    /// none on Rings. Counts all fixed affixes plus one Open Affix option. Limits come from loot_global.csv.
    /// </summary>
    [ValidationRule]
    public sealed class PrimaryLimitRule : IValidationRule
    {
        /// <summary>Rings never carry a primary attribute (02 section 3.9.3).</summary>
        public const int RingMaxPrimary = 0;

        public string Name => "PrimaryLimit";

        public void Validate(ValidationContext context)
        {
            var index = new ItemRuleIndex(context);
            if (index.NamedItems.Count == 0) return;

            if (!index.Globals.TryGet(LootGlobalKeys.MaxPrimaryPerItem, out float perItem)
                || !index.Globals.TryGet(LootGlobalKeys.MaxPrimaryAmulet, out float amulet))
            {
                context.Report.Error(Name, null, "loot_global is missing '" + LootGlobalKeys.MaxPrimaryPerItem + "' or '"
                    + LootGlobalKeys.MaxPrimaryAmulet + "'");
                return;
            }

            foreach (NamedItemDefinition item in index.NamedItems)
            {
                if (!index.Bases.TryGetValue(item.BaseId ?? string.Empty, out ItemBaseDefinition baseDef)) continue;
                int limit = baseDef.Slot == EquipSlot.Ring ? RingMaxPrimary
                    : baseDef.Slot == EquipSlot.Amulet ? (int)amulet : (int)perItem;
                int count = ItemRuleIndex.EffectiveCount(item, e => index.IsPrimary(e.affixId));
                if (count > limit)
                    context.Report.Error(Name, item, "item has " + count + " primary attributes on " + baseDef.Slot + ", maximum is " + limit);
            }
        }
    }
}
