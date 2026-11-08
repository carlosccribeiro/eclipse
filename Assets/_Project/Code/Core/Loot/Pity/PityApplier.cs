using System;
using Eclipse.Core.Items;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot.Pity
{
    /// <summary>What <see cref="PityApplier.Apply"/> decided (informational; the effects are already in the context).</summary>
    [Flags]
    public enum PityOutcome : byte
    {
        None = 0, HardPity = 1, SetPity = 2, BossUnique = 4, FirstKill = 8, FirstRegionClear = 16
    }

    /// <summary>
    /// Facade that turns the per-hero <see cref="IPityState"/> into <c>ctx.Pity</c> and <c>ctx.Guarantees</c> (05 sections 8.1-8.5).
    /// <para>Order of use for one item: <c>Apply</c> -> <c>ItemGenerator.Generate</c> (calls <c>OnItemGenerated</c>) -> after the
    /// boss kill / region clear, <c>HeroPityState.RegisterBossKill</c> / <c>MarkRegionClearedOnNormal</c>.
    /// Call Apply on a fresh context for each item (soft/hard/set pity act through ctx.Pity inside the generator; the extra
    /// first-kill / boss Unique item and the first-clear Rift Cache are the ones that need the guarantees).</para>
    /// Boss kills: set <c>ctx.Source = Boss</c> and <c>ctx.Guarantees.BossId</c> before calling.
    /// Priority when several apply to the same item: boss Unique over the first-kill set chance; the first kill is recorded by
    /// RegisterBossKill either way. Draws exactly one value from the stream when a boss Unique roll is eligible.
    /// </summary>
    public static class PityApplier
    {
        public static PityOutcome Apply(LootContext ctx, ILootCatalog cat, IPityState pity, bool isBossEcho, ref Xoshiro128StarStar rng)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (cat == null) throw new ArgumentNullException(nameof(cat));
            ctx.Pity = pity;
            if (pity == null) return PityOutcome.None;

            var cfg = cat.GetPity(ctx.Difficulty);
            var src = cat.GetSource(ctx.Source);
            var outcome = PityOutcome.None;
            var g = ctx.Guarantees;

            // 8.1 hard pity
            if (PityRules.IsHardPity(cfg, src, pity.Drought)) { g.ForceLegendary = true; outcome |= PityOutcome.HardPity; }

            // 8.2 set pity (only consulted if the item ends up Legendary)
            if (PityRules.IsSetPity(cfg, pity.LegendariesWithoutSet) && !g.ForceKind.HasValue)
            {
                g.ForceKind = ItemKind.SetPiece;
                outcome |= PityOutcome.SetPity;
            }

            if (ctx.Source == LootSourceKind.Boss && !string.IsNullOrEmpty(g.BossId))
            {
                // 8.5 boss Unique (Nightmare+): guaranteed on the Nth kill, else chance per kill.
                bool unique = false;
                if (PityRules.BossUniquesAvailable(ctx.Difficulty))
                {
                    if (PityRules.IsBossUniqueGuaranteed(cfg, ctx.Difficulty, pity.KillsWithoutUnique(g.BossId))) unique = true;
                    else unique = rng.Chance(PityRules.BossUniqueChance(cfg, ctx.Difficulty, ctx.AscensionLevel, isBossEcho));
                }
                if (unique)
                {
                    g.ForceLegendary = true; g.ForceKind = ItemKind.Unique;
                    outcome |= PityOutcome.BossUnique;
                }

                // 8.4 first kill of this boss on this difficulty
                if (!pity.HasFirstKill(g.BossId, ctx.Difficulty))
                {
                    outcome |= PityOutcome.FirstKill;
                    if (!unique)
                    {
                        g.ForceLegendary = true; g.RaiseILvlToLegendaryMin = true;
                        if (ctx.Difficulty == Difficulty.Normal) g.ForceKind = ItemKind.NamedLegendary;
                        else { g.ForceKind = null; g.SetPieceChanceOverride = cfg.FirstKillSetChance; }
                    }
                }
            }
            else if (ctx.Source == LootSourceKind.RiftCache && ctx.Difficulty == Difficulty.Normal
                     && !string.IsNullOrEmpty(ctx.RegionId) && !pity.HasClearedRegionOnNormal(ctx.RegionId))
            {
                // 8.4 first clear of a region on Normal
                g.ForceEpicPlus = true;
                outcome |= PityOutcome.FirstRegionClear;
            }

            ctx.Guarantees = g;
            return outcome;
        }
    }
}
