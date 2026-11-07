using Eclipse.Core.Items;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// Step 3 of 05 section 6.2: kind of a Legendary (05 section 5.5).
    /// Shares come from <see cref="DifficultyLootData"/> (Normal has 0 set/Unique). A share with no eligible item for the hero/iLvl/source
    /// (Unique below its <c>MinDropILvl</c>, no set piece, no Unique of the source) is redistributed to the standard Legendary.
    /// Forcing: <c>Guarantees.ForceKind</c> (boss Unique, set pity) and the set pity of 05 section 8.2; a forced kind with no candidate falls back to the normal roll.
    /// The Astral Citadel +3 pp Unique bonus (05 section 9.2) is added to the Unique share only when the difficulty has Uniques.
    /// Returns <see cref="ItemKind.Generic"/> when no named Legendary at all is available (caller demotes to Epic).
    /// </summary>
    public sealed class LegendaryKindSelector
    {
        private readonly ILootCatalog _loot;
        private readonly NamedItemSelector _named;

        public LegendaryKindSelector(ILootCatalog loot, NamedItemSelector named) { _loot = loot; _named = named; }

        public ItemKind Pick(LootContext ctx, string hero, int ilvl, ref Xoshiro128StarStar rng)
        {
            var g = ctx.Guarantees;
            bool setOk = _named.HasCandidates(ItemKind.SetPiece, ctx, hero, ilvl);
            bool uniOk = _named.HasCandidates(ItemKind.Unique, ctx, hero, ilvl);
            bool stdOk = _named.HasCandidates(ItemKind.NamedLegendary, ctx, hero, ilvl);

            if (g.ForceKind.HasValue)
            {
                var k = g.ForceKind.Value;
                if ((k == ItemKind.SetPiece && setOk) || (k == ItemKind.Unique && uniOk) || (k == ItemKind.NamedLegendary && stdOk)) return k;
            }

            var d = _loot.GetDifficulty(ctx.Difficulty);
            if (ctx.Pity != null)
            {
                var cfg = _loot.GetPity(ctx.Difficulty);
                if (setOk && d.SetPieceShare > 0f && cfg.SetPityAfter > 0 && ctx.Pity.LegendariesWithoutSet >= cfg.SetPityAfter)
                    return ItemKind.SetPiece;
            }

            float rawSet, rawUnique;
            if (g.SetPieceChanceOverride >= 0f) { rawSet = g.SetPieceChanceOverride; rawUnique = 0f; }
            else
            {
                rawSet = d.SetPieceShare;
                rawUnique = d.UniqueShare;
                if (rawUnique > 0f)
                {
                    var region = _named.RegionOf(ctx);
                    if (region != null) rawUnique += region.UniqueBonus;
                }
            }
            float set = setOk ? rawSet : 0f;
            float uni = uniOk ? rawUnique : 0f;

            float u = rng.NextFloat01();
            if (u < set) return ItemKind.SetPiece;
            if (u < set + uni) return ItemKind.Unique;
            if (stdOk) return ItemKind.NamedLegendary;
            // Standard unavailable: only fall back to kinds the difficulty actually allows (Normal never yields set/Unique).
            if (setOk && rawSet > 0f) return ItemKind.SetPiece;
            if (uniOk && rawUnique > 0f) return ItemKind.Unique;
            return ItemKind.Generic;
        }
    }
}
