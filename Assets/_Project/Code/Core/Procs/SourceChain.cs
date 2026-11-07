using Eclipse.Core.Random;

namespace Eclipse.Core.Procs
{
    /// <summary>
    /// Short allocation-free trail of the sources that led to a proc (for debugging and loop detection).
    /// Holds at most <see cref="MaxLength"/> entries because Generation is capped at 2 (06 3.2, R07):
    /// Gen 0 origin, Gen 1 proc, Gen 2 proc. Entries are stable FNV-1a hashes of source slugs.
    /// </summary>
    public readonly struct SourceChain
    {
        /// <summary>Structural limit: one entry per Generation 0..2 (06 3.2 Generation table).</summary>
        public const int MaxLength = 3;

        private readonly ulong _a;
        private readonly ulong _b;
        private readonly ulong _c;

        public int Length { get; }

        private SourceChain(int length, ulong a, ulong b, ulong c)
        {
            Length = length;
            _a = a;
            _b = b;
            _c = c;
        }

        public static SourceChain Empty => default;

        /// <summary>Hash of the entry at <paramref name="index"/> (0 = original source).</summary>
        public ulong this[int index]
        {
            get
            {
                if (index < 0 || index >= Length) throw new System.ArgumentOutOfRangeException(nameof(index));
                return index == 0 ? _a : index == 1 ? _b : _c;
            }
        }

        public static SourceChain FromSlug(string sourceSlug) => Empty.Append(RngStreams.Fnv1a64(sourceSlug));

        /// <summary>Returns a new chain with the source hash appended; entries beyond the cap are dropped.</summary>
        public SourceChain Append(ulong sourceHash)
        {
            switch (Length)
            {
                case 0: return new SourceChain(1, sourceHash, 0, 0);
                case 1: return new SourceChain(2, _a, sourceHash, 0);
                case 2: return new SourceChain(3, _a, _b, sourceHash);
                default: return this;
            }
        }

        public bool Contains(ulong sourceHash)
        {
            return (Length > 0 && _a == sourceHash) || (Length > 1 && _b == sourceHash) || (Length > 2 && _c == sourceHash);
        }
    }
}
