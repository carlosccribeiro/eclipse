namespace Eclipse.AI.StateMachine
{
    /// <summary>Per-tick snapshot the brain reads. Filled by the controller; no allocation (array is caller-owned).</summary>
    public struct AiPerception
    {
        public bool IsDead;
        public bool HasTarget;
        public bool OnScreen;
        /// <summary>Distance to the target (meters).</summary>
        public float Distance;
        /// <summary>Absolute angle (degrees, 0..180) between facing and direction to target.</summary>
        public float AngleDeg;
        /// <summary>Own HP as a fraction 0..1.</summary>
        public float HpFraction;
        public int AlliesNearby;
        /// <summary>Remaining cooldown (s) per attack, indexed like UtilityConfig.Attacks; null means all ready.</summary>
        public float[] CooldownsRemaining;
    }
}
