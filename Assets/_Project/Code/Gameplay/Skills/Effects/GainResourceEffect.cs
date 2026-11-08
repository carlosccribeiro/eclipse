using System;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Grants the caster's resource (Fury on a Leap, on a counter-attack). The amount is a literal or a skill param; the
    /// <see cref="ISkillResource"/> applies Resource Generation. With <see cref="EffectTargeting.HitTargets"/> it grants once per target
    /// hit (the target is only the trigger). With <c>scaleByProcCoefficient</c> the amount follows the Proc Coefficient of the cast's
    /// hit source (06 3.2: on-hit gains scale with it).
    /// </summary>
    [Serializable]
    public sealed class GainResourceEffect : SkillEffect
    {
        [SerializeField] private string amountParam;
        [SerializeField] private float amount;
        [SerializeField] private float procCoefficient = 1f;
        [SerializeField] private bool scaleByProcCoefficient;

        public GainResourceEffect()
        {
            At(EffectTiming.Impact, EffectTargeting.Caster);
        }

        public GainResourceEffect(string amountParam, float amount = 0f, bool scaleByProcCoefficient = false, float procCoefficient = 1f)
        {
            this.amountParam = amountParam;
            this.amount = amount;
            this.scaleByProcCoefficient = scaleByProcCoefficient;
            this.procCoefficient = procCoefficient;
            At(EffectTiming.Impact, EffectTargeting.Caster);
        }

        public override void Apply(SkillExecContext ctx, SkillTarget target)
        {
            if (ctx.Resource == null || ctx.Cast.Spec == null) return;
            float a = SkillParams.Resolve(ctx.Cast.Spec, amountParam, amount);
            if (scaleByProcCoefficient) a *= procCoefficient;
            if (a > 0f) ctx.Resource.Gain(a);
        }
    }
}
