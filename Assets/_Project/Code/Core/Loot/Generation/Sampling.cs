using System;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// Sampling helpers the Core RNG does not provide (float range, weighted pick, quality roll).
    /// All of them draw from the caller's stream by <c>ref</c>, so the sequence is fully deterministic.
    /// </summary>
    public static class Sampling
    {
        /// <summary>Uniform float in [min, max).</summary>
        public static float Range(ref Xoshiro128StarStar rng, float min, float max) => min + (max - min) * rng.NextFloat01();

        /// <summary>
        /// Index drawn proportionally to <paramref name="weights"/>[0..count) (non-positive weights are never picked).
        /// Returns -1, consuming no output, when the total weight is zero; otherwise consumes exactly one output.
        /// </summary>
        public static int PickWeighted(float[] weights, int count, ref Xoshiro128StarStar rng)
        {
            float total = 0f;
            for (int i = 0; i < count; i++) if (weights[i] > 0f) total += weights[i];
            if (total <= 0f) return -1;
            float t = rng.NextFloat01() * total;
            float acc = 0f;
            int last = -1;
            for (int i = 0; i < count; i++)
            {
                if (weights[i] <= 0f) continue;
                acc += weights[i];
                last = i;
                if (t < acc) return i;
            }
            return last;
        }

        /// <summary>
        /// Roll01 with a difficulty floor (05 section 10.1): resampled uniform in [min, 1), not a clamp, so no mass piles on the floor.
        /// Always consumes one output.
        /// </summary>
        public static float RollQuality(ref Xoshiro128StarStar rng, float min)
        {
            if (min < 0f) min = 0f;
            if (min > 1f) min = 1f;
            return min + (1f - min) * rng.NextFloat01();
        }

        /// <summary>Stochastic rounding: floor(x) plus 1 with probability frac(x). Always consumes one output.</summary>
        public static int StochasticRound(float x, ref Xoshiro128StarStar rng)
        {
            if (x < 0f) x = 0f;
            int whole = (int)Math.Floor(x);
            float frac = x - whole;
            return rng.NextFloat01() < frac ? whole + 1 : whole;
        }

        /// <summary>Grows <paramref name="arr"/> so it holds at least <paramref name="n"/> items (contents are not preserved as meaningful).</summary>
        public static void Ensure<T>(ref T[] arr, int n)
        {
            if (arr.Length >= n) return;
            int size = arr.Length * 2;
            if (size < n) size = n;
            Array.Resize(ref arr, size);
        }
    }
}
