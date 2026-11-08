using System;
using Eclipse.Core.CrowdControl;
using Eclipse.Gameplay.Combat;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>Generic combat states a skill can grant or impose (06 3.8.3).</summary>
    public enum SkillStateKind : byte { Root = 0, Unstoppable = 1, ParryWindow = 2, Taunt = 3 }

    /// <summary>
    /// Applies a M05 <see cref="StateComponent"/> state: Root and Taunt go through the CC rules (DR, caps, Unstoppable and role immunity),
    /// Unstoppable and the Parry window are self buffs. Duration is a literal or a skill param in seconds. The Parry window is the
    /// state's own base length plus an optional bonus (capped by the state rules, R20) and is mirrored into the entity.
    /// </summary>
    [Serializable]
    public sealed class ApplyStateEffect : SkillEffect
    {
        [SerializeField] private SkillStateKind state;
        [SerializeField] private string durationParam;
        [SerializeField] private float durationSeconds;
        /// <summary>Parry only: skill param (seconds) with the extra window beyond the base frames; empty = none.</summary>
        [SerializeField] private string parryBonusParam;

        public ApplyStateEffect() { }

        public ApplyStateEffect(SkillStateKind state, string durationParam = null, float durationSeconds = 0f, string parryBonusParam = null)
        {
            this.state = state;
            this.durationParam = durationParam;
            this.durationSeconds = durationSeconds;
            this.parryBonusParam = parryBonusParam;
            At(EffectTiming.Impact, state == SkillStateKind.Root || state == SkillStateKind.Taunt ? EffectTargeting.HitTargets : EffectTargeting.Caster);
        }

        public SkillStateKind State => state;

        public override void Apply(SkillExecContext ctx, SkillTarget target)
        {
            if (target == null || target.State == null) return;
            SkillSpec spec = ctx.Cast.Spec;
            float seconds = spec != null ? SkillParams.Resolve(spec, durationParam, durationSeconds) : durationSeconds;
            StateComponent st = target.State;
            int frame = ctx.Frame;

            switch (state)
            {
                case SkillStateKind.Root:
                    st.ApplyCc(CcKind.Root, seconds, ctx.Caster.Id, frame, ctx.Generation);
                    break;
                case SkillStateKind.Taunt:
                    st.ApplyCc(CcKind.Taunt, seconds, ctx.Caster.Id, frame, ctx.Generation);
                    break;
                case SkillStateKind.Unstoppable:
                    st.GrantUnstoppable(frame + CombatStateRules.Frames(seconds));
                    break;
                case SkillStateKind.ParryWindow:
                    float bonus = spec != null && !string.IsNullOrEmpty(parryBonusParam) ? SkillParams.Get(spec, parryBonusParam) : 0f;
                    int end = st.OpenParry(frame, bonus);
                    if (target.Entity is CombatEntity entity) entity.ParryActiveUntilFrame = end;
                    break;
            }
        }
    }
}
