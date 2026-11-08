using System;
using Eclipse.Gameplay.Combat;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>Pure math of an aimed jump: landing point and the pose along the path (horizontal progress and height from curves).</summary>
    public static class LeapTrajectory
    {
        /// <summary>Aim point pulled back to <paramref name="range"/> metres from the caster (planar); Y stays at the caster height.</summary>
        public static Vector3 Landing(Vector3 from, Vector3 aim, Vector3 fallbackForward, float range)
        {
            Vector3 d = aim - from;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-8f)
            {
                fallbackForward.y = 0f;
                d = fallbackForward.sqrMagnitude < 1e-8f ? Vector3.forward : fallbackForward.normalized;
                return from + d * range;
            }
            float len = d.magnitude;
            return len > range ? from + d / len * range : new Vector3(aim.x, from.y, aim.z);
        }

        /// <summary>
        /// Pose at normalised time <paramref name="t"/> (0..1). <paramref name="progress"/> maps time to horizontal progress (null = linear);
        /// <paramref name="height"/> is the vertical offset curve scaled by <paramref name="heightMeters"/> (null = flat).
        /// </summary>
        public static Vector3 Evaluate(Vector3 start, Vector3 end, float t, AnimationCurve progress, AnimationCurve height, float heightMeters)
        {
            t = Mathf.Clamp01(t);
            float p = progress != null && progress.length > 0 ? Mathf.Clamp01(progress.Evaluate(t)) : t;
            Vector3 pos = Vector3.Lerp(start, end, p);
            if (height != null && height.length > 0) pos.y += height.Evaluate(t) * heightMeters;
            return pos;
        }
    }

    /// <summary>
    /// Aimed jump (11 3.6.1 Leap): the caster travels to the aimed point (up to <c>range_m</c>) over the Active phase following the
    /// authored curves, and the area connects on landing (first frame of Recovery). Movement goes through <see cref="ISkillCasterMotion"/>;
    /// airborne protection (Unstoppable) is a separate <see cref="ApplyStateEffect"/> at ActiveStart.
    /// </summary>
    [Serializable]
    public sealed class LeapDelivery : SkillDelivery
    {
        [SerializeField] private string rangeParam = "range_m";
        [SerializeField] private string radiusParam = "radius_m";
        [SerializeField] private AnimationCurve progress = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve height = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
        /// <summary>Peak of the arc in metres; purely visual (0 = no arc), not a balance value.</summary>
        [SerializeField] private float heightMeters;

        public LeapDelivery() { }

        public LeapDelivery(string rangeParam, string radiusParam, AnimationCurve progress = null, AnimationCurve height = null,
            float heightMeters = 0f)
        {
            this.rangeParam = rangeParam;
            this.radiusParam = radiusParam;
            if (progress != null) this.progress = progress;
            if (height != null) this.height = height;
            this.heightMeters = heightMeters;
        }

        public override void OnPhaseEnter(SkillExecContext ctx, SkillPhase phase)
        {
            SkillSpec spec = ctx.Cast.Spec;
            if (phase == SkillPhase.Active)
            {
                Vector3 from = ctx.CasterPosition;
                ctx.PathStart = from;
                ctx.PathEnd = LeapTrajectory.Landing(from, ctx.AimPoint, ctx.Forward, SkillParams.Get(spec, rangeParam));
                ctx.PathStartFrame = ctx.Frame;
                ctx.PathFrames = Math.Max(1, spec.Frames.Active);
                ctx.InFlight = true;
            }
            else if (phase == SkillPhase.Recovery)
            {
                Vector3 landing = ctx.InFlight ? ctx.PathEnd : ctx.CasterPosition;
                if (ctx.InFlight)
                {
                    ctx.InFlight = false;
                    ctx.Motion?.SetPosition(landing);
                }
                HitboxShape shape = HitboxShape.Sector(SkillParams.Get(spec, radiusParam), 360f);
                ctx.Strike(in shape, landing, ctx.Forward);
            }
        }

        public override void Tick(SkillExecContext ctx)
        {
            if (!ctx.InFlight || ctx.Motion == null) return;
            float t = (ctx.Frame - ctx.PathStartFrame) / (float)ctx.PathFrames;
            ctx.Motion.SetPosition(LeapTrajectory.Evaluate(ctx.PathStart, ctx.PathEnd, t, progress, height, heightMeters));
        }

        public override void OnCastEnd(SkillExecContext ctx, bool cancelled)
        {
            ctx.InFlight = false;
        }
    }
}
