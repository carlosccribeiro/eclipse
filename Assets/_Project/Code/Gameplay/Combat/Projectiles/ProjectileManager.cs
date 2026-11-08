using System;
using Eclipse.Core.Damage;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Physics;
using Eclipse.Gameplay.Simulation;
using Unity.Collections;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Kinematic projectile simulation (06 3.9, 11 3.9). State lives in pre-allocated arrays (struct-of-arrays); each tick
    /// every flying projectile is swept with a batched <see cref="SpherecastCommand"/> job (one command per projectile, the
    /// hits of the step are processed in distance order). Blocked by Wall/Environment; LowCover blocks too unless the skill has the
    /// Area tag; a projectile never hits the same target twice (per-projectile <see cref="HitRegistry"/>) except after Return.
    /// Damage values are resolved by the caller (Core resolvers); mitigation happens in the target <see cref="IDamageReceiver"/>.
    /// Construct it directly (no scene needed); call <see cref="Dispose"/> to free the native buffers.
    /// Zero GC per tick after construction (HitRegistry growth beyond its initial capacity aside).
    /// </summary>
    public sealed class ProjectileManager : IFixedTickable, IDisposable
    {
        /// <summary>Default capacity; the target is at least 2,000 active (M05 T8).</summary>
        public const int DefaultCapacity = 2048;

        /// <summary>Engineering: hits kept per swept step (closest first). A step is at most one tick of travel, so 4 is ample.</summary>
        public const int MaxHitsPerCast = 4;

        /// <summary>Engineering floor: SphereCast needs a positive radius.</summary>
        public const float MinCastRadius = 0.01f;

        private const int RegistryInitialCapacity = 4;

        private readonly int _capacity;
        private readonly CombatEventStream _events;
        private readonly IProjectileTargetSearch _search;
        private readonly DamageInstance _hit = new DamageInstance();

        // Per-slot state.
        private readonly bool[] _active;
        private readonly ProjectileSpec[] _spec;
        private readonly ProjectileBehavior[] _beh;
        private readonly Vector3[] _pos;
        private readonly Vector3[] _dir;
        private readonly float[] _rangeLeft;
        private readonly int[] _pierce;
        private readonly int[] _chain;
        private readonly bool[] _forked;
        private readonly bool[] _returned;
        private readonly int[] _mask;
        private readonly int[] _blockMask;
        private readonly HitRegistry[] _registry;
        private readonly float[] _arcElapsed;
        private readonly float[] _arcDuration;
        private readonly int[] _castFrame; // frame a sweep command was built for the slot (guards slots reused mid-tick)
        private readonly int[] _free;
        private int _freeCount;
        private int _highWater;

        // Batched sweep buffers.
        private NativeArray<SpherecastCommand> _commands;
        private NativeArray<RaycastHit> _results;
        private readonly int[] _commandSlot;
        private readonly int[] _sorted = new int[MaxHitsPerCast];
        private readonly float[] _sortedDist = new float[MaxHitsPerCast];
        private bool _disposed;

        public ProjectileManager(int capacity = DefaultCapacity, CombatEventStream events = null,
            IProjectileTargetSearch search = null, IProjectileListener listener = null)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _events = events;
            _search = search;
            Listener = listener;
            _active = new bool[capacity];
            _spec = new ProjectileSpec[capacity];
            _beh = new ProjectileBehavior[capacity];
            _pos = new Vector3[capacity];
            _dir = new Vector3[capacity];
            _rangeLeft = new float[capacity];
            _pierce = new int[capacity];
            _chain = new int[capacity];
            _forked = new bool[capacity];
            _returned = new bool[capacity];
            _mask = new int[capacity];
            _blockMask = new int[capacity];
            _registry = new HitRegistry[capacity];
            _arcElapsed = new float[capacity];
            _arcDuration = new float[capacity];
            _free = new int[capacity];
            _castFrame = new int[capacity];
            _commandSlot = new int[capacity];
            _commands = new NativeArray<SpherecastCommand>(capacity, Allocator.Persistent);
            _results = new NativeArray<RaycastHit>(capacity * MaxHitsPerCast, Allocator.Persistent);
        }

        public IProjectileListener Listener { get; set; }
        public int Capacity => _capacity;
        public int ActiveCount { get; private set; }
        /// <summary>Spawns refused because the pool was full.</summary>
        public int DroppedSpawns { get; private set; }

        public bool IsActive(int handle) => (uint)handle < (uint)_capacity && _active[handle];
        public Vector3 GetPosition(int handle) => _pos[handle];
        public Vector3 GetDirection(int handle) => _dir[handle];
        public float GetRangeLeft(int handle) => _rangeLeft[handle];

        /// <summary>Spawns a projectile; returns its handle (valid while <see cref="IsActive"/>) or -1 when refused.</summary>
        public int Spawn(in ProjectileSpec spec) => Spawn(in spec, ProjectileBehavior.None);

        public int Spawn(in ProjectileSpec spec, in ProjectileBehavior behavior)
        {
            if (spec.Speed <= 0f || spec.MaxRange <= 0f || spec.Direction.sqrMagnitude < 1e-10f) return -1;
            int slot = Acquire();
            if (slot < 0) { DroppedSpawns++; return -1; }

            _active[slot] = true;
            _castFrame[slot] = int.MinValue;
            _spec[slot] = spec;
            _beh[slot] = behavior;
            _pos[slot] = spec.Origin;
            _dir[slot] = spec.Direction.normalized;
            _rangeLeft[slot] = spec.MaxRange;
            _pierce[slot] = Math.Max(0, spec.Pierce);
            _chain[slot] = Math.Max(0, behavior.ChainCount);
            _forked[slot] = false;
            _returned[slot] = false;
            int blockers = spec.Tags.Has(Tag.Area) ? PhysicsLayers.ProjectileBlockersMask : PhysicsLayers.ProjectileBlockersNonAreaMask;
            _blockMask[slot] = blockers;
            _mask[slot] = blockers | (spec.Team == Team.Player ? PhysicsLayers.PlayerAttackTargetsMask : PhysicsLayers.EnemyAttackTargetsMask);
            _arcElapsed[slot] = 0f;
            _arcDuration[slot] = behavior.Arc ? ProjectileMath.ArcDuration(spec.Origin, behavior.ArcTarget, spec.Speed) : 0f;
            if (_registry[slot] == null) _registry[slot] = new HitRegistry(RegistryInitialCapacity);
            else _registry[slot].Clear();
            ActiveCount++;
            return slot;
        }

        private int Acquire()
        {
            if (_freeCount > 0) return _free[--_freeCount];
            return _highWater < _capacity ? _highWater++ : -1;
        }

        public void Despawn(int handle)
        {
            if (!IsActive(handle)) return;
            _active[handle] = false;
            _free[_freeCount++] = handle;
            ActiveCount--;
        }

        public void Clear()
        {
            for (int i = 0; i < _highWater; i++) Despawn(i);
        }

        public void Tick(in SimulationFrame frame)
        {
            if (_disposed || ActiveCount == 0) return;
            float dt = SimulationFrame.FixedDeltaSeconds;
            int k = 0;

            for (int s = 0; s < _highWater; s++)
            {
                if (!_active[s]) continue;
                if (_beh[s].Arc) { StepArc(s, dt, frame.Index); continue; }

                Steer(s, dt);
                float step = Mathf.Min(_spec[s].Speed * dt, _rangeLeft[s]);
                var query = new QueryParameters(_mask[s], false, QueryTriggerInteraction.Collide, false);
                _commands[k] = new SpherecastCommand(_pos[s], Mathf.Max(_spec[s].Radius, MinCastRadius), _dir[s], query, step);
                _commandSlot[k] = s;
                _castFrame[s] = frame.Index;
                k++;
            }
            if (k == 0) return;

            for (int i = 0; i < k * MaxHitsPerCast; i++) _results[i] = default;
            SpherecastCommand.ScheduleBatch(_commands.GetSubArray(0, k), _results.GetSubArray(0, k * MaxHitsPerCast), 32, MaxHitsPerCast).Complete();

            for (int j = 0; j < k; j++)
            {
                int s = _commandSlot[j];
                if (_active[s] && _castFrame[s] == frame.Index) StepLinear(s, j, frame.Index, dt);
            }
        }

        private void Steer(int s, float dt)
        {
            ProjectileBehavior b = _beh[s];
            if (b.HomingDegreesPerSecond <= 0f || _search == null) return;
            if (!_search.TryFindNearest(_pos[s], b.HomingRadius, _spec[s].Team, _registry[s], _spec[s].HitGroupId, out Vector3 p)) return;
            _dir[s] = ProjectileMath.TurnTowards(_dir[s], p - _pos[s], b.HomingDegreesPerSecond * dt);
        }

        private void StepArc(int s, float dt, int frame)
        {
            _arcElapsed[s] += dt;
            float u = _arcDuration[s] <= 0f ? 1f : _arcElapsed[s] / _arcDuration[s];
            ProjectileBehavior b = _beh[s];
            _pos[s] = ProjectileMath.ArcPosition(_spec[s].Origin, b.ArcTarget, b.ArcApexHeight, u);
            if (u < 1f) return;
            Listener?.OnArcLanded(in _spec[s], b.ArcTarget, frame);
            Despawn(s);
        }

        private void StepLinear(int s, int cmd, int frame, float dt)
        {
            float step = Mathf.Min(_spec[s].Speed * dt, _rangeLeft[s]);
            Vector3 pos = _pos[s];
            Vector3 dir = _dir[s];
            HitRegistry reg = _registry[s];
            int group = _spec[s].HitGroupId;

            // Collect valid hits of this step sorted by distance (insertion sort over <= MaxHitsPerCast).
            int nh = 0;
            int baseIndex = cmd * MaxHitsPerCast;
            for (int h = 0; h < MaxHitsPerCast; h++)
            {
                RaycastHit r = _results[baseIndex + h];
                if (r.collider == null) continue; // empty slot of the batch result
                int at = nh++;
                while (at > 0 && _sortedDist[at - 1] > r.distance)
                {
                    _sortedDist[at] = _sortedDist[at - 1];
                    _sorted[at] = _sorted[at - 1];
                    at--;
                }
                _sortedDist[at] = r.distance;
                _sorted[at] = baseIndex + h;
            }

            bool ended = false, redirected = false;
            for (int n = 0; n < nh && !ended && !redirected; n++)
            {
                RaycastHit r = _results[_sorted[n]];
                Collider col = r.collider;
                if (col == null) continue;
                Vector3 point = pos + dir * r.distance;

                if ((_blockMask[s] & (1 << col.gameObject.layer)) != 0)
                {
                    ended = true;
                    _pos[s] = point;
                    break;
                }
                if (!col.TryGetComponent(out Hurtbox hb) || hb.Receiver == null || hb.Entity == null) continue;
                ICombatEntity target = hb.Entity;
                if (!target.IsAlive || target.Team == _spec[s].Team || target.Id == _spec[s].Source) continue;
                if (!reg.TryRegister(group, target.Id, frame, 0)) continue;

                Deliver(s, hb, dir, point, frame);

                float remaining = _rangeLeft[s] - r.distance;
                if (!_forked[s] && _beh[s].ForkCount > 0)
                {
                    _forked[s] = true;
                    SpawnForkChildren(s, point, dir, remaining, target.Id);
                }

                if (_pierce[s] > 0) { _pierce[s]--; continue; }

                if (_chain[s] > 0 && _search != null &&
                    _search.TryFindNearest(point, _beh[s].ChainRadius, _spec[s].Team, reg, group, out Vector3 next))
                {
                    _chain[s]--;
                    Vector3 to = next - point;
                    float dist = to.magnitude;
                    _dir[s] = dist < 1e-6f ? dir : to / dist;
                    _pos[s] = point;
                    _rangeLeft[s] = Mathf.Max(remaining, dist);
                    redirected = true;
                    break;
                }
                ended = true;
                _pos[s] = point;
            }

            if (ended) { Despawn(s); return; }
            if (redirected) return;

            _pos[s] = pos + dir * step;
            _rangeLeft[s] -= step;
            if (_rangeLeft[s] > 1e-4f) return;

            if (_beh[s].Return && !_returned[s])
            {
                Vector3 back = _spec[s].Origin - _pos[s];
                float dist = back.magnitude;
                if (dist > 1e-4f)
                {
                    _returned[s] = true;
                    _dir[s] = back / dist;
                    _rangeLeft[s] = dist;
                    reg.Clear(); // Return may hit the same targets again (06 3.9)
                    return;
                }
            }
            Despawn(s);
        }

        private void SpawnForkChildren(int parent, Vector3 point, Vector3 dir, float remainingRange, EntityId hitTarget)
        {
            if (remainingRange <= 1e-4f) return;
            ProjectileBehavior b = _beh[parent];
            ProjectileSpec p = _spec[parent];
            for (int i = 0; i < b.ForkCount; i++)
            {
                Vector3 d = ProjectileMath.ForkDirection(dir, i, b.ForkCount, b.ForkAngleDegrees);
                var child = new ProjectileSpec(p.Source, p.Team, point, d, p.Speed, p.Radius, remainingRange, 0, p.Type, p.Damage,
                    p.StaggerValue, p.ProcCoefficient, p.Generation, p.HitGroupId, p.Tags, p.Flags);
                int slot = Spawn(in child, ProjectileBehavior.None);
                if (slot >= 0) _registry[slot].TryRegister(p.HitGroupId, hitTarget, 0, 0);
            }
        }

        private void Deliver(int s, Hurtbox hb, Vector3 dir, Vector3 point, int frame)
        {
            ProjectileSpec p = _spec[s];
            _hit.Reset();
            _hit.SourceId = p.Source.Value;
            _hit.TargetId = hb.Entity.Id.Value;
            _hit.Kind = HitKind.Hit;
            _hit.Tags = p.Tags;
            _hit.Flags = p.Flags;
            _hit.Generation = p.Generation;
            _hit.ProcCoefficient = p.ProcCoefficient;
            _hit.StaggerValue = p.StaggerValue;
            _hit.HitGroupId = p.HitGroupId;
            _hit.AddComponent(p.Type, p.Damage);

            DamageOutcome o = hb.Receiver.ReceiveHit(_hit, dir);
            if (o.Result == HitResult.Hit)
                _events?.Publish(new OnHitDealt(frame, p.Generation, p.Source, hb.Entity.Id, o.FinalDamage, p.Type, false, p.Flags, point));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_commands.IsCreated) _commands.Dispose();
            if (_results.IsCreated) _results.Dispose();
        }
    }
}
