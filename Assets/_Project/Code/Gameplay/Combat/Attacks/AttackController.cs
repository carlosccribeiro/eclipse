using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Physics;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using UnityEngine;
using UPhysics = UnityEngine.Physics;

namespace Eclipse.Gameplay.Combat.Attacks
{
    /// <summary>
    /// Thin MonoBehaviour around <see cref="AttackSequencer"/> (all rules are pure and tested in EditMode).
    /// Runs only in FixedTick and only on ticks the owner's <see cref="EntityClock"/> lets it run, so hit-stop freezes
    /// the attacker without scaling phases. Per tick: sequencer, lunge, motor locks, hitbox evaluation in Active.
    /// Scheduler order: motor (-100) &lt; this (-95) &lt; DodgeController (-90): the dodge must tick after this
    /// controller because it holds the Dodge press during an attack until the cancel table allows it.
    /// This controller is the only caller of <c>Clock.Advance()</c> of its entity.
    /// </summary>
    public sealed class AttackController : MonoBehaviour, IFixedTickable
    {
        private const int LungeBuffer = 16;

        [SerializeField] private int tickPriority = -95;
        [Tooltip("Attack/Cast Speed cap (06 3.5 / 01, provisional 2.0). 0 = uncapped. Set by the hero prefab, never hardcoded.")]
        [SerializeField] private float maxAttackSpeed;
        [Tooltip("Chakrams / Twin Wands: Startup dodge-cancel regardless of weight class (06 3.5).")]
        [SerializeField] private bool startupDodgeCancelOverride;
        [Tooltip("Attacker body radius used as contact clearance by the lunge.")]
        [SerializeField] private float bodyRadius = 0.4f;

        private readonly Collider[] _overlaps = new Collider[LungeBuffer];
        private readonly LungeCandidate[] _candidates = new LungeCandidate[LungeBuffer];

        private TickScheduler _scheduler;
        private PlayerInputReader _input;
        private PlayerMotor _motor;
        private ICombatEntity _entity;
        private StateComponent _states;
        private HitboxEvaluator _evaluator;
        private DodgeController _dodge;
        private WeaponDefinition _weapon;
        private AttackSequencer _sequencer;
        private StatId _speedStat = StatId.AttackSpeed;
        private TagMask _basicTags, _heavyTags;
        private LungePlan _lunge;
        private int _lungeTicksDone;
        private Vector3 _prevOrigin, _prevForward;
        private bool _hasPrev;
        private bool _locksApplied;
        private bool _registered;
        private int _lastFrame;

        public AttackSequencer Sequencer => _sequencer;
        public bool IsAttacking => _sequencer != null && _sequencer.IsAttacking;
        public AttackPhase Phase => _sequencer == null ? AttackPhase.None : _sequencer.State.Phase;
        public float MaxAttackSpeed { get => maxAttackSpeed; set { maxAttackSpeed = value; if (_sequencer != null) _sequencer.Set.MaxAttackSpeed = value; } }

        /// <summary>Cancel table query for abilities (M08): can <paramref name="to"/> start now?</summary>
        public bool CanCancelInto(ActionKind to) => _sequencer == null ? to != ActionKind.None : _sequencer.CanCancelInto(to);

        /// <summary>Ability variant: pass the ability's Commit flag (Skill to Dodge).</summary>
        public bool CanCancelInto(ActionKind to, bool commit) => _sequencer == null ? to != ActionKind.None : _sequencer.CanCancelInto(to, commit);

        /// <summary>M08 took over: ends the running attack (call after <see cref="CanCancelInto(ActionKind)"/> returned true).</summary>
        public void CancelForAction()
        {
            if (_sequencer == null) return;
            _sequencer.Cancel(_lastFrame);
            ReleaseLocks();
        }

        /// <summary>Builds the pure moveset from the weapon data (basic chain, Heavy, Dash).</summary>
        public static AttackSetData BuildSet(WeaponDefinition weapon, float attackSpeedCap, bool startupOverride)
        {
            var set = new AttackSetData
            {
                Weight = weapon.WeightClass,
                MaxAttackSpeed = attackSpeedCap,
                StartupDodgeCancelOverride = startupOverride,
            };
            var chain = weapon.BasicChain;
            set.Basic = new AttackStep[chain.Count];
            for (int i = 0; i < chain.Count; i++) set.Basic[i] = chain[i].Step;
            if (weapon.Heavy != null) { set.HasHeavy = true; set.Heavy = weapon.Heavy.Step; }
            if (weapon.DashAttack != null) { set.HasDash = true; set.Dash = weapon.DashAttack.Step; }
            if (weapon.ComboResetSeconds > 0f)
                set.ComboResetFrames = Mathf.RoundToInt(weapon.ComboResetSeconds * SimulationFrame.TicksPerSecond);
            return set;
        }

        public void Initialize(TickScheduler scheduler, PlayerInputReader input, PlayerMotor motor, ICombatEntity entity,
            HitboxEvaluator evaluator, WeaponDefinition weapon, StateComponent states = null, DodgeController dodge = null)
        {
            Shutdown();
            _scheduler = scheduler;
            _input = input;
            _motor = motor;
            _entity = entity;
            _evaluator = evaluator;
            _states = states;
            _dodge = dodge;
            _weapon = weapon;
            _sequencer = new AttackSequencer(BuildSet(weapon, maxAttackSpeed, startupDodgeCancelOverride));
            _speedStat = weapon.WeightClass == WeaponWeightClass.Caster ? StatId.CastSpeed : StatId.AttackSpeed;

            bool ranged = weapon.WeightClass == WeaponWeightClass.Ranged || weapon.WeightClass == WeaponWeightClass.Caster;
            TagMask baseTags = new TagMask(Tag.Attack).With(ranged ? Tag.Ranged : Tag.Melee);
            _basicTags = baseTags.With(Tag.Basic);
            _heavyTags = baseTags.With(Tag.Heavy);

            _scheduler.Register(this, tickPriority);
            _registered = true;
        }

