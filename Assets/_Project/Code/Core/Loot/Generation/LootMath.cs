using System;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>Shared per-difficulty math of 05: anchor interpolation, quantity, roll floor.</summary>
    public static class LootMath
    {
        /// <summary>Ascension spans levels 1..100 (05 sections 3.2 and 5.2); anchors A1 and A100 are interpolated linearly.</summary>
        private const int AscensionLevels = 100;

        /// <summary>Value at the current difficulty from the 6 anchors (Normal, Veteran, Nightmare, Eclipse, A1, A100).</summary>
        public static float Anchor(float[] anchors, LootContext ctx)
        {
            switch (ctx.Difficulty)
            {
                case Difficulty.Normal: return anchors[0];
                case Difficulty.Veteran: return anchors[1];
                case Difficulty.Nightmare: return anchors[2];
                case Difficulty.Eclipse: return anchors[3];
                default:
                    int n = Clamp(ctx.AscensionLevel, 1, AscensionLevels);
                    float t = (n - 1) / (float)(AscensionLevels - 1);
                    return anchors[4] + (anchors[5] - anchors[4]) * t;
            }
        }

        /// <summary>Quantity multiplier = min(cap, 1 + difficulty bonus + external bonus), all additive (05 section 3.2, R45).</summary>
        public static float QuantityMultiplier(ILootCatalog loot, LootContext ctx)
        {
            var d = loot.GetDifficulty(ctx.Difficulty);
            float bonus = d.QuantityBonus + ctx.QuantityBonus;
            if (ctx.Difficulty == Difficulty.Ascension) bonus += d.QuantityBonusPerAscension * ctx.AscensionLevel;
            float q = 1f + bonus;
            if (q < 0f) q = 0f;
            return Math.Min(loot.QuantityCap, q);
        }

        /// <summary>Roll01 floor of the difficulty (05 section 10.1): base + per-level x N in Ascension.</summary>
        public static float RollFloor(ILootCatalog loot, LootContext ctx)
        {
            var d = loot.GetDifficulty(ctx.Difficulty);
            float f = d.RollFloor;
            if (ctx.Difficulty == Difficulty.Ascension) f += d.RollFloorPerAscension * ctx.AscensionLevel;
            return Math.Max(0f, Math.Min(1f, f));
        }

        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
