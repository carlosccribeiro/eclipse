using System.Collections.Generic;

namespace Eclipse.Core.Items.Tests
{
    /// <summary>In-memory <see cref="IItemCatalog"/> for tests; lists are mutable so each test adds what it needs.</summary>
    public sealed class FakeItemCatalog : IItemCatalog
    {
        public readonly List<AffixData> AffixList = new List<AffixData>();
        public readonly List<AffixPoolEntry> PoolList = new List<AffixPoolEntry>();
        public readonly List<SlotMultiplierData> SlotMultiplierList = new List<SlotMultiplierData>();
        public readonly List<ItemBaseData> BaseList = new List<ItemBaseData>();
        public readonly List<NamedItemData> NamedList = new List<NamedItemData>();
        public readonly Dictionary<Rarity, RarityData> RarityMap = new Dictionary<Rarity, RarityData>();
        public readonly Dictionary<NamedBand, NamedBandRange> BandMap = new Dictionary<NamedBand, NamedBandRange>();

        public float ArmorImplicitPerILvl { get; set; } = 4f;
        public float UpgradePerLevel { get; set; } = 0.03f;

        public IReadOnlyList<AffixData> Affixes => AffixList;
        public IReadOnlyList<AffixPoolEntry> Pool => PoolList;
        public IReadOnlyList<SlotMultiplierData> SlotMultipliers => SlotMultiplierList;
        public IReadOnlyList<ItemBaseData> Bases => BaseList;
        public IReadOnlyList<NamedItemData> NamedItems => NamedList;

        public AffixData GetAffix(string id) => AffixList.Find(a => a.Id == id);
        public ItemBaseData GetBase(string id) => BaseList.Find(b => b.Id == id);
        public NamedItemData GetNamedItem(string id) => NamedList.Find(n => n.Id == id);
        public RarityData GetRarity(Rarity rarity) => RarityMap[rarity];
        public NamedBandRange GetBandRange(NamedBand band) => BandMap[band];
    }
}
