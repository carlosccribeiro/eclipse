namespace Eclipse.Gameplay.Feedback
{
    /// <summary>Plain copy of the numeric part of a HitFeedbackProfile (policy classes need no ScriptableObject).</summary>
    public readonly struct HitFeedbackValues
    {
        public readonly int CritHitStopBonusFrames;
        public readonly int MinHitStopSpacingMs;
        public readonly float ShakeTrauma;
        public readonly float ShakeTraumaCrit;
        public readonly int FlashFrames;
        public readonly float SquashSeconds;

        public HitFeedbackValues(int critHitStopBonusFrames, int minHitStopSpacingMs, float shakeTrauma, float shakeTraumaCrit, int flashFrames, float squashSeconds)
        {
            CritHitStopBonusFrames = critHitStopBonusFrames;
            MinHitStopSpacingMs = minHitStopSpacingMs;
            ShakeTrauma = shakeTrauma;
            ShakeTraumaCrit = shakeTraumaCrit;
            FlashFrames = flashFrames;
            SquashSeconds = squashSeconds;
        }

        public static HitFeedbackValues From(Eclipse.Data.Combat.HitFeedbackProfile p) =>
            new HitFeedbackValues(p.CritHitStopBonusFrames, p.MinHitStopSpacingMs, p.ShakeTrauma, p.ShakeTraumaCrit, p.FlashFrames, p.SquashSeconds);
    }

    /// <summary>Everything the feedback layer needs to know about the attack that produced a hit.</summary>
    public readonly struct HitFeedbackInfo
    {
        /// <summary>Per-hit hit-stop of the attack step (AttackStep.HitStopFrames).</summary>
        public readonly int HitStopFrames;
        public readonly HitFeedbackValues Values;
        /// <summary>Projectile hit: only the target freezes (06 3.11).</summary>
        public readonly bool IsProjectile;
        /// <summary>Ultimate: global hit-stop and Ultimate trauma.</summary>
        public readonly bool IsUltimate;

        public HitFeedbackInfo(int hitStopFrames, HitFeedbackValues values, bool isProjectile, bool isUltimate)
        { HitStopFrames = hitStopFrames; Values = values; IsProjectile = isProjectile; IsUltimate = isUltimate; }
    }
}
