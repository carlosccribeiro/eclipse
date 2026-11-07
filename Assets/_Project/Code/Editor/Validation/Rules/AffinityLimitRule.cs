using Eclipse.Data.Items;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// At most <c>max_affinity_per_item</c> affinity affixes per named item (02 section 3.9.3, R23), counting Infusions
    /// that are flagged <c>counts_as_affinity</c>. Counts all fixed affixes plus one Open Affix option.
    /// </summary>
    [ValidationRule]
    public sealed class AffinityLimitRule : IValidationRule
    {
        public string Name => "AffinityLimit";

        public void Validate(ValidationContext context)
        {
            var index = new ItemRuleIndex(context);
            if (index.NamedItems.Count == 0) return;

            if (!index.Globals.TryGet(LootGlobalKeys.MaxAffinityPerItem, out float limit))
            {
                context.Report.Error(Name, null, "loot_global is missing '" + LootGlobalKeys.MaxAffinityPerItem + "'");
                return;
            }

            foreach (NamedItemDefinition item in index.NamedItems)
            {
                int count = ItemRuleIndex.EffectiveCount(item, e => index.IsAffinity(e.affixId));
                if (count > (int)limit)
                    context.Report.Error(Name, item, "item has " + count + " affinity affixes, maximum is " + (int)limit);
            }
        }
    }
}
