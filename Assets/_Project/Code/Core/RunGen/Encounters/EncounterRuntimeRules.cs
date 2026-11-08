namespace Eclipse.Core.RunGen.Encounters
{
    /// <summary>Pure runtime wave rules of 09 section 10.2; the spawner of M16 only calls these.</summary>
    public static class EncounterRuntimeRules
    {
        /// <summary>
        /// True when alive PA is below 30% of the current wave PA, or the wave is 25 s old (whichever first).
        /// A wave with no PA counts as already cleared.
        /// </summary>
        public static bool ShouldSpawnNextWave(EncounterConfig cfg, float aliveThreat, float currentWaveThreat, float secondsSinceWaveStart)
        {
            if (secondsSinceWaveStart >= cfg.NextWaveMaxSeconds) return true;
            if (currentWaveThreat <= 0f) return true;
            return aliveThreat < cfg.NextWaveAliveFraction * currentWaveThreat;
        }

        public static bool ShouldSpawnNextWave(float aliveThreat, float currentWaveThreat, float secondsSinceWaveStart) =>
            ShouldSpawnNextWave(EncounterConfig.Default(), aliveThreat, currentWaveThreat, secondsSinceWaveStart);

        /// <summary>True when a spawn point is at least the minimum distance (8 m) from the player.</summary>
        public static bool IsValidSpawnDistance(EncounterConfig cfg, float distanceMeters) => distanceMeters >= cfg.MinSpawnDistanceMeters;

        public static bool IsValidSpawnDistance(float distanceMeters) => IsValidSpawnDistance(EncounterConfig.Default(), distanceMeters);

        /// <summary>Portal duration in seconds (0.8).</summary>
        public static float PortalSeconds(EncounterConfig cfg) => cfg.SpawnPortalSeconds;

        public static float PortalSeconds() => EncounterConfig.Default().SpawnPortalSeconds;
    }
}
