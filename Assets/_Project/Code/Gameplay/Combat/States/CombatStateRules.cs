using System;
using Eclipse.Core.CrowdControl;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Entities;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>Stagger bar tier of a target (06 section 3.8.2). Light has no bar (it only flinches).</summary>
    public enum StaggerTier { None = 0, Medium = 1, Heavy = 2, Guardian = 3, Boss = 4 }

    /// <summary>Difficulty tiers that scale Stagger thresholds (06 section 3.8.2).</summary>
    public enum DifficultyTier { Normal = 0, Veteran = 1, Nightmare = 2, Eclipse = 3, Ascension = 4 }

    /// <summary>Visible Stagger state of a target.</summary>
    public enum StaggerState { Normal = 0, Staggered = 1, Broken = 2 }

    /// <summary>
    /// Typed, validated view over <see cref="CombatStatesValues"/> (data from CombatStates.csv). Holds no balance numbers:
    /// only derivations (thresholds, factors, CC rules). Pure C#.
    /// </summary>
    public sealed class CombatStateRules
    {
        public CombatStateRules(CombatStatesValues values)
        {
            V = values ?? throw new ArgumentNullException(nameof(values));
            Cc = new CcRules(V.CcDrWindowSeconds, new[] { V.CcDrFirst, V.CcDrSecond, V.CcDrThird },
                V.PlayerHardMaxSeconds, V.PlayerFreezeMaxSeconds, V.PlayerRootMaxSeconds, V.PlayerHardImmunitySeconds);
        }

        public CombatStatesValues V { get; }
        /// <summary>Rules for the Core <see cref="CrowdControlTracker"/> (R09).</summary>
        public CcRules Cc { get; }

        /// <summary>
        /// Float seconds hold representation noise (0.3f is 0.30000001), which would push a ceil to the next tick. This
        /// tolerance (a tenth of a millisecond) is far below one tick (16.7 ms) and only absorbs that noise.
        /// </summary>
        public const float FloatNoiseSeconds = 1e-4f;

        /// <summary>Seconds to whole 60 Hz frames, rounded up, absorbing float noise.</summary>
        public static int Frames(float seconds) => CcRules.SecondsToTicksCeil(seconds - FloatNoiseSeconds);

        /// <summary>Threshold multiplier by difficulty; Ascension = Eclipse + level x per-level (assumed base, see M05 log).</summary>
        public float DifficultyMultiplier(DifficultyTier tier, int ascensionLevel = 0)
        {
            switch (tier)
            {
                case DifficultyTier.Normal: return V.DifficultyNormal;
                case DifficultyTier.Veteran: return V.DifficultyVeteran;
                case DifficultyTier.Nightmare: return V.DifficultyNightmare;
                case DifficultyTier.Eclipse: return V.DifficultyEclipse;
                default: return V.DifficultyEclipse + V.DifficultyAscensionPerLevel * ascensionLevel;
            }
        }

        /// <summary>Base Stagger threshold of a tier (Elite multiplies Medium/Heavy only).</summary>
        public float BaseThreshold(StaggerTier tier, bool isElite)
        {
            switch (tier)
            {
                case StaggerTier.Medium: return V.StaggerThresholdMedium * (isElite ? V.StaggerThresholdEliteMult : 1f);
                case StaggerTier.Heavy: return V.StaggerThresholdHeavy * (isElite ? V.StaggerThresholdEliteMult : 1f);
                case StaggerTier.Guardian: return V.StaggerThresholdGuardian;
                case StaggerTier.Boss: return V.StaggerThresholdBoss;
                default: return 0f;
            }
        }

        /// <summary>Minimum Stagger of a single hit that makes the mass class flinch; +infinity = never flinches.</summary>
        public float FlinchMinimum(MassClass mass, bool isElite)
        {
            float e = isElite ? V.EliteFlinchMult : 1f;
            switch (mass)
            {
                case MassClass.Light: return V.FlinchMinStaggerLight * e;
                case MassClass.Medium: return V.FlinchMinStaggerMedium * e;
                default: return float.PositiveInfinity; // Heavy: no flinch; Colossal: never (06 3.8.1)
            }
        }

        /// <summary>Knockback/Pull received fraction by mass class (Elite multiplies it).</summary>
        public float KnockbackFactor(MassClass mass, bool isElite)
        {
            float f;
            switch (mass)
            {
                case MassClass.Light: f = V.KnockbackLight; break;
                case MassClass.Medium: f = V.KnockbackMedium; break;
                case MassClass.Heavy: f = V.KnockbackHeavy; break;
                default: return V.KnockbackColossal;
            }
            return isElite ? f * V.EliteKnockbackMult : f;
        }
    }
}
