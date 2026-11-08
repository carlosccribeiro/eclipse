using System;
using Eclipse.Core.Ids;
using Eclipse.Core.Random;
using Eclipse.Data.Skills;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>The runnable parts of a skill: Delivery, Effects and Triggers already typed for the runtime.</summary>
    public sealed class SkillProgram
    {
        public SkillProgram(SkillDelivery delivery, SkillEffect[] effects, SkillTrigger[] triggers = null)
        {
            Delivery = delivery;
            Effects = effects ?? Array.Empty<SkillEffect>();
            Triggers = triggers ?? Array.Empty<SkillTrigger>();
        }

        public SkillDelivery Delivery { get; }
        public SkillEffect[] Effects { get; }
        public SkillTrigger[] Triggers { get; }

        /// <summary>Builds the program from the authored asset; a Delivery/Effect/Trigger that is not a runtime type is a data error.</summary>
        public static SkillProgram From(SkillDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            SkillDelivery delivery = null;
            if (definition.Delivery != null)
            {
                delivery = definition.Delivery as SkillDelivery;
                if (delivery == null) throw new InvalidOperationException(definition.StableIdString + ": Delivery '" + definition.Delivery.GetType().Name + "' has no runtime implementation");
            }
            var effects = new SkillEffect[definition.Effects.Count];
            for (int i = 0; i < effects.Length; i++)
            {
                effects[i] = definition.Effects[i] as SkillEffect;
                if (effects[i] == null) throw new InvalidOperationException(definition.StableIdString + ": Effect #" + i + " has no runtime implementation");
            }
            var triggers = new SkillTrigger[definition.Triggers.Count];
            for (int i = 0; i < triggers.Length; i++)
            {
                triggers[i] = definition.Triggers[i] as SkillTrigger;
                if (triggers[i] == null) throw new InvalidOperationException(definition.StableIdString + ": Trigger #" + i + " has no runtime implementation");
            }
            return new SkillProgram(delivery, effects, triggers);
        }
    }

    /// <summary>
    /// Connects the <see cref="SkillController"/> (T3) to the Delivery/Effect library: on each announced phase it runs the matching
    /// Effects and the skill's Delivery, advances the Delivery and the tethers once per frame, and registers the skill's Triggers on the
    /// first cast. Programs are looked up by skill StableId. Plain C#; allocation-free after registration.
    /// </summary>
    public sealed class SkillRunner : ISkillDeliveryRunner
    {
        private struct Registration
        {
            public StableId Id;
            public SkillProgram Program;
            public int HitGroupId;
            public bool TriggersRegistered;
        }

        private readonly SkillExecContext _ctx;
        private readonly SkillTriggerSystem _triggers;
        private Registration[] _regs = new Registration[8];
        private int _count;
        private int _current = -1;
        private int _startFrame;

        public SkillRunner(SkillExecContext ctx, SkillTriggerSystem triggers = null)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _triggers = triggers;
        }

        public bool IsRunning => _current >= 0;

        public void Register(StableId skill, SkillProgram program)
        {
            if (program == null) throw new ArgumentNullException(nameof(program));
            for (int i = 0; i < _count; i++)
                if (_regs[i].Id == skill) throw new InvalidOperationException("Skill already registered: " + skill.Value);
            if (_count == _regs.Length) Array.Resize(ref _regs, _count * 2);
            _regs[_count++] = new Registration
            {
                Id = skill, Program = program, HitGroupId = (int)(RngStreams.Fnv1a64(skill.Value) & 0x7FFFFFFF),
            };
        }

        public void Register(SkillDefinition definition) =>
            Register(StableId.Parse(definition.StableIdString), SkillProgram.From(definition));

        public void OnPhaseEnter(in SkillCastInfo cast, SkillPhase phase, int frame)
        {
            int idx = Find(cast.Spec.SkillId);
            if (idx < 0) return;
            SkillProgram program = _regs[idx].Program;

            _ctx.Frame = frame;
            if (_current != idx || _startFrame != cast.StartFrame)
            {
                _current = idx;
                _startFrame = cast.StartFrame;
                _ctx.BeginCast(in cast, frame, program.Effects, _regs[idx].HitGroupId);
                RegisterTriggers(idx, cast.Spec);
                _ctx.RunOnce(EffectTiming.CastStart);
            }
            _ctx.Cast = cast;

            if (phase == SkillPhase.Active) _ctx.RunOnce(EffectTiming.ActiveStart);
            program.Delivery?.OnPhaseEnter(_ctx, phase);
        }

        /// <summary>Once per simulation frame, after <see cref="SkillController.Tick"/>: moves the Delivery and checks the tethers.</summary>
        public void Tick(int frame)
        {
            _ctx.Frame = frame;
            if (_current >= 0) _regs[_current].Program.Delivery?.Tick(_ctx);
            _ctx.Tethers?.Tick(_ctx);
        }

        public void OnCastEnd(in SkillCastInfo cast, bool cancelled, int frame)
        {
            if (_current < 0 || _startFrame != cast.StartFrame) return;
            SkillProgram program = _regs[_current].Program;
            _current = -1;
            _ctx.Frame = frame;
            if (!cancelled) _ctx.RunOnce(EffectTiming.CastEnd);
            program.Delivery?.OnCastEnd(_ctx, cancelled);
        }

        private void RegisterTriggers(int idx, SkillSpec spec)
        {
            if (_triggers == null || _regs[idx].TriggersRegistered) return;
            _regs[idx].TriggersRegistered = true;
            SkillTrigger[] list = _regs[idx].Program.Triggers;
            for (int i = 0; i < list.Length; i++) _triggers.Add(spec, list[i]);
        }

        private int Find(StableId id)
        {
            for (int i = 0; i < _count; i++)
                if (_regs[i].Id == id) return i;
            return -1;
        }
    }
}
