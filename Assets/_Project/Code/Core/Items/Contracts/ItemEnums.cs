using System;

namespace Eclipse.Core.Items
{
    /// <summary>Item rarity (02 section 3.1). Set pieces and Uniques are <see cref="Legendary"/> with another <see cref="ItemKind"/>.</summary>
    public enum Rarity : byte { Common, Rare, Epic, Legendary }

    /// <summary>Structure family of an item (00 section 5 R25; 11 section 3.4.2).</summary>
    public enum ItemKind : byte
    {
        /// <summary>Common/Rare/Epic: random affixes from the slot pool.</summary>
        Generic,
        /// <summary>Standard Legendary: 4 fixed affixes + 1 Open Affix + 2 modifiers + effect.</summary>
        NamedLegendary,
        /// <summary>Set piece: 4 fixed + 1 Open Affix + 2 modifiers, no own effect (04).</summary>
        SetPiece,
        /// <summary>Unique: 5 fixed affixes + 2 modifiers (at most 1 Signature) + effect.</summary>
        Unique
    }

    /// <summary>The 10 item slot kinds. Ring and Relic occupy 2 equipment slots but are one kind (05 section 6.1).</summary>
    public enum EquipSlot : byte { MainHand, OffHand, Head, Chest, Hands, Legs, Feet, Ring, Amulet, Relic }

    /// <summary>Armor family (01 section 3.9.3, Armor implicit factor).</summary>
    public enum ArmorFamily : byte { None, Plate, Leather, Vestment }

    /// <summary>Named Range band of a fixed affix of a named item (02 section 3.9.1; 00 section 5 R24). Ranges come from data.</summary>
    public enum NamedBand : byte
    {
        /// <summary>Not a named affix: the plain tier range is used.</summary>
        None,
        /// <summary>The whole Named Range envelope (70%-115% of tier max, R24).</summary>
        Envelope,
        /// <summary>Standard band of a named affix.</summary>
        Default,
        /// <summary>Raised band (up arrow in 02).</summary>
        High,
        /// <summary>Lowered band (down arrow in 02).</summary>
        Low
    }

    /// <summary>How an affix value is expressed. Percent values are fractions in Core (0.15 = 15%).</summary>
    public enum AffixValueKind : byte { Flat, Percent }

    /// <summary>Affix category (01 section 3.9.2).</summary>
    public enum AffixCategory : byte { Primary, Flat, Offensive, Defensive, Utility, Infusion }

    /// <summary>Exclusion groups of 05 section 6.2 (and the primary limit of 02 section 3.9.3).</summary>
    public enum AffixGroup : byte
    {
        None,
        /// <summary>Strength/Dexterity/Intelligence.</summary>
        Primary,
        /// <summary>Ember/Frost/Storm/Void/Echo/Ruin Damage and Infusions (max 2 per item, 02 section 3.9.3).</summary>
        Affinity,
        /// <summary>Pickup Radius, Experience Gain, Gold Acquisition (max 1 per item).</summary>
        Comfort,
        /// <summary>Physical/Elemental/Void Resistance (max 1 in Common/Rare).</summary>
        SingleResistance
    }

    /// <summary>Per-instance flags (11 section 3.4.3).</summary>
    [Flags]
    public enum ItemFlags : ushort
    {
        None = 0,
        Locked = 1 << 0,
        New = 1 << 1,
        Favorite = 1 << 2,
        Primal = 1 << 3,
        Anchored = 1 << 4
    }
}
