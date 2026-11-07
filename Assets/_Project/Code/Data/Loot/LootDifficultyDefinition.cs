using Eclipse.Core.Loot;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Loot
{
    /// <summary>
    /// Per-difficulty loot values (05 sections 3.2, 4.1, 5.5, 10.1, 10.3): quantity, Legendary kind split, roll floor, Ascension, Primal.
    /// Percentages are fractions. Source: loot_difficulty.csv.
    /// </summary>
    public sealed class LootDifficultyDefinition : Definition
    {
        [SerializeField] private Difficulty difficulty;
        [SerializeField] private float quantityBonus;
        [SerializeField] private float quantityBonusPerAscension;
        [SerializeField] private float setShare;
        [SerializeField] private float standardShare;
        [SerializeField] private float uniqueShare;
        [SerializeField] private float rollFloor;
        [SerializeField] private float rollFloorPerAscension;
        [SerializeField] private int ascensionBaseILvl;
        [SerializeField] private int ascensionMaxILvl;
        [SerializeField] private int ascensionILvlCapLevel;
        [SerializeField] private int ascensionPrimalMinLevel;
        [SerializeField] private float primalBaseChance;
        [SerializeField] private float primalChancePerLevel;
        [SerializeField] private float primalRollMin;

        public Difficulty Difficulty => difficulty;
        public float QuantityBonus => quantityBonus;
        public float QuantityBonusPerAscension => quantityBonusPerAscension;
        public float SetShare => setShare;
        public float StandardShare => standardShare;
        public float UniqueShare => uniqueShare;
        public float RollFloor => rollFloor;
        public float RollFloorPerAscension => rollFloorPerAscension;
        public int AscensionBaseILvl => ascensionBaseILvl;
        public int AscensionMaxILvl => ascensionMaxILvl;
        public int AscensionILvlCapLevel => ascensionILvlCapLevel;
        public int AscensionPrimalMinLevel => ascensionPrimalMinLevel;
        public float PrimalBaseChance => primalBaseChance;
        public float PrimalChancePerLevel => primalChancePerLevel;
        public float PrimalRollMin => primalRollMin;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(Difficulty value, float quantity, float quantityPerAscension, float set, float standard, float unique,
            float floor, float floorPerAscension, int baseILvl, int maxILvl, int capLevel, int primalMinLevel,
            float primalBase, float primalPerLevel, float primalRoll)
        {
            difficulty = value;
            quantityBonus = quantity;
            quantityBonusPerAscension = quantityPerAscension;
            setShare = set;
            standardShare = standard;
            uniqueShare = unique;
            rollFloor = floor;
            rollFloorPerAscension = floorPerAscension;
            ascensionBaseILvl = baseILvl;
            ascensionMaxILvl = maxILvl;
            ascensionILvlCapLevel = capLevel;
            ascensionPrimalMinLevel = primalMinLevel;
            primalBaseChance = primalBase;
            primalChancePerLevel = primalPerLevel;
            primalRollMin = primalRoll;
        }
#endif
    }
}
