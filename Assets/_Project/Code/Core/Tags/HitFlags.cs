using System;

namespace Eclipse.Core.Tags
{
    /// <summary>
    /// Per-hit flags of a DamageInstance (R02, 06 3.3). Separate from <see cref="Tag"/>; never mix the two.
    /// </summary>
    [Flags]
    public enum HitFlags : ushort
    {
        None = 0,
        Unavoidable = 1 << 0,
        Unblockable = 1 << 1,
        Annihilation = 1 << 2,
        Knockdown = 1 << 3,
        Execute = 1 << 4,
        IsEcho = 1 << 5,
        IsResonance = 1 << 6,
        IsTriggered = 1 << 7,
    }

    /// <summary>Allocation-free helpers for <see cref="HitFlags"/>.</summary>
    public static class HitFlagsExtensions
    {
        /// <summary>True if every bit of <paramref name="flag"/> is set (bit test, no boxing).</summary>
        public static bool Has(this HitFlags flags, HitFlags flag) => ((ushort)flags & (ushort)flag) == (ushort)flag;
    }
}
