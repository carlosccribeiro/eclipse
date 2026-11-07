namespace Eclipse.Core.Stats
{
    /// <summary>Bitmask of <see cref="StatId"/> (bit index = enum value).</summary>
    public readonly struct StatMask
    {
        public StatMask(ulong bits) { Bits = bits; }

        public ulong Bits { get; }
        public bool IsEmpty => Bits == 0;

        public static StatMask Of(StatId stat) => new StatMask(1UL << (int)stat);
        public bool Has(StatId stat) => (Bits & (1UL << (int)stat)) != 0;
        public bool HasAny(StatMask other) => (Bits & other.Bits) != 0;
        public StatMask With(StatId stat) => new StatMask(Bits | (1UL << (int)stat));
    }

    /// <summary>Published by <see cref="StatBlock"/> when mods or bases of the listed stats change (11 3.5.3).</summary>
    public readonly struct StatsChanged
    {
        public StatsChanged(StatMask mask) { Mask = mask; }
        public StatMask Mask { get; }
    }
}
