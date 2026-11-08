using System;
using Eclipse.Core.Stats;
using Eclipse.Gameplay.Combat;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    public enum TimedModifierMode : byte
    {
        /// <summary>A <see cref="StatMod"/> on the target's StatBlock through the M05 <c>TimedModifierSet</c> (buffs, damage reduction).</summary>
        Stat = 0,
        /// <summary>Stacking Vulnerability on the target (<see cref="TimedVulnerabilitySet"/>), e.g. Ruin Vulnerability.</summary>
        Vulnerability = 1,
    }

    /// <summary>
    /// Temporary modifier for a duration. <see cref="TimedModifierMode.Stat"/> adds one mod and renews it when applied again with the
    /// same <c>sourceKey</c>. <see cref="TimedModifierMode.Vulnerability"/> adds <c>stacksPerHit</c> stacks for each fissure/line that
    /// reached the target (<see cref="SkillExecContext.CurrentHits"/>), up to <c>maxStacks</c>, each worth <c>value</c>. All numbers can
    /// be literals or skill params (<c>*Param</c> wins when set; "_pct" params arrive as fractions).
    /// </summary>
    [Serializable]
    public sealed class ApplyTimedModifierEffect : SkillEffect
    {
        [SerializeField] private TimedModifierMode mode;
        [SerializeField] private string sourceKey;
        [SerializeField] private string durationParam;
        [SerializeField] private float durationSeconds;
        [SerializeField] private string valueParam;
        [SerializeField] private float value;

        // Stat mode
        [SerializeField] private StatId stat;
        [SerializeField] private ModBucket bucket;

        // Vulnerability mode
        [SerializeField] private string stacksPerHitParam;
        [SerializeField] private int stacksPerHit = 1;
        [SerializeField] private string maxStacksParam;
        [SerializeField] private int maxStacks = 1;

        public ApplyTimedModifierEffect() { }

        public static ApplyTimedModifierEffect ForStat(string sourceKey, StatId stat, ModBucket bucket, string valueParam, float value,
            string durationParam, float durationSeconds)
        {
            return new ApplyTimedModifierEffect
            {
                mode = TimedModifierMode.Stat, sourceKey = sourceKey, stat = stat, bucket = bucket, valueParam = valueParam,
                value = value, durationParam = durationParam, durationSeconds = durationSeconds,
            };
        }

        public static ApplyTimedModifierEffect ForVulnerability(string sourceKey, string valueParam, float valuePerStack,
            string durationParam, float durationSeconds, string stacksPerHitParam, int stacksPerHit, string maxStacksParam, int maxStacks)
        {
            return new ApplyTimedModifierEffect
            {
                mode = TimedModifierMode.Vulnerability, sourceKey = sourceKey, valueParam = valueParam, value = valuePerStack,
                durationParam = durationParam, durationSeconds = durationSeconds, stacksPerHitParam = stacksPerHitParam,
                stacksPerHit = stacksPerHit, maxStacksParam = maxStacksParam, maxStacks = maxStacks,
            };
        }

        public override void Apply(SkillExecContext ctx, SkillTarget target)
        {
            SkillSpec spec = ctx.Cast.Spec;
            if (target == null || spec == null) return;
            float seconds = SkillParams.Resolve(spec, durationParam, durationSeconds);
            int frames = CombatStateRules.Frames(seconds);
            if (frames <= 0) return;
            float v = SkillParams.Resolve(spec, valueParam, value);
            int key = SkillParams.KeyHash(sourceKey);

            if (mode == TimedModifierMode.Stat)
            {
                if (target.Timed == null) return;
                var mod = new StatMod(stat, bucket, v);
                target.Timed.Apply(in mod, frames, key);
                return;
            }

            if (target.Vulnerability == null) return;
            int perHit = (int)SkillParams.Resolve(spec, stacksPerHitParam, stacksPerHit);
            int max = (int)SkillParams.Resolve(spec, maxStacksParam, maxStacks);
            int hits = ctx.CurrentHits < 1 ? 1 : ctx.CurrentHits;
            target.Vulnerability.Apply(key, v, perHit * hits, max, frames, ctx.Frame);
        }
    }
}
