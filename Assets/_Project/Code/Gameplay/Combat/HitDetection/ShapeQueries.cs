using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Pure shape math for logical hitboxes (06 3.9). All tests are planar (XZ): the game is isometric with no
    /// vertical combat, so hitbox height is not modelled (gap: no height in the data). Targets are circles
    /// (the vertical hurtbox capsule seen from above). No Unity physics, no allocations.
    /// </summary>
    public static class ShapeQueries
    {
        /// <summary>Engineering cap on interpolated poses per sweep (keeps the per-tick cost bounded).</summary>
        public const int MaxSweepSamples = 16;

        /// <summary>Engineering floor for the sweep step so thin/zero-radius targets do not explode the sample count.</summary>
        public const float MinSweepStep = 0.05f;

        /// <summary>Unit planar forward; falls back to world forward when degenerate.</summary>
        public static Vector3 FlatForward(Vector3 forward)
        {
            forward.y = 0f;
            float m = forward.magnitude;
            return m < 1e-6f ? Vector3.forward : forward / m;
        }

        /// <summary>Right vector of a flat forward (x right, z forward, left-handed Unity convention).</summary>
        public static Vector3 FlatRight(Vector3 flatForward) => new Vector3(flatForward.z, 0f, -flatForward.x);

        /// <summary>Converts a local offset (x right, y up, z forward) to world space.</summary>
        public static Vector3 ToWorld(Vector3 origin, Vector3 forward, Vector3 localOffset)
        {
            Vector3 f = FlatForward(forward);
            Vector3 r = FlatRight(f);
            return origin + r * localOffset.x + Vector3.up * localOffset.y + f * localOffset.z;
        }

        /// <summary>Circle (target) inside a sector with apex at <paramref name="apex"/>. arc &gt;= 360 is a full circle.</summary>
        public static bool CircleInSector(Vector3 apex, Vector3 forward, float radius, float arcDegrees,
            Vector3 center, float targetRadius)
        {
            float dx = center.x - apex.x;
            float dz = center.z - apex.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist > radius + targetRadius) return false;
            if (arcDegrees >= 360f || dist <= targetRadius) return true;

            Vector3 f = FlatForward(forward);
            float cos = Mathf.Clamp((dx * f.x + dz * f.z) / dist, -1f, 1f);
            float angle = Mathf.Acos(cos) * Mathf.Rad2Deg;
            float slack = Mathf.Asin(Mathf.Clamp01(targetRadius / dist)) * Mathf.Rad2Deg;
            return angle <= arcDegrees * 0.5f + slack;
        }

        /// <summary>Circle inside a box that starts at <paramref name="rearCenter"/> and extends <paramref name="length"/> forward.</summary>
        public static bool CircleInBox(Vector3 rearCenter, Vector3 forward, float length, float width,
            Vector3 center, float targetRadius)
        {
            Vector3 f = FlatForward(forward);
            Vector3 r = FlatRight(f);
            float dx = center.x - rearCenter.x;
            float dz = center.z - rearCenter.z;
            float lz = dx * f.x + dz * f.z;
            float lx = dx * r.x + dz * r.z;
            float cz = Mathf.Clamp(lz, 0f, length);
            float cx = Mathf.Clamp(lx, -width * 0.5f, width * 0.5f);
            float ex = lx - cx;
            float ez = lz - cz;
            return ex * ex + ez * ez <= targetRadius * targetRadius;
        }

        /// <summary>Circle against a capsule whose segment runs from <paramref name="start"/> forward by <paramref name="length"/>.</summary>
        public static bool CircleInCapsule(Vector3 start, Vector3 forward, float radius, float length,
            Vector3 center, float targetRadius)
        {
            Vector3 f = FlatForward(forward);
            float dx = center.x - start.x;
            float dz = center.z - start.z;
            float t = Mathf.Clamp(dx * f.x + dz * f.z, 0f, Mathf.Max(0f, length));
            float ex = dx - f.x * t;
            float ez = dz - f.z * t;
            float rr = radius + targetRadius;
            return ex * ex + ez * ez <= rr * rr;
        }

        /// <summary>Single-pose overlap of a hitbox anchored at <paramref name="origin"/>/<paramref name="forward"/> with a target circle.</summary>
        public static bool Overlaps(in HitboxShape shape, Vector3 origin, Vector3 forward, Vector3 center, float targetRadius)
        {
            Vector3 anchor = ToWorld(origin, forward, shape.LocalOffset);
            switch (shape.Kind)
            {
                case HitboxKind.Sector:
                    return CircleInSector(anchor, forward, shape.Radius, shape.ArcDegrees, center, targetRadius);
                case HitboxKind.Box:
                    return CircleInBox(anchor, forward, shape.Length, shape.Width, center, targetRadius);
                case HitboxKind.Capsule:
                    return CircleInCapsule(anchor, forward, shape.Radius, shape.Length, center, targetRadius);
                default:
                    return false;
            }
        }

        /// <summary>Farthest planar distance from the attacker origin the shape can reach (broad-phase radius).</summary>
        public static float Reach(in HitboxShape shape)
        {
            float off = new Vector2(shape.LocalOffset.x, shape.LocalOffset.z).magnitude;
            switch (shape.Kind)
            {
                case HitboxKind.Sector: return off + shape.Radius;
                case HitboxKind.Box:
                    return off + Mathf.Sqrt(shape.Length * shape.Length + shape.Width * shape.Width * 0.25f);
                case HitboxKind.Capsule: return off + shape.Length + shape.Radius;
                default: return 0f;
            }
        }

        /// <summary>Number of interpolated poses (including both ends) needed so no step skips a target of the given radius.</summary>
        public static int SweepSamples(in HitboxShape shape, Vector3 prevOrigin, Vector3 prevForward,
            Vector3 origin, Vector3 forward, float targetRadius)
        {
            float mx = origin.x - prevOrigin.x;
            float mz = origin.z - prevOrigin.z;
            float move = Mathf.Sqrt(mx * mx + mz * mz);
            Vector3 a = FlatForward(prevForward);
            Vector3 b = FlatForward(forward);
            float turn = Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f)); // radians
            float travel = Mathf.Max(move, turn * Reach(shape));
            float step = Mathf.Max(targetRadius, MinSweepStep);
            int segments = Mathf.CeilToInt(travel / step);
            return Mathf.Clamp(segments + 1, 2, MaxSweepSamples);
        }

        /// <summary>
        /// Sweep between the previous and current attacker pose (anti-tunneling, 06 3.9): the hitbox is tested at
        /// interpolated poses; true if any one overlaps the target.
        /// </summary>
        public static bool OverlapsSwept(in HitboxShape shape, Vector3 prevOrigin, Vector3 prevForward,
            Vector3 origin, Vector3 forward, Vector3 center, float targetRadius)
        {
            int samples = SweepSamples(shape, prevOrigin, prevForward, origin, forward, targetRadius);
            Vector3 a = FlatForward(prevForward);
            Vector3 b = FlatForward(forward);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);
                Vector3 o = Vector3.Lerp(prevOrigin, origin, t);
                Vector3 f = Vector3.Slerp(a, b, t);
                if (Overlaps(shape, o, f, center, targetRadius)) return true;
            }
            return false;
        }
    }
}
