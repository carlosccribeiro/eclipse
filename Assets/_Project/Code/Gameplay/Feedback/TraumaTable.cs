namespace Eclipse.Gameplay.Feedback
{
    /// <summary>Trauma per event (06 3.11). Hit trauma comes from the profile (Light/Medium profiles hold 0).</summary>
    public static class TraumaTable
    {
        public static float Hit(in HitFeedbackInfo info, bool isCrit, in FeedbackRules rules)
        {
            float t = isCrit ? info.Values.ShakeTraumaCrit : info.Values.ShakeTrauma;
            if (info.IsUltimate && rules.UltimateTrauma > t) t = rules.UltimateTrauma;
            return t;
        }

        public static float WallImpact(in FeedbackRules rules) => rules.WallImpactTrauma;
        public static float BossHitReceived(in FeedbackRules rules) => rules.BossHitReceivedTrauma;
    }
}
