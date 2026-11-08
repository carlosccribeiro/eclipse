using System;
using Eclipse.Core.Damage;
using Eclipse.Gameplay.Physics;
using UPhysics = UnityEngine.Physics;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>A potential victim found by an <see cref="IAreaTargetQuery"/>.</summary>
    public struct AreaCandidate
    {
        public EntityId Id;
        public IDamageReceiver Receiver;
        /// <summary>Planar position of the target (used for the push direction).</summary>
        public Vector3 Position;
    }

    /// <summary>Finds the targets inside an area. The physics version is <see cref="PhysicsAreaQuery"/>; tests inject fakes.</summary>
    public interface IAreaTargetQuery
    {
        /// <summary>Fills <paramref name="buffer"/> with enemies of <c>spec.Team</c> inside the shape with line of sight from the centre; returns the count.</summary>
        int Collect(in AreaSpec spec, AreaCandidate[] buffer);
    }

    /// <summary>
    /// Overlap query on the target hurtbox layer + analytic shape test + Wall line of sight from the area centre
    /// (06 3.9: "overlap no frame de detonacao com LOS do centro"; LowCover does not block).
    /// </summary>
    public sealed class PhysicsAreaQuery : IAreaTargetQuery
    {
        private readonly Collider[] _overlaps;

        public PhysicsAreaQuery(int bufferSize = 128)
        {
            _overlaps = new Collider[bufferSize];
        }

        public int Collect(in AreaSpec spec, AreaCandidate[] buffer)
        {
            int mask = spec.Team == Team.Player ? PhysicsLayers.PlayerAttackTargetsMask : PhysicsLayers.EnemyAttackTargetsMask;
            float reach = ShapeQueries.Reach(spec.Shape);
            int n = UPhysics.OverlapSphereNonAlloc(spec.Origin, reach + 1f, _overlaps, mask, QueryTriggerInteraction.Collide);
            int count = 0;
            Vector3 eye = spec.Origin + Vector3.up * 0.5f; // centre sample height: engineering choice, area has no height in the data
            for (int i = 0; i < n && count < buffer.Length; i++)
            {
                if (!_overlaps[i].TryGetComponent(out Hurtbox hb) || hb.Receiver == null || hb.Entity == null) continue;
                ICombatEntity e = hb.Entity;
                if (!e.IsAlive || e.Team == spec.Team || e.Id == spec.Source) continue;
                if (!ShapeQueries.Overlaps(spec.Shape, spec.Origin, spec.Forward, hb.transform.position, hb.EffectiveRadius)) continue;
                if (LineOfSight.IsBlocked(eye, hb.Center)) continue;
                buffer[count++] = new AreaCandidate { Id = e.Id, Receiver = hb.Receiver, Position = hb.transform.position };
            }
            return count;
        }
    }

    /// <summary>
    /// Instant areas (06 3.9) and the shared delivery used by zones. Damage values are already resolved by the caller
    /// with the Core resolvers; mitigation happens in the target <see cref="IDamageReceiver"/>. One hit per target per
    /// detonation. Publishes OnHitDealt (instant) or OnTickDealt (zone tick). No allocation after construction.
    /// </summary>
    public sealed class AreaDeliverer
    {
        private readonly IAreaTargetQuery _query;
        private readonly CombatEventStream _events;
        private readonly AreaCandidate[] _buffer;
        private readonly DamageInstance _hit = new DamageInstance();

        public AreaDeliverer(IAreaTargetQuery query, CombatEventStream events = null, int bufferSize = 128)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _events = events;
            _buffer = new AreaCandidate[bufferSize];
        }

        /// <summary>Candidates of the last <see cref="Collect"/>.</summary>
        public AreaCandidate GetCandidate(int index) => _buffer[index];

        public int Collect(in AreaSpec spec) => _query.Collect(in spec, _buffer);

        /// <summary>Detonates an instant area at <paramref name="frame"/>; returns the number of targets hit.</summary>
        public int Detonate(in AreaSpec spec, int frame)
        {
            int n = Collect(in spec);
            int hits = 0;
            for (int i = 0; i < n; i++)
                if (Hit(in spec, in _buffer[i], HitKind.Hit, frame)) hits++;
            return hits;
        }

        /// <summary>Delivers the spec damage to one candidate. Ticks never crit and never apply on-hit (06 3.2).</summary>
        public bool Hit(in AreaSpec spec, in AreaCandidate c, HitKind kind, int frame)
        {
            _hit.Reset();
            _hit.SourceId = spec.Source.Value;
            _hit.TargetId = c.Id.Value;
            _hit.Kind = kind;
            _hit.Tags = spec.Tags;
            _hit.Flags = spec.Flags;
            _hit.Generation = spec.Generation;
            _hit.ProcCoefficient = spec.ProcCoefficient;
            _hit.StaggerValue = spec.StaggerValue;
            _hit.HitGroupId = spec.HitGroupId;
            _hit.AddComponent(spec.Type, spec.Damage);

            Vector3 dir = c.Position - spec.Origin;
            dir.y = 0f;
            dir = dir.sqrMagnitude < 1e-8f ? ShapeQueries.FlatForward(spec.Forward) : dir.normalized;

            DamageOutcome o = c.Receiver.ReceiveHit(_hit, dir);
            if (o.Result != HitResult.Hit) return false;
            if (_events != null)
            {
                if (kind == HitKind.Tick) _events.Publish(new OnTickDealt(frame, spec.Generation, spec.Source, c.Id, o.FinalDamage, spec.Type));
                else _events.Publish(new OnHitDealt(frame, spec.Generation, spec.Source, c.Id, o.FinalDamage, spec.Type, false, spec.Flags, c.Position));
            }
            return true;
        }
    }
}
