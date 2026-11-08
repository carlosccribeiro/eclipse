using System;
using Eclipse.Core.Stats;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Grants a Shield (06 3.3 step 6) to the target: flat points plus a fraction of the target's maximum health, both literals or
    /// skill params ("_pct" arrives as a fraction). Amplified by the caster's Shield Strength.
    /// </summary>
    [Serializable]
    public sealed class ShieldEffect : SkillEffect
    {
        [SerializeField] private string flatParam;
        [SerializeField] private float flat;
        [SerializeField] private string maxHealthFractionParam;
        [SerializeField] private float maxHealthFraction;

        public ShieldEffect()
        {
            At(EffectTiming.Impact, EffectTargeting.Caster);
        }

        public ShieldEffect(string flatParam, float flat, string maxHealthFractionParam, float maxHealthFraction,
            EffectTargeting who = EffectTargeting.Caster)
        {
            this.flatParam = flatParam;
            this.flat = flat;
            this.maxHealthFractionParam = maxHealthFractionParam;
            this.maxHealthFraction = maxHealthFraction;
            At(EffectTiming.Impact, who);
        }

        public override void Apply(SkillExecContext ctx, SkillTarget target)
        {
            if (target == null || target.Shield == null || ctx.Cast.Spec == null) return;
            SkillSpec spec = ctx.Cast.Spec;
            float points = SkillParams.Resolve(spec, flatParam, flat);
            float fraction = SkillParams.Resolve(spec, maxHealthFractionParam, maxHealthFraction);
            if (fraction > 0f && target.Entity != null) points += fraction * target.Entity.Stats.Evaluate(StatId.MaximumHealth);
            float strength = ctx.Caster.Entity != null ? ctx.Caster.Entity.Stats.Evaluate(StatId.ShieldStrength) : 0f;
            target.Shield.Grant(points, strength);
        }
    }
}
