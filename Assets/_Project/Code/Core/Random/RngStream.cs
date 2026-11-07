namespace Eclipse.Core.Random
{
    /// <summary>Independent RNG streams of a run. Order is part of the seed derivation: never reorder.</summary>
    public enum RngStream : byte
    {
        Map,
        Encounter,
        Loot,
        Combat,
        Cosmetic
    }
}
