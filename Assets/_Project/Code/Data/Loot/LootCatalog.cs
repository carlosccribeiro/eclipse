using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Core.Loot;
using Eclipse.Data.Definitions;

namespace Eclipse.Data.Loot
{
    /// <summary>
    /// <see cref="ILootCatalog"/> built from the loot Definitions. Constants come from <c>loot_global.csv</c>; a missing key throws
    /// <see cref="InvalidOperationException"/>. Lists are ordered by StableId (ordinal).
    /// </summary>
    public sealed class LootCatalog : ILootCatalog
    {
        /// <summary>Ascension spans A1..A100: 99 steps (05 section 4.1 / 8.1; structural).</summary>
        public const int AscensionSteps = 99;

        private readonly Dictionary<LootSourceKind, LootSourceData> _sources = new Dictionary<LootSourceKind, LootSourceData>();
        private readonly Dictionary<LootSourceKind, LootChanceData> _chances = new Dictionary<LootSourceKind, LootChanceData>();
        private readonly Dictionary<Difficulty, DifficultyLootData> _difficulties = new Dictionary<Difficulty, DifficultyLootData>();
        private readonly Dictionary<Difficulty, PityConfigData> _pity = new Dictionary<Difficulty, PityConfigData>();
        private readonly List<IlvlRangeData> _ranges = new List<IlvlRangeData>();
        private readonly List<HomeRegionData> _homeRegions = new List<HomeRegionData>();
        private readonly List<RegionAffinityData> _regionAffinities = new List<RegionAffinityData>();

        public LootCatalog(IEnumerable<Definition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            List<Definition> all = definitions.Where(d => d != null).OrderBy(d => d.StableIdString, StringComparer.Ordinal).ToList();

            var g = new LootGlobalTable(all.OfType<LootGlobalDefinition>());
            QuantityCap = g.Get(LootGlobalKeys.QuantityCap);
            UndiscoveredUniqueWeight = g.Get(LootGlobalKeys.UndiscoveredUniqueWeight);
            RequiredLevelFactor = g.Get(LootGlobalKeys.RequiredLevelFactor);
            RequiredLevelMax = (int)Math.Round(g.Get(LootGlobalKeys.RequiredLevelMax));
            SmartLootCurrentHeroGeneric = g.Get(LootGlobalKeys.SmartCurrentHeroGeneric);
            SmartLootCurrentHeroLegendary = g.Get(LootGlobalKeys.SmartCurrentHeroLegendary);
            SmartWeights = new SmartLootWeights
            {
                IrrelevantAffix = g.Get(LootGlobalKeys.WeightIrrelevantAffix),
                PrimaryCore = g.Get(LootGlobalKeys.WeightPrimaryCore),
                PrimarySecondary = g.Get(LootGlobalKeys.WeightPrimarySecondary),
                PrimaryTertiary = g.Get(LootGlobalKeys.WeightPrimaryTertiary),
                EpicHeroModifierShare = g.Get(LootGlobalKeys.EpicHeroModifierShare),
                EquippedSkillMultiplier = g.Get(LootGlobalKeys.EquippedSkillMultiplier),
            };
            SlotWeights = new SlotWeightData();
            for (int i = 0; i < LootGlobalKeys.SlotWeightSuffixes.Length; i++)
                SlotWeights.Weights[i] = g.Get(LootGlobalKeys.SlotWeightPrefix + LootGlobalKeys.SlotWeightSuffixes[i]);

            foreach (LootSourceDefinition d in all.OfType<LootSourceDefinition>())
            {
                _sources[d.Source] = new LootSourceData
                {
                    Source = d.Source,
                    DropChance = d.DropChance,
                    GuaranteedItems = d.GuaranteedItems,
                    ExtraItemChance = d.ExtraItemChance,
                    ExtraItemsIfAllChallenges = d.ExtraItemsIfAllChallenges,
                    ChooseOf = d.ChooseOf,
                    GuaranteedEpicPlus = d.GuaranteedEpicPlus,
                    RarityFloor = d.RarityFloor,
                    AffectedByQuantity = d.AffectedByQuantity,
                    ILvlBonus = d.ILvlBonus,
                    ILvlFromRangeTop = d.ILvlFromRangeTop,
                    ConsumesHardPity = d.ConsumesHardPity,
                };
            }

            foreach (LootChanceDefinition d in all.OfType<LootChanceDefinition>())
            {
                var data = new LootChanceData { Source = d.Source };
                Array.Copy(d.Legendary, data.Legendary, 6);
                Array.Copy(d.Epic, data.Epic, 6);
                Array.Copy(d.Rare, data.Rare, 6);
                _chances[d.Source] = data;
            }

            foreach (IlvlRangeDefinition d in all.OfType<IlvlRangeDefinition>())
            {
                _ranges.Add(new IlvlRangeData
                {
                    Difficulty = d.Difficulty,
                    RegionId = string.IsNullOrEmpty(d.RegionId) ? null : d.RegionId,
                    Min = d.ILvlMin,
                    Max = d.ILvlMax,
                });
            }

            foreach (LootDifficultyDefinition d in all.OfType<LootDifficultyDefinition>())
            {
                _difficulties[d.Difficulty] = new DifficultyLootData
                {
                    Difficulty = d.Difficulty,
                    QuantityBonus = d.QuantityBonus,
                    QuantityBonusPerAscension = d.QuantityBonusPerAscension,
                    SetPieceShare = d.SetShare,
                    StandardShare = d.StandardShare,
                    UniqueShare = d.UniqueShare,
                    RollFloor = d.RollFloor,
                    RollFloorPerAscension = d.RollFloorPerAscension,
                    AscensionBaseILvl = d.AscensionBaseILvl,
                    AscensionMaxILvl = d.AscensionMaxILvl,
                    AscensionILvlCapLevel = d.AscensionILvlCapLevel,
                    AscensionPrimalMinLevel = d.AscensionPrimalMinLevel,
                    PrimalBaseChance = d.PrimalBaseChance,
                    PrimalChancePerLevel = d.PrimalChancePerLevel,
                    PrimalRollMin = d.PrimalRollMin,
                };
            }

            foreach (PityDefinition d in all.OfType<PityDefinition>())
            {
                _pity[d.Difficulty] = new PityConfigData
                {
                    Difficulty = d.Difficulty,
                    SoftStart = d.SoftStart,
                    Hard = d.Hard,
                    SoftStep = d.SoftStep,
                    LegendaryCapMultiplier = d.LegendaryCapMultiplier,
                    SetPityAfter = d.SetPityAfter,
                    DuplicateUnownedWeight = d.DuplicateUnownedWeight,
                    BossUniqueChance = d.BossUniqueChance,
                    BossUniqueChancePerAscension = (d.BossUniqueChanceA100 - d.BossUniqueChance) / AscensionSteps,
                    BossUniqueGuaranteeKills = d.BossUniqueGuaranteeKills,
                    BossEchoMultiplier = d.BossEchoMultiplier,
                    FirstKillSetChance = d.FirstKillSetChance,
                };
            }

            foreach (HomeRegionDefinition d in all.OfType<HomeRegionDefinition>())
            {
                if (d.RowKind == HomeRegionRowKind.Set)
                {
                    _homeRegions.Add(new HomeRegionData
                    {
                        SetId = d.SetId,
                        HeroId = d.HeroId,
                        RegionId = d.RegionId,
                        HomeWeight = d.HomeWeight,
                        HomeBossWeight = d.HomeBossWeight,
                        AttunementMultiplier = d.AttunementMultiplier,
                    });
                }
                else
                {
                    _regionAffinities.Add(new RegionAffinityData
                    {
                        RegionId = d.RegionId,
                        DominantAffinities = d.DominantAffinities,
                        AffinityWeight = d.AffinityWeight,
                        UniqueBonus = d.UniqueBonus,
                    });
                }
            }
        }

