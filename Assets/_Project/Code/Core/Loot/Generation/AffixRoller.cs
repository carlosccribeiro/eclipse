using System;
using Eclipse.Core.Items;
using Eclipse.Core.Random;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// Steps 7 and 8 of 05 section 6.2 for Common/Rare/Epic: N random affixes from the slot pool, weighted, without repetition.
    /// Weight = slot weight (02 section 3.9.3) x relevance (not relevant to the hero skills: x0.25; primaries by role: Core x2 / Secondary x1 / Tertiary x0.5, R30).
    /// Exclusions: group limits of <see cref="GeneratorRules"/>; pool restriction codes (below). Roll01 = <see cref="Sampling.RollQuality"/>.
    /// <para>Restriction codes (affix_pools.csv): <c>weapon_tag:Tag</c> (Main Hand base must carry the tag; ignored in other slots),
    /// <c>base:warbrand</c> (Off Hand must be a base named warbrand; ignored in other slots), <c>hero:orun_except_ring_relic</c>
    /// (Ring/Relic open; other slots need hero "orun"), <c>relic_family_affinity</c> (Relic: the affix affinity must be in the base tags).
    /// Unknown codes throw (data error).</para>
    /// Not thread-safe (reusable buffers).
    /// </summary>
    public sealed class AffixRoller
    {
        private static readonly TagMask AffinityTags = new TagMask(Tag.Ember, Tag.Frost, Tag.Storm, Tag.Void, Tag.Echo, Tag.Ruin);
        private readonly IItemCatalog _items;
        private readonly ILootCatalog _loot;
        private readonly IModifierPoolProvider _mods;
        private readonly GeneratorRules _rules;
        private AffixData[] _aff = new AffixData[64];
        private float[] _base = new float[64];
        private float[] _w = new float[64];
        private bool[] _taken = new bool[64];

        public AffixRoller(IItemCatalog items, ILootCatalog loot, IModifierPoolProvider mods, GeneratorRules rules)
        {
            _items = items; _loot = loot; _mods = mods; _rules = rules ?? new GeneratorRules();
        }

        public void RollRandom(ItemInstance item, ItemBaseData baseData, EquipSlot slot, string hero, int count, int tier,
            float rollMin, ref Xoshiro128StarStar rng)
        {
            var pool = _items.Pool;
            Sampling.Ensure(ref _aff, pool.Count);
            Sampling.Ensure(ref _base, pool.Count);
            Sampling.Ensure(ref _w, pool.Count);
            Sampling.Ensure(ref _taken, pool.Count);
            var smart = _loot.SmartWeights;

            int n = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                var entry = pool[i];
                float sw = entry.SlotWeights[(int)slot];
                if (sw <= 0f) continue;
                var a = _items.GetAffix(entry.AffixId);
                if (a == null || !RestrictionOk(entry.Restriction, slot, baseData, a, hero)) continue;
                _aff[n] = a;
                _base[n] = sw * Relevance(a, hero, smart);
                _taken[n] = false;
                n++;
            }

            int primary = 0, affinity = 0, comfort = 0, resist = 0;
            int maxPrimary = MaxPrimary(slot);
            bool limitResist = item.Rarity == Rarity.Common || item.Rarity == Rarity.Rare;
            for (int pick = 0; pick < count; pick++)
            {
                for (int k = 0; k < n; k++)
                {
                    var a = _aff[k];
                    bool ok = !_taken[k]
                        && !(IsPrimary(a) && primary >= maxPrimary)
                        && !(IsAffinity(a) && affinity >= _rules.MaxAffinity)
                        && !(a.Group == AffixGroup.Comfort && comfort >= _rules.MaxComfort)
                        && !(limitResist && a.Group == AffixGroup.SingleResistance && resist >= _rules.MaxSingleResistanceCommonRare);
                    _w[k] = ok ? _base[k] : 0f;
                }
                int idx = Sampling.PickWeighted(_w, n, ref rng);
                if (idx < 0) break; // pool exhausted
                var chosen = _aff[idx];
                _taken[idx] = true;
                if (IsPrimary(chosen)) primary++;
                if (IsAffinity(chosen)) affinity++;
                if (chosen.Group == AffixGroup.Comfort) comfort++;
                if (chosen.Group == AffixGroup.SingleResistance) resist++;
                item.Affixes.Add(new RolledAffix
                {
                    AffixId = chosen.Id,
                    Tier = (byte)tier,
                    Roll01 = Sampling.RollQuality(ref rng, rollMin),
                    Named = false,
                    Band = NamedBand.None
                });
            }
        }

        private int MaxPrimary(EquipSlot slot)
        {
            switch (slot)
            {
                case EquipSlot.Ring:
                case EquipSlot.OffHand:
                case EquipSlot.Relic: return 0;
                case EquipSlot.Amulet: return _rules.MaxPrimaryAmulet;
                default: return _rules.MaxPrimary;
            }
        }

        private static bool IsPrimary(AffixData a) => a.Group == AffixGroup.Primary || a.Category == AffixCategory.Primary;
        private static bool IsAffinity(AffixData a) => a.Group == AffixGroup.Affinity || a.Category == AffixCategory.Infusion;

        private float Relevance(AffixData a, string hero, SmartLootWeights w)
        {
            if (IsPrimary(a))
            {
                switch (_mods.GetPrimaryRole(hero, a))
                {
                    case PrimaryRole.Core: return w.PrimaryCore;
                    case PrimaryRole.Secondary: return w.PrimarySecondary;
                    default: return w.PrimaryTertiary;
                }
            }
            // Affixes that do not care about skill tags (Armor, Life...) are always relevant.
            if (a.RelevanceTags.IsEmpty) return 1f;
            return _mods.IsAffixRelevant(hero, a) ? 1f : w.IrrelevantAffix;
        }

        private static bool RestrictionOk(string restriction, EquipSlot slot, ItemBaseData b, AffixData a, string hero)
        {
            if (string.IsNullOrEmpty(restriction)) return true;
            if (restriction.StartsWith("weapon_tag:", StringComparison.Ordinal))
            {
                if (slot != EquipSlot.MainHand) return true;
                if (!TagMask.TryParseTag(restriction.Substring("weapon_tag:".Length), out var tag))
                    throw new InvalidOperationException("Unknown tag in pool restriction: " + restriction);
                return b != null && b.Tags.Has(tag);
            }
            switch (restriction)
            {
                case "base:warbrand":
                    return slot != EquipSlot.OffHand || (b != null
                        && (string.Equals(b.Category, "warbrand", StringComparison.OrdinalIgnoreCase)
                            || b.Id.EndsWith("warbrand", StringComparison.OrdinalIgnoreCase)));
                case "hero:orun_except_ring_relic":
                    return slot == EquipSlot.Ring || slot == EquipSlot.Relic || string.Equals(hero, "orun", StringComparison.OrdinalIgnoreCase);
                case "relic_family_affinity":
                    return slot != EquipSlot.Relic || a.Affinity.IsEmpty || (b != null && b.Tags.HasAny(a.Affinity & AffinityTags));
                default:
                    throw new InvalidOperationException("Unknown pool restriction: " + restriction);
            }
        }
    }
}
