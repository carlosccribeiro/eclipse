using UnityEngine;

namespace Eclipse.Gameplay.Player
{
    /// <summary>
    /// Per-hero movement data (08 section 3.1; values from 01). Asset: Content/Heroes/Kael/Data/Kael_MovementProfile.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Eclipse/Movement Profile", fileName = "MovementProfile")]
    public sealed class MovementProfile : ScriptableObject
    {
        /// <summary>
        /// Explicit marker for "no ramp": velocity snaps to the target in one tick.
        /// Stored in the asset as <c>Infinity</c>. Any finite value is a ramp in m/s^2.
        /// </summary>
        public const float Instant = float.PositiveInfinity;

        /// <summary>Base Movement Speed in m/s (08 section 3.1: Kael 6.0).</summary>
        [Min(0.01f)] public float baseSpeed = 6.0f;

        /// <summary>Acceleration in m/s^2, or <see cref="Instant"/> (infinity) for none.</summary>
        public float acceleration = Instant;

        /// <summary>Deceleration in m/s^2, or <see cref="Instant"/> (infinity) for none.</summary>
        public float deceleration = Instant;

        /// <summary>Floor of the final speed as a fraction of base speed, after slows (01 attributes: 40%).</summary>
        [Range(0f, 1f)] public float speedFloorFraction = 0.4f;

        public bool IsInstantAcceleration => float.IsPositiveInfinity(acceleration);
        public bool IsInstantDeceleration => float.IsPositiveInfinity(deceleration);

        /// <summary>Final speed in m/s. <paramref name="speedMultiplier"/> = (1 + Movement Speed bonus) net of slows; floored at <see cref="speedFloorFraction"/>.</summary>
        public float ComputeSpeed(float speedMultiplier)
        {
            float m = speedMultiplier < speedFloorFraction ? speedFloorFraction : speedMultiplier;
            return baseSpeed * m;
        }
    }
}
