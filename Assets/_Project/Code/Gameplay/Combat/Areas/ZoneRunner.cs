using System;
using Eclipse.Core.Damage;
using Eclipse.Gameplay.Simulation;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Persistent zones (06 3.2/3.9): occupants are evaluated every <see cref="AreaSpec.TickIntervalFrames"/> (30 = 0.5 s) and
    /// receive a periodic tick (no crit, no on-hit). Zones of the same skill + source never stack on a target: a target takes at
    /// most one tick of that group per interval even when it stands in several zones. The first evaluation happens one
    /// interval after the zone is created (the docs do not define an immediate tick). Plain C#, no allocation per tick.
    /// </summary>
    public sealed class ZoneRunner : IFixedTickable
    {
        private struct Zone
        {
            public bool Active;
            public AreaSpec Spec;
            public int SkillKey;
            public int Remaining, UntilTick;
        }

        private struct Recent
        {
            public bool Used;
            public int SkillKey, Source, Target, LastFrame, Interval;
        }

        private readonly AreaDeliverer _deliverer;
        private Zone[] _zones;
        private Recent[] _recent = new Recent[32];
        private int _used;
        private int _active;

        public ZoneRunner(AreaDeliverer deliverer, int capacity = 16)
        {
            _deliverer = deliverer ?? throw new ArgumentNullException(nameof(deliverer));
            _zones = new Zone[Math.Max(capacity, 4)];
        }

        public int ActiveCount => _active;

        /// <summary>Spawns a zone using <see cref="AreaSpec.HitGroupId"/> as the skill key (AreaSpec has no skill id).</summary>
        public bool Spawn(in AreaSpec spec) => Spawn(in spec, spec.HitGroupId);

        public bool Spawn(in AreaSpec spec, int skillKey)
        {
            if (!spec.IsPersistent || spec.TickIntervalFrames <= 0) return false;
            int free = -1;
            for (int i = 0; i < _used; i++)
                if (!_zones[i].Active) { free = i; break; }
            if (free < 0)
            {
                if (_used == _zones.Length) Array.Resize(ref _zones, _used * 2);
                free = _used++;
            }
            _zones[free] = new Zone
            {
                Active = true, Spec = spec, SkillKey = skillKey, Remaining = spec.DurationFrames, UntilTick = spec.TickIntervalFrames,
            };
            _active++;
            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < _used; i++) _zones[i].Active = false;
            _active = 0;
        }

        public void Tick(in SimulationFrame frame)
        {
            int n = _used;
            for (int i = 0; i < n; i++)
            {
                if (!_zones[i].Active) continue;
                _zones[i].Remaining--;
                _zones[i].UntilTick--;
                if (_zones[i].UntilTick <= 0)
                {
                    _zones[i].UntilTick = _zones[i].Spec.TickIntervalFrames;
                    Evaluate(i, frame.Index);
                }
                if (_zones[i].Remaining <= 0)
                {
                    _zones[i].Active = false;
                    _active--;
                }
            }
        }

        private void Evaluate(int zoneIndex, int frame)
        {
            AreaSpec spec = _zones[zoneIndex].Spec;
            int skillKey = _zones[zoneIndex].SkillKey;
            int count = _deliverer.Collect(in spec);
            for (int c = 0; c < count; c++)
            {
                AreaCandidate cand = _deliverer.GetCandidate(c);
                if (!TryClaim(skillKey, spec.Source.Value, cand.Id.Value, frame, spec.TickIntervalFrames)) continue;
                _deliverer.Hit(in spec, in cand, HitKind.Tick, frame);
            }
        }

        // One tick per (skill, source, target) per interval across all zones of that group.
        private bool TryClaim(int skillKey, int source, int target, int frame, int interval)
        {
            int free = -1;
            for (int i = 0; i < _recent.Length; i++)
            {
                if (!_recent[i].Used) { if (free < 0) free = i; continue; }
                if (_recent[i].SkillKey == skillKey && _recent[i].Source == source && _recent[i].Target == target)
                {
                    if (frame - _recent[i].LastFrame < interval) return false;
                    _recent[i].LastFrame = frame;
                    _recent[i].Interval = interval;
                    return true;
                }
                if (frame - _recent[i].LastFrame >= _recent[i].Interval && free < 0) free = i; // stale: reusable
            }
            if (free < 0)
            {
                free = _recent.Length;
                Array.Resize(ref _recent, _recent.Length * 2);
            }
            _recent[free] = new Recent { Used = true, SkillKey = skillKey, Source = source, Target = target, LastFrame = frame, Interval = interval };
            return true;
        }
    }
}
