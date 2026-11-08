using System;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Tracks which targets a use of an attack already hit, per Hit Group (06 3.9): one hit per target per group,
    /// or a re-hit after an interval in frames. Call <see cref="Clear"/> when a new use/step starts. Allocation-free
    /// after the initial capacity is exceeded.
    /// </summary>
    public sealed class HitRegistry
    {
        private int[] _groups;
        private int[] _targets;
        private int[] _frames;
        private int _count;

        public HitRegistry(int capacity = 32)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            _groups = new int[capacity];
            _targets = new int[capacity];
            _frames = new int[capacity];
        }

        public int Count => _count;

        public void Clear() => _count = 0;

        /// <summary>
        /// Registers a hit if allowed and returns true. <paramref name="rehitIntervalFrames"/> 0 = one hit per target
        /// per group; otherwise allowed again once <c>frame - lastHit &gt;= interval</c>.
        /// </summary>
        public bool TryRegister(int hitGroup, EntityId target, int frame, int rehitIntervalFrames)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_groups[i] != hitGroup || _targets[i] != target.Value) continue;
                if (rehitIntervalFrames <= 0 || frame - _frames[i] < rehitIntervalFrames) return false;
                _frames[i] = frame;
                return true;
            }

            if (_count == _groups.Length)
            {
                int n = _count * 2;
                Array.Resize(ref _groups, n);
                Array.Resize(ref _targets, n);
                Array.Resize(ref _frames, n);
            }
            _groups[_count] = hitGroup;
            _targets[_count] = target.Value;
            _frames[_count] = frame;
            _count++;
            return true;
        }

        /// <summary>True if the target was already registered in the group (regardless of the re-hit interval).</summary>
        public bool Contains(int hitGroup, EntityId target)
        {
            for (int i = 0; i < _count; i++)
                if (_groups[i] == hitGroup && _targets[i] == target.Value) return true;
            return false;
        }
    }
}
