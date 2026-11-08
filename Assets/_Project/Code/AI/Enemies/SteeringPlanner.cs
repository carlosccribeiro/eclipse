using Eclipse.AI.Locomotion;
using Eclipse.AI.StateMachine;
using UnityEngine;

namespace Eclipse.AI.Enemies
{
    /// <summary>What the locomotion system should do for one agent.</summary>
    public struct SteeringCommand
    {
        public SteeringMode Mode;
        public Vector3 Target;
        public float OrbitRadius;
        public bool Clockwise;

        public static SteeringCommand Hold => new SteeringCommand { Mode = SteeringMode.Hold };
    }

    /// <summary>Inputs of <see cref="SteeringPlanner.Plan"/>; all values come from data or the world, none is balance.</summary>
    public struct SteeringContext
    {
        public Vector3 Self;
        public Vector3 Target;
        public ArchetypeTemplate Template;
        /// <summary>Preferred range band of the enemy (09 section 3.1).</summary>
        public float PreferredMin;
        public float PreferredMax;
        /// <summary>Orbit band of an enemy without token (09 section 3.2: 4 to 7 m).</summary>
        public float OrbitMin;
        public float OrbitMax;
        /// <summary>Distinct per enemy; picks the surround slot and the orbit direction.</summary>
        public int Slot;
        public bool HasLineOfSight;
        public bool HasEscortWard;
        public Vector3 EscortWard;
        /// <summary>Ranged retreat in progress (09 section 4.2).</summary>
        public bool Retreating;
    }

    /// <summary>
    /// Maps the HFSM state plus the archetype template to a locomotion steering mode (11 section 3.10): Approach is
    /// Seek/Arrive, Reposition is Orbit (or Arrive to the preferred band), Flee is Flee. Pure; no allocation.
    /// </summary>
    public static class SteeringPlanner
    {
        /// <summary>
        /// States that move: Approach/Reposition/Flee. Attack, Recover, Idle and Dead hold position (the wind-up keeps the
        /// telegraph where it was drawn). <paramref name="moveState"/> is the last decided movement, used while the brain
        /// waits in Engage between decisions.
        /// </summary>
        public static SteeringCommand Plan(EnemyState state, EnemyState moveState, in SteeringContext c)
        {
            if (state == EnemyState.Engage) state = moveState;
            if (c.Retreating && (state == EnemyState.Approach || state == EnemyState.Reposition || state == EnemyState.Engage))
                return new SteeringCommand { Mode = SteeringMode.Flee, Target = c.Target };

            switch (state)
            {
                case EnemyState.Approach: return Approach(in c);
                case EnemyState.Reposition: return Reposition(in c);
                case EnemyState.Flee: return new SteeringCommand { Mode = SteeringMode.Flee, Target = c.Target };
                default: return SteeringCommand.Hold;
            }
        }

        private static SteeringCommand Approach(in SteeringContext c)
        {
            switch (c.Template.Style)
            {
                case SteeringStyle.Swarm:
                    return new SteeringCommand { Mode = SteeringMode.Seek, Target = c.Target };
                case SteeringStyle.Flank:
                    return new SteeringCommand { Mode = SteeringMode.Arrive, Target = SurroundPoint(in c) };
                case SteeringStyle.KeepDistance:
                    return new SteeringCommand { Mode = SteeringMode.Arrive, Target = BandPoint(in c) };
                default: // Escort
                    if (c.HasEscortWard) return new SteeringCommand { Mode = SteeringMode.Arrive, Target = EscortPoint(in c) };
                    return new SteeringCommand { Mode = SteeringMode.Seek, Target = c.Target };
            }
        }

        private static SteeringCommand Reposition(in SteeringContext c)
        {
            float distance = PlanarDistance(c.Self, c.Target);
            bool clockwise = (c.Slot & 1) == 0;

            if (c.Template.Style == SteeringStyle.KeepDistance)
            {
                // Outside the band: walk back into it. Inside: strafe at the current radius (looks for line of sight).
                if (distance < c.PreferredMin || distance > c.PreferredMax)
                    return new SteeringCommand { Mode = SteeringMode.Arrive, Target = BandPoint(in c) };
                return new SteeringCommand { Mode = SteeringMode.Orbit, Target = c.Target, OrbitRadius = distance, Clockwise = clockwise };
            }

            if (c.Template.Style == SteeringStyle.Escort && c.HasEscortWard)
                return new SteeringCommand { Mode = SteeringMode.Arrive, Target = EscortPoint(in c) };

            float radius = distance < c.OrbitMin ? c.OrbitMin : (distance > c.OrbitMax ? c.OrbitMax : distance);
            return new SteeringCommand { Mode = SteeringMode.Orbit, Target = c.Target, OrbitRadius = radius, Clockwise = clockwise };
        }

        /// <summary>Point of the preferred band (middle of min/max) on the line from the target through the enemy.</summary>
        public static Vector3 BandPoint(in SteeringContext c)
        {
            Vector3 away = AttackGeometry.FlatDirection(c.Target, c.Self, Vector3.back);
            float mid = (c.PreferredMin + c.PreferredMax) * 0.5f;
            return c.Target + away * mid;
        }

        /// <summary>
        /// Melee surround: the point at the preferred reach on the line from the target to the enemy, rotated by the slot
        /// angle so a group spreads around the target instead of queuing in a line (09 4.1 "desvio leve para cercar").
        /// </summary>
        public static Vector3 SurroundPoint(in SteeringContext c)
        {
            ArchetypeTemplate t = c.Template;
            Vector3 away = AttackGeometry.FlatDirection(c.Target, c.Self, Vector3.back);
            int count = t.FlankSlotCount < 1 ? 1 : t.FlankSlotCount;
            int slot = ((c.Slot % count) + count) % count;
            float angle = (slot - (count - 1) * 0.5f) * t.FlankSlotSpreadDegrees;
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * away;
            return c.Target + dir * c.PreferredMax;
        }

        /// <summary>Tank escort: stands on the line from the ward to the target, at the escort distance from the ward.</summary>
        public static Vector3 EscortPoint(in SteeringContext c)
        {
            Vector3 toTarget = AttackGeometry.FlatDirection(c.EscortWard, c.Target, Vector3.forward);
            return c.EscortWard + toTarget * c.Template.EscortDistance;
        }

        public static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
