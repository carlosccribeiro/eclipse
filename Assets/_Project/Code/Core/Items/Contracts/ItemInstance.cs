using System;
using System.Collections.Generic;
using Eclipse.Core.Loot;

namespace Eclipse.Core.Items
{
    /// <summary>
    /// Runtime item (11 section 3.4.3). Stores only ids and normalized rolls; effective values are computed at read time
    /// by <c>ItemMath</c> (never saved). Serializable as a POCO (public fields) for the save module (M18).
    /// </summary>
    [Serializable]
    public sealed class ItemInstance
    {
        public Guid InstanceId;
        /// <summary>StableId of the named <c>ItemDefinition</c> or of the generic <c>ItemBaseDefinition</c>.</summary>
        public string DefinitionId;
        /// <summary>StableId of the base (weapon/armor/jewel type); equals <see cref="DefinitionId"/> for generic items.</summary>
        public string BaseId;
        public Rarity Rarity;
        public ItemKind Kind;
        /// <summary>1-100. Tier = ceil(iLvl / 20).</summary>
        public int ItemLevel;
        /// <summary>Hero-bound owner (StableId string); null = any hero (R27).</summary>
        public string BoundHeroId;
        /// <summary>Fixed + random attributes; for named items the 4/5 fixed first, then the chosen Open Affix.</summary>
        public List<RolledAffix> Affixes = new List<RolledAffix>();
        /// <summary>Roll of the base implicit (Armor implicit is derived from iLvl, not rolled; see 02 section 3.4).</summary>
        public float ImplicitRoll01;
        /// <summary>Index (0-2) of the chosen Open Affix option in Legendary/SetPiece.</summary>
        public byte OpenAffixChoice;
        public List<RolledModifier> Modifiers = new List<RolledModifier>();
        /// <summary>Blacksmith +0..+10 (R31).</summary>
        public int UpgradeLevel;
        public ItemFlags Flags;
        public int EnchantedAffixIndex = -1;
        public ushort EnchantCount;
        public ushort RefineCount;
        public ushort ImbueCount;
        /// <summary>Seed of the Loot stream that produced the item.</summary>
        public uint DropSeed;
        public LootSourceKind Source;
    }

    /// <summary>One rolled attribute. Value is derived: lerp over the tier range (or Named band) x slot multiplier.</summary>
    [Serializable]
    public struct RolledAffix
    {
        public string AffixId;
        /// <summary>1-5 (tier of the item at generation).</summary>
        public byte Tier;
        /// <summary>0-1, uniform before the difficulty floor.</summary>
        public float Roll01;
        /// <summary>True for fixed affixes of named items (Named Range).</summary>
        public bool Named;
        /// <summary>Named band for the range; <see cref="NamedBand.None"/> when not named.</summary>
        public NamedBand Band;
    }

    /// <summary>One rolled modifier (id from 03, e.g. <c>KAE-CS-01</c>, <c>GEN-27</c>, <c>SIG-K16</c>).</summary>
    [Serializable]
    public struct RolledModifier
    {
        public string ModifierId;
        public float Roll01;
    }
}
