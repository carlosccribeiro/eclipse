using Eclipse.AI.Telegraphs;
using Eclipse.Data.Enemies;
using Eclipse.Gameplay.Combat;
using UnityEngine;

namespace Eclipse.AI.Enemies
{
    /// <summary>Where and which way an attack was aimed when its wind-up started (locked for the telegraph).</summary>
    public struct AttackAim
    {
        /// <summary>Attacker ground position.</summary>
        public Vector3 Origin;
        /// <summary>Planar unit direction toward the target.</summary>
        public Vector3 Forward;
        /// <summary>Ground position of the target (destination of lobbed attacks).</summary>
        public Vector3 Target;
    }

    /// <summary>Pure geometry shared by the telegraph and the hit so they always match (09 section 3.3).</summary>
    public static class AttackGeometry
    {
        /// <summary>Planar unit vector from one point to another; <paramref name="fallback"/> when they coincide.</summary>
        public static Vector3 FlatDirection(Vector3 from, Vector3 to, Vector3 fallback)
        {
            Vector3 d = to - from;
            d.y = 0f;
            float m = d.magnitude;
            return m < 1e-4f ? fallback : d / m;
        }

        /// <summary>Ground point where the telegraph and the hit shape are anchored.</summary>
        public static Vector3 ShapeOrigin(AttackPlan plan, in AttackAim aim)
        {
            if (plan.IsLobbed) return aim.Target;
            return plan.ForwardOffset > 0f ? aim.Origin + aim.Forward * plan.ForwardOffset : aim.Origin;
        }

        /// <summary>
        /// Logical hit shape: circle = full sector, cone = sector with the data arc, line = box starting at the origin.
        /// False for shapes the frozen contract cannot express (Ring has no inner radius in <see cref="HitboxShape"/>).
        /// </summary>
        public static bool TryShape(AttackPlan plan, out HitboxShape shape)
        {
            switch (plan.Shape)
            {
                case TelegraphShape.Circle:
                    shape = HitboxShape.Sector(plan.Radius, 360f);
                    return plan.Radius > 0f;
                case TelegraphShape.Cone:
                    shape = HitboxShape.Sector(plan.Radius, plan.ArcDegrees);
                    return plan.Radius > 0f && plan.ArcDegrees > 0f;
                case TelegraphShape.Line:
                    shape = HitboxShape.Box(plan.Length, plan.Width);
                    return plan.Length > 0f && plan.Width > 0f;
                default:
                    shape = default;
                    return false;
            }
        }

        /// <summary>Yaw in degrees around Y of a planar direction (0 = +Z), the convention of <see cref="TelegraphRequest"/>.</summary>
        public static float YawDegrees(Vector3 forward) => Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        public static TelegraphRequest ToTelegraph(AttackPlan plan, in AttackAim aim)
        {
            return new TelegraphRequest
            {
                visual = plan.Visual,
                shape = plan.Shape,
                origin = ShapeOrigin(plan, in aim),
                yawDegrees = YawDegrees(aim.Forward),
                radius = plan.Radius,
                arcDegrees = plan.ArcDegrees,
                length = plan.Length,
                width = plan.Width,
                innerRadius = plan.InnerRadius,
            };
        }

        /// <summary>
        /// Seconds an arc needs from the enemy to the target (planar distance over data speed). The executor launches it
        /// that long before the wind-up ends so the landing coincides with the telegraph impact (09 4.2, 06 section 3.9).
        /// </summary>
        public static float FlightSeconds(AttackPlan plan, in AttackAim aim)
        {
            if (plan.ProjectileSpeed <= 0f) return 0f;
            Vector3 d = aim.Target - aim.Origin;
            d.y = 0f;
            return d.magnitude / plan.ProjectileSpeed;
        }
    }
}
