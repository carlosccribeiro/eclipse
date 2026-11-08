using Eclipse.Data.Skills;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>When inside the cast an Effect runs (11 3.6.3: windup, active, hit, end).</summary>
    public enum EffectTiming : byte
    {
        /// <summary>First announced phase of the cast.</summary>
        CastStart = 0,
        /// <summary>Entering the Active phase (Delivery begins: Unstoppable in the air, stance modifiers).</summary>
        ActiveStart = 1,
        /// <summary>When the Delivery connects with its targets (and once when it lands, even with no target).</summary>
        Impact = 2,
        /// <summary>The cast ended without being cancelled.</summary>
        CastEnd = 3,
    }

    /// <summary>Who receives an Effect.</summary>
    public enum EffectTargeting : byte
    {
        /// <summary>Once per target found by the Delivery.</summary>
        HitTargets = 0,
        /// <summary>The caster, once.</summary>
        Caster = 1,
        /// <summary>Nobody in particular: runs once and reads <see cref="SkillExecContext.ImpactPoint"/>.</summary>
        ImpactPoint = 2,
    }

    /// <summary>
    /// Runtime Effect (11 3.6.1). Authored data of the subclass plus the rule that applies it; the numbers come from the skill's
    /// params (CSV) through <see cref="SkillParams"/>, never from code. Subclasses are [Serializable] and stored in the
    /// SkillDefinition via [SerializeReference].
    /// </summary>
    [System.Serializable]
    public abstract class SkillEffect : Effect
    {
        [SerializeField] private EffectTiming timing = EffectTiming.Impact;
        [SerializeField] private EffectTargeting targeting = EffectTargeting.HitTargets;

        public EffectTiming Timing => timing;
        public EffectTargeting Targeting => targeting;

        /// <summary>Builder-style setters for code-assembled skills (tests, code-only Kael parts).</summary>
        public SkillEffect At(EffectTiming when, EffectTargeting who)
        {
            timing = when;
            targeting = who;
            return this;
        }

        /// <summary>
        /// Applies the Effect. <paramref name="target"/> is the victim for <see cref="EffectTargeting.HitTargets"/>, the caster for
        /// <see cref="EffectTargeting.Caster"/> and null for <see cref="EffectTargeting.ImpactPoint"/>.
        /// </summary>
        public abstract void Apply(SkillExecContext ctx, SkillTarget target);
    }
}
