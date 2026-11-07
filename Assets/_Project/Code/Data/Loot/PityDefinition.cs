using Eclipse.Core.Loot;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Loot
{
    /// <summary>Pity parameters of one difficulty (05 sections 8.1-8.5). Percentages are fractions. Source: pity.csv.</summary>
    public sealed class PityDefinition : Definition
    {
        [SerializeField] private Difficulty difficulty;
        [SerializeField] private int softStart;
        [SerializeField] private int hard;
        [SerializeField] private float softStep;
        [SerializeField] private float legendaryCapMultiplier;
        [SerializeField] private int setPityAfter;
        [SerializeField] private int duplicateUnownedWeight;
        [SerializeField] private float bossUniqueChance;
        [SerializeField] private float bossUniqueChanceA100;
        [SerializeField] private int bossUniqueGuaranteeKills;
        [SerializeField] private float bossEchoMultiplier;
        [SerializeField] private float firstKillSetChance;

        public Difficulty Difficulty => difficulty;
        public int SoftStart => softStart;
        public int Hard => hard;
        public float SoftStep => softStep;
        public float LegendaryCapMultiplier => legendaryCapMultiplier;
        public int SetPityAfter => setPityAfter;
        public int DuplicateUnownedWeight => duplicateUnownedWeight;
        public float BossUniqueChance => bossUniqueChance;
        public float BossUniqueChanceA100 => bossUniqueChanceA100;
        public int BossUniqueGuaranteeKills => bossUniqueGuaranteeKills;
        public float BossEchoMultiplier => bossEchoMultiplier;
        public float FirstKillSetChance => firstKillSetChance;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(Difficulty value, int soft, int hardPity, float step, float capMult, int setAfter, int dupWeight,
            float bossChance, float bossChanceA100, int guaranteeKills, float echoMult, float firstKillSet)
        {
            difficulty = value;
            softStart = soft;
            hard = hardPity;
            softStep = step;
            legendaryCapMultiplier = capMult;
            setPityAfter = setAfter;
            duplicateUnownedWeight = dupWeight;
            bossUniqueChance = bossChance;
            bossUniqueChanceA100 = bossChanceA100;
            bossUniqueGuaranteeKills = guaranteeKills;
            bossEchoMultiplier = echoMult;
            firstKillSetChance = firstKillSet;
        }
#endif
    }
}
