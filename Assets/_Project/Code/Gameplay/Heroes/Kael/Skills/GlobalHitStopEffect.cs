using System;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Skills;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Heroes.Kael
{
    /// <summary>Camera shake requested by a skill (06 3.11: Ultimate shake 0.5). The feedback layer turns it into trauma.</summary>
    public readonly struct OnSkillShake
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source;
        public readonly float Amount;

        public OnSkillShake(int frame, int generation, EntityId source, float amount)
        { Frame = frame; Generation = generation; Source = source; Amount = amount; }
    }

    /// <summary>
    /// Ultimate feedback (06 3.11): publishes a world-wide hit-stop of <c>hitstop_f</c> frames (<see cref="OnGlobalHitStop"/>) and a
    /// shake of <c>shake</c> on the event stream; Gameplay never freezes the world itself. Both numbers come from the skill params.
    /// </summary>
    [Serializable]
    public sealed class GlobalHitStopEffect : SkillEffect
    {
        [SerializeField] private string framesParam = "hitstop_f";
        [SerializeField] private string shakeParam = "shake";

        public GlobalHitStopEffect()
        {
            At(EffectTiming.Impact, EffectTargeting.ImpactPoint);
        }

        public override void Apply(SkillExecContext ctx, SkillTarget target)
        {
            SkillSpec spec = ctx.Cast.Spec;
            if (ctx.Events == null || spec == null) return;
            int frames = (int)SkillParams.Get(spec, framesParam);
            if (frames > 0) ctx.Events.Publish(new OnGlobalHitStop(ctx.Frame, ctx.Generation, ctx.Caster.Id, frames));
            if (!string.IsNullOrEmpty(shakeParam) && spec.TryGetParam(shakeParam, out float shake) && shake > 0f)
                ctx.Events.Publish(new OnSkillShake(ctx.Frame, ctx.Generation, ctx.Caster.Id, shake));
        }
    }
}
