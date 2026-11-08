using Eclipse.Data.Combat;
using UnityEngine;

namespace Eclipse.Gameplay.Combat.Attacks
{
    /// <summary>A body the lunge must not cross (enemy hurtbox/capsule), horizontal position and radius.</summary>
    public readonly struct LungeCandidate
    {
        public readonly Vector3 Position;
        public readonly float Radius;

        public LungeCandidate(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
    }

    /// <summary>Result of <see cref="LungeSolver.Plan"/>: flat unit direction, total distance and tick count.</summary>
    public readonly struct LungePlan
    {
        public readonly Vector3 Direction;
        public readonly float Distance;
        public readonly int Ticks;

        public LungePlan(Vector3 direction, float distance, int ticks)
        {
            Direction = direction;
            Distance = distance;
            Ticks = ticks;
        }

        public bool HasLunge => Distance > 0f && Ticks > 0;

        /// <summary>Displacement of each tick (uniform spread; the docs give no curve - logged gap).</summary>
        public Vector3 StepPerTick => HasLunge ? Direction * (Distance / Ticks) : Vector3.zero;
    }

    /// <summary>
    /// Melee lunge (06 3.4: advances min..max metres toward the nearest target inside a cone in front, defined per hit;
    /// never crosses enemies). Pure math over <see cref="AttackStep"/> data: lunge distance 0 in the data means no lunge.
    /// With a target: moves up to contact, clamped to LungeMax. Without a target: moves LungeMin forward.
    /// The cone is read from the data (0 = no target seeking).
    /// </summary>
    public static class LungeSolver
    {
        /// <param name="step">Data of the attack.</param>
        /// <param name="origin">Attacker feet position.</param>
        /// <param name="forward">Attacker facing (flattened internally).</param>
        /// <param name="candidates">Bodies in range; only [0, count) is read.</param>
        /// <param name="selfRadius">Attacker body radius (contact clearance).</param>
        /// <param name="ticks">Number of ticks over which the lunge is spread.</param>
        public static LungePlan Plan(in AttackStep step, Vector3 origin, Vector3 forward, LungeCandidate[] candidates,
            int count, float selfRadius, int ticks)
        {
            float max = step.LungeMaxMeters;
            if (max <= 0f || ticks <= 0) return default;
            float min = Mathf.Clamp(step.LungeMinMeters, 0f, max);

            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-8f) return default;
            forward.Normalize();

            Vector3 dir = forward;
            float distance = min;

            // Nearest target inside the cone.
            float halfCone = step.LungeConeDegrees * 0.5f;
            int best = -1;
            float bestDist = float.MaxValue;
            if (halfCone > 0f && candidates != null)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 d = candidates[i].Position - origin;
                    d.y = 0f;
                    float dist = d.magnitude;
                    if (dist < 1e-4f) continue;
                    if (Vector3.Angle(forward, d / dist) > halfCone) continue;
                    if (dist < bestDist) { bestDist = dist; best = i; }
                }
            }

            if (best >= 0)
            {
                Vector3 d = candidates[best].Position - origin;
                d.y = 0f;
                dir = d.normalized;
                float gap = bestDist - candidates[best].Radius - selfRadius;
                distance = Mathf.Clamp(gap, 0f, max);
            }

            // Never cross any body along the path (blockers may be outside the cone).
            if (candidates != null)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 d = candidates[i].Position - origin;
                    d.y = 0f;
                    float ahead = Vector3.Dot(d, dir);
                    if (ahead <= 0f) continue;
                    float lateral = (d - dir * ahead).magnitude;
                    float clearance = candidates[i].Radius + selfRadius;
                    if (lateral >= clearance) continue;
                    float reach = ahead - Mathf.Sqrt(clearance * clearance - lateral * lateral);
                    distance = Mathf.Min(distance, Mathf.Max(0f, reach));
                }
            }

            return distance > 0f ? new LungePlan(dir, distance, ticks) : default;
        }
    }
}
