using System;
using UnityEngine;

namespace Eclipse.AI.Spawning
{
    /// <summary>
    /// Test-only wave (M07 T8): a fixed composition and a cap of live units. The real Encounter Director is M15/M16, so this is
    /// not game content and carries no balance number; values are sandbox settings chosen when the asset is created.
    /// </summary>
    [CreateAssetMenu(menuName = "Eclipse/AI/Test Wave Definition", fileName = "TestWave")]
    public sealed class TestWaveDefinition : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("Enemy prefab (EnemyController on the root).")]
            public GameObject prefab;
            public int count;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [Tooltip("Spawns wait while this many of the spawner's enemies are alive.")]
        [SerializeField] private int maxAlive = 8;
        [Tooltip("Seconds between two consecutive spawns of the wave.")]
        [SerializeField] private float spawnIntervalSeconds = 0.5f;
        [Tooltip("Monster Level of the spawned enemies (rows of EnemyScaling).")]
        [SerializeField] private int monsterLevel = 1;

        public Entry[] Entries => entries;
        public int MaxAlive => maxAlive;
        public float SpawnIntervalSeconds => spawnIntervalSeconds;
        public int MonsterLevel => monsterLevel;

        public int TotalCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < entries.Length; i++) n += Mathf.Max(0, entries[i].count);
                return n;
            }
        }
    }
}
