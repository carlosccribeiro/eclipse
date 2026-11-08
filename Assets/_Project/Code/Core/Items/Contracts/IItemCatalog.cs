using System.Collections.Generic;

namespace Eclipse.Core.Items
{
    /// <summary>
    /// Read-only item data seen by Core (affixes, pools, bases, named items). The Data layer builds one from ScriptableObjects;
    /// tests build one from fixtures, so the generator runs under <c>dotnet test</c>.
    /// </summary>
    public interface IItemCatalog
    {
        AffixData GetAffix(string id);
        IReadOnlyList<AffixData> Affixes { get; }
        /// <summary>Pool entries ordered by id (deterministic).</summary>
        IReadOnlyList<AffixPoolEntry> Pool { get; }
        IReadOnlyList<SlotMultiplierData> SlotMultipliers { get; }
        ItemBaseData GetBase(string id);
        IReadOnlyList<ItemBaseData> Bases { get; }
        NamedItemData GetNamedItem(string id);
        IReadOnlyList<NamedItemData> NamedItems { get; }
        RarityData GetRarity(Rarity rarity);
        /// <summary>Range of a Named band as fractions of the tier max.</summary>
        NamedBandRange GetBandRange(NamedBand band);
        /// <summary>Constant of the Armor implicit formula (4 x iLvl x slot mult x family factor; 01 section 3.9.3).</summary>
        float ArmorImplicitPerILvl { get; }
        /// <summary>Per-level upgrade factor (R31: 0.03).</summary>
        float UpgradePerLevel { get; }
    }
}
