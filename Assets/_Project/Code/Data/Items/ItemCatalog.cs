using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Core.Items;
using Eclipse.Data.Definitions;
using Eclipse.Data.Loot;

namespace Eclipse.Data.Items
{
    /// <summary>
    /// <see cref="IItemCatalog"/> built from the item Definitions (affixes, pools, bases, named items, rarities, bands) and
    /// <c>loot_global.csv</c>. Lists are ordered by StableId (ordinal) so generation is deterministic.
    /// </summary>
    public sealed class ItemCatalog : IItemCatalog
    {
        private readonly List<AffixData> _affixes = new List<AffixData>();
        private readonly Dictionary<string, AffixData> _affixById = new Dictionary<string, AffixData>(StringComparer.Ordinal);
        private readonly List<AffixPoolEntry> _pool = new List<AffixPoolEntry>();
        private readonly List<SlotMultiplierData> _slotMultipliers = new List<SlotMultiplierData>();
        private readonly List<ItemBaseData> _bases = new List<ItemBaseData>();
        private readonly Dictionary<string, ItemBaseData> _baseById = new Dictionary<string, ItemBaseData>(StringComparer.Ordinal);
        private readonly List<NamedItemData> _named = new List<NamedItemData>();
        private readonly Dictionary<string, NamedItemData> _namedById = new Dictionary<string, NamedItemData>(StringComparer.Ordinal);
        private readonly Dictionary<Rarity, RarityData> _rarities = new Dictionary<Rarity, RarityData>();
        private readonly Dictionary<NamedBand, NamedBandRange> _bands = new Dictionary<NamedBand, NamedBandRange>();

