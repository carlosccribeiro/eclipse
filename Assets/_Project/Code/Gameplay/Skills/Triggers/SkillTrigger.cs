using System;
using System.Collections.Generic;
using Eclipse.Core.Procs;
using Eclipse.Core.Random;
using Eclipse.Core.Tags;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>Hooks a skill Trigger can listen to (subset of the closed vocabulary of 11 3.6.4 / 06 3.11 that M08 wires).</summary>
    public enum TriggerEventKind : byte
    {
        OnHitDealt = 0, OnCritDealt, OnKill, OnSkillCast, OnSkillHit, OnParry, OnBlock, OnStatusApplied, OnCastEnd, OnHitTaken,
    }

    public enum TriggerConditionKind : byte
    {
        None = 0,
        /// <summary>Only when the event is a critical hit.</summary>
        CritOnly,
        /// <summary>Only when the event value (damage, resource) reaches <c>conditionValue</c>.</summary>
        MinValue,
        /// <summary>Only when the event carries every tag of <c>conditionTagBits</c>.</summary>
        RequiresTags,
    }

    /// <summary>One event as seen by the trigger system: the hook plus the proc state of the event that raised it (R07).</summary>
    public readonly struct TriggerEventData
    {
        public readonly TriggerEventKind Kind;
        public readonly int Frame;
        public readonly int Generation;
        public readonly HitFlags Flags;
        public readonly SourceChain Chain;
        public readonly EntityId Source;
        public readonly EntityId Target;
        public readonly float Value;
        public readonly bool IsCrit;
        public readonly TagMask Tags;

        public TriggerEventData(TriggerEventKind kind, int frame, int generation, HitFlags flags, SourceChain chain, EntityId source,
            EntityId target, float value = 0f, bool isCrit = false, TagMask tags = default)
        {
            Kind = kind; Frame = frame; Generation = generation; Flags = flags; Chain = chain; Source = source; Target = target;
            Value = value; IsCrit = isCrit; Tags = tags;
        }
    }

    /// <summary>
    /// (Event, Condition, Effects[], ICD) of 11 3.6.1. Evaluated by <see cref="SkillTriggerSystem"/>, only for events of Generation
    /// below 2 (11 3.6.5). The cooldown has a floor of 0.1 s and defaults to 0.5 s; the same trigger never fires from a hit it originated.
    /// </summary>
    [Serializable]
    public sealed class SkillTrigger : Trigger
    {
        /// <summary>11 3.6.5: minimum ICD of any trigger.</summary>
        public const float MinIcdSeconds = 0.1f;
        /// <summary>11 3.6.5: default ICD.</summary>
        public const float DefaultIcdSeconds = 0.5f;

        [SerializeField] private string triggerId;
        [SerializeField] private TriggerEventKind eventKind;
        [SerializeField] private TriggerConditionKind condition;
        [SerializeField] private float conditionValue;
        [SerializeField] private ulong conditionTagBits;
        [SerializeField] private float icdSeconds = DefaultIcdSeconds;
        [SerializeReference] private List<Effect> effects = new List<Effect>();

        private SkillEffect[] _resolved;

        public SkillTrigger() { }

        public SkillTrigger(string triggerId, TriggerEventKind eventKind, float icdSeconds, TriggerConditionKind condition = TriggerConditionKind.None,
            float conditionValue = 0f, ulong conditionTagBits = 0UL, params SkillEffect[] effects)
        {
            this.triggerId = triggerId;
            this.eventKind = eventKind;
            this.icdSeconds = icdSeconds;
            this.condition = condition;
            this.conditionValue = conditionValue;
            this.conditionTagBits = conditionTagBits;
            this.effects = new List<Effect>(effects ?? Array.Empty<SkillEffect>());
        }

        public string TriggerId => triggerId;
        public TriggerEventKind EventKind => eventKind;
        /// <summary>Stable hash recorded in the SourceChain of everything this trigger causes.</summary>
        public ulong SourceHash => RngStreams.Fnv1a64(triggerId ?? string.Empty);
        /// <summary>ICD in frames, never below <see cref="MinIcdSeconds"/>.</summary>
        public int IcdFrames => CombatStateRules.Frames(Math.Max(icdSeconds, MinIcdSeconds));

        public bool ConditionMet(in TriggerEventData e)
        {
            switch (condition)
            {
                case TriggerConditionKind.CritOnly: return e.IsCrit;
                case TriggerConditionKind.MinValue: return e.Value >= conditionValue;
                case TriggerConditionKind.RequiresTags:
                    return (e.Tags.Bits & conditionTagBits) == conditionTagBits;
                default: return true;
            }
        }

        public SkillEffect[] ResolvedEffects()
        {
            if (_resolved != null) return _resolved;
            var list = new List<SkillEffect>(effects.Count);
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] is SkillEffect fx) list.Add(fx);
            _resolved = list.ToArray();
            return _resolved;
        }
    }
}
