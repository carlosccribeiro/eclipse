using System;

namespace Eclipse.AI.Locomotion
{
    /// <summary>
    /// Locomotion tuning. Per-archetype speeds come from EnemyDefinition (09 section 4) and are passed per agent;
    /// the values here are structural limits and steering weights (TUNED placeholders, not in the GDD).
    /// </summary>
    [Serializable]
    public sealed class LocomotionConfig
    {
        /// <summary>No enemy moves faster than this outside a charge (09 section 4, R50).</summary>
        public float GlobalMaxSpeed = 5.8f;

        /// <summary>Acceleration toward the desired velocity, m/s^2 (TUNED).</summary>
        public float Acceleration = 24f;

        /// <summary>Arrive starts slowing inside this distance, m (TUNED).</summary>
        public float ArriveSlowRadius = 3f;

        /// <summary>Arrive stops inside this distance, m (TUNED).</summary>
        public float ArriveStopDistance = 0.1f;

        /// <summary>Weight of the radial correction when orbiting (TUNED).</summary>
        public float OrbitRadialWeight = 2f;

        /// <summary>Avoidance push as a fraction of the agent max speed (TUNED).</summary>
        public float AvoidanceWeight = 1.5f;

        /// <summary>Extra personal space added to the sum of radii for avoidance, m (TUNED).</summary>
        public float AvoidanceMargin = 0.1f;
    }
}
