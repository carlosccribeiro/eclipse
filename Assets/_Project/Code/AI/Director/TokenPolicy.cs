using Eclipse.Data.Difficulties;

namespace Eclipse.AI.Director
{
    /// <summary>Enemy classes for token purposes (09 section 3.2).</summary>
    public enum TokenClass : byte
    {
        Normal = 0,
        /// <summary>Does not consume a token; strong attacks obey the minimum interval.</summary>
        Elite = 1,
        /// <summary>Does not consume a token; strong attacks obey the minimum interval.</summary>
        Guardian = 2,
        /// <summary>Ignores tokens and interval entirely.</summary>
        Boss = 3,
        /// <summary>Uses the exclusive ambush token pool.</summary>
        Assassin = 4
    }

    /// <summary>
    /// Orbit band for enemies without a token (09 section 3.2: orbits 4 to 7 m from the player). Read by the brain.
    /// </summary>
    public struct TokenOrbitConfig
    {
        public float MinMeters;
        public float MaxMeters;

        /// <summary>09 section 3.2: 4 to 7 m.</summary>
        public static TokenOrbitConfig Default => new TokenOrbitConfig { MinMeters = 4f, MaxMeters = 7f };
    }

    /// <summary>
    /// Token limits for one difficulty (09 section 3.2). Fill from <see cref="DifficultyDefinition"/> via
    /// <see cref="From"/>; no balance numbers live in code.
    /// </summary>
    public struct TokenPolicy
    {
        public int MeleeTokens;
        public int RangedTokens;
        /// <summary>Ambush tokens for Assassins (1 Normal/Veteran, 2 Nightmare+).</summary>
        public int AmbushTokens;
        /// <summary>Minimum seconds between attacks granted to the same enemy.</summary>
        public float MinIntervalSeconds;
        public TokenOrbitConfig Orbit;

        public static TokenPolicy From(DifficultyDefinition difficulty)
        {
            return new TokenPolicy
            {
                MeleeTokens = difficulty.TokensMelee,
                RangedTokens = difficulty.TokensRanged,
                AmbushTokens = difficulty.AmbushTokens,
                MinIntervalSeconds = difficulty.MinTokenIntervalSeconds,
                Orbit = TokenOrbitConfig.Default
            };
        }
    }
}
