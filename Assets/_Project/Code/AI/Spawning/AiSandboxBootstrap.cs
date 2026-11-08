using Eclipse.AI.Director;
using Eclipse.AI.Enemies;
using Eclipse.AI.Locomotion;
using Eclipse.AI.Lod;
using Eclipse.AI.Telegraphs;
using Eclipse.Data.Difficulties;
using Eclipse.Data.Enemies;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Pooling;
using Eclipse.Gameplay.Simulation;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.AI.Spawning
{
    /// <summary>
    /// Composition root of the AI Sandbox scene (M07 T8): thin MonoBehaviour that builds the scene services the enemies need
    /// (tokens, telegraph pool, projectile and area services, status pools, pools) on top of <see cref="CombatWorld"/> and the
    /// <see cref="LocomotionSystem"/>, hands them to <see cref="EnemyServices"/> and ticks the plain-C# ones through the
    /// simulation scheduler. Also resolves the player's provisional <see cref="StatusStub"/> for the status pools
    /// (<see cref="IStatusDirectory"/>; M11 replaces it). No rule lives here.
    /// Tick order (scheduler priority): enemies -70, projectiles -50, status pools -45, status stub -40.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    [RequireComponent(typeof(LocomotionSystem))]
    public sealed class AiSandboxBootstrap : MonoBehaviour, IStatusDirectory
    {
        public const int ProjectilePriority = -50;
        public const int StatusZonePriority = -45;
        public const int StatusStubPriority = -40;

        [SerializeField] private EnemyScalingDefinition scaling;
        [SerializeField] private DifficultyDefinition difficulty;
        [SerializeField] private TelegraphPalette palette;
        [SerializeField] private Shader telegraphShader;
        [SerializeField] private SimpleWaveSpawner spawner;
        [SerializeField] private TestWaveDefinition wave;
        [Tooltip("Debug-key order: 1 Cinder Husk, 2 Ashborn Marauder, 3 Slag Hurler, 4 Kiln Brute.")]
        [SerializeField] private GameObject[] enemyPrefabs = new GameObject[0];
        [SerializeField] private int telegraphPoolSize = 32;
        [SerializeField] private int projectileCapacity = 512;
        [Tooltip("SANDBOX: distance within which an off-screen enemy still decides at the near rate. Set from data when the Encounter Director lands.")]
        [SerializeField] private float nearRadius = 20f;
        [Tooltip("SANDBOX: half size of the XZ box scanned for Wall-layer colliders.")]
        [SerializeField] private float arenaHalfExtent = 20f;
        [SerializeField] private bool spawnWaveOnStart;

        private CombatWorld _world;
        private LocomotionSystem _locomotion;
        private TelegraphViewPool _telegraphs;
        private ProjectileManager _projectiles;
        private EnemyStatusZones _zones;
        private StatusStub _playerStatus;
        private bool _stubRegistered;

        public bool IsReady { get; private set; }
        public EnemyServices Services { get; private set; }
        public SimpleWaveSpawner Spawner => spawner;
        public ProjectileManager Projectiles => _projectiles;
        public EnemyStatusZones StatusZones => _zones;
        public GameObject[] EnemyPrefabs => enemyPrefabs;

        private void Start()
        {
            _world = CombatWorld.Current;
            if (_world == null || _world.Services == null || _world.Events == null)
            {
                Debug.LogError("AiSandboxBootstrap: CombatWorld (with GameplayServices) missing from the scene.", this);
                enabled = false;
                return;
            }
            if (scaling == null || difficulty == null || palette == null || telegraphShader == null || spawner == null)
            {
                Debug.LogError("AiSandboxBootstrap: scaling, difficulty, palette, telegraph shader or spawner not assigned.", this);
                enabled = false;
                return;
            }

            _locomotion = GetComponent<LocomotionSystem>();
            _locomotion.CollectWalls(new Bounds(Vector3.zero, new Vector3(arenaHalfExtent * 2f, 20f, arenaHalfExtent * 2f)));

            var tokens = new AttackTokenDirector(TokenPolicy.From(difficulty));
            var lod = new DecisionLodConfig { NearRadius = nearRadius };
            Transform pools = new GameObject("Pools").transform;
            pools.SetParent(transform, false);
            var pool = new PoolService(pools);
            Transform telegraphRoot = new GameObject("Telegraphs").transform;
            telegraphRoot.SetParent(transform, false);
            _telegraphs = new TelegraphViewPool(telegraphShader, telegraphPoolSize, telegraphRoot);

            _projectiles = new ProjectileManager(projectileCapacity, _world.Events, new PhysicsProjectileTargetSearch());
            var areas = new AreaDeliverer(new PhysicsAreaQuery(), _world.Events);

            Services = new EnemyServices(_world, _locomotion, tokens, pool, _telegraphs, palette, _projectiles, areas, scaling, difficulty, lod)
            {
                Camera = Camera.main,
            };
            _projectiles.Listener = new EnemyProjectileLandings(Services);

            // 07 section 4 / R19: zone statuses tick every 0.5 s (half the tick rate, 30 ticks).
            _zones = new EnemyStatusZones(areas, this, SimulationFrame.TicksPerSecond / 2);
            Services.StatusZones = _zones;

            TickScheduler scheduler = _world.Services.Scheduler;
            scheduler.Register(_projectiles, ProjectilePriority);
            scheduler.Register(_zones, StatusZonePriority);

            spawner.Initialize(Services, pool);
            for (int i = 0; i < enemyPrefabs.Length; i++) spawner.Warmup(enemyPrefabs[i]);
            if (wave != null)
                for (int e = 0; e < wave.Entries.Length; e++) spawner.Warmup(wave.Entries[e].prefab, wave.Entries[e].count);

            IsReady = true;
            if (spawnWaveOnStart) SpawnWave();
        }

        /// <summary>Spawns one enemy of the debug list (0 Husk, 1 Marauder, 2 Hurler, 3 Brute).</summary>
        public EnemyController SpawnEnemy(int index)
        {
            if (!IsReady || (uint)index >= (uint)enemyPrefabs.Length) return null;
            return spawner.SpawnOne(enemyPrefabs[index]);
        }

        public void SpawnWave()
        {
            if (IsReady && wave != null) spawner.StartWave(wave);
        }

        // ------------------------------------------------------------------ IStatusDirectory

        public bool TryGetStatusReceiver(EntityId id, out IStatusReceiver receiver)
        {
            receiver = null;
            CombatActor player = _world != null ? _world.Player : null;
            if (player == null || player.Entity == null || player.Entity.Id.Value != id.Value) return false;
            if (_playerStatus == null)
            {
                _playerStatus = new StatusStub(player.Entity.Id, new[] { StatusStubDefinition.BurningProvisional() }, _world.Events);
                if (!_stubRegistered)
                {
                    _world.Services.Scheduler.Register(_playerStatus, StatusStubPriority);
                    _stubRegistered = true;
                }
            }
            receiver = _playerStatus;
            return true;
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Services != null)
            {
                TickScheduler scheduler = _world.Services.Scheduler;
                if (_projectiles != null) scheduler.Unregister(_projectiles);
                if (_zones != null) scheduler.Unregister(_zones);
                if (_stubRegistered) scheduler.Unregister(_playerStatus);
            }
            _projectiles?.Dispose();
            _projectiles = null;
            if (_telegraphs != null) _telegraphs.Destroy();
            _telegraphs = null;
        }
    }
}
