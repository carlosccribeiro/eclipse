using System;

namespace Eclipse.Core.Random
{
    /// <summary>
    /// xoshiro128** generator (Blackman/Vigna). Mutable value type: pass by ref or keep in a field.
    /// Pure integer arithmetic, so results are identical on every platform.
    /// </summary>
    public struct Xoshiro128StarStar
    {
        private uint _s0, _s1, _s2, _s3;

        /// <summary>Raw state constructor. An all-zero state is invalid and throws.</summary>
        public Xoshiro128StarStar(uint s0, uint s1, uint s2, uint s3)
        {
            if ((s0 | s1 | s2 | s3) == 0)
                throw new ArgumentException("xoshiro128** state must not be all zero.");
            _s0 = s0; _s1 = s1; _s2 = s2; _s3 = s3;
        }

        /// <summary>
        /// Seeds via SplitMix64: two 64-bit outputs split into (lo, hi) uints give s0..s3.
        /// If the result is all zero (practically impossible), s0 is set to 0x9E3779B9.
        /// </summary>
        public static Xoshiro128StarStar FromSeed(ulong seed)
        {
            ulong sm = seed;
            ulong a = SplitMix64.Next(ref sm);
            ulong b = SplitMix64.Next(ref sm);
            uint s0 = unchecked((uint)a), s1 = unchecked((uint)(a >> 32));
            uint s2 = unchecked((uint)b), s3 = unchecked((uint)(b >> 32));
            if ((s0 | s1 | s2 | s3) == 0) s0 = 0x9E3779B9u;
            return new Xoshiro128StarStar(s0, s1, s2, s3);
        }

        /// <summary>Current state (for save/tests); rebuild with the 4-uint constructor.</summary>
        public (uint s0, uint s1, uint s2, uint s3) GetState() => (_s0, _s1, _s2, _s3);

        private static uint Rotl(uint x, int k) => (x << k) | (x >> (32 - k));

        /// <summary>Next 32 random bits.</summary>
        public uint NextUInt()
        {
            unchecked
            {
                uint result = Rotl(_s1 * 5u, 7) * 9u;
                uint t = _s1 << 9;
                _s2 ^= _s0;
                _s3 ^= _s1;
                _s1 ^= _s2;
                _s0 ^= _s3;
                _s2 ^= t;
                _s3 = Rotl(_s3, 11);
                return result;
            }
        }

        /// <summary>Next value as a non-negative int (31 high bits).</summary>
        public int NextInt() => (int)(NextUInt() >> 1);

        /// <summary>Maps 32 bits to [0,1) using the 24 high bits; never returns 1.</summary>
        public static float ToFloat01(uint x) => (x >> 8) * (1f / 16777216f);

        /// <summary>Float in [0,1).</summary>
        public float NextFloat01() => ToFloat01(NextUInt());

        /// <summary>Unbiased integer in [min, max) by rejection sampling. Throws if max &lt;= min.</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentException("maxExclusive must be greater than minInclusive.");
            unchecked
            {
                uint range = (uint)maxExclusive - (uint)minInclusive; // 1..2^32-1
                uint threshold = (0u - range) % range;
                uint x;
                do { x = NextUInt(); } while (x < threshold);
                return (int)((uint)minInclusive + x % range);
            }
        }

        /// <summary>
        /// True with probability p. Always consumes exactly one output (keeps sequences stable);
        /// p &lt;= 0 (or NaN) is always false, p &gt;= 1 always true.
        /// </summary>
        public bool Chance(float p) => NextFloat01() < p;
    }
}
