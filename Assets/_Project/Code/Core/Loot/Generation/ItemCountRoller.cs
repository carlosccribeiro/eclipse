using System;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// How many items one source occurrence yields (05 sections 3.1 and 3.2). Quantity is additive and capped (R45) and only
    /// applies to sources with <see cref="LootSourceData.AffectedByQuantity"/>.
    /// </summary>
    public sealed class ItemCountRoller
    {
        private readonly ILootCatalog _loot;
        public ItemCountRoller(ILootCatalog loot) { _loot = loot; }

        /// <summary>
        /// Count rules (data-driven):
        /// <list type="bullet">
        /// <item>ChooseOf &gt; 0 (Reliquary): all N options are generated.</item>
        /// <item>GuaranteedItems &gt; 0: guaranteed x quantity, rounded stochastically (Elite/Boss); <see cref="LootSourceData.DropChance"/> is then ignored.</item>
        /// <item>Otherwise one item with probability DropChance x quantity (trash).</item>
        /// <item>ExtraItemChance x quantity: +1 item. ExtraItemsIfAllChallenges when <see cref="LootContext.AllChallengesCleared"/>.</item>
        /// </list>
        /// </summary>
        public int Roll(LootContext ctx, ref Xoshiro128StarStar rng)
        {
            var src = _loot.GetSource(ctx.Source);
            float q = src.AffectedByQuantity ? LootMath.QuantityMultiplier(_loot, ctx) : 1f;
            int n;
            if (src.ChooseOf > 0) n = src.ChooseOf;
            else if (src.GuaranteedItems > 0) n = Sampling.StochasticRound(src.GuaranteedItems * q, ref rng);
            else n = src.DropChance > 0f && rng.Chance(Math.Min(1f, src.DropChance * q)) ? 1 : 0;
            if (src.ExtraItemChance > 0f && rng.Chance(Math.Min(1f, src.ExtraItemChance * q))) n++;
            if (src.ExtraItemsIfAllChallenges > 0 && ctx.AllChallengesCleared) n += src.ExtraItemsIfAllChallenges;
            return n;
        }
    }
}
