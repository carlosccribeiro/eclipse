using Eclipse.Core.Tags;

namespace Eclipse.Core.Procs
{
    /// <summary>
    /// Immutable proc state carried by every hit/event (06 3.2 Generation table, R07).
    /// Gen 0 = original hit; each proc is Gen+1; nothing triggers at Gen >= 2.
    /// </summary>
    public readonly struct ProcContext
    {
        /// <summary>Structural cap of the Generation model (06 3.2, R07): values are 0..2.</summary>
        public const int MaxGeneration = 2;

        public int Generation { get; }
        public HitFlags Flags { get; }
        public SourceChain SourceChain { get; }

        /// <summary>Proc Coefficient 0..1 (06 3.2).</summary>
        public float ProcCoefficient { get; }

        /// <summary>The power declares "Echo Recursion 1x" (06 3.2; 04, 08).</summary>
        public bool AllowEchoRecursionOnce { get; }

        /// <summary>The single recursion allowed by Echo Recursion 1x was already spent in this replica chain.</summary>
        public bool EchoRecursionUsed { get; }

        public bool IsEcho => Flags.Has(HitFlags.IsEcho);

        public ProcContext(int generation, HitFlags flags, SourceChain sourceChain, float procCoefficient,
            bool allowEchoRecursionOnce = false, bool echoRecursionUsed = false)
        {
            if (generation < 0) generation = 0;
            if (generation > MaxGeneration) generation = MaxGeneration;
            Generation = generation;
            Flags = flags;
            SourceChain = sourceChain;
            ProcCoefficient = procCoefficient < 0f ? 0f : procCoefficient > 1f ? 1f : procCoefficient;
            AllowEchoRecursionOnce = allowEchoRecursionOnce;
            EchoRecursionUsed = echoRecursionUsed;
        }

        /// <summary>Original hit: Generation 0, coefficient of the source (default 1.0).</summary>
        public static ProcContext Original(SourceChain chain, float procCoefficient = 1f, HitFlags flags = HitFlags.None,
            bool allowEchoRecursionOnce = false)
        {
            return new ProcContext(0, flags, chain, procCoefficient, allowEchoRecursionOnce);
        }

        public ProcContext WithGeneration(int generation) =>
            new ProcContext(generation, Flags, SourceChain, ProcCoefficient, AllowEchoRecursionOnce, EchoRecursionUsed);

        public ProcContext WithFlags(HitFlags flags) =>
            new ProcContext(Generation, flags, SourceChain, ProcCoefficient, AllowEchoRecursionOnce, EchoRecursionUsed);

        public ProcContext WithProcCoefficient(float coefficient) =>
            new ProcContext(Generation, Flags, SourceChain, coefficient, AllowEchoRecursionOnce, EchoRecursionUsed);

        public ProcContext WithEchoRecursionUsed(bool used) =>
            new ProcContext(Generation, Flags, SourceChain, ProcCoefficient, AllowEchoRecursionOnce, used);

        public ProcContext WithSourceChain(SourceChain chain) =>
            new ProcContext(Generation, Flags, chain, ProcCoefficient, AllowEchoRecursionOnce, EchoRecursionUsed);
    }
}
