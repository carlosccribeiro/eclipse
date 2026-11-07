namespace Eclipse.Core.Random
{
    /// <summary>SplitMix64 (Vigna): seed expander and 64-bit mixer. Integer-only, unchecked wraparound.</summary>
    public static class SplitMix64
    {
        /// <summary>Advances <paramref name="state"/> and returns the next 64-bit output.</summary>
        public static ulong Next(ref ulong state)
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
