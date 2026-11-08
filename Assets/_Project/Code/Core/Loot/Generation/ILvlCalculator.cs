using System;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>Step 1 of 05 section 6.2: item level of a drop (05 section 4.2). Ranges, Ascension base and source bonus come from the loot catalog.</summary>
    public static class ILvlCalculator
    {
        /// <summary>Global iLvl cap (01 section 3.9.1: tiers end at 100).</summary>
        public const int MaxILvl = 100;
        /// <summary>The drop may exceed the range top by at most +2 (05 section 4.2).</summary>
        private const int RangeTopOverflow = 2;

        public static int Compute(ILootCatalog loot, LootContext ctx, LootSourceData src)
        {
            if (ctx.ForcedILvl.HasValue) return LootMath.Clamp(ctx.ForcedILvl.Value, 1, MaxILvl);
            if (ctx.Difficulty == Difficulty.Ascension) return Ascension(loot, ctx, src);

            var range = FindRange(loot, ctx);
            int cap = Math.Min(MaxILvl, range.Max + RangeTopOverflow);
            if (src.ILvlFromRangeTop) return cap;
            int total = Math.Max(1, ctx.TotalRooms);
            int room = range.Min + (range.Max - range.Min) * Math.Max(0, ctx.RoomIndex) / total;
            return LootMath.Clamp(room + src.ILvlBonus, 1, cap);
        }

        /// <summary>Ascension: base + floor((min(N, capLevel) - 1) x (max - base) / (capLevel - 1)), plus source bonus, capped (05 section 4.1).</summary>
        private static int Ascension(ILootCatalog loot, LootContext ctx, LootSourceData src)
        {
            var d = loot.GetDifficulty(Difficulty.Ascension);
            int n = Math.Max(1, ctx.AscensionLevel);
            int span = d.AscensionMaxILvl - d.AscensionBaseILvl;
            int denom = d.AscensionILvlCapLevel - 1;
            int baseILvl = d.AscensionBaseILvl;
            if (denom > 0) baseILvl += (Math.Min(n, d.AscensionILvlCapLevel) - 1) * span / denom;
            return LootMath.Clamp(baseILvl + src.ILvlBonus, 1, Math.Min(MaxILvl, d.AscensionMaxILvl));
        }

        private static IlvlRangeData FindRange(ILootCatalog loot, LootContext ctx)
        {
            var list = loot.IlvlRanges;
            IlvlRangeData anyRegion = null, anyOfDifficulty = null;
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                if (r.Difficulty != ctx.Difficulty) continue;
                if (anyOfDifficulty == null) anyOfDifficulty = r;
                if (string.IsNullOrEmpty(r.RegionId)) { if (anyRegion == null) anyRegion = r; continue; }
                if (string.Equals(r.RegionId, ctx.RegionId, StringComparison.OrdinalIgnoreCase)) return r;
            }
            // Documented fallback: entry valid for every region, else the first range of the difficulty (hub/test contexts).
            var found = anyRegion ?? anyOfDifficulty;
            if (found == null) throw new InvalidOperationException("No iLvl range for difficulty " + ctx.Difficulty);
            return found;
        }
    }
}
