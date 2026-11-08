using System;
using System.Collections.Generic;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Items
{
    /// <summary>Min/max of one tier of an affix, already in stat units (percent as fraction).</summary>
    public readonly struct TierRange
    {
        public readonly float Min;
        public readonly float Max;
        public TierRange(float min, float max) { Min = min; Max = max; }
    }

    /// <summary>Core view of an affix (<c>AffixDefinition</c>, 01 section 3.9.2). Tier ranges T1..T5 are indices 0..4.</summary>
    public sealed class AffixData
    {
        public string Id;
        /// <summary>Stat the affix feeds; null for Infusions (no StatId yet).</summary>
        public StatId? Stat;
        public AffixValueKind ValueKind;
        public AffixCategory Category;
        public AffixGroup Group;
        /// <summary>Affinity tag mask for affinity damage/Infusions/Status Potency; empty otherwise.</summary>
        public TagMask Affinity;
        /// <summary>Tags matched against hero skill tags for the smart-loot relevance weight (05 section 6.2).</summary>
        public TagMask RelevanceTags;
        public TierRange[] Tiers = new TierRange[5];
        public TierRange GetTier(int tier) => Tiers[tier - 1];
    }

    /// <summary>One slot multiplier rule (01 section 3.9.3). Applies to the listed affix ids on a slot (optionally one base).</summary>
    public sealed class SlotMultiplierData
    {
        public string Id;
        public EquipSlot Slot;
        public List<string> AffixIds = new List<string>();
        /// <summary>Empty = any base of the slot.</summary>
        public string BaseId;
        public float Multiplier;
        /// <summary>True when the multiplier also applies to the base implicit (see Log: Warbrand Block).</summary>
        public bool AppliesToImplicit;
    }

    /// <summary>Pool entry: weight of an affix per slot (0 = not in pool) and restriction (02 section 3.9.3).</summary>
    public sealed class AffixPoolEntry
    {
        public string AffixId;
        /// <summary>Indexed by <see cref="EquipSlot"/>.</summary>
        public float[] SlotWeights = new float[10];
        /// <summary>Free-form restriction code from the table (e.g. <c>weapon_tag:Attack</c>); interpreted by the generator.</summary>
        public string Restriction;
    }

    /// <summary>Weapon, armor, jewel or relic base (<c>ItemBaseDefinition</c>, 02 sections 3.4-3.8).</summary>
    public sealed class ItemBaseData
    {
        public string Id;
        public EquipSlot Slot;
        public string Category;
        public ArmorFamily Family;
        /// <summary>Hero that can use the base (StableId string); null = any hero.</summary>
        public string HeroId;
        public TagMask Tags;
        public string ImplicitAffixId;
        /// <summary>Fraction of the affix tier max used as implicit range (02 section 3.4); data.</summary>
        public float ImplicitFactor;
        /// <summary>Armor implicit base factor by family (Plate 1.5 / Leather 1.0 / Vestment 0.6); 0 when not armor.</summary>
        public float ArmorFactor;
        public bool Droppable = true;
    }

    /// <summary>A fixed affix of a named item with its Named band.</summary>
    public readonly struct NamedAffixSpec
    {
        public readonly string AffixId;
        public readonly NamedBand Band;
        /// <summary>The <c>*</c> flag: affix outside the slot pool (02 section 2.14).</summary>
        public readonly bool OffPool;
        public NamedAffixSpec(string affixId, NamedBand band, bool offPool) { AffixId = affixId; Band = band; OffPool = offPool; }
    }

    /// <summary>Named item (<c>ItemDefinition</c>): Legendary, SetPiece or Unique (02 sections 3.11, 3.12).</summary>
    public sealed class NamedItemData
    {
        public string Id;
        public ItemKind Kind;
        public string BaseId;
        /// <summary>Hero binding (StableId string); null = any hero.</summary>
        public string HeroId;
        public string EffectId;
        /// <summary>4 (Legendary/Set) or 5 (Unique) fixed affixes.</summary>
        public List<NamedAffixSpec> FixedAffixes = new List<NamedAffixSpec>();
        /// <summary>The 3 Open Affix options (empty for Unique).</summary>
        public List<NamedAffixSpec> OpenOptions = new List<NamedAffixSpec>();
        /// <summary>Modifier codes from 03 (2 entries; <c>SIG-*</c> allowed in Uniques).</summary>
        public List<string> ModifierIds = new List<string>();
        public TagMask Affinity;
        /// <summary>SetPiece only.</summary>
        public string SetId;
        /// <summary>Unique only: kind/ref of the single source (02 section 3.12.6).</summary>
        public UniqueSourceKind UniqueSource;
        public string UniqueSourceRef;
        public int MinDropILvl;
    }

    /// <summary>Source of a Unique (05 section 9.3).</summary>
    public enum UniqueSourceKind : byte { None, Boss, Pinnacle, Region, Challenge }

    /// <summary>Per-rarity structure (<c>RarityDefinition</c>, 02 sections 3.1-3.2).</summary>
    public sealed class RarityData
    {
        public Rarity Rarity;
        public int RandomAffixCount;
        public int ModifierCount;
        public int MinDropILvl;
    }

    /// <summary>Fraction range of tier max for a Named band (data; 02 section 3.9.1, R24).</summary>
    public readonly struct NamedBandRange
    {
        public readonly float Lo;
        public readonly float Hi;
        public NamedBandRange(float lo, float hi) { Lo = lo; Hi = hi; }
    }
}
