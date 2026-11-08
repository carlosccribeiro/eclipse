using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Optional behaviours layered on a <see cref="ProjectileSpec"/> (06 3.9). Every value comes from the caller (skill or
    /// enemy data); none is a balance number. Basic implementations of Chain/Fork/Return/Homing; M10 deepens them.
    /// </summary>
    public readonly struct ProjectileBehavior
    {
        public static readonly ProjectileBehavior None = default;

        /// <summary>Extra jumps to a new target within <see cref="ChainRadius"/> after a hit.</summary>
        public readonly int ChainCount;
        public readonly float ChainRadius;
        /// <summary>Children spawned on the first hit (spread symmetrically by <see cref="ForkAngleDegrees"/> between neighbours).</summary>
        public readonly int ForkCount;
        public readonly float ForkAngleDegrees;
        /// <summary>At the end of the range the projectile flies back to its origin and may hit targets again.</summary>
        public readonly bool Return;
        /// <summary>Max steering rate towards the nearest target within <see cref="HomingRadius"/> (0 = straight).</summary>
        public readonly float HomingDegreesPerSecond;
        public readonly float HomingRadius;
        /// <summary>
        /// Artillery arc (09 4.2): parabola from the origin to <see cref="ArcTarget"/> with the given apex height above the
        /// straight line. It does not collide in flight and reports the landing to <see cref="IProjectileListener"/>.
        /// </summary>
        public readonly bool Arc;
        public readonly Vector3 ArcTarget;
        public readonly float ArcApexHeight;

        public ProjectileBehavior(int chainCount, float chainRadius, int forkCount, float forkAngleDegrees, bool returns,
            float homingDegreesPerSecond, float homingRadius, bool arc, Vector3 arcTarget, float arcApexHeight)
        {
            ChainCount = chainCount; ChainRadius = chainRadius; ForkCount = forkCount; ForkAngleDegrees = forkAngleDegrees;
            Return = returns; HomingDegreesPerSecond = homingDegreesPerSecond; HomingRadius = homingRadius;
            Arc = arc; ArcTarget = arcTarget; ArcApexHeight = arcApexHeight;
        }

        public static ProjectileBehavior Artillery(Vector3 target, float apexHeight) =>
            new ProjectileBehavior(0, 0f, 0, 0f, false, 0f, 0f, true, target, apexHeight);

        public static ProjectileBehavior Chaining(int count, float radius) =>
            new ProjectileBehavior(count, radius, 0, 0f, false, 0f, 0f, false, default, 0f);

        public static ProjectileBehavior Forking(int count, float angleDegrees) =>
            new ProjectileBehavior(0, 0f, count, angleDegrees, false, 0f, 0f, false, default, 0f);

        public static ProjectileBehavior Boomerang() =>
            new ProjectileBehavior(0, 0f, 0, 0f, true, 0f, 0f, false, default, 0f);

        public static ProjectileBehavior Homing(float degreesPerSecond, float radius) =>
            new ProjectileBehavior(0, 0f, 0, 0f, false, degreesPerSecond, radius, false, default, 0f);
    }

    /// <summary>Receives projectile lifecycle events that need game-side action (e.g. detonating the landing area of an arc).</summary>
    public interface IProjectileListener
    {
        void OnArcLanded(in ProjectileSpec spec, Vector3 position, int frame);
    }

    /// <summary>Target search used by Chain and Homing. Implemented over physics by <see cref="PhysicsProjectileTargetSearch"/>.</summary>
    public interface IProjectileTargetSearch
    {
        /// <summary>
        /// Nearest living enemy of <paramref name="sourceTeam"/> within <paramref name="radius"/> of <paramref name="from"/> that is not yet
        /// registered in <paramref name="alreadyHit"/> for <paramref name="hitGroup"/>.
        /// </summary>
        bool TryFindNearest(Vector3 from, float radius, Team sourceTeam, HitRegistry alreadyHit, int hitGroup, out Vector3 point);
    }

    /// <summary>Pure steering / ballistic helpers (no Unity physics).</summary>
    public static class ProjectileMath
    {
        /// <summary>Rotates <paramref name="dir"/> towards <paramref name="targetDir"/> by at most <paramref name="maxDegrees"/> (planar-agnostic, unit result).</summary>
        public static Vector3 TurnTowards(Vector3 dir, Vector3 targetDir, float maxDegrees)
        {
            if (targetDir.sqrMagnitude < 1e-10f) return dir;
            float max = maxDegrees * Mathf.Deg2Rad;
            return Vector3.RotateTowards(dir, targetDir.normalized, max, 0f).normalized;
        }

        /// <summary>Position on the arc: linear interpolation start-&gt;end plus a parabolic lift of <paramref name="apex"/> at u = 0.5.</summary>
        public static Vector3 ArcPosition(Vector3 start, Vector3 end, float apex, float u)
        {
            u = Mathf.Clamp01(u);
            Vector3 p = Vector3.Lerp(start, end, u);
            p.y += 4f * apex * u * (1f - u);
            return p;
        }

        /// <summary>Flight time of an arc at the given horizontal speed (planar distance / speed).</summary>
        public static float ArcDuration(Vector3 start, Vector3 end, float speed)
        {
            float dx = end.x - start.x, dz = end.z - start.z;
            return speed <= 0f ? 0f : Mathf.Sqrt(dx * dx + dz * dz) / speed;
        }

        /// <summary>Directions of fork children: <paramref name="count"/> rays centred on <paramref name="dir"/>, <paramref name="angle"/> apart, rotated around Y.</summary>
        public static Vector3 ForkDirection(Vector3 dir, int index, int count, float angleDegrees)
        {
            float offset = (index - (count - 1) * 0.5f) * angleDegrees;
            return Quaternion.AngleAxis(offset, Vector3.up) * dir;
        }
    }
}
