using System;
using Eclipse.AI.Director;
using Eclipse.AI.Locomotion;
using Eclipse.AI.StateMachine;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Core.Stats;
using Eclipse.Data.Enemies;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Pooling;
using Eclipse.Gameplay.Simulation;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.AI.Enemies
{
    /// <summary>
    /// Thin scene shell of a common enemy (M07 T6): joins the brain (<see cref="EnemyBrainBinder"/>), the locomotion agent, the attack
    /// tokens, the telegraph and the M05 combat components of the sibling <see cref="CombatActor"/> (entity, states, Stagger bar).
    /// No rule lives here: stats come from <see cref="EnemyStatsBuilder"/>, behaviour from the binder and the archetype template,
    /// the frontal plate from <see cref="FrontalGuard"/>.
    ///
    /// Lifecycle: the spawner takes the object from a <see cref="GameObjectPool"/>, places it and calls <see cref="Initialize"/>.
    /// On death the entity publishes OnKill itself (CombatEntity); once the on-death attack is over the object returns to its pool.
    /// Reuse revives the same <see cref="CombatEntity"/> (see <see cref="PrepareEntity"/>). Needs <see cref="CombatWorld"/> in the
    /// scene; no <c>MassBodyMarker</c> on the prefab (this component is the <see cref="IMassBody"/>).
    /// </summary>
    [DefaultExecutionOrder(-600)]
    [RequireComponent(typeof(CombatActor))]
    public sealed class EnemyController : MonoBehaviour, IFixedTickable, IPoolable, IMassBody
    {
        /// <summary>After the actor pre-tick (-250), attack/dodge (-95/-90) and post-tick (-80).</summary>
        public const int TickPriority = -70;

        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private CombatActor actor;
        [SerializeField] private Hurtbox hurtbox;
        [SerializeField] private TokenClass tokenClass = TokenClass.Normal;
        [SerializeField] private bool isElite;

        private EnemyServices _services;
        private GameObjectPool _ownerPool;
        private EnemyBlueprint _blueprint;
        private EnemyBrainBinder _binder;
        private EnemyAttackDelivery _delivery;
        private int _level = 1;
        private bool _pendingInit;
        private bool _ready;
        private bool _guardBound;
        private int _agent = -1;
        private int _ownerId;
        private bool _registeredTicks;
        private bool _lastTaunted;
        private float _lastStagger;
        private int _losEvery = 6;
        private bool _hasLos = true;
        private bool _shotBlocked;
        private int _allies;
        private bool _hasWard;
        private Vector3 _ward;
        private bool _deadHandled;
        private BinderOutput _output;

        public EnemyDefinition Definition => definition;
        public CombatActor Actor => actor;
        public CombatEntity Entity => actor != null ? actor.Entity : null;
        public EnemyBrainBinder Binder => _binder;
        public EnemyBlueprint Blueprint => _blueprint;
        public EnemyArchetype Archetype => definition != null ? definition.Archetype : EnemyArchetype.Standard;
        public bool IsReady => _ready;
        public EnemyState State => _binder != null ? _binder.State : EnemyState.Idle;
        public int MonsterLevel => _level;
        public float HurtboxRadius => hurtbox != null ? hurtbox.EffectiveRadius : 0.5f;

        /// <summary>Raised when the enemy is about to return to the pool (after death and any on-death attack).</summary>
        public event Action<EnemyController> Despawning;

        // IMassBody
        public MassClass MassClass => definition != null ? EnemyStatsBuilder.ToGameplay(definition.MassClass) : MassClass.Light;
        public bool IsElite { get => isElite; set => isElite = value; }

        private void Awake()
        {
            if (actor == null) actor = GetComponent<CombatActor>();
            if (hurtbox == null) hurtbox = GetComponentInChildren<Hurtbox>();
            // CombatActor.Start builds the StaggerComponent from this tier (06 section 3.8.2: Light has no bar).
            if (definition != null && actor != null) actor.Tier = EnemyStatsBuilder.TierFor(definition.MassClass);
        }

        private void Start()
        {
            if (_pendingInit) ApplyInit();
        }

        /// <summary>
        /// Prepares a freshly taken object: stats at the Monster Level, brain, locomotion agent, token registration. Call after
        /// positioning it. Safe to call before the first <c>Start</c> (the work is deferred until the actor exists).
        /// </summary>
        public void Initialize(EnemyServices services, int monsterLevel, GameObjectPool ownerPool = null)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _level = monsterLevel;
            _ownerPool = ownerPool;
            _pendingInit = true;
            if (actor != null && actor.Entity != null) ApplyInit();
        }

        private void ApplyInit()
        {
            _pendingInit = false;
            if (definition == null || actor.Entity == null)
            {
                Debug.LogError("EnemyController: EnemyDefinition or CombatActor entity missing.", this);
                enabled = false;
                return;
            }
            if (!_services.TryGetBlueprint(definition, _level, out EnemyBlueprint blueprint))
            {
                Debug.LogError("EnemyController: no scaling row for Monster Level " + _level + ".", this);
                enabled = false;
                return;
            }

            CombatEntity entity = actor.Entity;
            bool blueprintChanged = !ReferenceEquals(_blueprint, blueprint);
            _blueprint = blueprint;
            PrepareEntity(entity, blueprint.Stats);

            if (_delivery == null) _delivery = new EnemyAttackDelivery(_services, entity.Id, hurtbox != null ? hurtbox.Height * 0.5f : 1f);
            if (_binder == null || blueprintChanged)
            {
                _binder = new EnemyBrainBinder(blueprint, _services.Tokens, _services.World.Rng, entity.Id.Value, _services.Lod,
                    TokenOrbitConfig.Default, _delivery);
            }
            else
            {
                _binder.Reset();
            }

            BindFrontalGuard(entity);

            int perSecond = SimulationFrame.TicksPerSecond;
            float nearHz = _services.Lod.NearHz > 0f ? _services.Lod.NearHz : 1f;
            _losEvery = Mathf.Max(1, Mathf.RoundToInt(perSecond / nearHz));
            _lastStagger = 0f;
            _hasLos = true;
            _shotBlocked = false;
            _allies = 0;
            _hasWard = false;
            _deadHandled = false;
            _lastTaunted = false;

            _ownerId = entity.Id.Value;
            _agent = _services.Locomotion.Register(transform.position, HurtboxRadius, definition.MoveSpeed);
            if (!_services.Tokens.Register(entity.Id.Value, tokenClass))
                Debug.LogWarning("EnemyController: token table full; this enemy will never get a token.", this);
            _services.Active.Add(this);
            _services.World.Services.Scheduler.Register(this, TickPriority);
            _registeredTicks = true;
            _ready = true;
        }

        // Stats from data. A pooled entity that died is revived through the only public API of the frozen contract
        // (DeathHandler.TryRevive, capped per boss room: reset first), then health is set from the curve.
        private void PrepareEntity(CombatEntity entity, EnemyStats stats)
        {
            if (!entity.IsAlive)
            {
                entity.Death.ResetBossRoom();
                if (!entity.Death.TryRevive(entity.Health, entity.Invulnerability, entity.Id, entity.CurrentFrame))
                    Debug.LogError("EnemyController: could not revive the pooled entity for reuse.", this);
            }
            entity.Stats.SetBase(StatId.MaximumHealth, stats.MaxHealth);
            entity.SyncMaxHealth();
            entity.Health.Restore(1f);
            entity.IsElite = isElite;
            // The enemy data has no Armor% (only per-type resistance); the sandbox dummy default must not leak in.
            actor.ArmorPercent = 0f;
        }

        private void BindFrontalGuard(CombatEntity entity)
        {
            if (_guardBound || hurtbox == null) return;
            var guard = new FrontalGuard(definition.FrontPlateReduction, definition.FrontPlateArcDegrees);
            if (!guard.Enabled) return;
            hurtbox.Bind(entity, new FrontalGuardReceiver(hurtbox.Receiver, guard, () => entity.Facing));
            _guardBound = true;
        }

        // ------------------------------------------------------------------ IPoolable

        public void OnSpawned()
        {
            _ready = false;
            _deadHandled = false;
        }

        public void OnDespawned() => Teardown();

        private void Teardown()
        {
            if (_registeredTicks && _services != null)
            {
                _services.World.Services.Scheduler.Unregister(this);
                _services.Active.Remove(this);
                _services.Tokens.Unregister(_ownerId);
            }
            _registeredTicks = false;
            if (_agent >= 0 && _services != null) _services.Locomotion.Unregister(_agent);
            _agent = -1;
            if (_binder != null) _binder.CancelAttack();
            _ready = false;
        }

        private void ReturnToPool()
        {
            Despawning?.Invoke(this);
            Teardown();
            if (_ownerPool != null) _ownerPool.Release(gameObject);
            else gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ tick

        public void Tick(in SimulationFrame frame)
        {
            if (!_ready) return;
            CombatEntity entity = actor.Entity;
            if (!entity.Clock.Advance())
            {
                if (_agent >= 0) _services.Locomotion.SetSteering(_agent, SteeringMode.Hold, transform.position);
                return; // hit-stop (06 section 3.11)
            }

            int f = frame.Index;
            float dt = SimulationFrame.FixedDeltaSeconds;
            _services.Tokens.Now = f / (double)SimulationFrame.TicksPerSecond;

            bool dead = !entity.IsAlive;
            if (dead && !_deadHandled)
            {
                _deadHandled = true;
                _services.Locomotion.SetSteering(_agent, SteeringMode.Hold, transform.position);
            }
            if (!dead) ReadBackPosition();

            CombatActor player = _services.World.Player;
            bool hasTarget = !dead && player != null && player.Entity != null && player.Entity.IsAlive;
            Vector3 self = transform.position;
            Vector3 target = hasTarget ? player.Entity.Position : self;

            StaggerComponent stagger = actor.Stagger;
            bool staggered = stagger != null && stagger.IsStaggered;
            bool canAct = !dead && !staggered && actor.States.CanAct(f);
            bool canMove = canAct && actor.States.CanMove(f);
            ReactToStagger(stagger, canAct);

            bool taunted = actor.States.IsTaunted(f);
            if (taunted != _lastTaunted)
            {
                _lastTaunted = taunted;
                _services.Tokens.SetTaunted(entity.Id.Value, taunted);
            }

            if (hasTarget && (f + entity.Id.Value) % _losEvery == 0) RefreshAwareness(self, target);

            var input = new BinderInput
            {
                IsDead = dead,
                HasTarget = hasTarget,
                OnScreen = _services.IsOnScreen(self, f),
                CanAct = canAct,
                Self = self,
                Forward = entity.Facing,
                Target = target,
                HpFraction = entity.Health.Fraction,
                AlliesNearby = _allies,
                HasLineOfSight = _hasLos,
                ShotBlockedByAlly = _shotBlocked,
                HasEscortWard = _hasWard,
                EscortWard = _ward,
            };
            _binder.Tick(in input, dt, ref _output);

            if (dead)
            {
                if (_output.ReadyToDespawn) ReturnToPool();
                return;
            }

            Steer(f, canMove);
            Face(dt, canAct);
        }

        private void ReadBackPosition()
        {
            Vector3 p = _services.Locomotion.GetPosition(_agent, transform.position.y);
            transform.position = p;
        }

        // Stagger damage inside the cyan window cancels the wind-up and staggers the enemy (06 section 3.10); stun or a full bar
        // cancels whatever is running.
        private void ReactToStagger(StaggerComponent stagger, bool canAct)
        {
            if (!canAct)
            {
                if (_binder.Executor.IsBusy) _binder.CancelAttack();
                if (stagger != null) _lastStagger = stagger.StaggerCurrent;
                return;
            }
            if (stagger == null) return;
            float current = stagger.StaggerCurrent;
            if (current > _lastStagger && _binder.Executor.InInterruptWindow && _binder.TryInterrupt())
                stagger.ForceStaggered(EntityId.None, 0);
            _lastStagger = current;
        }

        private void Steer(int frame, bool canMove)
        {
            if (_agent < 0) return;
            var data = _services.Locomotion.Data;
            if (!canMove)
            {
                _services.Locomotion.SetSteering(_agent, SteeringMode.Hold, transform.position);
                return;
            }
            data.SetMaxSpeed(_agent, definition.MoveSpeed * actor.States.MoveSpeedMultiplier(frame));
            SteeringCommand c = _output.Steering;
            _services.Locomotion.SetSteering(_agent, c.Mode, c.Target, c.OrbitRadius, c.Clockwise);
        }

        private void Face(float dt, bool canAct)
        {
            Vector3 dir = _output.FaceDirection;
            bool snap = _output.AttackStarted >= 0 && _binder.Executor.IsBusy; // the aim of the telegraph is the facing
            if (!canAct || (!_output.FaceTarget && !snap) || dir.sqrMagnitude < 1e-6f) return;
            Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
            float rate = snap ? 360f : _blueprint.Template.TurnDegreesPerSecond * dt;
            transform.rotation = snap ? want : Quaternion.RotateTowards(transform.rotation, want, rate);
        }

        // Allies nearby, escort ward, wall line of sight and Tank-in-the-way, refreshed at the decision rate (staggered per enemy).
        private void RefreshAwareness(Vector3 self, Vector3 target)
        {
            var list = _services.Active;
            ArchetypeTemplate template = _blueprint.Template;
            float r2 = template.AlliesRadius * template.AlliesRadius;
            int allies = 0;
            bool ward = false;
            float wardDist = float.MaxValue;
            bool blocked = false;
            bool needsShot = false;
            for (int i = 0; i < _blueprint.Heads.Length; i++)
                if (_blueprint.Heads[i].IsStraightProjectile) { needsShot = true; break; }

            for (int i = 0; i < list.Count; i++)
            {
                EnemyController other = list[i];
                if (other == this || !other._ready || !other.actor.Entity.IsAlive) continue;
                Vector3 p = other.transform.position;
                float dx = p.x - self.x, dz = p.z - self.z;
                float d2 = dx * dx + dz * dz;
                if (d2 <= r2) allies++;
                EnemyArchetype a = other.Archetype;
                if (template.Style == SteeringStyle.Escort && (a == EnemyArchetype.Caster || a == EnemyArchetype.Support) && d2 < wardDist)
                {
                    wardDist = d2;
                    _ward = p;
                    ward = true;
                }
                if (needsShot && !blocked && a == EnemyArchetype.Tank
                    && ShotLine.SegmentHitsCircle(self, target, p, other.HurtboxRadius))
                    blocked = true;
            }
            _allies = allies;
            _hasWard = ward;
            _shotBlocked = blocked;

            bool needsLos = false;
            for (int i = 0; i < _blueprint.Heads.Length; i++)
                if (_blueprint.Heads[i].IsProjectileLike) { needsLos = true; break; }
            if (needsLos)
            {
                float eye = hurtbox != null ? hurtbox.Height * 0.5f : 1f;
                _hasLos = !LineOfSight.IsBlocked(self + Vector3.up * eye, target + Vector3.up * eye);
            }
        }

        private void OnDestroy()
        {
            if (_registeredTicks && _services != null && _services.World != null) Teardown();
        }
    }
}
