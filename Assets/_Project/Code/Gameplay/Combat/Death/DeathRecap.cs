using Eclipse.Core.Damage;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>One landed hit in the Death Recap.</summary>
    public struct RecapEntry
    {
        public int Frame;
        public EntityId Source;
        /// <summary>Dominant damage type of the hit.</summary>
        public DamageType Type;
        /// <summary>Damage entering the pipeline, before defenses.</summary>
        public float Incoming;
        /// <summary>Damage that reached Shield + Health.</summary>
        public float Final;
        /// <summary>Mitigation applied (Incoming - Final).</summary>
        public float Mitigated;
        public bool Blocked;
        /// <summary>Bitmask of active statuses at the moment (filled by the status system, M11).</summary>
        public ulong ActiveStatuses;
    }

    /// <summary>Ring of the last <see cref="Capacity"/> hits received (06 3.14). Fixed storage, no allocation.</summary>
    public sealed class DeathRecap
    {
        /// <summary>Last 5 hits (06 3.14).</summary>
        public const int Capacity = 5;

        private readonly RecapEntry[] _e = new RecapEntry[Capacity];
        private int _next;

        public int Count { get; private set; }

        public void Record(in RecapEntry entry)
        {
            _e[_next] = entry;
            _next = (_next + 1) % Capacity;
            if (Count < Capacity) Count++;
        }

        /// <summary>Entry by age: 0 = oldest kept, Count-1 = most recent.</summary>
        public RecapEntry Get(int index)
        {
            if ((uint)index >= (uint)Count) throw new System.ArgumentOutOfRangeException(nameof(index));
            int start = Count < Capacity ? 0 : _next;
            return _e[(start + index) % Capacity];
        }

        public void Clear() { Count = 0; _next = 0; }
    }
}
