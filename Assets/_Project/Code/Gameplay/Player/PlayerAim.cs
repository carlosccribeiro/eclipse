using Eclipse.Gameplay.Input;
using UnityEngine;

namespace Eclipse.Gameplay.Player
{
    /// <summary>
    /// Aim resolution (06 section 3.4): M+K cursor projected on the ground plane at chest height; gamepad right stick;
    /// without a right stick the aim is the movement direction. Math lives in static, testable functions.
    /// </summary>
    public sealed class PlayerAim
    {
        /// <summary>Engineering constant: stick deadzone (not a balance number).</summary>
        public const float StickDeadzone = 0.2f;

        /// <summary>Camera used to project the cursor. If null, <see cref="Camera.main"/> is looked up lazily.</summary>
        public Camera Camera;

        /// <summary>Chest height in meters above the feet (06 section 3.4 gives no number; placeholder, tune per hero).</summary>
        public float ChestHeight = 1.2f;

        /// <summary>Yaw (degrees) rotating stick input into world space; match the camera yaw (isometric = 45).</summary>
        public float InputBasisYawDegrees;

        public Vector3 AimPoint { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;

        /// <summary>Intersects a ray with the horizontal plane y = planeY. False if parallel or behind the origin.</summary>
        public static bool RayPlane(Vector3 origin, Vector3 direction, float planeY, out Vector3 point)
        {
            point = default;
            if (Mathf.Abs(direction.y) < 1e-6f) return false;
            float t = (planeY - origin.y) / direction.y;
            if (t < 0f) return false;
            point = origin + direction * t;
            return true;
        }

        /// <summary>Projects a screen position onto the plane y = planeY through <paramref name="camera"/>.</summary>
        public static bool ProjectScreenPoint(Camera camera, Vector2 screenPosition, float planeY, out Vector3 point)
        {
            Ray ray = camera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));
            return RayPlane(ray.origin, ray.direction, planeY, out point);
        }

        /// <summary>Normalized horizontal direction from origin to target; false (and +Z) if they coincide on XZ.</summary>
        public static bool DirectionTo(Vector3 origin, Vector3 target, out Vector3 direction)
        {
            direction = new Vector3(target.x - origin.x, 0f, target.z - origin.z);
            float sq = direction.sqrMagnitude;
            if (sq < 1e-6f) { direction = Vector3.forward; return false; }
            direction /= Mathf.Sqrt(sq);
            return true;
        }

        /// <summary>Maps a 2D stick (x = right, y = forward) to world XZ rotated by <paramref name="yawDegrees"/>; magnitude is kept.</summary>
        public static Vector3 StickToWorld(Vector2 stick, float yawDegrees)
        {
            return Quaternion.Euler(0f, yawDegrees, 0f) * new Vector3(stick.x, 0f, stick.y);
        }

        /// <summary>Gamepad rule: right stick above the deadzone wins; else movement direction; else <paramref name="fallback"/>.</summary>
        public static Vector3 ResolveGamepadDirection(Vector3 stickWorld, Vector3 moveWorld, Vector3 fallback)
        {
            const float sq = StickDeadzone * StickDeadzone;
            if (stickWorld.sqrMagnitude >= sq) return stickWorld.normalized;
            if (moveWorld.sqrMagnitude >= sq) return moveWorld.normalized;
            return fallback;
        }

        /// <summary>Updates <see cref="AimPoint"/> and <see cref="AimDirection"/>. Call once per fixed tick.</summary>
        public void Evaluate(PlayerInputReader input, Vector3 feetPosition, Vector3 moveWorld, Vector3 currentFacing)
        {
            float chestY = feetPosition.y + ChestHeight;
            var chest = new Vector3(feetPosition.x, chestY, feetPosition.z);

            if (input.AimIsGamepad)
            {
                Vector3 stick = StickToWorld(input.Aim, InputBasisYawDegrees);
                AimDirection = ResolveGamepadDirection(stick, moveWorld, currentFacing);
                AimPoint = chest + AimDirection;
                return;
            }

            Camera cam = Camera != null ? Camera : (Camera = Camera.main);
            if (cam != null && ProjectScreenPoint(cam, input.Aim, chestY, out Vector3 p) && DirectionTo(feetPosition, p, out Vector3 d))
            {
                AimPoint = p;
                AimDirection = d;
                return;
            }
            AimDirection = currentFacing;
            AimPoint = chest + currentFacing;
        }
    }
}
