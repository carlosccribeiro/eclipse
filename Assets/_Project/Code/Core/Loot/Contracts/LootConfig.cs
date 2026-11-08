using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Loot
{
    /// <summary>Static data of one loot source (05 section 3.1). All numbers come from <c>loot_sources.csv</c>.</summary>
    public sealed class LootSourceData
    {
        public LootSourceKind Source;
        /// <summary>Drop chance of 1 item (trash 3%); 1 when the source always drops.</summary>
        public float DropChance;
        /// <summary>Guaranteed item count.</summary>
        public int GuaranteedItems;
        /// <summary>Chance of one extra item (Elite 30%).</summary>
        public float ExtraItemChance;
        /// <summary>Extra items when all Challenge Rooms were cleared (Rift Cache +1).</summary>
        public int ExtraItemsIfAllChallenges;
        /// <summary>Reliquary: pick 1 of N (N shown; all N are generated).</summary>
        public int ChooseOf;
        /// <summary>Number of items that are guaranteed Epic+ (Guardian/Boss 1).</summary>
        public int GuaranteedEpicPlus;
        public Rarity RarityFloor;
        public bool AffectedByQuantity;
        public int ILvlBonus;
        /// <summary>True for Boss/Rift Cache: iLvl = min(100, range max + 2).</summary>
        public bool ILvlFromRangeTop;
        /// <summary>False for trash (05 section 8.1); true if the source can consume the hard pity.</summary>
        public bool ConsumesHardPity;
    }

    /// <summary>L/E/R chances (fractions) at the 6 anchor points of 05 sections 5.2-5.4. Ascension interpolates A1..A100.</summary>
    public sealed class LootChanceData
    {
        public LootSourceKind Source;
        public float[] Legendary = new float[6]; // Normal, Veteran, Nightmare, Eclipse, A1, A100
        /// <summary>Negative = source has the Epic floor (Reliquary, Challenge).</summary>
        public float[] Epic = new float[6];
        /// <summary>Trash only; others have a floor.</summary>
        public float[] Rare = new float[6];
    }

    /// <summary>iLvl range of a difficulty/region (05 section 4.1).</summary>
    public sealed class IlvlRangeData
    {
        public Difficulty Difficulty;
        /// <summary>Region id; null/empty = every region.</summary>
        public string RegionId;
        public int Min;
        public int Max;
    }

    /// <summary>Per-difficulty values (quantity bonus, Legendary kind split, roll floor, Primal). Ascension fields give A1/A100 anchors.</summary>
    public sealed class DifficultyLootData
    {
        public Difficulty Difficulty;
        public float QuantityBonus;
        public float QuantityBonusPerAscension;
        public float SetPieceShare;
        public float StandardShare;
        public float UniqueShare;
        public float RollFloor;
        public float RollFloorPerAscension;
        /// <summary>Ascension iLvl base and cap level (05 section 4.1: 85, 100, cap at A90).</summary>
        public int AscensionBaseILvl;
        public int AscensionMaxILvl;
        public int AscensionILvlCapLevel;
        public int AscensionPrimalMinLevel;
        public float PrimalBaseChance;
        public float PrimalChancePerLevel;
        public float PrimalRollMin;
    }

    /// <summary>Pity parameters of one difficulty (05 section 8).</summary>
    public sealed class PityConfigData
    {
        public Difficulty Difficulty;
        public int SoftStart;
        public int Hard;
        public float SoftStep;
        public float LegendaryCapMultiplier;
        public int SetPityAfter;
        public int DuplicateUnownedWeight;
        public float BossUniqueChance;
        public float BossUniqueChancePerAscension;
        public int BossUniqueGuaranteeKills;
        public float BossEchoMultiplier;
        public float FirstKillSetChance;
    }

    /// <summary>Home region of a set (05 section 9.1).</summary>
    public sealed class HomeRegionData
    {
        public string SetId;
        public string HeroId;
        public string RegionId;
        public float HomeWeight;
        public float HomeBossWeight;
        public float AttunementMultiplier;
    }

    /// <summary>Dominant affinities of a region (05 section 9.2).</summary>
    public sealed class RegionAffinityData
    {
        public string RegionId;
        public TagMask DominantAffinities;
        public float AffinityWeight;
        /// <summary>Added to the Unique share, in fraction (Astral Citadel +0.03).</summary>
        public float UniqueBonus;
    }

    /// <summary>Slot weights for Common/Rare/Epic (05 section 6.1), indexed by <see cref="EquipSlot"/>.</summary>
    public sealed class SlotWeightData
    {
        public float[] Weights = new float[10];
    }

    /// <summary>Read-only loot tables seen by Core (built from SOs by the Data layer; fixtures in tests).</summary>
    public interface ILootCatalog
    {
        LootSourceData GetSource(LootSourceKind kind);
        LootChanceData GetChances(LootSourceKind kind);
        IReadOnlyList<IlvlRangeData> IlvlRanges { get; }
        DifficultyLootData GetDifficulty(Difficulty difficulty);
        PityConfigData GetPity(Difficulty difficulty);
        IReadOnlyList<HomeRegionData> HomeRegions { get; }
        IReadOnlyList<RegionAffinityData> RegionAffinities { get; }
        SlotWeightData SlotWeights { get; }
        /// <summary>Quantity ceiling (R45: 2.0).</summary>
        float QuantityCap { get; }
        /// <summary>Weight of an undiscovered Unique (x2, 05 section 6 step 5) and of the standard-Legendary affinity bonus come from here.</summary>
        float UndiscoveredUniqueWeight { get; }
        /// <summary>Required level: ceil(iLvl * factor) clamped to [1, max] (05 section 4.3).</summary>
        float RequiredLevelFactor { get; }
        int RequiredLevelMax { get; }
        /// <summary>Smart loot: probability of dropping for the current hero (Common/Rare/Epic, Legendary kinds). 05 section 7.1.</summary>
        float SmartLootCurrentHeroGeneric { get; }
        float SmartLootCurrentHeroLegendary { get; }
        /// <summary>Relevance weight for affixes not matching hero skills (x0.25), primary role weights (2/1/0.5), Epic hero share (0.75), equipped skill weight (x3). 05 sections 6.2, 7.2.</summary>
        SmartLootWeights SmartWeights { get; }
    }

    /// <summary>Smart-loot weights (05 sections 6.2, 7.2; R30).</summary>
    public sealed class SmartLootWeights
    {
        public float IrrelevantAffix;
        public float PrimaryCore;
        public float PrimarySecondary;
        public float PrimaryTertiary;
        public float EpicHeroModifierShare;
        public float EquippedSkillMultiplier;
    }
}
