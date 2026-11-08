using System;
using System.Collections.Generic;

namespace Eclipse.Gameplay.Simulation
{
    /// <summary>
    /// Deterministic fixed-step scheduler (60 Hz). Order: ascending priority, then registration order.
    /// Tickables registered during a tick start on the next tick. No allocation per tick once capacity is warm.
    /// </summary>
    public sealed class TickScheduler
    {
        private struct Entry
        {
            public IFixedTickable Tickable;
            public int Priority;
            public bool Removed;
        }

        private readonly List<Entry> _entries = new List<Entry>(64);
        private readonly List<Entry> _pending = new List<Entry>(16);
        private bool _ticking;
        private bool _dirtyRemovals;
        private float _accumulator;

        public int CurrentFrame { get; private set; }

        public void Register(IFixedTickable tickable, int priority = 0)
        {
            if (tickable == null) throw new ArgumentNullException(nameof(tickable));
            var e = new Entry { Tickable = tickable, Priority = priority };
            if (_ticking) _pending.Add(e); else Insert(e);
        }

        public void Unregister(IFixedTickable tickable)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
                if (ReferenceEquals(_pending[i].Tickable, tickable)) _pending.RemoveAt(i);
            for (int i = 0; i < _entries.Count; i++)
            {
                if (!ReferenceEquals(_entries[i].Tickable, tickable)) continue;
                if (_ticking)
                {
                    var e = _entries[i];
                    e.Removed = true;
                    _entries[i] = e;
                    _dirtyRemovals = true;
                }
                else
                {
                    _entries.RemoveAt(i);
                }
                return;
            }
        }

        /// <summary>Runs exactly <paramref name="ticks"/> ticks without real time (tests, replays).</summary>
        public void StepManual(int ticks)
        {
            for (int i = 0; i < ticks; i++) RunTick();
        }

        /// <summary>Accumulates real time and runs as many fixed ticks as fit; returns ticks run.</summary>
        public int Advance(float deltaSeconds, int maxTicks = 8)
        {
            _accumulator += deltaSeconds;
            int n = 0;
            while (_accumulator >= SimulationFrame.FixedDeltaSeconds && n < maxTicks)
            {
                _accumulator -= SimulationFrame.FixedDeltaSeconds;
                RunTick();
                n++;
            }
            if (n == maxTicks) _accumulator = 0f; // drop backlog (spiral of death)
            return n;
        }

        private void RunTick()
        {
            if (_pending.Count > 0)
            {
                for (int i = 0; i < _pending.Count; i++) Insert(_pending[i]);
                _pending.Clear();
            }
            CurrentFrame++;
            var frame = new SimulationFrame(CurrentFrame);
            _ticking = true;
            try
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    var e = _entries[i];
                    if (!e.Removed) e.Tickable.Tick(in frame);
                }
            }
            finally
            {
                _ticking = false;
                if (_dirtyRemovals)
                {
                    for (int i = _entries.Count - 1; i >= 0; i--)
                        if (_entries[i].Removed) _entries.RemoveAt(i);
                    _dirtyRemovals = false;
                }
            }
        }

        // Stable insert: after all entries with priority <= e.Priority (keeps registration order).
        private void Insert(Entry e)
        {
            int i = _entries.Count;
            while (i > 0 && _entries[i - 1].Priority > e.Priority) i--;
            _entries.Insert(i, e);
        }
    }
}
