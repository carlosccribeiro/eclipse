using System;
using Eclipse.Core.Damage;
using Eclipse.Core.Procs;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Spawns a persistent zone at <see cref="SkillExecContext.ImpactPoint"/> in the M05 <see cref="ZoneRunner"/> (ticks every 0.5 s,
    /// no crit, no on-hit; 06 3.2). The zone is a generated entity, so its hits are Generation + 1 (11 3.6.5) and carry Proc Coefficient
    /// 0.25 (11 3.6.5 "tick de área persistente"). Tick damage is resolved once at spawn (Weapon Power x coefficient through the Core
    /// resolver, Tick kind) without target defenses, because <c>AreaSpec.Damage</c> is a single number (M05 limitation, logged).
    /// </summary>
    [Serializable]
    public sealed class GroundZoneEffect : SkillEffect
    {
        [SerializeField] private DamageType damageType = DamageType.Ember;
        [SerializeField] private string radiusParam = "radius_m";
        [SerializeField] private float radius;
        [SerializeField] private string durationParam;
        [SerializeField] private float durationSeconds;
        [SerializeField] private string coefficientParam;
        [SerializeField] private float coefficientScale = 1f;
        /// <summary>30 frames = 0.5 s, the tick of persistent areas (06 3.2, R19).</summary>
        [SerializeField] private int tickIntervalFrames = 30;
        /// <summary>Proc Coefficient of a persistent-area tick (11 3.6.5).</summary>
        [SerializeField] private float procCoefficient = 0.25f;

        public GroundZoneEffect()
        {
            At(EffectTiming.Impact, EffectTargeting.ImpactPoint);
        }

        public GroundZoneEffect(DamageType type, string radiusParam, float radius, string durationParam, float durationSeconds,
            string coefficientParam, float coefficientScale = 1f)
        {
            damageType = type;
            this.radiusParam = radiusParam;
            this.radius = radius;
            this.durationParam = durationParam;
            this.durationSeconds = durationSeconds;
            this.coefficientParam = coefficientParam;
            this.coefficientScale = coefficientScale;
            At(EffectTiming.Impact, EffectTargeting.ImpactPoint);
        }

        public override void Apply(SkillExecContext ctx, SkillTarget target)
        {
            SkillSpec spec = ctx.Cast.Spec;
            if (ctx.Zones == null || spec == null || ctx.Caster.Entity == null) return;

            float coefficient = string.IsNullOrEmpty(coefficientParam) ? ctx.Cast.DamageCoefficient : SkillParams.Get(spec, coefficientParam);
            float baseDamage = ctx.WeaponPower * coefficient * coefficientScale * ctx.DamageMultiplier;
            int frames = CombatStateRules.Frames(SkillParams.Resolve(spec, durationParam, durationSeconds));
            if (baseDamage <= 0f || frames <= 0) return;

            int generation = Math.Min(ctx.Generation + 1, ProcContext.MaxGeneration);
            HitFlags flags = ctx.ExtraFlags | HitFlags.IsTriggered;

            DamageInstance tick = ctx.Hit;
            tick.Reset();
            tick.SourceId = ctx.Caster.Id.Value;
            tick.Kind = HitKind.Tick;
            tick.Tags = spec.Tags;
            tick.Flags = flags;
            tick.Generation = generation;
            float perTick = ctx.Damage.Resolve(tick, ctx.Caster.Entity.Stats, damageType, baseDamage, 0f, null, null, 0f, 0f, ctx.Rng);

            HitboxShape shape = HitboxShape.Sector(SkillParams.Resolve(spec, radiusParam, radius), 360f);
            var area = new AreaSpec(ctx.Caster.Id, ctx.Caster.Entity.Team, shape, ctx.ImpactPoint, ctx.Forward, frames, tickIntervalFrames,
                damageType, perTick, 0f, procCoefficient, generation, ctx.HitGroupId, spec.Tags, flags);
            ctx.Zones.Spawn(in area);
        }
    }
}
