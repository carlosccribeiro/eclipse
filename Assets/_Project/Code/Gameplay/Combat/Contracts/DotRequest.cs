using Eclipse.Core.Damage;
using Eclipse.Core.Tags;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Damage over time with a snapshot taken at application (buffs applied later do not change it - 06 3.2).
    /// Tick interval is 0.5 s = 30 ticks by default (R19). Frozen contract (M05 T1).
    /// </summary>
    public readonly struct DotRequest
    {
        public readonly EntityId Source;
        public readonly EntityId Target;
        public readonly DamageType Type;
        /// <summary>Damage per tick, already resolved (snapshot).</summary>
        public readonly float DamagePerTick;
        public readonly int TickIntervalFrames;
        public readonly int DurationFrames;
        public readonly int Generation;
        public readonly float ProcCoefficient;
        public readonly int HitGroupId;
        public readonly TagMask Tags;
        public readonly HitFlags Flags;

        public DotRequest(EntityId source, EntityId target, DamageType type, float damagePerTick, int tickIntervalFrames,
            int durationFrames, int generation, float procCoefficient, int hitGroupId, TagMask tags, HitFlags flags)
        {
            Source = source; Target = target; Type = type; DamagePerTick = damagePerTick; TickIntervalFrames = tickIntervalFrames;
            DurationFrames = durationFrames; Generation = generation; ProcCoefficient = procCoefficient; HitGroupId = hitGroupId;
            Tags = tags; Flags = flags;
        }
    }
}
