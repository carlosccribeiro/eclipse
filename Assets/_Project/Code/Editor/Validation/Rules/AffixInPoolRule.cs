using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// Every affix of a named item (fixed and Open Affix options) must have a positive weight in the pool of the base's slot
    /// (02 section 3.9.3), unless it is marked <c>*</c> (then it must be outside the pool, otherwise a warning) or is one of the
    /// unmarked exceptions of <see cref="OffPoolAffixRule.Exceptions"/> (U1 DUR, U3 HEF).
    /// </summary>
    [ValidationRule]
    public sealed class AffixInPoolRule : IValidationRule
    {
        public string Name => "AffixInPool";

        public void Validate(ValidationContext context)
        {
            var index = new ItemRuleIndex(context);
            foreach (NamedItemDefinition item in index.NamedItems)
            {
                if (!index.Bases.TryGetValue(item.BaseId ?? string.Empty, out ItemBaseDefinition baseDef))
                {
                    context.Report.Error(Name, item, "base '" + item.BaseId + "' does not exist");
                    continue;
                }

                foreach (NamedAffixEntry e in ItemRuleIndex.AllEntries(item))
                {
                    if (!index.Affixes.ContainsKey(e.affixId ?? string.Empty))
                    {
                        context.Report.Error(Name, item, "affix '" + e.affixId + "' does not exist");
                        continue;
                    }
                    if (!index.PoolByAffix.ContainsKey(e.affixId))
                    {
                        context.Report.Error(Name, item, "affix '" + e.affixId + "' has no row in affix_pools");
                        continue;
                    }

                    bool inPool = index.IsInPool(e.affixId, baseDef.Slot);
                    if (e.offPool)
                    {
                        if (inPool)
                            context.Report.Warning(Name, item, "'" + e.affixId + "' is marked * but is in the " + baseDef.Slot + " pool");
                    }
                    else if (!inPool && !OffPoolAffixRule.IsException(item.CatalogNo, index.AbbrevOf(e.affixId),
                                 OffPoolAffixRule.ExceptionKind.UnmarkedOpenOption))
                    {
                        context.Report.Error(Name, item, "affix '" + e.affixId + "' is not in the " + baseDef.Slot + " pool and is not marked *");
                    }
                }
            }
        }
    }
}
