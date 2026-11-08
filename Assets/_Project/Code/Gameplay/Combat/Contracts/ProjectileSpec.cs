using Eclipse.Core.Damage;
using Eclipse.Core.Tags;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>Parameters to spawn one projectile (06 3.9). Damage is resolved by the caller. Frozen contract (M05 T1).</summary>
    public readonly struct ProjectileSpec
    {
        public readonly EntityId Source;
        public readonly Team Team;
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly float Speed;
        public readonly float Radius;
        public readonly float MaxRange;
        /// <summary>Extra targets it can pass through (0 = stops on first hit).</summary>
        public readonly int Pierce;
        public readonly DamageType Type;
        public readonly float Damage;
        public readonly float StaggerValue;
        public readonly float ProcCoefficient;
        public readonly int Generation;
        public readonly int HitGroupId;
        public readonly TagMask Tags;
        public readonly HitFlags Flags;

        public ProjectileSpec(EntityId source, Team team, Vector3 origin, Vector3 direction, float speed, float radius,
            float maxRange, int pierce, DamageType type, float damage, float staggerValue, float procCoefficient,
            int generation, int hitGroupId, TagMask tags, HitFlags flags)
        {
            Source = source; Team = team; Origin = origin; Direction = direction; Speed = speed; Radius = radius;
            MaxRange = maxRange; Pierce = pierce; Type = type; Damage = damage; StaggerValue = staggerValue;
            ProcCoefficient = procCoefficient; Generation = generation; HitGroupId = hitGroupId; Tags = tags; Flags = flags;
        }
    }
}
