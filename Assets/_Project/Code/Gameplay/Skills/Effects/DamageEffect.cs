using System;
using Eclipse.Core.Damage;
using Eclipse.Core.Stats;
using Eclipse.Gameplay.Combat;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Direct skill damage (06 3.2): Base = Weapon Power x coefficient, then conversion / extra, Increased, More, crit, mitigation and
    /// Vulnerability through the Core resolver. Generation, tags and source come from the running cast, so a Trigger-run copy is one
    /// Generation deeper. The coefficient is the cast coefficient (CSV "damage_coefficient", plus the per-resource part) unless
    /// <c>coefficientParam</c> names another param of the skill (counter-attack 250%, break damage 60%).
    /// </summary>
    [Serializable]
    public sealed class DamageEffect : SkillEffect
    {
        [SerializeField] private DamageType damageType = DamageType.Physical;
        [SerializeField] private string coefficientParam;
        [SerializeField] private float coefficientScale = 1f;
        [SerializeField] private float procCoefficient = 1f;
        [SerializeField] private float staggerValue;
        [SerializeField] private DamageTransfer[] gainAsExtra;
        [SerializeField] private DamageTransfer[] conversions;

        public DamageEffect() { }

        public DamageEffect(DamageType type, string coefficientParam = null, float coefficientScale = 1f, float staggerValue = 0f,
            float procCoefficient = 1f, DamageTransfer[] gainAsExtra = null, DamageTransfer[] conversions = null)
        {
            damageType = type;
            this.coefficientParam = coefficientParam;
            this.coefficientScale = coefficientScale;
            this.staggerValue = staggerValue;
            this.procCoefficient = procCoefficient;
            this.gainAsExtra = gainAsExtra;
            this.conversions = conversions;
        }

        public DamageType PrimaryType => damageType;

        /// <summary>Coefficient of the running cast for this effect, before the context multiplier.</summary>
        public float CoefficientFor(SkillExecContext ctx)
        {
            float c = string.IsNullOrEmpty(coefficientParam) ? ctx.Cast.DamageCoefficient : SkillParams.Get(ctx.Cast.Spec, coefficientParam);
            return c * coefficientScale;
        }

        public override void Apply(SkillExecContext ctx, SkillTarget target)
        {
            if (target == null || target.Receiver == null || !target.IsAlive) return;
            if (ctx.Cast.Spec == null || ctx.Caster.Entity == null) return;

            StatBlock stats = ctx.Caster.Entity.Stats;
            DamageInstance hit = ctx.Hit;
            hit.Reset();
            hit.SourceId = ctx.Caster.Id.Value;
            hit.TargetId = target.Id.Value;
            hit.Kind = HitKind.Hit;
            hit.Tags = ctx.Cast.Spec.Tags;
            hit.Flags = ctx.ExtraFlags;
            hit.Generation = ctx.Generation; // 11 3.6.5: direct cast 0, every proc one deeper
            hit.SourceChain = ctx.Chain;
            hit.ProcCoefficient = procCoefficient;
            hit.HitGroupId = ctx.HitGroupId;
            hit.StaggerValue = staggerValue * (1f + stats.Evaluate(StatId.StaggerDamage)); // Stagger Damage% (01), as in the weapon resolver

            float vulnerability = target.ExternalVulnerability + (target.Vulnerability != null ? target.Vulnerability.Total(ctx.Frame) : 0f);
            float baseDamage = ctx.WeaponPower * CoefficientFor(ctx) * ctx.DamageMultiplier;
            ctx.Damage.Resolve(hit, stats, damageType, baseDamage, ctx.FlatDamage, gainAsExtra, conversions,
                target.ArmorPercent, vulnerability, ctx.Rng);

            Vector3 dir = target.Position - ctx.CasterPosition;
            dir.y = 0f;
            dir = dir.sqrMagnitude < 1e-8f ? ctx.Forward : dir.normalized;

            DamageOutcome o = target.Receiver.ReceiveHit(hit, dir);
            if (o.Result != HitResult.Hit) return;

            ctx.RecordHit(o.FinalDamage);
            ctx.Controller?.ReportHit(target.Id, o.FinalDamage, ctx.Frame);
            if (ctx.Events != null)
            {
                ctx.Events.Publish(new OnHitDealt(ctx.Frame, ctx.Generation, ctx.Caster.Id, target.Id, o.FinalDamage, damageType,
                    hit.IsCrit, hit.Flags, target.Position));
                if (hit.IsCrit)
                    ctx.Events.Publish(new OnCritDealt(ctx.Frame, ctx.Generation, ctx.Caster.Id, target.Id, o.FinalDamage, damageType));
            }
            if (hit.StaggerValue > 0f && target.Stagger != null)
                target.Stagger.AddStagger(hit.StaggerValue, ctx.Caster.Id, ctx.Generation);
        }
    }
}
