using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Entities;

namespace Eclipse.Tests.EditMode.Gameplay.States
{
    /// <summary>Values copied from 06 section 3.8 / R09 / R11; runtime code receives them as data.</summary>
    internal static class StatesFixtures
    {
        public static CombatStatesValues Values() => new CombatStatesValues
        {
            FlinchMinStaggerLight = 1f, FlinchMinStaggerMedium = 10f, EliteFlinchMult = 2f,
            KnockbackLight = 1f, KnockbackMedium = 0.7f, KnockbackHeavy = 0.3f, KnockbackColossal = 0f,
            EliteKnockbackMult = 0.6f, EliteCcDurationMult = 0.6f,
            StaggerThresholdMedium = 40f, StaggerThresholdHeavy = 120f, StaggerThresholdEliteMult = 2.5f,
            StaggerThresholdGuardian = 400f, StaggerThresholdBoss = 800f,
            DifficultyNormal = 1f, DifficultyVeteran = 1.2f, DifficultyNightmare = 1.4f, DifficultyEclipse = 1.6f,
            DifficultyAscensionPerLevel = 0.01f,
            StaggerDecayDelaySeconds = 3f, StaggerDecayFractionPerSecond = 0.15f,
            StaggeredSeconds = 1.5f, StaggeredEliteSeconds = 2f, StaggeredGuardianSeconds = 3f, StaggeredVulnerability = 0.2f,
            BrokenSeconds = 4f, BrokenVulnerability = 0.3f, BrokenHitStopFrames = 6,
            BreakThresholdGrowth = 0.5f, BreakThresholdMaxGrowths = 3, CcToStaggerPerSecond = 40f,
            CcDrWindowSeconds = 8f, CcDrFirst = 1f, CcDrSecond = 0.5f, CcDrThird = 0.25f,
            PlayerHardMaxSeconds = 1.5f, PlayerFreezeMaxSeconds = 1f, PlayerRootMaxSeconds = 2f, PlayerHardImmunitySeconds = 2f,
            TauntMaxSeconds = 3f, TauntEliteMult = 0.5f, WallImpactDamageFraction = 0.1f, WallImpactStunSeconds = 0.5f,
            SlowEnemyCap = 0.6f, SlowPlayerMinSpeed = 0.4f,
            StealthMaxSeconds = 4f, StealthReenterIcdSeconds = 1f, StealthRevealMeters = 3f,
            ParryWindowFrames = 12, ParryCapSeconds = 0.3f, ParryAttackerStagger = 50f,
            PlayerFlinchFrames = 8, PlayerFlinchCancelFrame = 6, KnockdownFrames = 24, KnockdownInvulnerableFrames = 10,
        };

        public static CombatStateRules Rules() => new CombatStateRules(Values());
    }

    internal sealed class FakeMass : IMassBody
    {
        public FakeMass(MassClass mass, bool elite = false) { MassClass = mass; IsElite = elite; }
        public MassClass MassClass { get; }
        public bool IsElite { get; }
    }
}
