using System;
using Eclipse.Core.Loot;

namespace Eclipse.Core.RunGen.Encounters
{
    /// <summary>Room budget math of 09 section 10.1 (pure functions).</summary>
    public static class ThreatBudget
    {
        private const double Epsilon = 1e-9;

        /// <summary>Density multiplier per difficulty; <paramref name="ascensionLevel"/> (1-100) only used by Ascension.</summary>
        public static float DensityMult(EncounterConfig cfg, Difficulty difficulty, int ascensionLevel)
        {
            switch (difficulty)
            {
                case Difficulty.Veteran: return cfg.DensityVeteran;
                case Difficulty.Nightmare: return cfg.DensityNightmare;
                case Difficulty.Eclipse: return cfg.DensityEclipse;
                case Difficulty.Ascension:
                    return Math.Min(cfg.DensityAscensionCap, cfg.DensityAscensionBase + cfg.DensityAscensionPerLevel * Math.Max(0, ascensionLevel));
                default: return cfg.DensityNormal;
            }
        }

        /// <summary>Exact (fractional) budget: (20 + 3 x row) x density x region, times the Elite Arena factor.</summary>
        public static double Compute(EncounterConfig cfg, int row, Difficulty difficulty, int ascensionLevel, float regionMult, bool eliteArena)
        {
            double b = (double)(cfg.BaseBudget + cfg.PerRowBudget * row) * DensityMult(cfg, difficulty, ascensionLevel) * regionMult;
            if (eliteArena) b *= cfg.EliteArenaMult;
            return Math.Round(b, 6); // absorbs float noise of the multipliers (e.g. 1.2f) so floor() is exact
        }

        /// <summary>Whole PA that can be spent without overshooting the budget.</summary>
        public static int Spendable(double budget) => (int)Math.Floor(budget + Epsilon);
    }
}
