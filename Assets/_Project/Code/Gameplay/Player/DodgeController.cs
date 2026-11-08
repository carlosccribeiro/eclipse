using Eclipse.Core.Events;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Events;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Physics;
using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Player
{
    /// <summary>
    /// Pure dodge logic (no MonoBehaviour): charges, sequential recharge, frame progress, cancel window.
    /// Per tick call <see cref="Advance"/> first, then optionally <see cref="TryStart"/>.
    /// </summary>
    public sealed class DodgeState
    {
        public const int InvulnerabilitySourceId = 1;

        private readonly DodgeConfig _cfg;
        private int _extraCharges;
        private int _rechargeProgress;

        public DodgeState(DodgeConfig config)
        {
            _cfg = config;
            Charges = config.baseCharges;
        }

        public int Charges { get; private set; }

        /// <summary>Max charges: base + explicit extras, clamped to the cap (06 3.7).</summary>
        public int MaxCharges => System.Math.Min(_cfg.baseCharges + _extraCharges, _cfg.maxChargesCap);

        /// <summary>Current dodge frame (1-based), 0 when not dodging.</summary>
        public int ActiveFrame { get; private set; }

        /// <summary>Global frame at which dodge frame 1 happened.</summary>
        public int StartFrame { get; private set; }

        public bool IsDodging => ActiveFrame > 0;

        /// <summary>True when attacks/abilities may cancel (idle or dodge frame &gt;= cancelFromFrame).</summary>
        public bool CanCancel => ActiveFrame == 0 || ActiveFrame >= _cfg.cancelFromFrame;

        /// <summary>Ticks accumulated toward the next charge.</summary>
        public int RechargeProgressTicks => _rechargeProgress;

        /// <summary>Explicit source of extra charges (item/talent). Clamped by the cap.</summary>
        public void SetExtraCharges(int extra)
        {
            _extraCharges = System.Math.Max(0, extra);
            if (Charges > MaxCharges) Charges = MaxCharges;
        }

        /// <summary>Advances recharge and the active dodge by one tick.</summary>
        public void Advance()
        {
            if (Charges < MaxCharges)
            {
                if (++_rechargeProgress >= _cfg.RechargeTicks) { Charges++; _rechargeProgress = 0; }
            }
            else _rechargeProgress = 0;

            if (ActiveFrame > 0 && ++ActiveFrame > _cfg.totalFrames) ActiveFrame = 0;
        }

        /// <summary>Starts a dodge at global frame <paramref name="frame"/>. Refused while dodging or without charges.</summary>
        public bool TryStart(int frame)
        {
            if (ActiveFrame > 0 || Charges <= 0) return false;
            Charges--;
            ActiveFrame = 1;
            StartFrame = frame;
            return true;
        }

        public void Reset()
        {
            ActiveFrame = 0;
            Charges = MaxCharges;
            _rechargeProgress = 0;
        }

        /// <summary>First i-frame as a global frame for a dodge started at <paramref name="startFrame"/>.</summary>
        public static int InvulnerableStart(DodgeConfig c, int startFrame) => startFrame + c.invulnerableStartFrame - 1;

        /// <summary>Last i-frame (inclusive) as a global frame.</summary>
        public static int InvulnerableEnd(DodgeConfig c, int startFrame) => startFrame + c.invulnerableEndFrame - 1;
    }

    /// <summary>
    /// Universal dodge driver (06 3.7). Consumes the Dodge entry of the InputBuffer (top priority) inside FixedTick, moves the
    /// body with <see cref="PlayerMotor.TryMove"/>, registers the i-frame window in <see cref="Invulnerability"/> and publishes
    /// <see cref="OnDodge"/>/<see cref="OnPerfectDodge"/>. Movement Speed and CDR are not read anywhere here.
    ///
    /// Pass-through (Light/Medium): Character-layer colliders are the blockers in the motor. Before each step the controller
    /// capsule-casts the step, and for every hit collider whose <see cref="IMassBody"/> (collider or parents) is Light/Medium it
    /// disables that body collider until the dodge ends. Heavy/Colossal and Wall/Environment/LowCover stay solid, so TryMove stops
    /// there. Hurtboxes live on other layers and are unaffected. Bodies entering the path mid-step are caught by the next step cast.
    /// </summary>
    public sealed class DodgeController : MonoBehaviour, IFixedTickable
    {
        private const int MaxDisabled = 32;
        private const float ProbeSkin = 0.05f;
        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        [SerializeField] private DodgeConfig config;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private int tickPriority = -90;
        [SerializeField] private float capsuleRadius = 0.4f;
        [SerializeField] private float capsuleHeight = 1.8f;

        private readonly Collider[] _disabled = new Collider[MaxDisabled];
        private int _disabledCount;
        private TickScheduler _scheduler;
        private PlayerInputReader _input;
        private DodgeState _state;
        private Vector3 _dir;
        private bool _registered;

        public InvulnerabilityState Invulnerability { get; } = new InvulnerabilityState();
        public EventBus<OnDodge> DodgeBus { get; } = new EventBus<OnDodge>();
        public EventBus<OnPerfectDodge> PerfectDodgeBus { get; } = new EventBus<OnPerfectDodge>();

        public DodgeConfig Config => config;
        public int Charges => _state.Charges;
        public int MaxCharges => _state.MaxCharges;
        public bool IsDodging => _state.IsDodging;
        public int ActiveFrame => _state.ActiveFrame;
        /// <summary>True when attack/ability may cancel the dodge (frame &gt;= 15 or idle).</summary>
        public bool CanCancel => _state.CanCancel;

        public void Initialize(TickScheduler scheduler, PlayerInputReader input, PlayerMotor playerMotor, DodgeConfig dodgeConfig = null)
        {
            Shutdown();
            _scheduler = scheduler;
            _input = input;
            if (playerMotor != null) motor = playerMotor;
            if (dodgeConfig != null) config = dodgeConfig;
            _state = new DodgeState(config);
            _scheduler.Register(this, tickPriority);
            _registered = true;
        }

        private void OnDisable() => Shutdown();

        private void Shutdown()
        {
            if (_registered && _scheduler != null) _scheduler.Unregister(this);
            _registered = false;
            EndDodgeEffects();
        }

        /// <summary>Explicit extra-charge source (item/talent); max 3 total.</summary>
        public void SetExtraCharges(int extra) => _state.SetExtraCharges(extra);

        /// <summary>Reports an incoming hit; publishes <see cref="OnPerfectDodge"/> on a Perfect Dodge. The caller skips damage unless NotNegated.</summary>
        public HitResult ReportIncomingHit(int impactFrame)
        {
            HitResult r = Invulnerability.NotifyIncomingHit(impactFrame);
            if (r == HitResult.PerfectDodge) PerfectDodgeBus.Publish(new OnPerfectDodge(impactFrame));
            return r;
        }

        public void Tick(in SimulationFrame frame)
        {
            bool wasDodging = _state.IsDodging;
            _state.Advance();
            if (wasDodging && !_state.IsDodging) EndDodgeEffects();

            if (!_state.IsDodging)
            {
                // Dodge has top buffer priority. With no charge the press is consumed and refused.
                if (_input.Buffer.TryConsume(frame.Index, InputActionId.Dodge.Bit(), out _) && _state.TryStart(frame.Index))
                    BeginDodge(frame.Index);
            }

            if (_state.IsDodging) StepDodge();
        }

        private void BeginDodge(int frameIndex)
        {
            Vector3 dir = PlayerAim.StickToWorld(_input.Move, motor.InputBasisYawDegrees);
            if (dir.sqrMagnitude < 1e-4f) dir = motor.Facing;
            dir.y = 0f;
            _dir = dir.normalized;

            motor.InputSuspended = true;
            motor.RotationLocked = true;
            motor.SetFacing(_dir);

            Invulnerability.AddWindow(DodgeState.InvulnerableStart(config, frameIndex), DodgeState.InvulnerableEnd(config, frameIndex),
                config.perfectDodgeWindowFrames, DodgeState.InvulnerabilitySourceId);
            DodgeBus.Publish(new OnDodge(frameIndex, motor.SimPosition, _dir, _state.Charges));
        }

        private void StepDodge()
        {
            float step = config.StepDistance(_state.ActiveFrame);
            if (step <= 0f) return;
            DisablePassThroughBodies(step);
            motor.TryMove(_dir * step);
        }

        private void EndDodgeEffects()
        {
            for (int i = 0; i < _disabledCount; i++)
            {
                if (_disabled[i] != null) _disabled[i].enabled = true;
                _disabled[i] = null;
            }
            _disabledCount = 0;
            if (motor != null)
            {
                motor.InputSuspended = false;
                motor.RotationLocked = false;
            }
        }

        private void DisablePassThroughBodies(float step)
        {
            float half = Mathf.Max(capsuleHeight * 0.5f, capsuleRadius);
            Vector3 center = motor.SimPosition + Vector3.up * half;
            float offset = half - capsuleRadius;
            int n = UnityEngine.Physics.CapsuleCastNonAlloc(center + Vector3.up * offset, center - Vector3.up * offset,
                capsuleRadius, _dir, Hits, step + ProbeSkin, PhysicsLayers.CharacterMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = Hits[i].collider;
                if (c == null || !c.enabled || c.transform == motor.transform || c.transform.IsChildOf(motor.transform)) continue;
                var body = c.GetComponentInParent<IMassBody>();
                if (body == null || body.MassClass > MassClass.Medium) continue;
                if (_disabledCount >= MaxDisabled) break;
                c.enabled = false;
                _disabled[_disabledCount++] = c;
            }
        }
    }
}
