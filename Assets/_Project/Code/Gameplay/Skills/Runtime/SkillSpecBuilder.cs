using System;
using System.Collections.Generic;
using Eclipse.Core.Ids;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>The 6 modifier phases of 11 3.6.2 / 03 3.4 (fixed order; M10 fills them).</summary>
    public enum SkillBuildPhase : byte
    {
        Conversion = 1,
        Form = 2,
        Parameters = 3,
        Replication = 4,
        Triggers = 5,
        Resource = 6,
    }

    /// <summary>Channel order inside a phase (11 3.6.2: Item, Augment, Set, Passive, Run).</summary>
    public enum SkillModifierChannel : byte { Item = 0, Augment = 1, Set = 2, Passive = 3, Run = 4 }

    /// <summary>
    /// Extension point for M10: a ModifierOp applied on the builder before the snapshot is taken. Adding implementations
    /// never changes the public signature of <see cref="SkillSpecBuilder"/>.
    /// </summary>
    public interface ISkillSpecModifier
    {
        SkillBuildPhase Phase { get; }
        SkillModifierChannel Channel { get; }
        /// <summary>Tie-break inside phase and channel (StableId of the modifier, compared ordinally).</summary>
        string OrderKey { get; }
        void Apply(SkillSpecBuilder builder);
    }

    /// <summary>Structural constants of skill timing (not balance data).</summary>
    public static class SkillBalance
    {
        /// <summary>01 3.6: Ultimates receive 50% of the final CDR.</summary>
        public const float UltimateCdrShare = 0.5f;

        /// <summary>Dodge charge recharge is not affected by CDR (00 R06 / 01 3.6): returns the base value by design.</summary>
        public static float DodgeRechargeSeconds(float baseSeconds, float cooldownReduction) => baseSeconds;
    }

    /// <summary>
    /// Mutable builder of <see cref="SkillSpec"/>. <see cref="Build"/> applies the registered modifiers sorted by
    /// (Phase, Channel, OrderKey), then resolves stat-dependent numbers (CDR, cost reduction, unconditional Skill Damage).
    /// </summary>
    public sealed class SkillSpecBuilder
    {
        private readonly List<ISkillSpecModifier> _modifiers = new List<ISkillSpecModifier>();
        private readonly List<string> _paramKeys = new List<string>();
        private readonly List<float> _paramValues = new List<float>();

        public StableId SkillId { get; set; }
        public SkillSlot Slot { get; set; }
        public TagMask Tags { get; set; }
        public StableId CostResource { get; set; }
        public float CostAmount { get; set; }
        public bool CostAllResource { get; set; }
        public float CostMinimum { get; set; }
        public float ResourceGain { get; set; }
        public float CooldownSeconds { get; set; }
        public float DamageCoefficient { get; set; }
        public float CoefficientPerResource { get; set; }
        public int MoveDuringCastPercent { get; set; }
        public bool Commit { get; set; }
        public bool Tracking { get; set; }
        public SkillFrames Frames { get; set; }

        /// <summary>Builder from the authored asset. Phase frames are not part of Skills.csv (gap logged), so the caller passes them.</summary>
        public static SkillSpecBuilder FromDefinition(SkillDefinition d, SkillFrames frames)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            var b = new SkillSpecBuilder
            {
                SkillId = StableId.Parse(d.StableIdString),
                Slot = d.Slot,
                Tags = d.Tags,
                CostResource = string.IsNullOrEmpty(d.CostResourceId) ? default : StableId.Parse(d.CostResourceId),
                CostAmount = d.CostAmount,
                CostAllResource = d.CostAllResource,
                CostMinimum = d.CostMinimum,
                ResourceGain = d.ResourceGain,
                CooldownSeconds = d.CooldownSeconds,
                DamageCoefficient = d.DamageCoefficient,
                CoefficientPerResource = d.DamageCoefficientPerResource,
                MoveDuringCastPercent = d.MoveDuringCastPercent,
                Commit = d.Commit,
                Tracking = d.Tracking,
                Frames = frames,
            };
            for (int i = 0; i < d.Params.Count; i++) b.SetParam(d.Params[i].Key, d.Params[i].Value);
            return b;
        }

        public SkillSpecBuilder SetParam(string key, float value)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Param key is empty", nameof(key));
            int i = _paramKeys.IndexOf(key);
            if (i >= 0) _paramValues[i] = value;
            else { _paramKeys.Add(key); _paramValues.Add(value); }
            return this;
        }

        public bool TryGetParam(string key, out float value)
        {
            int i = _paramKeys.IndexOf(key);
            value = i >= 0 ? _paramValues[i] : 0f;
            return i >= 0;
        }

        /// <summary>Registers a modifier op (M10). Registration order does not matter.</summary>
        public SkillSpecBuilder AddModifier(ISkillSpecModifier modifier)
        {
            _modifiers.Add(modifier ?? throw new ArgumentNullException(nameof(modifier)));
            return this;
        }

        public SkillSpec Build(StatBlock stats)
        {
            // Work on a copy so Build is repeatable (rebuild after StatsChanged) with identical results.
            SkillSpecBuilder work = Clone();
            var ordered = new List<ISkillSpecModifier>(_modifiers);
            ordered.Sort(Compare);
            for (int i = 0; i < ordered.Count; i++) ordered[i].Apply(work);
            return work.Snapshot(stats);
        }

        private static int Compare(ISkillSpecModifier a, ISkillSpecModifier b)
        {
            int c = ((int)a.Phase).CompareTo((int)b.Phase);
            if (c != 0) return c;
            c = ((int)a.Channel).CompareTo((int)b.Channel);
            if (c != 0) return c;
            return string.CompareOrdinal(a.OrderKey, b.OrderKey);
        }

        private SkillSpecBuilder Clone()
        {
            var c = new SkillSpecBuilder
            {
                SkillId = SkillId, Slot = Slot, Tags = Tags, CostResource = CostResource, CostAmount = CostAmount,
                CostAllResource = CostAllResource, CostMinimum = CostMinimum, ResourceGain = ResourceGain,
                CooldownSeconds = CooldownSeconds, DamageCoefficient = DamageCoefficient,
                CoefficientPerResource = CoefficientPerResource, MoveDuringCastPercent = MoveDuringCastPercent,
                Commit = Commit, Tracking = Tracking, Frames = Frames,
            };
            c._paramKeys.AddRange(_paramKeys);
            c._paramValues.AddRange(_paramValues);
            return c;
        }

        private SkillSpec Snapshot(StatBlock stats)
        {
            bool ultimate = Tags.Has(Tag.Ultimate) || Slot == SkillSlot.F;
            float cdr = 0f, costReduction = 0f, inc = 0f, more = 1f;
            if (stats != null)
            {
                cdr = stats.Evaluate(StatId.CooldownReduction); // soft/hard cap come from IStatRules (01 3.6)
                if (ultimate) cdr *= SkillBalance.UltimateCdrShare;
                costReduction = stats.Evaluate(StatId.ResourceCostReduction);
                inc = stats.SumBucket(StatId.SkillDamage, ModBucket.Increased, Tags, null);
                more = stats.ProductMore(StatId.SkillDamage, Tags, null);
            }
            float cost = CostAllResource ? 0f : Math.Max(0f, CostAmount * (1f - costReduction));
            int cdFrames = CooldownSeconds <= 0f ? 0 : CombatStateRules.Frames(CooldownSeconds * (1f - cdr));
            return new SkillSpec(SkillId, Slot, Tags, CostResource, cost, CostAllResource, CostMinimum, ResourceGain,
                cdFrames, DamageCoefficient, CoefficientPerResource, MoveDuringCastPercent, Commit, Tracking, Frames,
                inc, more, _paramKeys.ToArray(), _paramValues.ToArray());
        }
    }
}
