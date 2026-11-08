using System;
using Eclipse.Gameplay.Combat;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Stance (Ashen Guard): a posture that lasts <c>stance_s</c> or until the cast button is released, then connects a counter area
    /// around the caster. Modifiers of the posture (damage reduction, Parry window) are separate Effects at ActiveStart. Releasing
    /// early ends the cast through <see cref="SkillController.Interrupt"/> (the controller has no early-finish), so the remaining Recovery
    /// is skipped and the cooldown stays spent. A cast cancelled by a dodge drops the stance without the counter.
    /// </summary>
    [Serializable]
    public sealed class StanceDelivery : SkillDelivery
    {
        [SerializeField] private string durationParam = "stance_s";
        [SerializeField] private string counterRadiusParam = "counter_radius_m";
        [SerializeField] private string counterArcParam = "counter_arc_deg";

        public StanceDelivery() { }

        public StanceDelivery(string durationParam, string counterRadiusParam, string counterArcParam)
        {
            this.durationParam = durationParam;
            this.counterRadiusParam = counterRadiusParam;
            this.counterArcParam = counterArcParam;
        }

        public override void OnPhaseEnter(SkillExecContext ctx, SkillPhase phase)
        {
            if (phase != SkillPhase.Active) return;
            ctx.StanceActive = true;
            ctx.StanceStartFrame = ctx.Frame;
        }

        public override void Tick(SkillExecContext ctx)
        {
            if (!ctx.StanceActive) return;
            int elapsed = ctx.Frame - ctx.StanceStartFrame;
            bool released = !ctx.ButtonHeld && elapsed > 0;
            bool timeUp = elapsed >= CombatStateRules.Frames(SkillParams.Get(ctx.Cast.Spec, durationParam));
            if (!released && !timeUp) return;

            ctx.StanceActive = false;
            SkillSpec spec = ctx.Cast.Spec;
            HitboxShape shape = HitboxShape.Sector(SkillParams.Get(spec, counterRadiusParam), SkillParams.Get(spec, counterArcParam));
            ctx.Strike(in shape, ctx.CasterPosition, ctx.Forward);
            if (released) ctx.Controller?.Interrupt(ctx.Frame);
        }

        public override void OnCastEnd(SkillExecContext ctx, bool cancelled)
        {
            ctx.StanceActive = false;
        }
    }

    /// <summary>
    /// Straight lines radiating from the caster (Worldbreaker fissures): <c>fissure_count</c> boxes of <c>radius_m</c> length spread evenly
    /// over 360 degrees. A target is hit once per cast, but <see cref="SkillExecContext.CurrentHits"/> tells how many lines crossed it
    /// (one Ruin stack per fissure). Line width is a skill param.
    /// </summary>
    [Serializable]
    public sealed class RadialLinesDelivery : SkillDelivery
    {
        [SerializeField] private string countParam = "fissure_count";
        [SerializeField] private string lengthParam = "radius_m";
        [SerializeField] private string widthParam = "fissure_width_m";
        [SerializeField] private float startAngleDegrees;

        public RadialLinesDelivery() { }

        public RadialLinesDelivery(string countParam, string lengthParam, string widthParam, float startAngleDegrees = 0f)
        {
            this.countParam = countParam;
            this.lengthParam = lengthParam;
            this.widthParam = widthParam;
            this.startAngleDegrees = startAngleDegrees;
        }

        public override void OnPhaseEnter(SkillExecContext ctx, SkillPhase phase)
        {
            if (phase != impactPhase) return;
            SkillSpec spec = ctx.Cast.Spec;
            int count = (int)SkillParams.Get(spec, countParam);
            if (count <= 0) return;
            float length = SkillParams.Get(spec, lengthParam);
            float width = SkillParams.Get(spec, widthParam);
            HitboxShape line = HitboxShape.Box(length, width);
            Vector3 origin = ctx.CasterPosition;

            ctx.BeginGather();
            for (int i = 0; i < count; i++)
                ctx.Gather(in line, origin, Rotate(ctx.Forward, startAngleDegrees + 360f / count * i));
            ctx.Complete(origin);
        }
    }
}
