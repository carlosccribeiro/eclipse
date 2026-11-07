using System;
using Eclipse.Core.Time;

namespace Eclipse.Core.CrowdControl
{
    /// <summary>
    /// Tunable CC parameters (06 §3.8.3, R09). Values are always injected (data-driven); no defaults live in runtime code. Durations are stored as whole ticks (60 Hz).
    /// </summary>
    public sealed class CcRules
    {
        private readonly float[] _sequence;

        public CcRules(float windowSeconds, float[] sequence, float playerHardMaxSeconds,
            float playerFreezeMaxSeconds, float playerRootMaxSeconds, float playerHardImmunitySeconds)
        {
            if (sequence == null || sequence.Length == 0) throw new ArgumentException("Sequence required.", nameof(sequence));
            WindowTicks = Ticks(windowSeconds, nameof(windowSeconds));
            _sequence = (float[])sequence.Clone();
            for (int i = 0; i < _sequence.Length; i++)
                if (float.IsNaN(_sequence[i]) || _sequence[i] < 0f)
                    throw new ArgumentOutOfRangeException(nameof(sequence));
            PlayerHardMaxTicks = Ticks(playerHardMaxSeconds, nameof(playerHardMaxSeconds));
            PlayerFreezeMaxTicks = Ticks(playerFreezeMaxSeconds, nameof(playerFreezeMaxSeconds));
            PlayerRootMaxTicks = Ticks(playerRootMaxSeconds, nameof(playerRootMaxSeconds));
            PlayerHardImmunityTicks = Ticks(playerHardImmunitySeconds, nameof(playerHardImmunitySeconds));
        }

        /// <summary>DR window length, counted from the first CC of the group.</summary>
        public int WindowTicks { get; }
        /// <summary>Number of applications that get a non-immune multiplier within a window.</summary>
        public int SequenceLength => _sequence.Length;
        public int PlayerHardMaxTicks { get; }
        public int PlayerFreezeMaxTicks { get; }
        public int PlayerRootMaxTicks { get; }
        /// <summary>Hard immunity after any Hard on the player, counted from the end of that Hard.</summary>
        public int PlayerHardImmunityTicks { get; }

        /// <summary>Duration multiplier for the (0-based) nth application in a window; 0 beyond the sequence.</summary>
        public float SequenceMultiplier(int index) => index >= 0 && index < _sequence.Length ? _sequence[index] : 0f;

        /// <summary>Seconds to whole ticks, rounded up (tolerates float noise of 1e-9 ticks).</summary>
        public static int SecondsToTicksCeil(double seconds)
        {
            double t = Math.Ceiling(seconds * SimulationClock.TickRate - 1e-9);
            if (t < 0.0) return 0;
            if (t > int.MaxValue) return int.MaxValue;
            return (int)t;
        }

        private static int Ticks(float seconds, string name)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(name, "Must be finite and non-negative.");
            return SecondsToTicksCeil(seconds);
        }
    }
}
