using System;
using Eclipse.Core.Ids;

namespace Eclipse.Core.Random
{
    /// <summary>
    /// The five seeded streams of a run. Derivation (frozen, affects saves and shared seeds):
    /// <code>
    /// sm = runSeed ^ ((index + 1) * 0xD1B54A32D192ED03)   // ulong, unchecked
    /// streamSeed[i] = SplitMix64.Next(ref sm)
    /// generator[i]  = Xoshiro128StarStar.FromSeed(streamSeed[i])
    /// </code>
    /// Fork: <c>FromSeed(streamSeed[i] ^ Fnv1a64(key.Value))</c>; Fnv1a64 is FNV-1a 64 over the
    /// UTF-16 chars, each char mixed as low byte then high byte (offset 0xCBF29CE484222325, prime 0x100000001B3).
    /// </summary>
    public sealed class RngStreams
    {
        private const int Count = 5;
        private readonly Xoshiro128StarStar[] _gens = new Xoshiro128StarStar[Count];
        private readonly ulong[] _seeds = new ulong[Count];

        public RngStreams(ulong runSeed)
        {
            RunSeed = runSeed;
            for (int i = 0; i < Count; i++)
            {
                ulong sm = unchecked(runSeed ^ ((ulong)(i + 1) * 0xD1B54A32D192ED03UL));
                _seeds[i] = SplitMix64.Next(ref sm);
                _gens[i] = Xoshiro128StarStar.FromSeed(_seeds[i]);
            }
        }

        public ulong RunSeed { get; }

        /// <summary>Reference to the live generator of a stream (consume without copying).</summary>
        public ref Xoshiro128StarStar Get(RngStream stream)
        {
            int i = (int)stream;
            if ((uint)i >= Count) throw new ArgumentOutOfRangeException(nameof(stream));
            return ref _gens[i];
        }

        /// <summary>Deterministic sub-generator from stream seed and key. Does not consume the parent stream.</summary>
        public Xoshiro128StarStar Fork(RngStream stream, StableId key)
        {
            int i = (int)stream;
            if ((uint)i >= Count) throw new ArgumentOutOfRangeException(nameof(stream));
            return Xoshiro128StarStar.FromSeed(_seeds[i] ^ Fnv1a64(key.Value));
        }

        /// <summary>FNV-1a 64 over UTF-16 chars (low byte, then high byte). Stable across platforms.</summary>
        public static ulong Fnv1a64(string s)
        {
            unchecked
            {
                ulong h = 0xCBF29CE484222325UL;
                for (int i = 0; i < s.Length; i++)
                {
                    h = (h ^ (byte)s[i]) * 0x100000001B3UL;
                    h = (h ^ (byte)(s[i] >> 8)) * 0x100000001B3UL;
                }
                return h;
            }
        }
    }
}
