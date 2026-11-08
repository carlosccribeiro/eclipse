using System;
using System.Threading;
using Eclipse.Core.Events;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Ring buffers of combat events, one per event struct type. Simulation publishes during FixedUpdate;
    /// UI/Audio read in LateUpdate and the owner calls <see cref="EndFrame"/> afterwards (11 3.7). Readers never mutate combat.
    /// Zero GC after a type first use (call <see cref="Prewarm{T}"/> to move that one-time allocation to load time).
    /// Overflow overwrites the oldest event (see <see cref="FrameEventBuffer{T}"/>). Frozen contract (M05 T1).
    /// </summary>
    public sealed class CombatEventStream
    {
        public const int DefaultCapacity = 512;

        private readonly int _capacity;
        private object[] _buffers = new object[64];

        public CombatEventStream(int capacityPerType = DefaultCapacity)
        {
            if (capacityPerType < 1) throw new ArgumentOutOfRangeException(nameof(capacityPerType));
            _capacity = capacityPerType;
        }

        /// <summary>Allocates the buffer of <typeparamref name="T"/> ahead of time.</summary>
        public void Prewarm<T>() where T : struct => Buffer<T>();

        public void Publish<T>(in T e) where T : struct => Buffer<T>().Push(in e);

        /// <summary>Events of type T collected this frame (index 0 = oldest).</summary>
        public FrameEventBuffer<T> Read<T>() where T : struct => Buffer<T>();

        public int Count<T>() where T : struct => Buffer<T>().Count;

        /// <summary>Clears every buffer; call once per frame after UI/Audio have read.</summary>
        public void EndFrame()
        {
            for (int i = 0; i < _buffers.Length; i++)
                if (_buffers[i] is IClearable c) c.Clear();
        }

        private FrameEventBuffer<T> Buffer<T>() where T : struct
        {
            int idx = TypeIndex<T>.Value;
            if (idx >= _buffers.Length) Array.Resize(ref _buffers, Math.Max(idx + 1, _buffers.Length * 2));
            var h = _buffers[idx] as Holder<T>;
            if (h == null)
            {
                h = new Holder<T>(_capacity);
                _buffers[idx] = h;
            }
            return h.Buffer;
        }

        private interface IClearable { void Clear(); }

        private sealed class Holder<T> : IClearable where T : struct
        {
            public readonly FrameEventBuffer<T> Buffer;
            public Holder(int capacity) { Buffer = new FrameEventBuffer<T>(capacity); }
            public void Clear() { if (Buffer.Count > 0 || Buffer.DroppedCount > 0) Buffer.Clear(); }
        }

        private static class TypeIndex<T>
        {
            public static readonly int Value = TypeCounter.Next();
        }

        private static class TypeCounter
        {
            private static int _next = -1;
            public static int Next() => Interlocked.Increment(ref _next);
        }
    }
}
