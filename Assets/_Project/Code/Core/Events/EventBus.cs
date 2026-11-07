using System;

namespace Eclipse.Core.Events
{
    /// <summary>
    /// Listener for an event published on an <see cref="EventBus{T}"/>. Named <c>InEventHandler</c> to avoid
    /// clashing with <see cref="System.EventHandler{TEventArgs}"/>; the event is passed by <c>in</c> (no copy).
    /// </summary>
    public delegate void InEventHandler<T>(in T e) where T : struct;

    /// <summary>Handle returned by <see cref="EventBus{T}.Subscribe"/>. <c>default</c> is invalid.</summary>
    public readonly struct SubscriptionToken : IEquatable<SubscriptionToken>
    {
        internal SubscriptionToken(int busId, int id) { BusId = busId; Id = id; }

        /// <summary>Process-unique id of the issuing bus; a token from another bus is rejected.</summary>
        public int BusId { get; }

        /// <summary>Id unique within the issuing bus; 0 means invalid.</summary>
        public int Id { get; }

        /// <summary>True unless this is <c>default</c>.</summary>
        public bool IsValid => Id != 0;

        public bool Equals(SubscriptionToken other) => Id == other.Id && BusId == other.BusId;
        public override bool Equals(object obj) => obj is SubscriptionToken t && Equals(t);
        public override int GetHashCode() => unchecked(BusId * 397 ^ Id);
    }

    /// <summary>Hands out process-unique bus ids so tokens cannot cross buses.</summary>
    internal static class EventBusIds
    {
        private static int _last;
        internal static int Next() => System.Threading.Interlocked.Increment(ref _last);
    }

    /// <summary>
    /// Typed, synchronous event bus with integer priorities. Zero allocation in <see cref="Publish"/>.
    /// <para>Order: higher priority runs first; ties run in subscription order.</para>
    /// <para>Subscribing the same handler twice creates two independent subscriptions.</para>
    /// <para>Unsubscribing during dispatch is safe: a removed handler that has not yet run in the current
    /// Publish will not run. Subscribing during dispatch is deferred: the new handler does not run in the
    /// Publish in progress, only in later ones. Re-entrant Publish (a handler publishing on the same bus)
    /// is supported.</para>
    /// <para>Exceptions thrown by a handler propagate out of Publish (remaining handlers of that Publish
    /// are skipped); bus state stays consistent and usable. Not thread-safe. Tokens are only valid on the
    /// bus that issued them.</para>
    /// </summary>
    public sealed class EventBus<T> where T : struct
    {
        private struct Entry
        {
            public int Id;
            public int Priority;
            public InEventHandler<T> Handler;
            public bool Alive;
        }

        private Entry[] _entries = new Entry[4];
        private int _count;          // slots used in _entries (alive + dead)
        private Entry[] _pending = new Entry[2];
        private int _pendingCount;   // subscriptions made during dispatch
        private readonly int _busId = EventBusIds.Next();
        private int _nextId = 1;
        private int _live;
        private int _depth;
        private bool _dirty;

        /// <summary>Number of active subscriptions (including ones deferred during dispatch).</summary>
        public int Count => _live;

        /// <summary>Registers <paramref name="handler"/>; higher <paramref name="priority"/> runs first.</summary>
        public SubscriptionToken Subscribe(InEventHandler<T> handler, int priority = 0)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var entry = new Entry { Id = _nextId++, Priority = priority, Handler = handler, Alive = true };
            _live++;
            if (_depth > 0)
            {
                if (_pendingCount == _pending.Length) Array.Resize(ref _pending, _pending.Length * 2);
                _pending[_pendingCount++] = entry;
            }
            else
            {
                InsertSorted(entry);
            }
            return new SubscriptionToken(_busId, entry.Id);
        }

        /// <summary>Removes a subscription; false if the token is invalid or already removed.</summary>
        public bool Unsubscribe(SubscriptionToken token)
        {
            if (token.Id == 0 || token.BusId != _busId) return false;
            for (int i = 0; i < _pendingCount; i++)
            {
                if (_pending[i].Id != token.Id) continue;
                if (!_pending[i].Alive) return false;
                _pending[i].Alive = false;
                _pending[i].Handler = null;
                _live--;
                return true;
            }
            for (int i = 0; i < _count; i++)
            {
                if (_entries[i].Id != token.Id) continue;
                if (!_entries[i].Alive) return false;
                _live--;
                if (_depth > 0)
                {
                    _entries[i].Alive = false;
                    _entries[i].Handler = null;
                    _dirty = true;
                }
                else
                {
                    Array.Copy(_entries, i + 1, _entries, i, _count - i - 1);
                    _entries[--_count] = default;
                }
                return true;
            }
            return false;
        }

        /// <summary>Invokes all active handlers synchronously, in priority order.</summary>
        public void Publish(in T e)
        {
            _depth++;
            try
            {
                int n = _count; // entries added during dispatch go to _pending, so [0, n) is stable
                for (int i = 0; i < n; i++)
                {
                    if (!_entries[i].Alive) continue;
                    _entries[i].Handler(in e);
                }
            }
            finally
            {
                if (--_depth == 0) Settle();
            }
        }

        /// <summary>Removes all subscriptions (safe during dispatch; they stop running immediately).</summary>
        public void Clear()
        {
            if (_depth > 0)
            {
                for (int i = 0; i < _count; i++)
                {
                    _entries[i].Alive = false;
                    _entries[i].Handler = null;
                }
                _dirty = true;
            }
            else
            {
                Array.Clear(_entries, 0, _count);
                _count = 0;
                _dirty = false;
            }
            Array.Clear(_pending, 0, _pendingCount);
            _pendingCount = 0;
            _live = 0;
        }

        // Runs when the outermost dispatch ends: drop dead entries, then merge deferred subscriptions.
        private void Settle()
        {
            if (_dirty)
            {
                int w = 0;
                for (int r = 0; r < _count; r++)
                {
                    if (!_entries[r].Alive) continue;
                    if (w != r) _entries[w] = _entries[r];
                    w++;
                }
                Array.Clear(_entries, w, _count - w);
                _count = w;
                _dirty = false;
            }
            if (_pendingCount > 0)
            {
                int n = _pendingCount;
                _pendingCount = 0;
                for (int i = 0; i < n; i++)
                {
                    if (_pending[i].Alive) InsertSorted(_pending[i]);
                    _pending[i] = default;
                }
            }
        }

        // Keeps order: priority descending, then id ascending (ids grow with subscription order).
        private void InsertSorted(Entry entry)
        {
            if (_count == _entries.Length) Array.Resize(ref _entries, _entries.Length * 2);
            int pos = _count;
            while (pos > 0 && _entries[pos - 1].Priority < entry.Priority) pos--;
            if (pos < _count) Array.Copy(_entries, pos, _entries, pos + 1, _count - pos);
            _entries[pos] = entry;
            _count++;
        }
    }
}
