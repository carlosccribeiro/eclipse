using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Core.Items;
using Eclipse.Data.Definitions;
using Eclipse.Data.Items;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.Validation
{
    /// <summary>Lookup tables over the item/loot definitions of a <see cref="ValidationContext"/>, shared by the M12 rules (not a rule itself).</summary>
    public sealed class ItemRuleIndex
    {
        public Dictionary<string, AffixDefinition> Affixes { get; } = new Dictionary<string, AffixDefinition>(StringComparer.Ordinal);
        public Dictionary<string, ItemBaseDefinition> Bases { get; } = new Dictionary<string, ItemBaseDefinition>(StringComparer.Ordinal);
        public Dictionary<string, AffixPoolDefinition> PoolByAffix { get; } = new Dictionary<string, AffixPoolDefinition>(StringComparer.Ordinal);
        public Dictionary<NamedBand, NamedBandDefinition> Bands { get; } = new Dictionary<NamedBand, NamedBandDefinition>();
        public HashSet<string> CountingInfusionAffixes { get; } = new HashSet<string>(StringComparer.Ordinal);
        public List<NamedItemDefinition> NamedItems { get; } = new List<NamedItemDefinition>();
        public LootGlobalTable Globals { get; }

        public ItemRuleIndex(ValidationContext context)
        {
            foreach (Definition d in context.Definitions)
            {
                switch (d)
                {
                    case AffixDefinition a: Affixes[a.StableIdString] = a; break;
                    case ItemBaseDefinition b: Bases[b.StableIdString] = b; break;
                    case AffixPoolDefinition p: PoolByAffix[p.AffixId ?? string.Empty] = p; break;
                    case NamedBandDefinition n: Bands[n.Band] = n; break;
                    case InfusionDefinition i:
                        if (i.CountsAsAffinity) CountingInfusionAffixes.Add(i.AffixId ?? string.Empty);
                        break;
                    case NamedItemDefinition item: NamedItems.Add(item); break;
                }
            }
            Globals = new LootGlobalTable(context.Definitions.OfType<LootGlobalDefinition>());
        }

        /// <summary>Fixed affixes followed by the Open Affix options.</summary>
        public static IEnumerable<NamedAffixEntry> AllEntries(NamedItemDefinition item) => item.FixedAffixes.Concat(item.OpenOptions);

        /// <summary>True when the affix has a positive weight in the slot's pool.</summary>
        public bool IsInPool(string affixId, EquipSlot slot) =>
            affixId != null && PoolByAffix.TryGetValue(affixId, out AffixPoolDefinition p)
            && p.SlotWeights != null && (int)slot < p.SlotWeights.Length && p.SlotWeights[(int)slot] > 0f;

        public string AbbrevOf(string affixId) =>
            affixId != null && Affixes.TryGetValue(affixId, out AffixDefinition a) ? a.Abbrev : null;

        public bool IsPrimary(string affixId) =>
            affixId != null && Affixes.TryGetValue(affixId, out AffixDefinition a)
            && (a.Category == AffixCategory.Primary || a.Group == AffixGroup.Primary);

        public bool IsAffinity(string affixId) =>
            affixId != null && ((Affixes.TryGetValue(affixId, out AffixDefinition a) && a.Group == AffixGroup.Affinity)
                                || CountingInfusionAffixes.Contains(affixId));

        /// <summary>Counts the entries matching a predicate in an item as generated: all fixed, plus one open option (the worst one).</summary>
        public static int EffectiveCount(NamedItemDefinition item, Func<NamedAffixEntry, bool> predicate)
        {
            int fixedCount = item.FixedAffixes.Count(predicate);
            bool anyOpen = item.OpenOptions.Any(predicate);
            return fixedCount + (anyOpen ? 1 : 0);
        }
    }
}
