using System;
using Eclipse.Data.Definitions;
using Eclipse.Data.Difficulties;
using UnityEngine;

namespace Eclipse.Data.Enemies
{
    /// <summary>One row of DataSource/EnemyScaling.csv: the Standard enemy at a Monster Level (06 section 3.13, R53).</summary>
    [Serializable]
    public struct EnemyScalingEntry
    {
        public int level;
        public DifficultyTier tier;
        public float stdHp;
        public float stdDamage;
    }

    /// <summary>
    /// Monster Level curve of Standard enemies (HP and damage per level, tier letality already included in damage).
    /// A single asset holds the whole table; the importer sorts rows by level.
    /// </summary>
    public sealed class EnemyScalingDefinition : Definition
    {
        [SerializeField] private EnemyScalingEntry[] entries = Array.Empty<EnemyScalingEntry>();

        public int Count => entries.Length;
        public EnemyScalingEntry this[int index] => entries[index];

        /// <summary>Finds the row of a Monster Level (binary search; rows are sorted by level).</summary>
        public bool TryGet(int level, out EnemyScalingEntry entry)
        {
            int lo = 0, hi = entries.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int l = entries[mid].level;
                if (l == level) { entry = entries[mid]; return true; }
                if (l < level) lo = mid + 1; else hi = mid - 1;
            }
            entry = default;
            return false;
        }

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSetEntries(EnemyScalingEntry[] value) => entries = value;
#endif
    }
}
