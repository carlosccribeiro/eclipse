using UnityEngine;

namespace Eclipse.Gameplay.Cameras
{
    /// <summary>Pure look-ahead / pose math (06 section 3.11).</summary>
    public static class CameraLookAhead
    {
        /// <summary>
        /// Horizontal offset toward the aim: <paramref name="fraction"/> of the player-to-aim distance, length capped at
        /// <paramref name="maxMeters"/>. Y is ignored.
        /// </summary>
        public static Vector3 ComputeOffset(Vector3 playerPosition, Vector3 aimPoint, float fraction, float maxMeters)
        {
            var delta = new Vector3(aimPoint.x - playerPosition.x, 0f, aimPoint.z - playerPosition.z);
            float length = delta.magnitude;
            if (length < 1e-6f || maxMeters <= 0f || fraction <= 0f) return Vector3.zero;
            float offsetLength = Mathf.Min(length * fraction, maxMeters);
            return delta * (offsetLength / length);
        }

        /// <summary>Camera rotation for fixed pitch/yaw.</summary>
        public static Quaternion Rotation(float pitchDegrees, float yawDegrees) => Quaternion.Euler(pitchDegrees, yawDegrees, 0f);

        /// <summary>Camera position <paramref name="distance"/> meters behind <paramref name="focus"/> along the view axis.</summary>
        public static Vector3 Position(Vector3 focus, float pitchDegrees, float yawDegrees, float distance)
        {
            return focus - Rotation(pitchDegrees, yawDegrees) * Vector3.forward * distance;
        }
    }
}
