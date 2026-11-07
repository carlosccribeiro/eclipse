using System;
using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// Step 5 of 05 section 6.2 for Legendary kinds: picks the set + piece, the standard Legendary or the Unique.
    /// Eligibility: kind, hero compatibility (definition hero null or equal), <c>MinDropILvl</c> of the item (Unique 35, Legendary 10),
    /// and for Uniques the single source of 05 section 9.3. The generator does NOT validate the slot pool of named items (off-pool `*` is copied as is).
    /// Not thread-safe (reusable buffers).
    /// </summary>
    public sealed class NamedItemSelector
    {
        private readonly IItemCatalog _items;
        private readonly ILootCatalog _loot;
        private readonly List<string> _setIds = new List<string>(16);
        private float[] _weights = new float[32];
        private NamedItemData[] _cands = new NamedItemData[32];

        public NamedItemSelector(IItemCatalog items, ILootCatalog loot) { _items = items; _loot = loot; }

        /// <summary>True when the item can drop for this hero/iLvl/context.</summary>
        public bool Eligible(NamedItemData n, ItemKind kind, LootContext ctx, string hero, int ilvl)
        {
            if (n.Kind != kind || ilvl < n.MinDropILvl) return false;
            if (!string.IsNullOrEmpty(n.HeroId) && n.HeroId != hero) return false;
            return kind != ItemKind.Unique || UniqueSourceAllows(n, ctx);
        }

        /// <summary>Unique sources (05 section 9.3). Boss/Pinnacle Uniques only through a forced Unique for that boss (05 section 8.5).</summary>
        public static bool UniqueSourceAllows(NamedItemData n, LootContext ctx)
        {
            var g = ctx.Guarantees;
            bool bossForced = g.ForceKind == ItemKind.Unique && !string.IsNullOrEmpty(g.BossId);
            if (bossForced)
                return (n.UniqueSource == UniqueSourceKind.Boss || n.UniqueSource == UniqueSourceKind.Pinnacle) && n.UniqueSourceRef == g.BossId;
            switch (n.UniqueSource)
            {
                case UniqueSourceKind.Region:
                    return string.Equals(n.UniqueSourceRef, ctx.RegionId, StringComparison.OrdinalIgnoreCase);
                case UniqueSourceKind.Challenge:
                    return ctx.Source == LootSourceKind.Challenge && ctx.Difficulty >= Difficulty.Eclipse;
                default:
                    return false;
            }
        }

        public bool HasCandidates(ItemKind kind, LootContext ctx, string hero, int ilvl)
        {
            var list = _items.NamedItems;
            for (int i = 0; i < list.Count; i++) if (Eligible(list[i], kind, ctx, hero, ilvl)) return true;
            return false;
        }

        /// <summary>Picks one item of the kind, or null when there is no candidate.</summary>
        public NamedItemData Pick(ItemKind kind, LootContext ctx, string hero, int ilvl, ref Xoshiro128StarStar rng)
        {
            switch (kind)
            {
                case ItemKind.SetPiece: return PickSetPiece(ctx, hero, ilvl, ref rng);
                case ItemKind.Unique: return PickUnique(ctx, hero, ilvl, ref rng);
                case ItemKind.NamedLegendary: return PickStandard(ctx, hero, ilvl, ref rng);
                default: return null;
            }
        }

        // ---- standard Legendary: x2 when the item affinity matches a dominant affinity of the region (05 section 9.2) ----
        private NamedItemData PickStandard(LootContext ctx, string hero, int ilvl, ref Xoshiro128StarStar rng)
        {
            var region = FindRegion(ctx.RegionId);
            int n = Gather(ItemKind.NamedLegendary, ctx, hero, ilvl);
            for (int i = 0; i < n; i++)
            {
                float w = 1f;
                if (region != null && region.AffinityWeight > 0f && !_cands[i].Affinity.IsEmpty && _cands[i].Affinity.HasAny(region.DominantAffinities))
                    w = region.AffinityWeight;
                _weights[i] = w;
            }
            return PickCandidate(n, ref rng);
        }

        // ---- Unique: x2 (UndiscoveredUniqueWeight) if not owned, x3 if Attunement target (05 sections 6, 9.4); boss pool uses the pity x3 (05 section 8.5) ----
        private NamedItemData PickUnique(LootContext ctx, string hero, int ilvl, ref Xoshiro128StarStar rng)
        {
            int n = Gather(ItemKind.Unique, ctx, hero, ilvl);
            bool bossPool = ctx.Guarantees.ForceKind == ItemKind.Unique && !string.IsNullOrEmpty(ctx.Guarantees.BossId);
            float unownedW = bossPool ? UnownedWeight(ctx) : _loot.UndiscoveredUniqueWeight;
            for (int i = 0; i < n; i++)
            {
                float w = 1f;
                if (!Owns(ctx, _cands[i].Id) && unownedW > 0f) w *= unownedW;
                if (ctx.Attunement.Kind == AttunementKind.Unique && ctx.Attunement.TargetId == _cands[i].Id) w *= AttunementMultiplier(null);
                _weights[i] = w;
            }
            return PickCandidate(n, ref rng);
        }

        // ---- set piece: first the set (home region x4, home boss x6, Attunement x3), then the piece (unowned x3, 05 section 8.3) ----
        private NamedItemData PickSetPiece(LootContext ctx, string hero, int ilvl, ref Xoshiro128StarStar rng)
        {
            int n = Gather(ItemKind.SetPiece, ctx, hero, ilvl);
            if (n == 0) return null;
            _setIds.Clear();
            for (int i = 0; i < n; i++)
            {
                string id = _cands[i].SetId ?? string.Empty;
                bool seen = false;
                for (int s = 0; s < _setIds.Count; s++) if (_setIds[s] == id) { seen = true; break; }
                if (!seen) _setIds.Add(id);
            }
            Sampling.Ensure(ref _weights, _setIds.Count);
            for (int s = 0; s < _setIds.Count; s++)
            {
                string setId = _setIds[s];
                float w = 1f;
                var home = FindHome(setId);
                if (home != null && string.Equals(home.RegionId, ctx.RegionId, StringComparison.OrdinalIgnoreCase))
                {
                    float hw = ctx.Source == LootSourceKind.Boss ? home.HomeBossWeight : home.HomeWeight;
                    if (hw > 0f) w = hw;
                }
                if (ctx.Attunement.Kind == AttunementKind.Set && ctx.Attunement.TargetId == setId) w *= AttunementMultiplier(setId);
                _weights[s] = w;
            }
            int si = Sampling.PickWeighted(_weights, _setIds.Count, ref rng);
            if (si < 0) return null;
            string chosenSet = _setIds[si];

            float dup = UnownedWeight(ctx);
            int m = 0;
            for (int i = 0; i < n; i++)
            {
                if ((_cands[i].SetId ?? string.Empty) != chosenSet) continue;
                _cands[m] = _cands[i];
                _weights[m] = !Owns(ctx, _cands[i].Id) && dup > 0f ? dup : 1f;
                m++;
            }
            return PickCandidate(m, ref rng);
        }

        // ---- helpers ----
        private int Gather(ItemKind kind, LootContext ctx, string hero, int ilvl)
        {
            var list = _items.NamedItems;
            Sampling.Ensure(ref _cands, list.Count);
            Sampling.Ensure(ref _weights, list.Count);
            int n = 0;
            for (int i = 0; i < list.Count; i++)
                if (Eligible(list[i], kind, ctx, hero, ilvl)) _cands[n++] = list[i];
            return n;
        }

        private NamedItemData PickCandidate(int n, ref Xoshiro128StarStar rng)
        {
            int i = Sampling.PickWeighted(_weights, n, ref rng);
            return i < 0 ? null : _cands[i];
        }

        private static bool Owns(LootContext ctx, string id) => ctx.Collection != null && ctx.Collection.Owns(id);

        /// <summary>Unowned set piece / boss Unique weight (05 sections 8.3 and 8.5): the DuplicateUnownedWeight of the pity config.</summary>
        private float UnownedWeight(LootContext ctx) => _loot.GetPity(ctx.Difficulty).DuplicateUnownedWeight;

        private HomeRegionData FindHome(string setId)
        {
            var list = _loot.HomeRegions;
            for (int i = 0; i < list.Count; i++) if (list[i].SetId == setId) return list[i];
            return null;
        }

        private RegionAffinityData FindRegion(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return null;
            var list = _loot.RegionAffinities;
            for (int i = 0; i < list.Count; i++)
                if (string.Equals(list[i].RegionId, regionId, StringComparison.OrdinalIgnoreCase)) return list[i];
            return null;
        }

        /// <summary>
        /// Attunement multiplier (x3, R39). Sets use their own row; Uniques have no row, so the first positive value of
        /// <c>HomeRegions</c> is used (ILootCatalog has no dedicated global; documented gap). 1 when nothing is configured.
        /// </summary>
        private float AttunementMultiplier(string setId)
        {
            var list = _loot.HomeRegions;
            float fallback = 1f;
            bool haveFallback = false;
            for (int i = 0; i < list.Count; i++)
            {
                float m = list[i].AttunementMultiplier;
                if (m <= 0f) continue;
                if (setId != null && list[i].SetId == setId) return m;
                if (!haveFallback) { fallback = m; haveFallback = true; }
            }
            return fallback;
        }

        /// <summary>Region of a context (used by <see cref="LegendaryKindSelector"/> for the Unique bonus).</summary>
        public RegionAffinityData RegionOf(LootContext ctx) => FindRegion(ctx.RegionId);
    }
}
