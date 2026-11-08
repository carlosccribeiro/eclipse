using System;
using Eclipse.Gameplay.Combat;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Melee swing (11 3.6.1 MeleeArc): a sector in front of the caster, connecting once when <c>impactPhase</c> starts. Target search,
    /// line of sight and one-hit-per-target come from the M05 area query. Radius and arc are skill params (Cleaving Strike: radius_m, arc_deg).
    /// </summary>
    [Serializable]
    public sealed class MeleeArcDelivery : SkillDelivery
    {
        [SerializeField] private string radiusParam = "radius_m";
        [SerializeField] private string arcParam = "arc_deg";

        public MeleeArcDelivery() { }

        public MeleeArcDelivery(string radiusParam, string arcParam)
        {
            this.radiusParam = radiusParam;
            this.arcParam = arcParam;
        }

        public override void OnPhaseEnter(SkillExecContext ctx, SkillPhase phase)
        {
            if (phase != impactPhase) return;
            SkillSpec spec = ctx.Cast.Spec;
            HitboxShape shape = HitboxShape.Sector(SkillParams.Get(spec, radiusParam), SkillParams.Get(spec, arcParam));
            ctx.Strike(in shape, ctx.CasterPosition, ctx.Forward);
        }
    }

    /// <summary>Circular area centred on the caster (11 3.6.1 AreaAtSelf), connecting when <c>impactPhase</c> starts.</summary>
    [Serializable]
    public sealed class AreaAtSelfDelivery : SkillDelivery
    {
        [SerializeField] private string radiusParam = "radius_m";

        public AreaAtSelfDelivery() { }

        public AreaAtSelfDelivery(string radiusParam)
        {
            this.radiusParam = radiusParam;
        }

        public override void OnPhaseEnter(SkillExecContext ctx, SkillPhase phase)
        {
            if (phase != impactPhase) return;
            HitboxShape shape = HitboxShape.Sector(SkillParams.Get(ctx.Cast.Spec, radiusParam), 360f);
            ctx.Strike(in shape, ctx.CasterPosition, ctx.Forward);
        }
    }

    /// <summary>
    /// Circular area at the aimed ground point (11 3.6.1 AreaAtPoint). When <c>rangeParam</c> is set the point is pulled back to that
    /// distance from the caster.
    /// </summary>
    [Serializable]
    public sealed class AreaAtPointDelivery : SkillDelivery
    {
        [SerializeField] private string radiusParam = "radius_m";
        [SerializeField] private string rangeParam;

        public AreaAtPointDelivery() { }

        public AreaAtPointDelivery(string radiusParam, string rangeParam = null)
        {
            this.radiusParam = radiusParam;
            this.rangeParam = rangeParam;
        }

        public override void OnPhaseEnter(SkillExecContext ctx, SkillPhase phase)
        {
            if (phase != impactPhase) return;
            SkillSpec spec = ctx.Cast.Spec;
            Vector3 from = ctx.CasterPosition;
            Vector3 point = ctx.AimPoint;
            if (!string.IsNullOrEmpty(rangeParam))
            {
                float range = SkillParams.Get(spec, rangeParam);
                Vector3 d = point - from;
                d.y = 0f;
                if (d.magnitude > range) point = from + d.normalized * range;
            }
            HitboxShape shape = HitboxShape.Sector(SkillParams.Get(spec, radiusParam), 360f);
            ctx.Strike(in shape, point, ctx.Forward);
        }
    }

    /// <summary>
    /// Cone that reaches at most N targets, the closest first (Burning Chains: range_m, arc_deg, max_targets). The cut is made after
    /// the line-of-sight filter, so a hidden target never uses up a slot.
    /// </summary>
    [Serializable]
    public sealed class ConeMultiTargetDelivery : SkillDelivery
    {
        [SerializeField] private string rangeParam = "range_m";
        [SerializeField] private string arcParam = "arc_deg";
        [SerializeField] private string maxTargetsParam = "max_targets";

        public ConeMultiTargetDelivery() { }

        public ConeMultiTargetDelivery(string rangeParam, string arcParam, string maxTargetsParam)
        {
            this.rangeParam = rangeParam;
            this.arcParam = arcParam;
            this.maxTargetsParam = maxTargetsParam;
        }

        public override void OnPhaseEnter(SkillExecContext ctx, SkillPhase phase)
        {
            if (phase != impactPhase) return;
            SkillSpec spec = ctx.Cast.Spec;
            HitboxShape shape = HitboxShape.Sector(SkillParams.Get(spec, rangeParam), SkillParams.Get(spec, arcParam));
            int max = (int)SkillParams.Get(spec, maxTargetsParam);
            ctx.Strike(in shape, ctx.CasterPosition, ctx.Forward, max);
        }
    }
}
