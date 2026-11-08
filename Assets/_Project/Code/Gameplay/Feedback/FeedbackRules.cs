namespace Eclipse.Gameplay.Feedback
{
    /// <summary>
    /// Feedback numbers that are not part of HitFeedbackProfile (06 section 3.11). Heavy/crit trauma, crit
    /// hit-stop bonus, spacing, flash and squash come from the profile; the values here are the doc-defined ones
    /// with no per-weapon column. Gap: no CSV hosts them yet, so <see cref="Doc"/> carries the 06 3.11 table.
    /// </summary>
    public readonly struct FeedbackRules
    {
        /// <summary>Hit-stop frames on an Elite kill (06 3.11).</summary>
        public readonly int EliteKillHitStopFrames;
        /// <summary>World-wide hit-stop: Ultimate, boss Break, phase change (06 3.11).</summary>
        public readonly int GlobalHitStopFrames;
        /// <summary>Trauma of a Wall Impact (06 3.11).</summary>
        public readonly float WallImpactTrauma;
        /// <summary>Trauma of an Ultimate (06 3.11).</summary>
        public readonly float UltimateTrauma;
        /// <summary>Trauma when a boss hit lands on the player (06 3.11).</summary>
        public readonly float BossHitReceivedTrauma;

        public FeedbackRules(int eliteKillHitStopFrames, int globalHitStopFrames, float wallImpactTrauma, float ultimateTrauma, float bossHitReceivedTrauma)
        {
            EliteKillHitStopFrames = eliteKillHitStopFrames;
            GlobalHitStopFrames = globalHitStopFrames;
            WallImpactTrauma = wallImpactTrauma;
            UltimateTrauma = ultimateTrauma;
            BossHitReceivedTrauma = bossHitReceivedTrauma;
        }

        /// <summary>06 3.11: 8 f Elite kill, 6 f global, trauma 0.2 / 0.5 / 0.4.</summary>
        public static FeedbackRules Doc => new FeedbackRules(8, 6, 0.2f, 0.5f, 0.4f);
    }
}
