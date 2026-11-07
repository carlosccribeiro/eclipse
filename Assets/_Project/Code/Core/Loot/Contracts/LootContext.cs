using Eclipse.Core.Items;

namespace Eclipse.Core.Loot
{
    /// <summary>Guarantees that pity/first-time rules impose on one generation (05 sections 8.1-8.5).</summary>
    public struct LootGuarantees
    {
        /// <summary>Force the Legendary rarity (hard pity, first kill).</summary>
        public bool ForceLegendary;
        /// <summary>Raise the item to Epic or better (Guardian/Boss guaranteed item, first region clear).</summary>
        public bool ForceEpicPlus;
        /// <summary>Force the Legendary kind (set pity, boss Unique); null = roll normally.</summary>
        public ItemKind? ForceKind;
        /// <summary>Overrides the set-piece share for this item (first kill Veteran+ = data value); negative = no override.</summary>
        public float SetPieceChanceOverride;
        /// <summary>Restrict a forced Unique to this boss's exclusive pool; null = source rules.</summary>
        public string BossId;
        /// <summary>First-kill Legendary may exceed the minDropILvl rule: iLvl = max(iLvl, 10) (05 section 5.2).</summary>
        public bool RaiseILvlToLegendaryMin;
    }

    /// <summary>Attunement target (Eclipse+): one set or one Unique gets x3 weight (05 section 9.4).</summary>
    public readonly struct AttunementTarget
    {
        public readonly AttunementKind Kind;
        /// <summary>Set id or Unique definition id.</summary>
        public readonly string TargetId;
        public AttunementTarget(AttunementKind kind, string targetId) { Kind = kind; TargetId = targetId; }
    }

    /// <summary>Everything the generator needs about one drop (11 section 3.12). Built by the caller; not serialized.</summary>
    public sealed class LootContext
    {
        public LootSourceKind Source;
        /// <summary>Region StableId string (e.g. the Ashen Wastes); null for hub/test contexts.</summary>
        public string RegionId;
        public Difficulty Difficulty;
        /// <summary>1-100 when Difficulty is Ascension, else 0.</summary>
        public int AscensionLevel;
        /// <summary>Hero receiving the drop (StableId string).</summary>
        public string HeroId;
        /// <summary>Room index and total rooms of the region (05 section 4.2).</summary>
        public int RoomIndex;
        public int TotalRooms = 1;
        /// <summary>If set, skips the iLvl computation (tests, fixed drops).</summary>
        public int? ForcedILvl;
        /// <summary>Additive Quantity bonus beyond difficulty (Sigils, Blessings...); total capped at x2.0 (R45).</summary>
        public float QuantityBonus;
        /// <summary>Rift Cache: all Challenge Rooms of the run were cleared (+ExtraItemsIfAllChallenges items; 05 section 3.1). Additive field (T4).</summary>
        public bool AllChallengesCleared;
        public AttunementTarget Attunement;
        public LootGuarantees Guarantees = new LootGuarantees { SetPieceChanceOverride = -1f };
        /// <summary>Per-hero pity state; null = no pity.</summary>
        public IPityState Pity;
        /// <summary>Ownership lookup for the unowned x3 weights; null = owns nothing.</summary>
        public IItemCollection Collection;
        /// <summary>Equipped skill ids of the hero (x3 modifier weight, 05 section 7.2).</summary>
        public System.Collections.Generic.IReadOnlyCollection<string> EquippedSkillIds;
    }
}
