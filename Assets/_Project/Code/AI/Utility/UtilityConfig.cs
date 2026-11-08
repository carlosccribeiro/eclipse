namespace Eclipse.AI.Utility
{
    /// <summary>Tuning for <see cref="UtilitySelector"/>. Plain data; filled from enemy definitions by the caller.</summary>
    public sealed class UtilityConfig
    {
        /// <summary>Design value: 15% weighted randomness in action choice (09 section 3.1). Passed as <see cref="RandomChance"/>.</summary>
        public const float DesignRandomChance = 0.15f;

        /// <summary>Probability of replacing the best action by a utility-weighted random pick.</summary>
        public float RandomChance = DesignRandomChance;

        /// <summary>Neutral multipliers (1 = no bias); not balance data.</summary>
        public float WeightAttack = 1f;
        public float WeightApproach = 1f;
        public float WeightReposition = 1f;

        /// <summary>Enemy flees (deterministically) when HP fraction is at or below this; 0 disables Flee.</summary>
        public float FleeHpFraction;

        /// <summary>Approach while farther than this from the target.</summary>
        public float DesiredRange = 2f;
        /// <summary>Distance beyond DesiredRange at which Approach reaches full score.</summary>
        public float ApproachFalloffDistance = 6f;
        /// <summary>Nearby allies at which crowding is maximal (drives Reposition and the ally attack bonus).</summary>
        public int CrowdAllyCount = 3;
        /// <summary>Extra attack score fraction with a full crowd of allies (pack pressure).</summary>
        public float AllyAttackBonus;

        public AttackOption[] Attacks = new AttackOption[0];
    }
}
