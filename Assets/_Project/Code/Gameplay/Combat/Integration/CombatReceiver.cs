using Eclipse.Core.Damage;
using UnityEngine;

namespace Eclipse.Gameplay.Combat.Integration
{
    /// <summary>
    /// The <see cref="IDamageReceiver"/> bound to a <see cref="Hurtbox"/>: runs the entity's incoming pipeline, then the
    /// bookkeeping every landed hit needs and no single component owns (M05 T10): <c>OnHitDealt</c>/<c>OnCritDealt</c>,
    /// the feedback info of the attack, Stagger accumulation, Stealth break and the sandbox infinite-health refill.
    /// </summary>
    public sealed class CombatReceiver : IDamageReceiver
    {
        private readonly CombatActor _actor;
        private readonly CombatWorld _world;

        public CombatReceiver(CombatActor actor, CombatWorld world)
        {
            _actor = actor;
            _world = world;
        }

        public EntityId Id => _actor.Entity.Id;

        public DamageOutcome ReceiveHit(DamageInstance hit, Vector3 hitDirection)
        {
            CombatEntity target = _actor.Entity;
            int frame = target.CurrentFrame;
            var source = new EntityId(hit.SourceId);
            DamageOutcome outcome = target.ReceiveHit(hit, hitDirection);

            bool landed = outcome.Result == HitResult.Hit || outcome.Result == HitResult.Blocked;
            if (!landed)
            {
                _world.DiscardStagedHit();
                return outcome;
            }

            _actor.TotalDamageTaken += outcome.FinalDamage;
            Vector3 position = _actor.Hurtbox != null ? _actor.Hurtbox.Center : target.Position;
            DamageType type = DamageApplier.DominantType(hit);
            if (_world.TryGetActor(source, out CombatActor attacker)) attacker.States.NotifyDamageDealt(frame);
            _world.Events.Publish(new OnHitDealt(frame, hit.Generation, source, target.Id, outcome.FinalDamage, type, hit.IsCrit, hit.Flags, position));
            if (hit.IsCrit) _world.Events.Publish(new OnCritDealt(frame, hit.Generation, source, target.Id, outcome.FinalDamage, type));
            _world.CommitStagedHit(frame, source, target.Id);

            if (!outcome.Killed)
            {
                _actor.States.NotifyDamageTaken(frame);
                if (_actor.Stagger != null && hit.StaggerValue > 0f)
                    _actor.Stagger.AddStagger(hit.StaggerValue, source, hit.Generation);
                if (_actor.InfiniteHealth) target.Health.Restore(1f);
            }
            return outcome;
        }
    }
}
