using System;
using Eclipse.Core.Stats;
using Eclipse.Gameplay.Simulation;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Applies <see cref="StatMod"/>s to a <see cref="StatBlock"/> for a duration (Vulnerability, temporary buffs).
    /// Each application owns a <see cref="SourceHandle"/> so it can be removed early; expiry removes it on the tick.
    /// With a non-zero <c>refreshKey</c> the same logical effect is replaced and its duration renewed instead of stacking.
    /// Plain C#; allocates only when the slot or mod capacity grows.
    /// </summary>
    public sealed class TimedModifierSet : IFixedTickable
    {
        private struct Slot
        {
            public bool Active;
            public SourceHandle Source;
            public int Remaining;
            public int RefreshKey;
        }

        private readonly StatBlock _stats;
        private Slot[] _slots = new Slot[8];
        private int _used;
        private int _active;

        public TimedModifierSet(StatBlock stats)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        public int ActiveCount => _active;

        /// <summary>Applies the mod for <paramref name="durationFrames"/> ticks; returns the handle usable by <see cref="Remove"/>.</summary>
        public SourceHandle Apply(in StatMod mod, int durationFrames, int refreshKey = 0)
        {
            if (durationFrames <= 0) throw new ArgumentOutOfRangeException(nameof(durationFrames));

            if (refreshKey != 0)
            {
                for (int i = 0; i < _used; i++)
                {
                    if (!_slots[i].Active || _slots[i].RefreshKey != refreshKey) continue;
                    _stats.RemoveSource(_slots[i].Source);
                    _stats.AddMod(_slots[i].Source, in mod);
                    _slots[i].Remaining = durationFrames;
                    return _slots[i].Source;
                }
            }

            int free = -1;
            for (int i = 0; i < _used; i++)
                if (!_slots[i].Active) { free = i; break; }
            if (free < 0)
            {
                if (_used == _slots.Length) Array.Resize(ref _slots, _used * 2);
                free = _used++;
            }
            SourceHandle handle = _stats.CreateSource();
            _stats.AddMod(handle, in mod);
            _slots[free] = new Slot { Active = true, Source = handle, Remaining = durationFrames, RefreshKey = refreshKey };
            _active++;
            return handle;
        }

        /// <summary>Removes the effect early; false if it already expired or was removed.</summary>
        public bool Remove(SourceHandle source)
        {
            for (int i = 0; i < _used; i++)
            {
                if (!_slots[i].Active || _slots[i].Source != source) continue;
                Expire(i);
                return true;
            }
            return false;
        }

        public void RemoveAll()
        {
            for (int i = 0; i < _used; i++)
                if (_slots[i].Active) Expire(i);
        }

        public void Tick(in SimulationFrame frame)
        {
            for (int i = 0; i < _used; i++)
            {
                if (!_slots[i].Active) continue;
                if (--_slots[i].Remaining <= 0) Expire(i);
            }
        }

        private void Expire(int i)
        {
            _stats.RemoveSource(_slots[i].Source);
            _slots[i].Active = false;
            _active--;
        }
    }
}
