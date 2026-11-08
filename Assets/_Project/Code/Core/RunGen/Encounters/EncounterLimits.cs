namespace Eclipse.Core.RunGen.Encounters
{
    /// <summary>
    /// Readability limits of 09 section 10.3. Defaults copied from the doc; overridden by data (M15 T5).
    /// "Live" is modeled as the units of ONE wave: leftovers of the previous wave (up to 30% of its PA, see
    /// <see cref="EncounterConfig.NextWaveAliveFraction"/>) are not added to the check.
    /// </summary>
    public sealed class EncounterLimits
    {
        /// <summary>09 section 10.3: live unit ceiling, summons included.</summary>
        public int MaxLiveUnits = 24;
        /// <summary>09 section 10.3: live PA ceiling.</summary>
        public int MaxLivePa = 40;
        /// <summary>09 section 10.3: casters per wave (themed recipes may override).</summary>
        public int MaxCastersPerWave = 2;
        /// <summary>09 section 10.3: summoners per wave (themed recipes may override).</summary>
        public int MaxSummonersPerWave = 1;
        /// <summary>09 section 10.3: specialists (caster, support, summoner, assassin) at most 40% of wave PA.</summary>
        public float MaxSpecialistShare = 0.40f;
        /// <summary>09 section 10.3: every wave has at least this many front line units.</summary>
        public int MinFrontlinePerWave = 1;
        /// <summary>09 section 10.3: tanks per wave up to <see cref="TankCapLastEarlyRow"/>.</summary>
        public int MaxTanksEarly = 1;
        /// <summary>09 section 10.3: tanks per wave after <see cref="TankCapLastEarlyRow"/>.</summary>
        public int MaxTanksLate = 2;
        /// <summary>09 section 10.3: "through row 6".</summary>
        public int TankCapLastEarlyRow = 6;

        public int MaxTanks(int row) => row <= TankCapLastEarlyRow ? MaxTanksEarly : MaxTanksLate;
    }
}
