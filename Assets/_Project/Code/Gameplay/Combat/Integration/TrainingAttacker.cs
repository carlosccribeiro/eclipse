using Eclipse.Core.Tags;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Combat.Integration
{
    /// <summary>
    /// Sandbox test attacker (M05 T10): every <c>testAttackIntervalSeconds</c> it winds up a red telegraphed strike
    /// (06 3.10: decal fills from the centre to the edge, impact when it touches the border) and then evaluates a sector
    /// hit through the same <see cref="HitboxEvaluator"/> as the player. Lets the user test dodge, Parry (P) and the grace
    /// period. Waits while Staggered / hard-CC'd, respects hit-stop (its own <see cref="EntityClock"/>). Tuning comes
    /// from <see cref="CombatRuntimeConfig"/> (SANDBOX fields).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class TrainingAttacker : MonoBehaviour, IFixedTickable
    {
        public const int TickPriority = -60;

        private enum Phase : byte { Idle, Startup, Active, Recovery }

        [SerializeField] private CombatActor actor;
        [SerializeField] private bool attackEnabled = true;
        [Tooltip("Flat disc scaled from the centre to the radius during the wind-up.")]
        [SerializeField] private Transform telegraphFill;
        [Tooltip("Flat disc at the full radius (the decal border).")]
        [SerializeField] private Transform telegraphOuter;

        private HitboxEvaluator _evaluator;
        private AttackStep _step;
        private TagMask _tags;
        private Phase _phase;
        private int _timer;
        private int _startupFrames, _idleFrames;
        private bool _ready;

        public bool AttackEnabled { get => attackEnabled; set => attackEnabled = value; }
        public bool IsWindingUp => _phase == Phase.Startup;
        public int StrikesStarted { get; private set; }

        private void Start()
        {
            var world = CombatWorld.Current;
            if (world == null || actor == null || actor.Entity == null)
            {
                enabled = false;
                return;
            }
            var cfg = world.Config;
            _startupFrames = Mathf.Max(1, Mathf.CeilToInt(cfg.testAttackWindupSeconds * SimulationFrame.TicksPerSecond));
            int cycle = Mathf.RoundToInt(cfg.testAttackIntervalSeconds * SimulationFrame.TicksPerSecond);
            _idleFrames = Mathf.Max(1, cycle - _startupFrames - cfg.testAttackActiveFrames - cfg.testAttackRecoveryFrames);
            _step = new AttackStep(_startupFrames, cfg.testAttackActiveFrames, cfg.testAttackRecoveryFrames, AttackHitShape.Sector,
                cfg.testAttackRadius, cfg.testAttackArcDegrees, 0f, 0f, Vector3.zero, 0f, 0f, 0, 1f, 1f, 1f, 0f, 0f, 0f, 1, 0, false, false, 0, 0);
            _tags = new TagMask(Tag.Attack).With(Tag.Melee);
            _evaluator = new HitboxEvaluator(new FractionHitResolver(world, cfg.testAttackHealthFraction));
            SetTelegraph(false, 0f, cfg.testAttackRadius);
            world.Services.Scheduler.Register(this, TickPriority);
            _ready = true;
        }

        private void OnDestroy()
        {
            var world = CombatWorld.Current;
            if (_ready && world != null) world.Services.Scheduler.Unregister(this);
        }

        public void Tick(in SimulationFrame frame)
        {
            var entity = actor.Entity;
            if (!entity.IsAlive) { Cancel(); return; }
            if (!entity.Clock.Advance()) return; // hit-stop
            int f = frame.Index;

            bool blocked = (actor.Stagger != null && actor.Stagger.IsStaggered) || !actor.States.CanAct(f);
            if (!attackEnabled || blocked)
            {
                if (_phase != Phase.Idle) Cancel();
                return;
            }

            var world = actor.World;
            var player = world.Player;
            switch (_phase)
            {
                case Phase.Idle:
                    if (++_timer < _idleFrames || player == null) return;
                    _phase = Phase.Startup;
                    _timer = 0;
                    StrikesStarted++;
                    _evaluator.Registry.Clear();
                    actor.States.SetAttacking(true);
                    break;
                case Phase.Startup:
                    Face(player);
                    SetTelegraph(true, (float)(_timer + 1) / _startupFrames, world.Config.testAttackRadius);
                    if (++_timer >= _startupFrames) { _phase = Phase.Active; _timer = 0; SetTelegraph(true, 1f, world.Config.testAttackRadius); }
                    break;
                case Phase.Active:
                    Strike(f);
                    if (++_timer >= _step.ActiveFrames) { _phase = Phase.Recovery; _timer = 0; SetTelegraph(false, 0f, world.Config.testAttackRadius); }
                    break;
                default:
                    if (++_timer >= _step.RecoveryFrames) { _phase = Phase.Idle; _timer = 0; actor.States.SetAttacking(false); }
                    break;
            }
        }

        private void Face(CombatActor player)
        {
            Vector3 d = player.Entity.Position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        private void Strike(int frame)
        {
            var request = new HitboxRequest
            {
                Attacker = actor.Entity,
                PrevOrigin = actor.Entity.Position,
                PrevForward = transform.forward,
                Origin = actor.Entity.Position,
                Forward = transform.forward,
                Step = _step,
                AttackSpeed = 1f,
                ChargeFraction = 0f,
                Tags = _tags,
                Frame = frame,
            };
            _evaluator.Evaluate(in request);
        }

        private void Cancel()
        {
            _phase = Phase.Idle;
            _timer = 0;
            if (actor.States != null) actor.States.SetAttacking(false);
            SetTelegraph(false, 0f, 0f);
        }

        private void SetTelegraph(bool visible, float fill, float radius)
        {
            if (telegraphOuter != null)
            {
                telegraphOuter.gameObject.SetActive(visible);
                telegraphOuter.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            }
            if (telegraphFill != null)
            {
                telegraphFill.gameObject.SetActive(visible);
                float d = radius * 2f * Mathf.Clamp01(fill);
                telegraphFill.localScale = new Vector3(d, 0.02f, d);
            }
        }
    }
}
