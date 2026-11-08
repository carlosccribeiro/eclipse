using System.Collections.Generic;
using Eclipse.Core.Items;

namespace Eclipse.Core.Loot.Generation.Tests
{
    /// <summary>In-memory <see cref="ILootCatalog"/> for tests; collections are mutable so each test adjusts what it needs.</summary>
    public sealed class FakeLootCatalog : ILootCatalog
    {
        public readonly Dictionary<LootSourceKind, LootSourceData> SourceMap = new Dictionary<LootSourceKind, LootSourceData>();
        public readonly Dictionary<LootSourceKind, LootChanceData> ChanceMap = new Dictionary<LootSourceKind, LootChanceData>();
        public readonly List<IlvlRangeData> RangeList = new List<IlvlRangeData>();
        public readonly Dictionary<Difficulty, DifficultyLootData> DifficultyMap = new Dictionary<Difficulty, DifficultyLootData>();
        public readonly Dictionary<Difficulty, PityConfigData> PityMap = new Dictionary<Difficulty, PityConfigData>();
        public readonly List<HomeRegionData> HomeList = new List<HomeRegionData>();
        public readonly List<RegionAffinityData> RegionList = new List<RegionAffinityData>();

        public SlotWeightData SlotWeights { get; set; } = new SlotWeightData();
        public float QuantityCap { get; set; } = 2f;
        public float UndiscoveredUniqueWeight { get; set; } = 2f;
        public float RequiredLevelFactor { get; set; } = 0.5f;
        public int RequiredLevelMax { get; set; } = 50;
        public float SmartLootCurrentHeroGeneric { get; set; } = 0.75f;
        public float SmartLootCurrentHeroLegendary { get; set; } = 0.85f;
        public SmartLootWeights SmartWeights { get; set; } = new SmartLootWeights();

        public LootSourceData GetSource(LootSourceKind kind) => SourceMap[kind];
        public LootChanceData GetChances(LootSourceKind kind) => ChanceMap[kind];
        public IReadOnlyList<IlvlRangeData> IlvlRanges => RangeList;
        public DifficultyLootData GetDifficulty(Difficulty difficulty) => DifficultyMap[difficulty];
        public PityConfigData GetPity(Difficulty difficulty) => PityMap[difficulty];
        public IReadOnlyList<HomeRegionData> HomeRegions => HomeList;
        public IReadOnlyList<RegionAffinityData> RegionAffinities => RegionList;
    }
}
