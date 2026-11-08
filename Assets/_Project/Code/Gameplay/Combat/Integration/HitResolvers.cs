using Eclipse.Core.Damage;
using Eclipse.Core.Stats;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Feedback;

namespace Eclipse.Gameplay.Combat.Integration
{
    /// <summary>
    /// Shared part of the attacker-side <see cref="IHitDamageResolver"/>s: owns the Core
    /// <see cref="OffensiveDamageResolver"/> and its scratch request; fills the <see cref="TargetDefense"/> of the victim
    /// (Armor% of non-player targets, Vulnerability from the Stagger state: +20% Staggered, +30% Broken - 06 3.8.2).
    /// The player's own defenses are applied later by the incoming pipeline, so a player target has none here.
    /// </summary>
    public abstract class HitResolverBase : IHitDamageResolver
    {
        protected readonly CombatWorld World;
        private readonly OffensiveDamageResolver _resolver;
        private readonly OffensiveRequest _request = new OffensiveRequest();
        private readonly TargetDefense _defense = new TargetDefense();

        protected HitResolverBase(CombatWorld world)
        {
            World = world;
            _resolver = new OffensiveDamageResolver(world.Config.BuildOffensive());
        }

        /// <summary>Base damage of the hit before Increased/More (Physical).</summary>
        protected abstract float BaseDamage(ICombatEntity attacker, Hurtbox target, float damageMultiplier);

        /// <summary>Called after the damage is resolved (feedback staging).</summary>
        protected virtual void OnResolved(DamageInstance hit, ICombatEntity attacker, Hurtbox target) { }

        public void Resolve(DamageInstance hit, ICombatEntity attacker, Hurtbox target, float damageMultiplier)
        {
            _defense.ArmorPercent = 0f;
            _defense.Vulnerability = 0f;
            if (target.Entity != null && World.TryGetActor(target.Entity.Id, out CombatActor victim) && !victim.IsPlayer)
            {
                _defense.ArmorPercent = victim.ArmorPercent;
                if (victim.Stagger != null) _defense.Vulnerability = victim.Stagger.VulnerabilityBonus;
            }

            StatBlock stats = attacker.Stats;
            _request.Clear();
            _request.AddBase(DamageType.Physical, BaseDamage(attacker, target, damageMultiplier));
            _resolver.Resolve(_request, hit, stats, Source(stats), _defense, World.Rng);
            hit.StaggerValue *= 1f + stats.Evaluate(StatId.StaggerDamage); // Stagger Damage% (01), applied before AddStagger
            OnResolved(hit, attacker, target);
        }

        private StatBlockDamageSource _source;
        private StatBlock _sourceStats;

        private StatBlockDamageSource Source(StatBlock stats)
        {
            if (_source == null || !ReferenceEquals(_sourceStats, stats))
            {
                _sourceStats = stats;
                _source = new StatBlockDamageSource(stats);
            }
            return _source;
        }
    }

    /// <summary>
    /// Player weapon strikes: Base = Weapon damage per hit at iLvl 1 (DPS_ref / APS, 02 3.4: Weapon Power 9 / 0.9 APS = 10)
    /// times the step multiplier (08 4.3). Stages the step's hit-stop and the weapon's feedback profile for the
    /// feedback layer.
    /// </summary>
    public sealed class WeaponHitResolver : HitResolverBase
    {
        private readonly WeaponDefinition _weapon;
        private readonly AttackController _attack;
        private readonly HitFeedbackValues _values;

        public WeaponHitResolver(CombatWorld world, WeaponDefinition weapon, AttackController attack) : base(world)
        {
            _weapon = weapon;
            _attack = attack;
            _values = weapon.Feedback != null ? HitFeedbackValues.From(weapon.Feedback) : default;
        }

        protected override float BaseDamage(ICombatEntity attacker, Hurtbox target, float damageMultiplier) =>
            _weapon.DamagePerHitILvl1 * damageMultiplier;

        protected override void OnResolved(DamageInstance hit, ICombatEntity attacker, Hurtbox target)
        {
            var info = new HitFeedbackInfo(_attack.Sequencer.State.Step.HitStopFrames, _values, false, false);
            World.StageHitInfo(in info);
        }
    }

    /// <summary>Test-rig attacker (sandbox): damage is a fraction of the target's max health; no feedback profile.</summary>
    public sealed class FractionHitResolver : HitResolverBase
    {
        private readonly float _fraction;

        public FractionHitResolver(CombatWorld world, float healthFraction) : base(world)
        {
            _fraction = healthFraction;
        }

        protected override float BaseDamage(ICombatEntity attacker, Hurtbox target, float damageMultiplier) =>
            _fraction * damageMultiplier * (target.Entity is CombatEntity e ? e.Health.Max : 1f);
    }
}
