using System;

namespace Eclipse.Data.Combat
{
    /// <summary>
    /// Every tunable of the generic combat states (06 section 3.8, R09, R11), as plain data. Percentages are fractions
    /// (0.15 = 15%). Source: CombatStates.csv; runtime code never carries these numbers.
    /// </summary>
    [Serializable]
    public sealed class CombatStatesValues
    {
        // 3.8.1 mass classes
        public float FlinchMinStaggerLight, FlinchMinStaggerMedium, EliteFlinchMult;
        public float KnockbackLight, KnockbackMedium, KnockbackHeavy, KnockbackColossal, EliteKnockbackMult, EliteCcDurationMult;
        // 3.8.2 stagger thresholds and difficulty
        public float StaggerThresholdMedium, StaggerThresholdHeavy, StaggerThresholdEliteMult, StaggerThresholdGuardian, StaggerThresholdBoss;
        public float DifficultyNormal, DifficultyVeteran, DifficultyNightmare, DifficultyEclipse, DifficultyAscensionPerLevel;
        // 3.8.2 decay, Staggered, Broken
        public float StaggerDecayDelaySeconds, StaggerDecayFractionPerSecond;
        public float StaggeredSeconds, StaggeredEliteSeconds, StaggeredGuardianSeconds, StaggeredVulnerability;
        public float BrokenSeconds, BrokenVulnerability;
        public int BrokenHitStopFrames, BreakThresholdMaxGrowths;
        public float BreakThresholdGrowth, CcToStaggerPerSecond;
        // 3.8.3 diminishing returns and player caps
        public float CcDrWindowSeconds, CcDrFirst, CcDrSecond, CcDrThird;
        public float PlayerHardMaxSeconds, PlayerFreezeMaxSeconds, PlayerRootMaxSeconds, PlayerHardImmunitySeconds;
        // 3.8.3 other states
        public float TauntMaxSeconds, TauntEliteMult, WallImpactDamageFraction, WallImpactStunSeconds;
        public float SlowEnemyCap, SlowPlayerMinSpeed;
        public float StealthMaxSeconds, StealthReenterIcdSeconds, StealthRevealMeters;
        public int ParryWindowFrames;
        public float ParryCapSeconds, ParryAttackerStagger;
        // 3.8.4 player reaction
        public int PlayerFlinchFrames, PlayerFlinchCancelFrame, KnockdownFrames, KnockdownInvulnerableFrames;
    }
}
