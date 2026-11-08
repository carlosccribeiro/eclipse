using System;
using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// Step 4 of 05 section 6.2 / 05 section 7.1: target hero. Current hero with 75% (Common/Rare/Epic) or 85% (Legendary kinds),
    /// otherwise uniform among all heroes. There is no hero list in the catalogs, so the heroes are the distinct ids found in
    /// <c>ItemBaseData.HeroId</c>, <c>NamedItemData.HeroId</c>, <c>HomeRegionData.HeroId</c> plus the context hero, sorted ordinally (deterministic).
    /// Not thread-safe (reusable buffer).
    /// </summary>
    public sealed class HeroSelector
    {
        private static readonly Comparison<string> Ordinal = string.CompareOrdinal;
        private readonly IItemCatalog _items;
        private readonly ILootCatalog _loot;
        private readonly List<string> _heroes = new List<string>(8);

        public HeroSelector(IItemCatalog items, ILootCatalog loot) { _items = items; _loot = loot; }

        public string Pick(LootContext ctx, bool legendaryKind, ref Xoshiro128StarStar rng)
        {
            float p = legendaryKind ? _loot.SmartLootCurrentHeroLegendary : _loot.SmartLootCurrentHeroGeneric;
            bool hasCurrent = !string.IsNullOrEmpty(ctx.HeroId);
            if (hasCurrent && rng.Chance(p)) return ctx.HeroId;
            Collect(ctx.HeroId);
            // Generic drops need a droppable base: heroes without any compatible base (e.g. only listed in home regions) are skipped.
            if (!legendaryKind) RemoveHeroesWithoutBase();
            if (_heroes.Count == 0) return ctx.HeroId;
            return _heroes[rng.Range(0, _heroes.Count)];
        }

        /// <summary>Distinct hero ids, sorted (exposed for tests).</summary>
        public IReadOnlyList<string> Collect(string currentHero)
        {
            _heroes.Clear();
            Add(currentHero);
            var bases = _items.Bases;
            for (int i = 0; i < bases.Count; i++) Add(bases[i].HeroId);
            var named = _items.NamedItems;
            for (int i = 0; i < named.Count; i++) Add(named[i].HeroId);
            var homes = _loot.HomeRegions;
            for (int i = 0; i < homes.Count; i++) Add(homes[i].HeroId);
            _heroes.Sort(Ordinal);
            return _heroes;
        }

        private void RemoveHeroesWithoutBase()
        {
            var bases = _items.Bases;
            for (int h = _heroes.Count - 1; h >= 0; h--)
            {
                bool any = false;
                for (int i = 0; i < bases.Count && !any; i++) any = BaseSelector.Compatible(bases[i], _heroes[h]);
                if (!any) _heroes.RemoveAt(h);
            }
        }

        private void Add(string hero)
        {
            if (string.IsNullOrEmpty(hero)) return;
            for (int i = 0; i < _heroes.Count; i++) if (_heroes[i] == hero) return;
            _heroes.Add(hero);
        }
    }
}