        /// <summary>Builds the catalog; throws <see cref="InvalidOperationException"/> when a required loot_global key is missing.</summary>
        public ItemCatalog(IEnumerable<Definition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            List<Definition> all = definitions.Where(d => d != null).OrderBy(d => d.StableIdString, StringComparer.Ordinal).ToList();

            var globals = new LootGlobalTable(all.OfType<LootGlobalDefinition>());
            ArmorImplicitPerILvl = globals.Get(LootGlobalKeys.ArmorImplicitPerILvl);
            UpgradePerLevel = globals.Get(LootGlobalKeys.UpgradePerLevel);

            foreach (AffixDefinition d in all.OfType<AffixDefinition>())
            {
                var data = new AffixData
                {
                    Id = d.StableIdString,
                    Stat = d.HasStat ? d.Stat : (Core.Stats.StatId?)null,
                    ValueKind = d.ValueKind,
                    Category = d.Category,
                    Group = d.Group,
                    Affinity = d.Affinity,
                    RelevanceTags = d.RelevanceTags,
                };
                for (int t = 0; t < 5; t++)
                    data.Tiers[t] = new TierRange(d.Tiers[t * 2], d.Tiers[t * 2 + 1]);
                _affixes.Add(data);
                _affixById[data.Id] = data;
            }

            foreach (AffixPoolDefinition d in all.OfType<AffixPoolDefinition>())
            {
                var entry = new AffixPoolEntry { AffixId = d.AffixId, Restriction = string.IsNullOrEmpty(d.Restriction) ? null : d.Restriction };
                Array.Copy(d.SlotWeights, entry.SlotWeights, Math.Min(d.SlotWeights.Length, entry.SlotWeights.Length));
                _pool.Add(entry);
            }

            foreach (SlotMultiplierDefinition d in all.OfType<SlotMultiplierDefinition>())
            {
                _slotMultipliers.Add(new SlotMultiplierData
                {
                    Id = d.StableIdString,
                    Slot = d.Slot,
                    AffixIds = new List<string>(d.AffixIds),
                    BaseId = string.IsNullOrEmpty(d.BaseId) ? null : d.BaseId,
                    Multiplier = d.Multiplier,
                    AppliesToImplicit = d.AppliesToImplicit,
                });
            }

            foreach (ItemBaseDefinition d in all.OfType<ItemBaseDefinition>())
            {
                var data = new ItemBaseData
                {
                    Id = d.StableIdString,
                    Slot = d.Slot,
                    Category = d.Category,
                    Family = d.Family,
                    HeroId = string.IsNullOrEmpty(d.HeroId) ? null : d.HeroId,
                    Tags = d.Tags,
                    ImplicitAffixId = string.IsNullOrEmpty(d.ImplicitAffixId) ? null : d.ImplicitAffixId,
                    ImplicitFactor = d.ImplicitFactor,
                    ArmorFactor = d.ArmorFactor,
                    Droppable = d.Droppable,
                };
                _bases.Add(data);
                _baseById[data.Id] = data;
            }

            foreach (NamedItemDefinition d in all.OfType<NamedItemDefinition>())
            {
                var data = new NamedItemData
                {
                    Id = d.StableIdString,
                    Kind = d.ItemKind,
                    BaseId = d.BaseId,
                    HeroId = string.IsNullOrEmpty(d.HeroId) ? null : d.HeroId,
                    EffectId = string.IsNullOrEmpty(d.EffectId) ? null : d.EffectId,
                    Affinity = d.Affinity,
                    SetId = string.IsNullOrEmpty(d.SetId) ? null : d.SetId,
                    UniqueSource = d.UniqueSourceKind,
                    UniqueSourceRef = string.IsNullOrEmpty(d.UniqueSourceRef) ? null : d.UniqueSourceRef,
                    MinDropILvl = d.MinDropILvl,
                };
                foreach (NamedAffixEntry e in d.FixedAffixes) data.FixedAffixes.Add(new NamedAffixSpec(e.affixId, e.band, e.offPool));
                foreach (NamedAffixEntry e in d.OpenOptions) data.OpenOptions.Add(new NamedAffixSpec(e.affixId, e.band, e.offPool));
                data.ModifierIds.AddRange(d.ModifierIds);
                _named.Add(data);
                _namedById[data.Id] = data;
            }

            foreach (RarityDefinition d in all.OfType<RarityDefinition>())
            {
                _rarities[d.Rarity] = new RarityData
                {
                    Rarity = d.Rarity,
                    RandomAffixCount = d.RandomAffixCount,
                    ModifierCount = d.ModifierCount,
                    MinDropILvl = d.MinDropILvl,
                };
            }

            foreach (NamedBandDefinition d in all.OfType<NamedBandDefinition>())
                _bands[d.Band] = new NamedBandRange(d.Lo, d.Hi);
        }

        /// <summary>Catalog from every definition in a registry (runtime entry point).</summary>
        public static ItemCatalog FromRegistry(DefinitionRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            return new ItemCatalog(registry.Definitions);
        }

        public float ArmorImplicitPerILvl { get; }
        public float UpgradePerLevel { get; }

        public IReadOnlyList<AffixData> Affixes => _affixes;
        public IReadOnlyList<AffixPoolEntry> Pool => _pool;
        public IReadOnlyList<SlotMultiplierData> SlotMultipliers => _slotMultipliers;
        public IReadOnlyList<ItemBaseData> Bases => _bases;
        public IReadOnlyList<NamedItemData> NamedItems => _named;

        public AffixData GetAffix(string id) => id != null && _affixById.TryGetValue(id, out AffixData a) ? a : null;
        public ItemBaseData GetBase(string id) => id != null && _baseById.TryGetValue(id, out ItemBaseData b) ? b : null;
        public NamedItemData GetNamedItem(string id) => id != null && _namedById.TryGetValue(id, out NamedItemData n) ? n : null;

        public RarityData GetRarity(Rarity rarity)
        {
            if (!_rarities.TryGetValue(rarity, out RarityData r))
                throw new KeyNotFoundException("rarities.csv has no row for " + rarity);
            return r;
        }

        public NamedBandRange GetBandRange(NamedBand band)
        {
            if (!_bands.TryGetValue(band, out NamedBandRange r))
                throw new KeyNotFoundException("named_bands.csv has no row for " + band);
            return r;
        }
    }
}
