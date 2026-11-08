namespace Eclipse.AI.Locomotion
{
    /// <summary>Per-agent steering behaviour (M07 T4; 11 section 3.10).</summary>
    public enum SteeringMode : byte
    {
        /// <summary>Stand still (avoidance still pushes the agent out of overlaps).</summary>
        Hold = 0,
        /// <summary>Move toward the target at full speed.</summary>
        Seek = 1,
        /// <summary>Move toward the target, slowing inside the arrive radius and stopping at the stop distance.</summary>
        Arrive = 2,
        /// <summary>Circle the target at the orbit radius.</summary>
        Orbit = 3,
        /// <summary>Move directly away from the target.</summary>
        Flee = 4,
    }
}
