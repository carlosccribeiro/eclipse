using System;
using Eclipse.Core.Items;

namespace Eclipse.Core.Loot.Pity
{
    /// <summary>
    /// Stateless pity formulas of 05 section 8. Every number comes from <see cref="PityConfigData"/> (loot catalog).
    /// The generator (RarityRoller / LegendaryKindSelector) applies the same soft/hard/set rules from <see cref="IPityState"/>;
    /// these helpers expose them for the facade, UI and tests. The x3 weight of unowned set pieces / boss Uniques (8.3, 8.5)
    /// is applied by NamedItemSelector through IItemCollection and <c>DuplicateUnownedWeight</c>.
    /// </summary>
    public static class PityRules
    {
        /// <summary>Soft pity multiplier 1 + SoftStep x (drought - SoftStart) for drought > SoftStart, else 1 (05 section 8.1). Not yet capped.</summary>
        public static float SoftMultiplier(PityConfigData cfg, int drought)
        {
            if (cfg.SoftStart <= 0 || drought <= cfg.SoftStart) return 1f;
            return 1f + cfg.SoftStep * (drought - cfg.SoftStart);
        }

        /// <summary>Legendary chance after soft pity, capped at LegendaryCapMultiplier x base.</summary>
        public static float LegendaryChance(PityConfigData cfg, float baseChance, int drought)
        {
            float l = baseChance * SoftMultiplier(cfg, drought);
            if (cfg.LegendaryCapMultiplier > 0f) l = Math.Min(l, baseChance * cfg.LegendaryCapMultiplier);
            return l;
        }

        /// <summary>True when the next item of this source must be a guaranteed Legendary (trash never consumes it).</summary>
        public static bool IsHardPity(PityConfigData cfg, LootSourceData src, int drought)
            => cfg.Hard > 0 && drought >= cfg.Hard && src.ConsumesHardPity;

        /// <summary>True when the next compatible Legendary must be a set piece (05 section 8.2; SetPityAfter 0 = disabled, e.g. Normal).</summary>
        public static bool IsSetPity(PityConfigData cfg, int legendariesWithoutSet)
            => cfg.SetPityAfter > 0 && legendariesWithoutSet >= cfg.SetPityAfter;

        /// <summary>Boss exclusive Uniques exist from Nightmare on (05 section 8.5).</summary>
        public static bool BossUniquesAvailable(Difficulty d) => d >= Difficulty.Nightmare;

        /// <summary>Per-kill chance of a boss Unique: base + per-Ascension step x (level - 1); times the Echo multiplier.</summary>
        public static float BossUniqueChance(PityConfigData cfg, Difficulty d, int ascensionLevel, bool isBossEcho)
        {
            if (!BossUniquesAvailable(d)) return 0f;
            float c = cfg.BossUniqueChance;
            if (d == Difficulty.Ascension && ascensionLevel > 1) c += cfg.BossUniqueChancePerAscension * (ascensionLevel - 1);
            if (isBossEcho && cfg.BossEchoMultiplier > 0f) c *= cfg.BossEchoMultiplier;
            return Math.Min(1f, Math.Max(0f, c));
        }

        /// <summary>True when this kill is the guaranteed one (the BossUniqueGuaranteeKills-th without a Unique).</summary>
        public static bool IsBossUniqueGuaranteed(PityConfigData cfg, Difficulty d, int killsWithoutUnique)
            => BossUniquesAvailable(d) && cfg.BossUniqueGuaranteeKills > 0 && killsWithoutUnique + 1 >= cfg.BossUniqueGuaranteeKills;
    }
}
