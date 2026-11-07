using System;

namespace Eclipse.Core.Procs
{
    /// <summary>Target class for Execute rules (06 3.2 Execute table, R08).</summary>
    public enum ExecuteTargetClass
    {
        Common = 0,
        Elite = 1,
        Guardian = 2,
        Boss = 3,
    }

    /// <summary>Balance values of Execute (R08): max threshold 20%, Elite factor 0.5, boss conversion +25% below 20% HP.</summary>
    public readonly struct ExecuteConfig
    {
        public float MaxThreshold { get; }
        public float EliteFactor { get; }
        public float BossVulnerabilityBonus { get; }
        public float BossHealthThreshold { get; }

        public ExecuteConfig(float maxThreshold, float eliteFactor, float bossVulnerabilityBonus, float bossHealthThreshold)
        {
            if (maxThreshold < 0f || maxThreshold > 1f) throw new ArgumentOutOfRangeException(nameof(maxThreshold));
            if (eliteFactor < 0f || eliteFactor > 1f) throw new ArgumentOutOfRangeException(nameof(eliteFactor));
            if (bossVulnerabilityBonus < 0f) throw new ArgumentOutOfRangeException(nameof(bossVulnerabilityBonus));
            if (bossHealthThreshold < 0f || bossHealthThreshold > 1f) throw new ArgumentOutOfRangeException(nameof(bossHealthThreshold));
            MaxThreshold = maxThreshold;
            EliteFactor = eliteFactor;
            BossVulnerabilityBonus = bossVulnerabilityBonus;
            BossHealthThreshold = bossHealthThreshold;
        }
    }

    /// <summary>Execute threshold and boss conversion (06 3.2 Execute, R08).</summary>
    public sealed class ExecuteRules
    {
        private readonly ExecuteConfig _config;

        public ExecuteRules(ExecuteConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Effective instant-kill threshold as a health fraction. The summed source threshold is clamped to the
        /// absolute cap; Elites get half of that effective value; Guardians and bosses are immune (0).
        /// </summary>
        public float EffectiveThreshold(ExecuteTargetClass target, float summedThreshold)
        {
            float capped = summedThreshold < 0f ? 0f : Math.Min(summedThreshold, _config.MaxThreshold);
            switch (target)
            {
                case ExecuteTargetClass.Common: return capped;
                case ExecuteTargetClass.Elite: return capped * _config.EliteFactor;
                default: return 0f;
            }
        }

        /// <summary>True if a hit carrying Execute kills the target instantly (health strictly below the threshold).</summary>
        public bool ShouldExecute(ExecuteTargetClass target, float healthFraction, float summedThreshold)
        {
            float t = EffectiveThreshold(target, summedThreshold);
            return t > 0f && healthFraction < t;
        }

        /// <summary>
        /// Vulnerability granted by the boss conversion: one single bonus while the boss is below the health threshold
        /// and at least one Execute source is active. Sources never stack (R08). Only bosses convert; the caller counts
        /// the bonus inside the 07 Vulnerability cap.
        /// </summary>
        public float BossVulnerability(ExecuteTargetClass target, float healthFraction, int activeExecuteSources)
        {
            if (target != ExecuteTargetClass.Boss || activeExecuteSources <= 0) return 0f;
            return healthFraction < _config.BossHealthThreshold ? _config.BossVulnerabilityBonus : 0f;
        }
    }
}
