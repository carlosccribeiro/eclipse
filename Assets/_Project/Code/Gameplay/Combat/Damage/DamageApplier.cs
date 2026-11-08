using Eclipse.Core.Damage;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Applies an already-resolved amount to Shield then Health, records the Death Recap, handles death and publishes
    /// OnHitTaken/OnBlock/OnShieldBroken/OnLowHealth/OnKill/OnOverkill (06 3.3 steps 6-7, 3.14). No damage math beyond
    /// subtraction: mitigation is the resolver's.
    /// </summary>
    public sealed class DamageApplier
    {
        private readonly HealthComponent _health;
        private readonly ShieldComponent _shield;
        private readonly DeathHandler _death;
        private readonly CombatEventStream _events;
        private readonly CombatEntityConfig _cfg;

        public DamageApplier(HealthComponent health, ShieldComponent shield, DeathHandler death, CombatEventStream events, CombatEntityConfig cfg)
        {
            _health = health; _shield = shield; _death = death; _events = events; _cfg = cfg;
        }

        /// <summary>Publishes OnHitTaken(value 0) for a hit that did no damage.</summary>
        public void ReportNegated(DamageInstance hit, EntityId self, HitResult result, int frame)
        {
            _events?.Publish(new OnHitTaken(frame, hit.Generation, new EntityId(hit.SourceId), self, 0f, DominantType(hit), result, hit.Flags));
        }

        public DamageOutcome Apply(DamageInstance hit, EntityId self, in IncomingResult r, bool targetIsElite, ulong activeStatuses,
            Vector3 position, int frame)
        {
            var source = new EntityId(hit.SourceId);
            DamageType type = DominantType(hit);
            float prevFraction = _health.Fraction;

            _shield.Absorb(r.ShieldAbsorbed, out bool broken);
            float applied = _health.Damage(r.HealthDamage, out float overkill);
            bool killed = _health.IsDead;
            float finalDamage = r.AfterDamageReduction;
            HitResult result = r.Blocked ? HitResult.Blocked : HitResult.Hit;

            _death.Recap.Record(new RecapEntry
            {
                Frame = frame, Source = source, Type = type, Incoming = r.Incoming, Final = finalDamage,
                Mitigated = r.Incoming - finalDamage, Blocked = r.Blocked, ActiveStatuses = activeStatuses,
            });

            if (_events != null)
            {
                _events.Publish(new OnHitTaken(frame, hit.Generation, source, self, applied, type, result, hit.Flags));
                if (r.Blocked) _events.Publish(new OnBlock(frame, hit.Generation, source, self, r.AfterMitigation - r.AfterBlock));
                if (broken) _events.Publish(new OnShieldBroken(frame, hit.Generation, source, self));
                if (_cfg.LowHealthFraction > 0f && !killed && prevFraction >= _cfg.LowHealthFraction && _health.Fraction < _cfg.LowHealthFraction)
                    _events.Publish(new OnLowHealth(frame, hit.Generation, self, _health.Fraction));
                if (killed)
                {
                    _events.Publish(new OnKill(frame, hit.Generation, source, self, finalDamage, targetIsElite, position));
                    if (overkill > 0f) _events.Publish(new OnOverkill(frame, hit.Generation, source, self, overkill));
                }
            }
            if (killed) _death.Die(self, source, position, frame, hit.Generation);
            return new DamageOutcome(result, finalDamage, applied, overkill, killed);
        }

        /// <summary>Dominant damage type of the hit (largest component; Physical when empty).</summary>
        public static DamageType DominantType(DamageInstance hit)
        {
            DamageType best = DamageType.Physical;
            float bestValue = -1f;
            for (int i = 0; i < hit.ComponentCount; i++)
            {
                DamageComponent c = hit.GetComponent(i);
                if (c.Value > bestValue) { bestValue = c.Value; best = c.Type; }
            }
            return best;
        }
    }
}
