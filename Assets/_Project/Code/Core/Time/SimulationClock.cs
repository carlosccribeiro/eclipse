using System;

namespace Eclipse.Core.Time
{
    /// <summary>
    /// Fixed 60 Hz simulation time base (R04). Converts frame data (frames at 60 fps) to seconds and
    /// turns variable frame deltas into a whole number of simulation ticks.
    /// </summary>
    public sealed class SimulationClock
    {
        /// <summary>Simulation ticks per second.</summary>
        public const int TickRate = 60;

        /// <summary>Duration of one tick in seconds (float).</summary>
        public const float TickDuration = 1f / 60f;

        /// <summary>Duration of one tick in seconds (double precision).</summary>
        public const double TickDurationDouble = 1.0 / 60.0;

        /// <summary>Default cap on ticks returned by one <see cref="Advance"/> call.</summary>
        public const int DefaultMaxTicksPerAdvance = 8;

        // Absorbs float rounding (e.g. 60 x (1f/60f) or Advance(1f)) so a tick is never lost by ~1e-9 s.
        private const double Epsilon = 1e-9;
        private const long MaxDroppedTicks = long.MaxValue;

        private double _accumulator;

        /// <summary>Creates a clock; <paramref name="maxTicksPerAdvance"/> must be at least 1.</summary>
        public SimulationClock(int maxTicksPerAdvance = DefaultMaxTicksPerAdvance)
        {
            if (maxTicksPerAdvance < 1)
                throw new ArgumentOutOfRangeException(nameof(maxTicksPerAdvance), "Must be >= 1.");
            MaxTicksPerAdvance = maxTicksPerAdvance;
        }

        /// <summary>Cap on ticks per <see cref="Advance"/> call (anti spiral of death).</summary>
        public int MaxTicksPerAdvance { get; }

        /// <summary>Simulated ticks since start (or last <see cref="Reset"/>).</summary>
        public long Tick { get; private set; }

        /// <summary>Total ticks discarded because of <see cref="MaxTicksPerAdvance"/> (diagnostics).</summary>
        public long DroppedTicks { get; private set; }

        /// <summary>Simulated time in seconds, derived from <see cref="Tick"/>.</summary>
        public double ElapsedSeconds => Tick / (double)TickRate;

        /// <summary>Fraction of the next tick already accumulated, in [0,1), for visual interpolation.</summary>
        public float Alpha
        {
            get
            {
                double a = _accumulator / TickDurationDouble;
                if (a <= 0.0) return 0f;
                float f = (float)a;
                return f >= 1f ? 0.99999994f : f;
            }
        }

        /// <summary>Converts frames at 60 fps to seconds (frames / 60f).</summary>
        public static float FramesToSeconds(int frames) => frames / 60f;

        /// <summary>
        /// Converts seconds to frames, rounding half away from zero on seconds*60.
        /// Throws <see cref="ArgumentOutOfRangeException"/> for negative, NaN or overflowing input.
        /// </summary>
        public static int SecondsToFrames(float seconds)
        {
            double f = (double)seconds * TickRate;
            if (double.IsNaN(f) || f < 0.0 || f > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(seconds), "Must be a finite non-negative value within range.");
            return (int)Math.Round(f, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Adds <paramref name="deltaSeconds"/> and returns how many ticks to simulate now (also added to
        /// <see cref="Tick"/>). The sub-tick remainder is kept for the next call. If more than
        /// <see cref="MaxTicksPerAdvance"/> ticks are due, the excess whole ticks are discarded and counted
        /// in <see cref="DroppedTicks"/> (saturating). Throws for negative, NaN or infinite input.
        /// </summary>
        public int Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Must be finite and non-negative.");

            _accumulator += deltaSeconds;
            double due = Math.Floor((_accumulator + Epsilon) / TickDurationDouble);
            if (due < 1.0) return 0;

            _accumulator -= due * TickDurationDouble;
            // With huge inputs the subtraction loses precision; never keep more than one tick of remainder.
            if (_accumulator < 0.0 || _accumulator >= TickDurationDouble) _accumulator = 0.0;

            // Compare in double before casting: a huge dt (e.g. float.MaxValue) must not overflow (long),
            // whose out-of-range behaviour differs between .NET and Mono.
            int run = due > MaxTicksPerAdvance ? MaxTicksPerAdvance : (int)due;
            double dropped = due - run;
            DroppedTicks = dropped >= MaxDroppedTicks - DroppedTicks ? MaxDroppedTicks : DroppedTicks + (long)dropped;
            Tick += run;
            return run;
        }

        /// <summary>Zeroes ticks, accumulator and dropped counter.</summary>
        public void Reset()
        {
            _accumulator = 0.0;
            Tick = 0;
            DroppedTicks = 0;
        }
    }
}
