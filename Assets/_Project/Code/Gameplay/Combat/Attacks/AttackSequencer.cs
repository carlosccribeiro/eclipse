using System;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Input;

namespace Eclipse.Gameplay.Combat.Attacks
{
    /// <summary>Weapon moveset as plain values (built from a WeaponDefinition or directly in tests).</summary>
    public sealed class AttackSetData
    {
        public AttackStep[] Basic = Array.Empty<AttackStep>();
        public bool HasHeavy;
        public AttackStep Heavy;
        public bool HasDash;
        public AttackStep Dash;
        public WeaponWeightClass Weight;
        /// <summary>Chakrams and Twin Wands (06 3.5): may dodge-cancel Startup regardless of weight class.</summary>
        public bool StartupDodgeCancelOverride;
        /// <summary>Frames without input before the combo resets (06 3.5: 0.8 s = 48 frames).</summary>
        public int ComboResetFrames = 48;
        /// <summary>Attack Speed cap from data/config (06 3.5 / 01); 0 or less = uncapped.</summary>
        public float MaxAttackSpeed;
    }

    /// <summary>State of the owner that the sequencer cannot know (dodge, stun/root, flinch).</summary>
    public struct AttackExternalState
    {
        /// <summary>False while stunned/frozen/knocked down: the running attack is interrupted and nothing starts.</summary>
        public bool CanAct;
        /// <summary>The dodge would really start: charges available and not rooted/stunned.</summary>
        public bool CanDodge;
        public bool DodgeActive;
        /// <summary>Frames left in the dodge counting the current one (1 = last).</summary>
        public int DodgeFramesRemaining;
        public bool DodgeCanCancel;

        public static AttackExternalState Free => new AttackExternalState { CanAct = true, CanDodge = true };
    }

    /// <summary>
    /// Pure combo/cancel logic over <see cref="AttackRuntimeState"/> and the <see cref="InputBuffer"/> (06 3.4, 3.5).
    /// Per tick call <see cref="Tick"/> once, before the DodgeController ticks: while an attack runs, the Dodge press is
    /// held here and re-pushed to the buffer (current frame) only when the cancel table allows it, so the dodge never
    /// starts mid-attack by itself.
    /// Basic/Heavy presses during Recovery are latched and start when Recovery ends (keeps APS = frames per hit);
    /// holding Basic repeats the combo; the combo restarts after <c>ComboResetFrames</c> without an attack.
    /// </summary>
    public sealed class AttackSequencer
    {
        private const uint AttackMask = (1u << (int)InputActionId.Basic) | (1u << (int)InputActionId.Heavy);

        private readonly AttackSetData _set;
        private readonly AttackRuntimeState _state = new AttackRuntimeState();
        private int _nextCombo;
        private int _currentCombo = -1;
        private int _lastEndFrame = int.MinValue / 2;
        private ActionKind _queued;
        private int _pendingDodgeFrame = int.MinValue;

        public AttackSequencer(AttackSetData set)
        {
            _set = set ?? throw new ArgumentNullException(nameof(set));
        }

        public AttackRuntimeState State => _state;
        public AttackSetData Set => _set;
        public bool IsAttacking => _state.IsActive;
        /// <summary>Kind of the running attack as an action (Dash counts as Basic); None when idle.</summary>
        public ActionKind CurrentAction => !_state.IsActive ? ActionKind.None : _state.Kind == AttackKind.Heavy ? ActionKind.Heavy : ActionKind.Basic;
        /// <summary>0-based combo index of the running/last Basic hit; -1 for Heavy/Dash or none.</summary>
        public int CurrentComboIndex => _currentCombo;
        /// <summary>0-based combo index the next Basic hit will use (after the reset rule).</summary>
        public int NextComboIndex => _nextCombo;
        public ActionKind Queued => _queued;

        /// <summary>True when an attack started during the last <see cref="Tick"/> (the controller clears the HitRegistry).</summary>
        public bool StartedThisTick { get; private set; }
        /// <summary>True when the running attack was cancelled into a Dodge during the last <see cref="Tick"/>.</summary>
        public bool DodgeCancelledThisTick { get; private set; }
        public AttackTickEvents LastEvents { get; private set; }

        /// <summary>Cancel table for the controller: can <paramref name="to"/> start right now? (M08 uses it for abilities.)</summary>
        public bool CanCancelInto(ActionKind to) => CanCancelInto(to, false);

        public bool CanCancelInto(ActionKind to, bool commit)
        {
            if (!_state.IsActive) return to != ActionKind.None;
            CancelContext c = BuildContext(commit);
            return CancelRules.Allows(c, to);
        }

        private CancelContext BuildContext(bool commit)
        {
            return new CancelContext
            {
                Current = CurrentAction,
                Phase = _state.Phase,
                PhaseFrame = _state.PhaseFrame,
                Charging = _state.IsCharging,
                Commit = commit,
                StartupDodgeCancelAllowed = _set.StartupDodgeCancelOverride || CancelRules.WeightAllowsStartupDodge(_set.Weight),
                FramesSinceParrySuccess = -1,
            };
        }

        /// <summary>Ends the running attack without side effects on the combo (used when another action takes over).</summary>
        public void Cancel(int frame)
        {
            if (_state.IsActive) _lastEndFrame = frame;
            _state.Interrupt();
            _queued = ActionKind.None;
        }

        public void Reset()
        {
            _state.Interrupt();
            _queued = ActionKind.None;
            _nextCombo = 0;
            _currentCombo = -1;
            _lastEndFrame = int.MinValue / 2;
            _pendingDodgeFrame = int.MinValue;
        }

