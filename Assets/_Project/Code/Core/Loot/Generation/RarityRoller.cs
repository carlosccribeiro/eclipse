using System;
using Eclipse.Core.Items;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// Step 2 of 05 section 6.2: rarity of the item (05 section 5).
    /// <para>
    /// DIVERGENCE from 05 section 5.1 (decision of the M12 orchestrator): 5.1 describes a conditional cascade (roll L, then E, then R),
    /// but the distribution of 5.4 (Normal ~ 72.9/25/2/0.1, Eclipse ~ 36.2/42/20/1.8) only comes out when L, E and R are ABSOLUTE
    /// probabilities. So a single U is drawn: U &lt; L gives Legendary; U &lt; L+E Epic; U &lt; L+E+R Rare; otherwise Common. Then the source floor applies.
    /// </para>
    /// Pity: soft pity multiplies L (capped at LegendaryCapMultiplier x base, 05 sections 3.2 and 8.1); hard pity forces Legendary.
    /// </summary>
    public sealed class RarityRoller
    {
        private readonly ILootCatalog _loot;
        public RarityRoller(ILootCatalog loot) { _loot = loot; }

        /// <summary>Legendary chance after pity and cap (exposed for tests).</summary>
        public float LegendaryChance(LootContext ctx)
        {
            var ch = _loot.GetChances(ctx.Source);
            float baseL = Math.Max(0f, LootMath.Anchor(ch.Legendary, ctx));
            float l = baseL;
            var pity = ctx.Pity;
            if (pity != null)
            {
                var cfg = _loot.GetPity(ctx.Difficulty);
                int drought = pity.Drought;
                if (cfg.SoftStart > 0 && drought > cfg.SoftStart) l *= 1f + cfg.SoftStep * (drought - cfg.SoftStart);
                if (cfg.LegendaryCapMultiplier > 0f && l > baseL * cfg.LegendaryCapMultiplier) l = baseL * cfg.LegendaryCapMultiplier;
            }
            return l;
        }

        /// <summary>Always consumes exactly one output (stable sequences whatever the forced outcome).</summary>
        public Rarity Roll(LootContext ctx, LootSourceData src, ref Xoshiro128StarStar rng)
        {
            float u = rng.NextFloat01();
            var ch = _loot.GetChances(ctx.Source);
            float l = LegendaryChance(ctx);
            // Negative = the source has a floor instead of a chance (Reliquary, Challenge; non-trash Rare).
            float e = Math.Max(0f, LootMath.Anchor(ch.Epic, ctx));
            float r = Math.Max(0f, LootMath.Anchor(ch.Rare, ctx));

            bool hard = false;
            if (ctx.Pity != null)
            {
                var cfg = _loot.GetPity(ctx.Difficulty);
                hard = cfg.Hard > 0 && ctx.Pity.Drought >= cfg.Hard && src.ConsumesHardPity;
            }

            Rarity rarity;
            if (hard || ctx.Guarantees.ForceLegendary || u < l) rarity = Rarity.Legendary;
            else if (u < l + e) rarity = Rarity.Epic;
            else if (u < l + e + r) rarity = Rarity.Rare;
            else rarity = Rarity.Common;

            Rarity floor = src.RarityFloor;
            if (ctx.Guarantees.ForceEpicPlus && floor < Rarity.Epic) floor = Rarity.Epic;
            return rarity < floor ? floor : rarity;
        }
    }
}
