using System;
using Eclipse.Core.Procs;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Evaluates registered <see cref="SkillTrigger"/>s for each event (11 3.6.3 "TriggerSystem avalia triggers inscritos").
    /// An event only fires triggers when its Generation is below 2; what a trigger does runs one Generation deeper, flagged
    /// IsTriggered, with the trigger appended to the SourceChain (a trigger never fires from its own hit). Each trigger has its own ICD.
    /// Plain C#, allocation-free after registration.
    /// </summary>
    public sealed class SkillTriggerSystem
    {
        private struct Slot
        {
            public SkillTrigger Trigger;
            public SkillSpec Spec;
            public ulong Hash;
            public int NextFrame;
        }

        private Slot[] _slots = new Slot[8];
        private int _count;

        public int Count => _count;

        /// <summary>Registers a trigger of <paramref name="spec"/> (its coefficient and params are used by the trigger's effects).</summary>
        public void Add(SkillSpec spec, SkillTrigger trigger)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));
            if (_count == _slots.Length) Array.Resize(ref _slots, _count * 2);
            _slots[_count++] = new Slot { Trigger = trigger, Spec = spec, Hash = trigger.SourceHash, NextFrame = int.MinValue };
        }

        public void Clear() => _count = 0;

        /// <summary>Feeds one event; returns how many triggers fired.</summary>
        public int Feed(SkillExecContext ctx, in TriggerEventData e)
        {
            // 11 3.6.5: nothing triggers at Generation >= 2.
            if (e.Generation >= ProcContext.MaxGeneration) return 0;

            int fired = 0;
            for (int i = 0; i < _count; i++)
            {
                SkillTrigger t = _slots[i].Trigger;
                if (t.EventKind != e.Kind) continue;
                if (e.Chain.Contains(_slots[i].Hash)) continue; // never from a hit this trigger originated
                if (e.Frame < _slots[i].NextFrame) continue;     // ICD
                if (!t.ConditionMet(in e)) continue;

                _slots[i].NextFrame = e.Frame + t.IcdFrames;
                Run(ctx, in _slots[i], in e);
                fired++;
            }
            return fired;
        }

        private static void Run(SkillExecContext ctx, in Slot slot, in TriggerEventData e)
        {
            SkillCastInfo cast = ctx.Cast;
            int frame = ctx.Frame, gen = ctx.Generation, group = ctx.HitGroupId, hits = ctx.CurrentHits;
            HitFlags flags = ctx.ExtraFlags;
            var chain = ctx.Chain;
            float mult = ctx.DamageMultiplier, flat = ctx.FlatDamage;

            ctx.Cast = new SkillCastInfo(slot.Spec, e.Frame, 0f, slot.Spec.DamageCoefficient);
            ctx.Frame = e.Frame;
            ctx.Generation = e.Generation + 1;
            ctx.ExtraFlags = e.Flags | HitFlags.IsTriggered;
            ctx.Chain = e.Chain.Append(slot.Hash);
            ctx.DamageMultiplier = 1f;
            ctx.FlatDamage = 0f;
            ctx.CurrentHits = 1;
            try
            {
                SkillTarget target = e.Target.IsValid ? ctx.Resolve(e.Target) : null;
                ctx.ApplyList(slot.Trigger.ResolvedEffects(), target);
            }
            finally
            {
                ctx.Cast = cast;
                ctx.Frame = frame;
                ctx.Generation = gen;
                ctx.HitGroupId = group;
                ctx.ExtraFlags = flags;
                ctx.Chain = chain;
                ctx.DamageMultiplier = mult;
                ctx.FlatDamage = flat;
                ctx.CurrentHits = hits;
            }
        }
    }
}
