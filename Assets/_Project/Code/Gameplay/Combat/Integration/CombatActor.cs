using Eclipse.Core.Stats;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Feedback;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Combat.Integration
{
    public enum ActorKind { Player = 0, Dummy = 1 }

    /// <summary>
    /// Thin scene shell of one combatant (M05 T10): builds <see cref="CombatEntity"/> + <see cref="StateComponent"/> +
    /// <see cref="StaggerComponent"/> + <see cref="TimedModifierSet"/> from <see cref="CombatRuntimeConfig"/>, binds the
    /// <see cref="Hurtbox"/> to a <see cref="CombatReceiver"/>, mirrors position/facing/Parry/dodge i-frames into the
    /// entity and ticks the pure parts. No rule lives here. Tick order: this (-250) before motor (-100) and attack (-95);
    /// post-tick (-80) after the dodge (-90) so the dodge's i-frame window can be mirrored the same tick it starts.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class CombatActor : MonoBehaviour, IFixedTickable, IImpactVisual
    {
        public const int PreTickPriority = -250;
        public const int PostTickPriority = -80;

        private sealed class Post : IFixedTickable
        {
            private readonly CombatActor _a;
            public Post(CombatActor a) { _a = a; }
            public void Tick(in SimulationFrame frame) => _a.PostTick(frame.Index);
        }

        [SerializeField] private ActorKind kind;
        [Tooltip("Stagger bar tier (06 3.8.2). None = Light (flinch only).")]
        [SerializeField] private StaggerTier staggerTier = StaggerTier.None;
        [SerializeField] private bool isBoss;
        [SerializeField] private bool infiniteHealth;
        [SerializeField] private Hurtbox hurtbox;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private DodgeController dodge;
        [SerializeField] private ImpactFlash flash;
        [SerializeField] private SquashEffect squash;

        private CombatWorld _world;
        private Post _post;
        private IMassBody _mass;
        private AttackController _attack;
        private bool _registered;
        private int _lastHp = -1, _lastStagger = -1;
        private StaggerState _lastState = (StaggerState)(-1);
        private string _debugLine = string.Empty;

        public bool IsPlayer => kind == ActorKind.Player;
        public bool IsBoss => isBoss;
        public bool InfiniteHealth { get => infiniteHealth; set => infiniteHealth = value; }
        public StaggerTier Tier { get => staggerTier; set => staggerTier = value; }
        public CombatEntity Entity { get; private set; }
        public StateComponent States { get; private set; }
        public StaggerComponent Stagger { get; private set; }
        public TimedModifierSet Timed { get; private set; }
        public CombatReceiver Receiver { get; private set; }
        public AetherFlask Flask { get; private set; }
        public Hurtbox Hurtbox => hurtbox;
        public CombatWorld World => _world;
        /// <summary>Target Armor% when this actor is hit by the player (01 3.4.2). Players mitigate through the incoming pipeline instead.</summary>
        public float ArmorPercent { get; set; }
        public string DebugLine => _debugLine;
        /// <summary>Sum of all damage received (sandbox read-out; also lets infinite-health dummies be measured).</summary>
        public float TotalDamageTaken { get; set; }

        private void Start()
        {
            _world = CombatWorld.Current;
            if (_world == null || _world.Events == null)
            {
                Debug.LogError("CombatActor: CombatWorld missing in the scene (add CombatWorld.prefab).", this);
                enabled = false;
                return;
            }

            var cfg = _world.Config;
            _mass = GetComponent<IMassBody>();
            var stats = new StatBlock(new StatRuleTable());
            if (IsPlayer)
            {
                stats.SetBase(StatId.MaximumHealth, cfg.kaelMaxHealth);
                stats.SetBase(StatId.Armor, cfg.kaelArmor);
                stats.SetBase(StatId.CriticalChance, cfg.kaelCriticalChance);
                stats.SetBase(StatId.CriticalDamage, cfg.kaelCriticalDamage);
            }
            else
            {
                stats.SetBase(StatId.MaximumHealth, cfg.dummyMaxHealth);
                ArmorPercent = cfg.dummyArmorPercent;
            }

            EntityId id = _world.AllocateId();
            Entity = new CombatEntity(id, IsPlayer ? Team.Player : Team.Enemy, stats, cfg.BuildEntityConfig(), _world.Events, _world.Rng)
            {
                IsElite = _mass != null && _mass.IsElite,
                IncomingAttackerLevel = cfg.attackerLevel,
            };
            States = new StateComponent(_world.Rules, id, IsPlayer, _mass, _world.Events, isBoss) { Invulnerability = Entity.Invulnerability };
            if (!IsPlayer)
            {
                Stagger = new StaggerComponent(_world.Rules, id, staggerTier, Entity.IsElite,
                    _world.Rules.DifficultyMultiplier(cfg.difficulty), _world.Events, States);
                States.SetStagger(Stagger);
            }
            Timed = new TimedModifierSet(stats);
            Receiver = new CombatReceiver(this, _world);
            if (hurtbox != null) hurtbox.Bind(Entity, Receiver);
            else Debug.LogError("CombatActor: Hurtbox reference missing.", this);
            if (IsPlayer) Flask = new AetherFlask(cfg.BuildFlask(), Entity);

            var scheduler = _world.Services.Scheduler;
            _post = new Post(this);
            scheduler.Register(this, PreTickPriority);
            scheduler.Register(_post, PostTickPriority);
            if (flash != null) flash.Bind(scheduler);
            _world.Register(this);
            _registered = true;
            SyncPose();
        }

        private void OnDestroy()
        {
            if (!_registered) return;
            _registered = false;
            if (_world != null)
            {
                _world.Services?.Scheduler.Unregister(this);
                _world.Services?.Scheduler.Unregister(_post);
                _world.Unregister(this);
            }
        }

        private void SyncPose()
        {
            if (IsPlayer && motor != null)
            {
                Entity.Position = motor.SimPosition;
                Entity.Facing = motor.Facing;
            }
            else
            {
                Entity.Position = transform.position;
                Entity.Facing = transform.forward;
            }
        }

        public void Tick(in SimulationFrame frame)
        {
            int f = frame.Index;
            SyncPose();
            Entity.Tick(in frame);
            States.Tick(f);
            Stagger?.Tick(f);
            Timed.Tick(in frame);
        }

        private void PostTick(int f)
        {
            // 06 3.8.3: the Parry window is owned by the state component; the incoming pipeline reads it from the entity.
            Entity.ParryActiveUntilFrame = States.ParryEndFrame;

            if (IsPlayer)
            {
                if (dodge != null && dodge.IsDodging && dodge.ActiveFrame == 1 && dodge.Config != null)
                {
                    DodgeConfig c = dodge.Config;
                    Entity.Invulnerability.AddWindow(DodgeState.InvulnerableStart(c, f), DodgeState.InvulnerableEnd(c, f),
                        c.perfectDodgeWindowFrames, DodgeState.InvulnerabilitySourceId);
                }
                UpdateFlask(f);
            }
            UpdateDebugLine();
        }

        private void UpdateFlask(int f)
        {
            if (Flask == null) return;
            if (_attack == null) _attack = GetComponent<AttackController>();
            Flask.HeavyOrUltimateActive = _attack != null && _attack.IsAttacking && _attack.Sequencer.CurrentAction == ActionKind.Heavy;
            var buffer = _world.Services.InputReader.Buffer;
            if (buffer.TryConsume(f, InputActionId.Flask.Bit(), out _)) Flask.TryUse(f);
            Flask.Tick();
        }

        private void UpdateDebugLine()
        {
            int hp = Mathf.CeilToInt(Entity.Health.Current);
            int stg = Stagger != null ? Mathf.RoundToInt(Stagger.StaggerCurrent) : 0;
            StaggerState st = Stagger != null ? Stagger.State : StaggerState.Normal;
            if (hp == _lastHp && stg == _lastStagger && st == _lastState) return;
            _lastHp = hp;
            _lastStagger = stg;
            _lastState = st;
            _debugLine = Stagger != null
                ? string.Concat(_mass != null ? _mass.MassClass.ToString() : "?", "  HP ", hp.ToString(), infiniteHealth ? " (inf)" : string.Empty,
                    "\nSTG ", stg.ToString(), "/", Mathf.RoundToInt(Stagger.StaggerMax).ToString(), "  ", st.ToString())
                : string.Concat("HP ", hp.ToString());
        }

        // IImpactVisual
        public void Flash(int frames) { if (flash != null) flash.Flash(frames); }
        public void Squash(float seconds) { if (squash != null) squash.Squash(seconds); }
    }
}