        public void Tick(int frame, InputBuffer buffer, bool basicHeld, bool heavyHeld, float attackSpeed, in AttackExternalState ext)
        {
            StartedThisTick = false;
            DodgeCancelledThisTick = false;
            LastEvents = AttackTickEvents.None;

            if (_state.IsActive && !ext.CanAct)
            {
                Cancel(frame);
                _pendingDodgeFrame = int.MinValue;
            }

            // 1) Advance the running attack.
            if (_state.IsActive)
            {
                LastEvents = _state.Tick(heavyHeld);
                if ((LastEvents & AttackTickEvents.Ended) != 0)
                {
                    _lastEndFrame = frame;
                    if (_queued != ActionKind.None && ext.CanAct)
                    {
                        ActionKind q = _queued;
                        _queued = ActionKind.None;
                        StartAttack(q, frame, attackSpeed, heavyHeld);
                    }
                    else _queued = ActionKind.None;
                }
            }

            // 2) Dodge handling while attacking.
            HandleDodge(frame, buffer, ext);

            if (!ext.CanAct) return;

            // 3) New inputs.
            if (!_state.IsActive) StartFromInput(frame, buffer, basicHeld, heavyHeld, attackSpeed, ext);
            else if (_state.Phase == AttackPhase.Recovery) LatchQueue(frame, buffer, basicHeld);
        }

        private void HandleDodge(int frame, InputBuffer buffer, in AttackExternalState ext)
        {
            if (_state.IsActive)
            {
                if (buffer.TryConsume(frame, InputActionId.Dodge.Bit(), out BufferedAction pressed))
                    _pendingDodgeFrame = pressed.Frame;
            }
            if (_pendingDodgeFrame == int.MinValue) return;

            if (frame - _pendingDodgeFrame >= InputBuffer.BufferFrames || !ext.CanDodge)
            {
                _pendingDodgeFrame = int.MinValue; // expired, or the dodge would be refused: keep the attack.
                return;
            }

            if (!_state.IsActive)
            {
                buffer.Push(InputActionId.Dodge, _pendingDodgeFrame); // attack already over: let the dodge run.
                _pendingDodgeFrame = int.MinValue;
                return;
            }

            if (CanCancelInto(ActionKind.Dodge))
            {
                _state.Interrupt();
                _queued = ActionKind.None;
                _lastEndFrame = frame;
                _pendingDodgeFrame = int.MinValue;
                DodgeCancelledThisTick = true;
                buffer.Push(InputActionId.Dodge, frame);
            }
        }

        private void LatchQueue(int frame, InputBuffer buffer, bool basicHeld)
        {
            if (buffer.TryConsume(frame, AttackMask, out BufferedAction a))
                _queued = a.Id == InputActionId.Heavy ? ActionKind.Heavy : ActionKind.Basic;
            else if (basicHeld && _queued == ActionKind.None)
                _queued = ActionKind.Basic;
        }

        private void StartFromInput(int frame, InputBuffer buffer, bool basicHeld, bool heavyHeld, float attackSpeed,
            in AttackExternalState ext)
        {
            // During a dodge, Basic only counts in its last frames (it becomes Dash Attack); otherwise it waits in the buffer.
            bool basicAllowed = !ext.DodgeActive || (_set.HasDash && DashWindow(ext));
            uint mask = (basicAllowed ? InputActionId.Basic.Bit() : 0u) | (ext.DodgeActive ? 0u : InputActionId.Heavy.Bit());

            ActionKind want = ActionKind.None;
            if (mask != 0 && buffer.TryConsume(frame, mask, out BufferedAction a))
                want = a.Id == InputActionId.Heavy ? ActionKind.Heavy : ActionKind.Basic;
            else if (basicHeld && basicAllowed)
                want = ActionKind.Basic;

            if (want == ActionKind.Heavy && !_set.HasHeavy) want = ActionKind.None;
            if (want == ActionKind.Basic && !ext.DodgeActive && _set.Basic.Length == 0) want = ActionKind.None;
            if (want == ActionKind.None) return;

            if (want == ActionKind.Basic && ext.DodgeActive) StartDash(frame, attackSpeed, heavyHeld);
            else StartAttack(want, frame, attackSpeed, heavyHeld);
        }

        private static bool DashWindow(in AttackExternalState ext) =>
            ext.DodgeFramesRemaining >= 1 && ext.DodgeFramesRemaining <= CancelRules.DodgeToDashAttackFrames;

        private void StartAttack(ActionKind kind, int frame, float attackSpeed, bool heavyHeld)
        {
            if (kind == ActionKind.Heavy && _set.HasHeavy)
            {
                _currentCombo = -1;
                _nextCombo = 0;
                Begin(_set.Heavy, AttackKind.Heavy, attackSpeed, heavyHeld);
                return;
            }
            if (_set.Basic.Length == 0) return;
            if (frame - _lastEndFrame >= _set.ComboResetFrames) _nextCombo = 0;
            if (_nextCombo >= _set.Basic.Length) _nextCombo = 0;
            _currentCombo = _nextCombo;
            _nextCombo = (_nextCombo + 1) % _set.Basic.Length;
            Begin(_set.Basic[_currentCombo], AttackKind.Basic, attackSpeed, heavyHeld);
        }

        private void StartDash(int frame, float attackSpeed, bool heavyHeld)
        {
            _currentCombo = -1;
            _nextCombo = 0;
            Begin(_set.Dash, AttackKind.Dash, attackSpeed, heavyHeld);
        }

        private void Begin(in AttackStep step, AttackKind kind, float attackSpeed, bool heavyHeld)
        {
            _state.Begin(step, kind, attackSpeed, _set.MaxAttackSpeed);
            StartedThisTick = true;
            LastEvents |= _state.Tick(heavyHeld);
        }
    }
}
