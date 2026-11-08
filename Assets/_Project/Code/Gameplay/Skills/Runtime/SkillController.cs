using System;
using Eclipse.Core.Damage;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Input;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Skill execution cycle of 11 3.6.3 (pure C#, driven once per simulation frame by a thin MonoBehaviour):
    /// validate (state, busy/cancel, cooldown, cost) -> OnSkillCast -> windup/active/recovery by frame -> Delivery runner
    /// -> OnSkillHit -> OnCastEnd. Phases are derived from the cast start frame, so they never drift.
    /// </summary>
    public sealed class SkillController
    {
        // At most one OnSkillHit per target per use (11 3.6.4); beyond this many distinct targets in one cast the extra ones are not deduplicated.
        private const int MaxTrackedTargets = 64;

        private readonly EntityId _owner;
        private readonly ISkillState _state;
        private readonly ISkillResource _resource;
        private readonly IAttackCancelHost _attacks;
        private readonly ISkillDeliveryRunner _runner;
        private readonly CombatEventStream _events;
        private readonly int[] _hitTargets = new int[MaxTrackedTargets];
        private int _hitCount;
        private int _enteredPhases; // 0 none, 1 windup entered, 2 active entered, 3 recovery entered

        public SkillController(EntityId owner, ISkillState state, ISkillResource resource, IAttackCancelHost attacks,
            ISkillDeliveryRunner runner, CombatEventStream events)
        {
            _owner = owner;
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _resource = resource;
            _attacks = attacks;
            _runner = runner;
            _events = events;
        }

        public CooldownTracker Cooldowns { get; } = new CooldownTracker();
        public bool IsCasting => Current != null;
        public SkillSpec Current { get; private set; }
        public SkillPhase Phase { get; private set; }
        /// <summary>1-based frame inside <see cref="Phase"/> (1 = first frame of the phase).</summary>
        public int PhaseFrame { get; private set; }
        public int StartFrame { get; private set; }
        public float ResourceSpent { get; private set; }
        /// <summary>Movement allowed during the running cast, 0/50/100 (06 3.5); 100 when idle.</summary>
        public int MoveDuringCastPercent => Current == null ? 100 : Current.MoveDuringCastPercent;

        /// <summary>Coefficient of the running cast: base plus the per-resource part (Worldbreaker 0.06 per Fury).</summary>
        public float CurrentDamageCoefficient =>
            Current == null ? 0f : Current.DamageCoefficient + Current.CoefficientPerResource * ResourceSpent;

        /// <summary>Consumes the best skill press from the buffer and casts it. Busy presses are re-queued with their original frame.</summary>
        public bool ProcessInput(InputBuffer buffer, ISkillSpecSource specs, int frame, out CastResult result)
        {
            result = CastResult.NoSkill;
            if (!buffer.TryConsume(frame, SkillSlotBinding.AcceptedMask, out BufferedAction action)) return false;
            if (!SkillSlotBinding.TryGetSlot(action.Id, out var slot)) return false;
            result = TryCast(specs.Get(slot), frame);
            if (result == CastResult.Busy) buffer.Push(action.Id, action.Frame);
            return true;
        }

        public bool TryCast(SkillSpec spec, int frame, out CastResult result)
        {
            result = TryCast(spec, frame);
            return result == CastResult.Started;
        }

        /// <summary>Validates (state, busy/cancel, cooldown, cost) and starts the cast.</summary>
        public CastResult TryCast(SkillSpec spec, int frame)
        {
            CastResult r = Validate(spec, frame, out bool needsCancel);
            return r == CastResult.Started ? Begin(spec, frame, needsCancel) : r;
        }

        private CastResult Validate(SkillSpec spec, int frame, out bool needsCancelAttack)
        {
            needsCancelAttack = false;
            if (spec == null) return CastResult.NoSkill;
            if (_state.IsStunned(frame)) return CastResult.Stunned;
            if (_state.IsFrozen(frame)) return CastResult.Frozen;
            if (_state.IsSilenced(frame)) return CastResult.Silenced;
            if (spec.IsMovement && _state.IsRooted(frame)) return CastResult.Rooted;

            // Skill -> Skill never cancels (06 3.5); Basic/Heavy -> Skill follows the M05 cancel table.
            if (IsCasting) return CastResult.Busy;
            if (_attacks != null)
            {
                if (!_attacks.CanCancelInto(spec.ActionKind)) return CastResult.Busy;
                needsCancelAttack = true;
            }

            if (!Cooldowns.IsReady(spec.Slot, frame)) return CastResult.OnCooldown;
            if (spec.HasCost && _resource != null)
            {
                float required = spec.CostAllResource ? Math.Max(spec.CostMinimum, 0f) : spec.CostAmount;
                if (_resource.Current + 1e-4f < required || (spec.CostAllResource && _resource.Current <= 0f))
                    return CastResult.InsufficientResource;
            }
            else if (spec.HasCost) return CastResult.InsufficientResource;
            return CastResult.Started;
        }

        private CastResult Begin(SkillSpec spec, int frame, bool cancelAttack)
        {
            if (cancelAttack) _attacks.CancelForAction();

            float spent = 0f;
            if (spec.HasCost)
            {
                spent = spec.CostAllResource ? _resource.SpendAll() : spec.CostAmount;
                if (!spec.CostAllResource) _resource.TrySpend(spent);
                if (!_resource.PublishesSpentEvent && spent > 0f)
                    _events?.Publish(new OnResourceSpent(frame, 0, _owner, spec.CostResource, spent));
            }
            if (spec.ResourceGain > 0f) _resource?.Gain(spec.ResourceGain);

            Cooldowns.Start(spec.Slot, frame, spec.CooldownFrames);
            Current = spec;
            StartFrame = frame;
            ResourceSpent = spent;
            Phase = SkillPhase.None;
            PhaseFrame = 0;
            _enteredPhases = 0;
            _hitCount = 0;

            _events?.Publish(new OnSkillCast(frame, 0, _owner, spec.SkillId));
            Advance(frame);
            return CastResult.Started;
        }

        /// <summary>Advances the running cast to <paramref name="frame"/>. Call once per simulation frame, before reading input.</summary>
        public void Tick(int frame)
        {
            if (Current != null) Advance(frame);
        }

        private void Advance(int frame)
        {
            SkillSpec s = Current;
            SkillFrames f = s.Frames;
            int elapsed = frame - StartFrame;
            var info = new SkillCastInfo(s, StartFrame, ResourceSpent, CurrentDamageCoefficient);

            int windupEnd = f.Windup;
            int activeEnd = windupEnd + f.Active;
            int total = activeEnd + f.Recovery;

            if (elapsed >= total)
            {
                // Phases skipped by a late tick are still announced in order so the runner never misses one.
                EnterUpTo(info, SkillPhase.Recovery, frame, f);
                Finish(frame, cancelled: false);
                return;
            }

            SkillPhase phase;
            int start;
            if (elapsed < windupEnd) { phase = SkillPhase.Windup; start = 0; }
            else if (elapsed < activeEnd) { phase = SkillPhase.Active; start = windupEnd; }
            else { phase = SkillPhase.Recovery; start = activeEnd; }

            EnterUpTo(info, phase, frame, f);
            Phase = phase;
            PhaseFrame = elapsed - start + 1;
        }

        private void EnterUpTo(in SkillCastInfo info, SkillPhase target, int frame, SkillFrames f)
        {
            for (int p = _enteredPhases + 1; p <= (int)target; p++)
            {
                int len = p == 1 ? f.Windup : p == 2 ? f.Active : f.Recovery;
                _enteredPhases = p;
                if (len <= 0) continue; // zero-length phases are not announced
                _runner?.OnPhaseEnter(info, (SkillPhase)p, frame);
            }
        }

        private void Finish(int frame, bool cancelled)
        {
            SkillSpec s = Current;
            var info = new SkillCastInfo(s, StartFrame, ResourceSpent, CurrentDamageCoefficient);
            Current = null;
            Phase = SkillPhase.None;
            PhaseFrame = 0;
            _runner?.OnCastEnd(info, cancelled, frame);
            _events?.Publish(new OnCastEnd(frame, 0, _owner, s.SkillId));
        }

        /// <summary>Skill to Dodge (06 3.5): only in Recovery, never with Commit, never from an Ultimate.</summary>
        public bool CanCancelIntoDodge()
        {
            if (Current == null) return false;
            var ctx = new CancelContext
            {
                Current = Current.ActionKind,
                Phase = Phase == SkillPhase.Recovery ? AttackPhase.Recovery : Phase == SkillPhase.Active ? AttackPhase.Active : AttackPhase.Startup,
                PhaseFrame = PhaseFrame,
                Commit = Current.Commit,
            };
            return CancelRules.Allows(ctx, ActionKind.Dodge);
        }

        /// <summary>Ends the cast for a dodge. Returns false (and changes nothing) when the table does not allow it.</summary>
        public bool CancelIntoDodge(int frame)
        {
            if (!CanCancelIntoDodge()) return false;
            Finish(frame, cancelled: true);
            return true;
        }

        /// <summary>Hard interruption (death, hard CC). Cooldown and cost stay spent.</summary>
        public void Interrupt(int frame)
        {
            if (Current != null) Finish(frame, cancelled: true);
        }

        /// <summary>Called by Delivery when the running skill hits a target. Publishes OnSkillHit once per target per use. Returns false if duplicated or idle.</summary>
        public bool ReportHit(EntityId target, float value, int frame)
        {
            if (Current == null) return false;
            for (int i = 0; i < _hitCount; i++)
                if (_hitTargets[i] == target.Value) return false;
            if (_hitCount < MaxTrackedTargets) _hitTargets[_hitCount++] = target.Value;
            _events?.Publish(new OnSkillHit(frame, 0, _owner, target, Current.SkillId, value));
            return true;
        }

        /// <summary>Configures a pooled hit for the running skill: Generation 0 (direct player action, 11 3.6.5), skill tags, own source.</summary>
        public void PrepareDamage(DamageInstance damage)
        {
            if (damage == null) throw new ArgumentNullException(nameof(damage));
            damage.Generation = 0;
            damage.SourceId = _owner.Value;
            if (Current != null) damage.Tags = Current.Tags;
        }
    }
}
