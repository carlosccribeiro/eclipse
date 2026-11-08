namespace Eclipse.AI.Utility
{
    /// <summary>Attack candidate seen by the Utility selector. Values come from AttackDefinition data (never literals).</summary>
    public sealed class AttackOption
    {
        public float MinRange;
        public float MaxRange;
        /// <summary>Max absolute angle (degrees) between facing and target for the attack to be usable.</summary>
        public float MaxAngleDeg;
        /// <summary>Relative preference between attacks of the same enemy.</summary>
        public float Weight = 1f;
        public bool Ranged;
        /// <summary>Wind-up plus active time, seconds (Attack state length).</summary>
        public float DurationSeconds;
        /// <summary>Recovery time after the attack, seconds (Recover state length).</summary>
        public float RecoverSeconds;
    }
}
