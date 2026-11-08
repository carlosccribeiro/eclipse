using System;
using Eclipse.Core.Ids;
using Eclipse.Core.Tags;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat.Attacks;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>Phase of a skill cast (11 3.6.3: windup / active / recovery).</summary>
    public enum SkillPhase : byte { None = 0, Windup = 1, Active = 2, Recovery = 3 }

    /// <summary>Frame data of a cast, in 60 Hz simulation frames at Attack/Cast Speed 1.0 (06 3.5).</summary>
    public readonly struct SkillFrames
    {
        public readonly int Windup, Active, Recovery;

        public SkillFrames(int windup, int active, int recovery)
        {
            if (windup < 0 || active < 0 || recovery < 0) throw new ArgumentOutOfRangeException(nameof(windup), "Frame counts must be >= 0");
            Windup = windup;
            Active = active;
            Recovery = recovery;
        }

        public int Total => Windup + Active + Recovery;
    }

    /// <summary>
    /// Immutable skill snapshot v1 (11 3.5.3 / 3.6.2): data numbers plus unconditional stats already filtered by the skill tags.
    /// Built only by <see cref="SkillSpecBuilder"/>; recompiled when the stats it depends on change.
    /// </summary>
    public sealed class SkillSpec
    {
        /// <summary>Schema version of the snapshot; M10 extends the builder, not this shape.</summary>
        public const int SchemaVersion = 1;

        private readonly string[] _paramKeys;
        private readonly float[] _paramValues;

        internal SkillSpec(StableId skillId, SkillSlot slot, TagMask tags, StableId costResource, float costAmount, bool costAll,
            float costMinimum, float resourceGain, int cooldownFrames, float damageCoefficient, float coefficientPerResource,
            int moveDuringCastPercent, bool commit, bool tracking, SkillFrames frames, float skillDamageIncreased,
            float skillDamageMore, string[] paramKeys, float[] paramValues)
        {
            SkillId = skillId;
            Slot = slot;
            Tags = tags;
            CostResource = costResource;
            CostAmount = costAmount;
            CostAllResource = costAll;
            CostMinimum = costMinimum;
            ResourceGain = resourceGain;
            CooldownFrames = cooldownFrames;
            DamageCoefficient = damageCoefficient;
            CoefficientPerResource = coefficientPerResource;
            MoveDuringCastPercent = moveDuringCastPercent;
            Commit = commit;
            Tracking = tracking;
            Frames = frames;
            SkillDamageIncreased = skillDamageIncreased;
            SkillDamageMore = skillDamageMore;
            _paramKeys = paramKeys;
            _paramValues = paramValues;
        }

        public StableId SkillId { get; }
        public SkillSlot Slot { get; }
        public TagMask Tags { get; }
        public StableId CostResource { get; }
        /// <summary>Resource cost after Resource Cost Reduction (0 when the skill is free or spends everything).</summary>
        public float CostAmount { get; }
        public bool CostAllResource { get; }
        public float CostMinimum { get; }
        public float ResourceGain { get; }
        /// <summary>Cooldown in frames after CDR (Ultimates get 50% of the final CDR, 01 3.6).</summary>
        public int CooldownFrames { get; }
        public float DamageCoefficient { get; }
        public float CoefficientPerResource { get; }
        public int MoveDuringCastPercent { get; }
        public bool Commit { get; }
        public bool Tracking { get; }
        public SkillFrames Frames { get; }
        /// <summary>Sum of unconditional Increased Skill Damage matching the tags (11 3.5.3).</summary>
        public float SkillDamageIncreased { get; }
        /// <summary>Product of (1 + More) of unconditional Skill Damage matching the tags.</summary>
        public float SkillDamageMore { get; }

        public bool IsUltimate => Tags.Has(Tag.Ultimate) || Slot == SkillSlot.F;
        public bool IsMovement => Tags.Has(Tag.Movement);
        public bool HasCost => !CostResource.IsEmpty && (CostAllResource || CostAmount > 0f);
        public ActionKind ActionKind => IsUltimate ? ActionKind.Ultimate : ActionKind.Skill;

        public bool TryGetParam(string key, out float value)
        {
            for (int i = 0; i < _paramKeys.Length; i++)
                if (string.Equals(_paramKeys[i], key, StringComparison.Ordinal))
                {
                    value = _paramValues[i];
                    return true;
                }
            value = 0f;
            return false;
        }

        public float GetParam(string key)
        {
            if (!TryGetParam(key, out float v))
                throw new System.Collections.Generic.KeyNotFoundException("Skill '" + SkillId.Value + "' has no param '" + key + "'");
            return v;
        }
    }
}
