using System;
using Eclipse.Core.Ids;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Simulation;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.AI.Enemies
{
    /// <summary>Resolves the status holder of an entity (the player's <c>StatusStub</c> in M07; M11 replaces it). Implemented by the scene.</summary>
    public interface IStatusDirectory
    {
        bool TryGetStatusReceiver(EntityId id, out IStatusReceiver receiver);
    }

    /// <summary>
    /// Ground pools that leave a status on whoever stands in them (Slag Hurler: Burning pool for 3 s, 09 section 5.1).
    /// A pooled fixed-size table ticked at the zone interval (0.5 s = 30 ticks, R19); every interval it reapplies the status
    /// (guaranteed, renewing the duration) to the targets inside through <see cref="AreaDeliverer.Collect"/> and the
    /// <see cref="IStatusDirectory"/>. No damage here: the DoT belongs to the status (M05 stub, M11). No allocation per tick.
    /// </summary>
    public sealed class EnemyStatusZones : IFixedTickable
    {
        private struct Zone
        {
            public bool Active;
            public Vector3 Origin;
            public float Radius;
            public int Remaining, UntilTick;
            public EntityId Source;
            public StableId Status;
        }

        private readonly AreaDeliverer _areas;
        private readonly IStatusDirectory _directory;
        private readonly int _intervalFrames;
        private Zone[] _zones;
        private int _used;

        public EnemyStatusZones(AreaDeliverer areas, IStatusDirectory directory, int intervalFrames, int capacity = 16)
        {
            _areas = areas ?? throw new ArgumentNullException(nameof(areas));
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _intervalFrames = intervalFrames < 1 ? 1 : intervalFrames;
            _zones = new Zone[capacity < 4 ? 4 : capacity];
        }

        public int ActiveCount { get; private set; }

        public bool Spawn(Vector3 origin, float radius, int durationFrames, EntityId source, StableId status)
        {
            if (radius <= 0f || durationFrames <= 0) return false;
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
                Active = true, Origin = origin, Radius = radius, Remaining = durationFrames, UntilTick = _intervalFrames,
                Source = source, Status = status,
            };
            ActiveCount++;
            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < _used; i++) _zones[i].Active = false;
            ActiveCount = 0;
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
                    _zones[i].UntilTick = _intervalFrames;
                    Apply(i);
                }
                if (_zones[i].Remaining <= 0)
                {
                    _zones[i].Active = false;
                    ActiveCount--;
                }
            }
        }

        private void Apply(int i)
        {
            Zone z = _zones[i];
            var spec = new AreaSpec(z.Source, Team.Enemy, HitboxShape.Sector(z.Radius, 360f), z.Origin, Vector3.forward, 0, 0,
                Eclipse.Core.Damage.DamageType.Ember, 0f, 0f, 0f, 0, 0, TagMask.None, HitFlags.None);
            int count = _areas.Collect(in spec);
            for (int c = 0; c < count; c++)
            {
                AreaCandidate cand = _areas.GetCandidate(c);
                if (!_directory.TryGetStatusReceiver(cand.Id, out IStatusReceiver receiver)) continue;
                receiver.TryApply(new StatusRequest(z.Status, z.Source, cand.Id, 0, 0, 1, 1f, true, HitFlags.None));
            }
        }
    }
}
