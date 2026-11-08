using Eclipse.Core.Items;
using Eclipse.Core.Loot;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Loot
{
    /// <summary>One of the 10 loot sources (05 sections 3.1, 3.2, 8.1). Chances are fractions. Source: loot_sources.csv.</summary>
    public sealed class LootSourceDefinition : Definition
    {
        [SerializeField] private LootSourceKind source;
        [SerializeField] private float dropChance;
        [SerializeField] private int guaranteedItems;
        [SerializeField] private float extraItemChance;
        [SerializeField] private int extraItemsIfAllChallenges;
        [SerializeField] private int chooseOf;
        [SerializeField] private int guaranteedEpicPlus;
        [SerializeField] private Rarity rarityFloor;
        [SerializeField] private bool affectedByQuantity;
        [SerializeField] private int ilvlBonus;
        [SerializeField] private bool ilvlFromRangeTop;
        [SerializeField] private bool consumesHardPity;

        public LootSourceKind Source => source;
        public float DropChance => dropChance;
        public int GuaranteedItems => guaranteedItems;
        public float ExtraItemChance => extraItemChance;
        public int ExtraItemsIfAllChallenges => extraItemsIfAllChallenges;
        public int ChooseOf => chooseOf;
        public int GuaranteedEpicPlus => guaranteedEpicPlus;
        public Rarity RarityFloor => rarityFloor;
        public bool AffectedByQuantity => affectedByQuantity;
        public int ILvlBonus => ilvlBonus;
        public bool ILvlFromRangeTop => ilvlFromRangeTop;
        public bool ConsumesHardPity => consumesHardPity;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(LootSourceKind kind, float drop, int guaranteed, float extraChance, int extraIfAll, int chooseCount,
            int epicPlus, Rarity floor, bool quantity, int bonus, bool fromTop, bool hardPity)
        {
            source = kind;
            dropChance = drop;
            guaranteedItems = guaranteed;
            extraItemChance = extraChance;
            extraItemsIfAllChallenges = extraIfAll;
            chooseOf = chooseCount;
            guaranteedEpicPlus = epicPlus;
            rarityFloor = floor;
            affectedByQuantity = quantity;
            ilvlBonus = bonus;
            ilvlFromRangeTop = fromTop;
            consumesHardPity = hardPity;
        }
#endif
    }
}
