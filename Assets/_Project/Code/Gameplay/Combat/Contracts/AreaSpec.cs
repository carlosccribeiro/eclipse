using Eclipse.Core.Damage;
using Eclipse.Core.Tags;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Instant area (DurationFrames = 0) or persistent zone (ticks every TickIntervalFrames; 30 = 0.5 s, 06 3.2).
    /// Frozen contract (M05 T1).
    /// </summary>
    public readonly struct AreaSpec
    {
        public readonly EntityId Source;
        public readonly Team Team;
        public readonly HitboxShape Shape;
        public readonly Vector3 Origin;
        public readonly Vector3 Forward;
        public readonly int DurationFrames;
        public readonly int TickIntervalFrames;
        public readonly DamageType Type;
        public readonly float Damage;
        public readonly float StaggerValue;
        public readonly float ProcCoefficient;
        public readonly int Generation;
        public readonly int HitGroupId;
        public readonly TagMask Tags;
        public readonly HitFlags Flags;

        public AreaSpec(EntityId source, Team team, HitboxShape shape, Vector3 origin, Vector3 forward, int durationFrames,
            int tickIntervalFrames, DamageType type, float damage, float staggerValue, float procCoefficient,
            int generation, int hitGroupId, TagMask tags, HitFlags flags)
        {
            Source = source; Team = team; Shape = shape; Origin = origin; Forward = forward; DurationFrames = durationFrames;
            TickIntervalFrames = tickIntervalFrames; Type = type; Damage = damage; StaggerValue = staggerValue;
            ProcCoefficient = procCoefficient; Generation = generation; HitGroupId = hitGroupId; Tags = tags; Flags = flags;
        }

        public bool IsPersistent => DurationFrames > 0;
    }
}
