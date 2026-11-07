namespace Eclipse.Core.Stats
{
    /// <summary>Soft cap / hard cap function of 01 3.1. Callable on its own for stats capped outside <see cref="StatBlock"/>.</summary>
    public static class DiminishingReturns
    {
        /// <summary>
        /// Effective = raw if raw &lt;= soft, else soft + (raw - soft) * AboveSoftFactor; then min(hard); then max(minimum).
        /// </summary>
        public static float Apply(float raw, StatRule rule)
        {
            float v = raw;
            if (rule.SoftCap.HasValue && raw > rule.SoftCap.Value)
                v = rule.SoftCap.Value + (raw - rule.SoftCap.Value) * rule.AboveSoftFactor;
            if (rule.HardCap.HasValue && v > rule.HardCap.Value) v = rule.HardCap.Value;
            if (rule.Minimum.HasValue && v < rule.Minimum.Value) v = rule.Minimum.Value;
            return v;
        }
    }
}