        /// <summary>Catalog from every definition in a registry (runtime entry point).</summary>
        public static LootCatalog FromRegistry(DefinitionRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            return new LootCatalog(registry.Definitions);
        }

        public float QuantityCap { get; }
        public float UndiscoveredUniqueWeight { get; }
        public float RequiredLevelFactor { get; }
        public int RequiredLevelMax { get; }
        public float SmartLootCurrentHeroGeneric { get; }
        public float SmartLootCurrentHeroLegendary { get; }
        public SmartLootWeights SmartWeights { get; }
        public SlotWeightData SlotWeights { get; }

        public IReadOnlyList<IlvlRangeData> IlvlRanges => _ranges;
        public IReadOnlyList<HomeRegionData> HomeRegions => _homeRegions;
        public IReadOnlyList<RegionAffinityData> RegionAffinities => _regionAffinities;

        public LootSourceData GetSource(LootSourceKind kind) => Find(_sources, kind, "loot_sources.csv");
        public LootChanceData GetChances(LootSourceKind kind) => Find(_chances, kind, "loot_chances.csv");
        public DifficultyLootData GetDifficulty(Difficulty difficulty) => Find(_difficulties, difficulty, "loot_difficulty.csv");
        public PityConfigData GetPity(Difficulty difficulty) => Find(_pity, difficulty, "pity.csv");

        private static TValue Find<TKey, TValue>(Dictionary<TKey, TValue> map, TKey key, string table)
        {
            if (!map.TryGetValue(key, out TValue value))
                throw new KeyNotFoundException(table + " has no row for " + key);
            return value;
        }
    }
}
