using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    public enum HitboxKind : byte
    {
        Sector = 0,
        Box = 1,
        Capsule = 2,
    }

    /// <summary>
    /// Logical hitbox in the attacker local space (06 3.9). Values come from data, never hardcoded.
    /// Frozen contract (M05 T1).
    /// </summary>
    public readonly struct HitboxShape
    {
        public readonly HitboxKind Kind;
        /// <summary>Sector: radius. Capsule: radius. Box: unused.</summary>
        public readonly float Radius;
        /// <summary>Sector only: full arc in degrees.</summary>
        public readonly float ArcDegrees;
        /// <summary>Box: size along forward. Capsule: segment length along forward.</summary>
        public readonly float Length;
        /// <summary>Box: size across.</summary>
        public readonly float Width;
        /// <summary>Local offset from the attacker origin (x right, y up, z forward).</summary>
        public readonly Vector3 LocalOffset;

        public HitboxShape(HitboxKind kind, float radius, float arcDegrees, float length, float width, Vector3 localOffset)
        {
            Kind = kind; Radius = radius; ArcDegrees = arcDegrees; Length = length; Width = width; LocalOffset = localOffset;
        }

        public static HitboxShape Sector(float radius, float arcDegrees, Vector3 offset = default)
            => new HitboxShape(HitboxKind.Sector, radius, arcDegrees, 0f, 0f, offset);

        public static HitboxShape Box(float length, float width, Vector3 offset = default)
            => new HitboxShape(HitboxKind.Box, 0f, 0f, length, width, offset);

        public static HitboxShape Capsule(float radius, float length, Vector3 offset = default)
            => new HitboxShape(HitboxKind.Capsule, radius, 0f, length, 0f, offset);
    }
}
