using System.Collections.Generic;

namespace Eclipse.Core.RunGen.Encounters
{
    /// <summary>
    /// All numbers of the encounter planner. Defaults are copied from 09 section 10 and are overridden by
    /// data (M15 T5); the planner never hardcodes them.
    /// </summary>
    public sealed class EncounterConfig
    {
        // --- Budget (09 section 10.1): (BaseBudget + PerRowBudget x row) x density x region ---
        public float BaseBudget = 20f;
        public float PerRowBudget = 3f;
        public float DensityNormal = 1.0f;
        public float DensityVeteran = 1.1f;
        public float DensityNightmare = 1.2f;
        public float DensityEclipse = 1.3f;
        /// <summary>Ascension: base + per-level x N, capped (09 section 10.1).</summary>
        public float DensityAscensionBase = 1.3f;
        public float DensityAscensionPerLevel = 0.005f;
        public float DensityAscensionCap = 1.8f;
        /// <summary>Elite Arena multiplier (09 section 10.1).</summary>
        public float EliteArenaMult = 1.2f;
        /// <summary>Minimum share of the total PA in Elite units for Elite Arena (09 section 10.1).</summary>
        public float EliteArenaMinEliteShare = 0.5f;

        // --- Waves (09 section 10.2) ---
        /// <summary>Rows 1..this use two waves; later rows use three.</summary>
        public int TwoWaveLastRow = 4;
        public float[] TwoWaveSplit = { 0.55f, 0.45f };
        public float[] ThreeWaveSplit = { 0.40f, 0.35f, 0.25f };

        // --- Runtime (09 section 10.2) ---
        /// <summary>Next wave when alive PA drops below this fraction of the current wave PA.</summary>
        public float NextWaveAliveFraction = 0.30f;
        /// <summary>... or after this many seconds, whichever comes first.</summary>
        public float NextWaveMaxSeconds = 25f;
        /// <summary>Spawns happen at least this far (m) from the player.</summary>
        public float MinSpawnDistanceMeters = 8f;
        /// <summary>Spawn portal duration (s); enemies cannot hit nor be hit during it.</summary>
        public float SpawnPortalSeconds = 0.8f;

        public EncounterLimits Limits = new EncounterLimits();
        public List<EncounterRecipe> Recipes = EncounterRecipe.CreateDefaults();

        public static EncounterConfig Default() => new EncounterConfig();
    }
}
