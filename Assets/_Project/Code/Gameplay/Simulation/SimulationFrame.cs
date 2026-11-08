namespace Eclipse.Gameplay.Simulation
{
    /// <summary>Immutable snapshot of one fixed simulation tick (06 §2.1: 60 Hz, frame data in frames).</summary>
    public readonly struct SimulationFrame
    {
        /// <summary>Fixed tick rate in Hz (06 §2.1, 11 §3.9).</summary>
        public const int TicksPerSecond = 60;
        public const float FixedDeltaSeconds = 1f / TicksPerSecond;

        /// <summary>Global frame counter; the first tick has index 1.</summary>
        public readonly int Index;

        public SimulationFrame(int index) { Index = index; }

        public float DeltaSeconds => FixedDeltaSeconds;
    }
}
