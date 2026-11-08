using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Difficulties
{
    /// <summary>
    /// One difficulty tier, subset needed by enemy AI (M07): attack tokens (09 section 3.2), wind-up reduction
    /// (06 section 3.10), Stagger threshold multiplier (06 section 3.8.2). M20 extends the same asset.
    /// </summary>
    public sealed class DifficultyDefinition : Definition
    {
        [SerializeField] private DifficultyTier tier;
        [SerializeField] private int tokensMelee;
        [SerializeField] private int tokensRanged;
        [SerializeField] private float minTokenIntervalSeconds;
        [SerializeField] private int ambushTokens;
        [SerializeField] private float maxWindupReduction;
        [SerializeField] private float staggerThresholdMult = 1f;
        [SerializeField] private float hpMult = 1f;
        [SerializeField] private float damageMult = 1f;

        public DifficultyTier Tier => tier;
        public int TokensMelee => tokensMelee;
        public int TokensRanged => tokensRanged;
        /// <summary>Minimum time between attacks of the same enemy, seconds.</summary>
        public float MinTokenIntervalSeconds => minTokenIntervalSeconds;
        /// <summary>Exclusive Assassin ambush tokens per encounter (09 section 3.2).</summary>
        public int AmbushTokens => ambushTokens;
        /// <summary>Maximum wind-up reduction as a fraction (0.15 = 15%); the absolute floor still applies.</summary>
        public float MaxWindupReduction => maxWindupReduction;
        public float StaggerThresholdMult => staggerThresholdMult;
        public float HpMult => hpMult;
        public float DamageMult => damageMult;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(DifficultyTier tierValue, int melee, int ranged, float interval, int ambush,
            float windupReduction, float staggerMult, float hp, float damage)
        {
            tier = tierValue; tokensMelee = melee; tokensRanged = ranged; minTokenIntervalSeconds = interval;
            ambushTokens = ambush; maxWindupReduction = windupReduction; staggerThresholdMult = staggerMult;
            hpMult = hp; damageMult = damage;
        }
#endif
    }
}