        private void OnDisable() => Shutdown();

        private void Shutdown()
        {
            if (_registered && _scheduler != null) _scheduler.Unregister(this);
            _registered = false;
            ReleaseLocks();
        }

        public void Tick(in SimulationFrame frame)
        {
            if (_sequencer == null || _entity == null) return;
            if (!_entity.Clock.Advance()) return; // hit-stop: frozen, nothing scales and nothing advances

            int f = _lastFrame = frame.Index;
            var ext = BuildExternal(f);
            float speed = (1f + _entity.Stats.Evaluate(_speedStat)) * (_states != null ? _states.AttackSpeedMultiplier(f) : 1f);

            _sequencer.Tick(f, _input.Buffer, _input.IsHeld(InputActionId.Basic), _input.IsHeld(InputActionId.Heavy), speed, in ext);
            _states?.SetAttacking(_sequencer.IsAttacking);

            if (_sequencer.StartedThisTick)
            {
                _evaluator.Registry.Clear(); // new use of an attack: fresh hit memory (06 3.9)
                PlanLunge();
            }

            if (!_sequencer.IsAttacking)
            {
                ReleaseLocks();
                _lunge = default;
                _hasPrev = false;
                return;
            }

            ApplyLocks();
            StepLunge();

            Vector3 origin = _motor.SimPosition;
            Vector3 forward = _motor.Facing;
            if (_sequencer.State.Phase == AttackPhase.Active)
            {
                var step = _sequencer.State.Step;
                var request = new HitboxRequest
                {
                    Attacker = _entity,
                    PrevOrigin = _hasPrev ? _prevOrigin : origin,
                    PrevForward = _hasPrev ? _prevForward : forward,
                    Origin = origin,
                    Forward = forward,
                    Step = step,
                    AttackSpeed = _sequencer.State.AttackSpeed,
                    ChargeFraction = _sequencer.State.ChargeFraction,
                    Tags = _sequencer.State.Kind == AttackKind.Heavy ? _heavyTags : _basicTags,
                    Frame = f,
                    ForceSweep = _lunge.HasLunge,
                };
                _evaluator.Evaluate(in request);
            }
            _prevOrigin = origin;
            _prevForward = forward;
            _hasPrev = true;
        }

        private AttackExternalState BuildExternal(int f)
        {
            var ext = AttackExternalState.Free;
            if (_states != null)
            {
                ext.CanAct = _states.CanAct(f);
                ext.CanDodge = _states.CanDodge(f) && _states.CanDodgeCancelReaction(f);
            }
            if (_dodge != null)
            {
                ext.DodgeActive = _dodge.IsDodging;
                ext.DodgeFramesRemaining = _dodge.IsDodging ? _dodge.Config.totalFrames - _dodge.ActiveFrame + 1 : 0;
                ext.DodgeCanCancel = _dodge.CanCancel;
                ext.CanDodge &= _dodge.Charges > 0;
            }
            return ext;
        }

        private void PlanLunge()
        {
            _lungeTicksDone = 0;
            _lunge = default;
            var state = _sequencer.State;
            AttackStep step = state.Step;
            if (step.LungeMaxMeters <= 0f || state.StartupTicks <= 0) return; // data without lunge (current Greatsword): nothing

            Vector3 origin = _motor.SimPosition;
            int n = UPhysics.OverlapSphereNonAlloc(origin, step.LungeMaxMeters + 2f, _overlaps, PhysicsLayers.PlayerAttackTargetsMask,
                QueryTriggerInteraction.Collide);
            int count = 0;
            for (int i = 0; i < n && count < LungeBuffer; i++)
            {
                if (!_overlaps[i].TryGetComponent(out Hurtbox hb) || hb.Entity == null || !hb.Entity.IsAlive) continue;
                _candidates[count++] = new LungeCandidate(hb.transform.position, hb.EffectiveRadius);
            }
            _lunge = LungeSolver.Plan(in step, origin, _motor.Facing, _candidates, count, bodyRadius, state.StartupTicks);
        }

        private void StepLunge()
        {
            if (!_lunge.HasLunge || _lungeTicksDone >= _lunge.Ticks || _sequencer.State.Phase != AttackPhase.Startup) return;
            _motor.TryMove(_lunge.StepPerTick);
            _lungeTicksDone++;
        }

        private void ApplyLocks()
        {
            var state = _sequencer.State;
            MovementLockKind kind = CancelRules.MovementFor(_sequencer.Set.Weight, state.Kind);
            _motor.Lock = kind == MovementLockKind.Reduced ? MovementLock.Reduced : MovementLock.Locked;
            _motor.RotationLocked = state.RotationLocked;
            _locksApplied = true;
        }

        private void ReleaseLocks()
        {
            if (!_locksApplied || _motor == null) return;
            _motor.Lock = MovementLock.Free;
            if (!_motor.InputSuspended) _motor.RotationLocked = false;
            _locksApplied = false;
        }
    }
}
