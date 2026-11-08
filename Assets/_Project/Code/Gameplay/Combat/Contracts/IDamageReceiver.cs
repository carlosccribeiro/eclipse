using Eclipse.Core.Damage;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>How an incoming hit ended (06 3.3 incoming pipeline).</summary>
    public enum HitResult : byte
    {
        Hit = 0,
        Invulnerable = 1,
        Dodged = 2,
        PerfectDodged = 3,
        Parried = 4,
        Blocked = 5,
        Evaded = 6,
        Absorbed = 7,
    }

    /// <summary>Outcome of <see cref="IDamageReceiver.ReceiveHit"/>.</summary>
    public readonly struct DamageOutcome
    {
        public readonly HitResult Result;
        /// <summary>Damage after mitigation, before Shield.</summary>
        public readonly float FinalDamage;
        /// <summary>Damage that actually reached health.</summary>
        public readonly float HealthDamage;
        /// <summary>Damage beyond the remaining health (0 if the target survived).</summary>
        public readonly float Overkill;
        public readonly bool Killed;

        public DamageOutcome(HitResult result, float finalDamage, float healthDamage, float overkill, bool killed)
        {
            Result = result; FinalDamage = finalDamage; HealthDamage = healthDamage; Overkill = overkill; Killed = killed;
        }
    }

    /// <summary>Receives a resolved hit. Frozen contract (M05 T1). The <see cref="DamageInstance"/> is owned by the caller (pooled).</summary>
    public interface IDamageReceiver
    {
        EntityId Id { get; }
        DamageOutcome ReceiveHit(DamageInstance hit, Vector3 hitDirection);
    }
}
