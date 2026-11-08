namespace Eclipse.Data.Difficulties
{
    /// <summary>Difficulty tiers (09 section 3.2, 06 section 3.13). Ascension is the open-ended tier above Eclipse.</summary>
    public enum DifficultyTier : byte
    {
        Normal = 0,
        Veteran = 1,
        Nightmare = 2,
        Eclipse = 3,
        Ascension = 4,
    }
}
