using Eclipse.AI.Enemies;
using Eclipse.Gameplay.Pooling;
using UnityEngine;

namespace Eclipse.AI.Spawning
{
    /// <summary>
    /// Sandbox spawner (M07 T8): spawns enemies from a <see cref="GameObjectPool"/> per prefab at <see cref="SpawnPoint"/>s, either
    /// one at a time (debug keys) or as a <see cref="TestWaveDefinition"/> queue that respects the live cap. Pools are warmed up
    /// before the first spawn (<see cref="Warmup"/>); afterwards no Instantiate happens. Not the Encounter Director (M15/M16).
    /// </summary>
    public sealed class SimpleWaveSpawner : MonoBehaviour
    {
        [SerializeField] private SpawnPoint[] points = new SpawnPoint[0];
        [SerializeField] private int defaultMonsterLevel = 1;
        [Tooltip("Pool prewarm per prefab (covers a few debug spawns; waves warm up their own count).")]
        [SerializeField] private int prewarmPerPrefab = 4;

        private EnemyServices _services;
        private PoolService _pool;
        private TestWaveDefinition _wave;
        private GameObject[] _queue = new GameObject[0];
        private int _queueHead, _queueCount;
        private float _nextSpawnAt;
        private int _alive;
        private int _spawnCounter;
        private System.Action<EnemyController> _onDespawning;

        public int Alive => _alive;
        public int TotalSpawned { get; private set; }
        public int PendingInWave => _queueCount - _queueHead;
        public SpawnPoint[] Points { get => points; set => points = value; }

        /// <summary>Raised after an enemy is placed and initialised.</summary>
        public event System.Action<EnemyController> Spawned;

        public void Initialize(EnemyServices services, PoolService pool)
        {
            _services = services;
            _pool = pool;
            _onDespawning = OnDespawning;
        }

        /// <summary>Creates the pool of a prefab ahead of time (the only place that instantiates).</summary>
        public void Warmup(GameObject prefab, int count = -1)
        {
            if (prefab == null || _pool == null) return;
            _pool.ForPrefab(prefab, new PoolConfig(count >= 0 ? count : prewarmPerPrefab));
        }

        /// <summary>Spawns one enemy now at the next spawn point. Returns null when not initialised.</summary>
        public EnemyController SpawnOne(GameObject prefab, int monsterLevel = -1)
        {
            if (_services == null || prefab == null || points == null || points.Length == 0) return null;
            SpawnPoint sp = points[_spawnCounter % points.Length];
            Vector3 pos = sp.PointFor(_spawnCounter / points.Length);
            _spawnCounter++;
            return SpawnAt(prefab, pos, monsterLevel);
        }

        public EnemyController SpawnAt(GameObject prefab, Vector3 position, int monsterLevel = -1)
        {
            if (_services == null || prefab == null) return null;
            GameObjectPool pool = _pool.ForPrefab(prefab, new PoolConfig(prewarmPerPrefab));
            Quaternion facing = Quaternion.identity;
            var player = _services.World.Player;
            if (player != null && player.Entity != null)
            {
                Vector3 d = player.Entity.Position - position;
                d.y = 0f;
                if (d.sqrMagnitude > 1e-4f) facing = Quaternion.LookRotation(d, Vector3.up);
            }
            GameObject go = pool.Get(position, facing);
            var controller = go.GetComponent<EnemyController>();
            controller.Despawning -= _onDespawning;
            controller.Despawning += _onDespawning;
            controller.Initialize(_services, monsterLevel > 0 ? monsterLevel : defaultMonsterLevel, pool);
            _alive++;
            TotalSpawned++;
            Spawned?.Invoke(controller);
            return controller;
        }

        /// <summary>Queues the whole wave; spawns happen in Update, one per interval, while alive is below the cap.</summary>
        public void StartWave(TestWaveDefinition wave)
        {
            if (wave == null || _services == null) return;
            _wave = wave;
            int total = wave.TotalCount;
            if (_queue.Length < total) _queue = new GameObject[total];
            _queueHead = 0;
            _queueCount = 0;
            for (int e = 0; e < wave.Entries.Length; e++)
            {
                TestWaveDefinition.Entry entry = wave.Entries[e];
                Warmup(entry.prefab, Mathf.Max(prewarmPerPrefab, entry.count));
                for (int i = 0; i < entry.count; i++) _queue[_queueCount++] = entry.prefab;
            }
            _nextSpawnAt = Time.time;
        }

        private void Update()
        {
            if (_wave == null || _queueHead >= _queueCount) return;
            if (Time.time < _nextSpawnAt || _alive >= _wave.MaxAlive) return;
            SpawnOne(_queue[_queueHead++], _wave.MonsterLevel);
            _nextSpawnAt = Time.time + _wave.SpawnIntervalSeconds;
        }

        private void OnDespawning(EnemyController c)
        {
            if (_alive > 0) _alive--;
        }
    }
}
