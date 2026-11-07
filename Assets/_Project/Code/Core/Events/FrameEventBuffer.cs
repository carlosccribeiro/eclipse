using System;

namespace Eclipse.Core.Events
{
    /// <summary>
    /// Fixed-capacity FIFO ring buffer that collects events during a frame so a consumer (UI/Audio, e.g. in
    /// LateUpdate) can read them all and then <see cref="Clear"/>. Zero allocation after construction.
    /// <para>Overflow policy: when full, <see cref="Push"/> overwrites the oldest event and increments
    /// <see cref="DroppedCount"/> (losing an old damage number is better than stalling combat).</para>
    /// </summary>
    public sealed class FrameEventBuffer<T> where T : struct
    {
        private readonly T[] _items;
        private int _head; // index of the oldest item
        private int _count;

        /// <summary>Creates a buffer holding up to <paramref name="capacity"/> (at least 1) events.</summary>
        public FrameEventBuffer(int capacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "Must be >= 1.");
            _items = new T[capacity];
        }

        /// <summary>Maximum number of events held.</summary>
        public int Capacity => _items.Length;

        /// <summary>Events currently held.</summary>
        public int Count => _count;

        /// <summary>Events overwritten by overflow since construction or the last <see cref="Clear"/>.</summary>
        public int DroppedCount { get; private set; }

        /// <summary>Appends an event; if full, overwrites the oldest one.</summary>
        public void Push(in T e)
        {
            if (_count == _items.Length)
            {
                _items[_head] = e;
                _head = _head + 1 == _items.Length ? 0 : _head + 1;
                DroppedCount++;
                return;
            }
            int tail = _head + _count;
            if (tail >= _items.Length) tail -= _items.Length;
            _items[tail] = e;
            _count++;
        }

        /// <summary>Removes and returns the oldest event; false if empty.</summary>
        public bool TryPop(out T e)
        {
            if (_count == 0)
            {
                e = default;
                return false;
            }
            e = _items[_head];
            _items[_head] = default;
            _head = _head + 1 == _items.Length ? 0 : _head + 1;
            _count--;
            return true;
        }

        /// <summary>Event by arrival order (0 = oldest).</summary>
        public ref readonly T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
                int i = _head + index;
                if (i >= _items.Length) i -= _items.Length;
                return ref _items[i];
            }
        }

        /// <summary>Empties the buffer and resets <see cref="DroppedCount"/>.</summary>
        public void Clear()
        {
            Array.Clear(_items, 0, _items.Length);
            _head = 0;
            _count = 0;
            DroppedCount = 0;
        }
    }
}
