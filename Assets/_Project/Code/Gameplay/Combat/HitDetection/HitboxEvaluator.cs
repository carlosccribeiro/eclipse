using System;
using Eclipse.Core.Damage;
using Eclipse.Core.Tags;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Physics;
using UPhysics = UnityEngine.Physics;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Fills the damage components of a hit using the Core OffensiveDamageResolver (06 3.2). Implemented by the
    /// attacker side (owns the request/mods/target defense); the evaluator never computes damage itself.
    /// </summary>
    public interface IHitDamageResolver
    {
        void Resolve(DamageInstance hit, ICombatEntity attacker, Hurtbox target, float damageMultiplier);
    }

    /// <summary>Per-tick input of <see cref="HitboxEvaluator.Evaluate"/>.</summary>
    public struct HitboxRequest
    {
        public ICombatEntity Attacker;
        public Vector3 PrevOrigin;
        public Vector3 PrevForward;
        public Vector3 Origin;
        public Vector3 Forward;
        public AttackStep Step;
        /// <summary>Attack Speed multiplier (1.0 = base). Above <see cref="HitboxEvaluator.SweepAttackSpeedThreshold"/> the sweep is used.</summary>
        public float AttackSpeed;
        /// <summary>0..1 charge fraction interpolating damage multiplier and Stagger between the step min/max.</summary>
        public float ChargeFraction;
        public TagMask Tags;
        public int Frame;
        /// <summary>Forces the sweep (fast shapes such as lunges); the "fast shape" criterion is the caller's call.</summary>
        public bool ForceSweep;
    }

    /// <summary>One resolved hit, readable after <see cref="HitboxEvaluator.Evaluate"/>.</summary>
    public struct HitRecord
    {
        public Hurtbox Target;
        public DamageOutcome Outcome;
        public Vector3 Direction;
    }

    /// <summary>
    /// Evaluates the logical hitbox of an active attack frame (06 3.9, 11 3.9): broad phase with a pre-allocated
    /// OverlapSphereNonAlloc on the target hurtbox layer, analytic shape test (<see cref="ShapeQueries"/>), sweep when
    /// needed, Wall line of sight, <see cref="HitRegistry"/>, then builds the DamageInstance (Generation 0) and calls
    /// <see cref="IDamageReceiver"/>. OnKill/OnOverkill are published by CombatEntity, not here.
    /// </summary>
    public sealed class HitboxEvaluator
    {
        /// <summary>Sweep is used above this Attack Speed (06 3.9). Structural contract constant.</summary>
        public const float SweepAttackSpeedThreshold = 1.3f;

        private const int NoneWarned = 1, SectorWarned = 2, BoxWarned = 4, CapsuleWarned = 8;

        private readonly Collider[] _overlaps;
        private readonly HitRecord[] _records;
        private readonly DamageInstance _hit = new DamageInstance();
        private readonly IHitDamageResolver _damage;
        private int _warned;

        public HitboxEvaluator(IHitDamageResolver damage, int bufferSize = 32)
        {
            _damage = damage ?? throw new ArgumentNullException(nameof(damage));
            _overlaps = new Collider[bufferSize];
            _records = new HitRecord[bufferSize];
            Registry = new HitRegistry(bufferSize);
        }

        public HitRegistry Registry { get; }
        public int HitCount { get; private set; }
        public HitRecord GetRecord(int index) => _records[index];

        /// <summary>
        /// Maps data to a logical shape. False (with a one-time warning) when the docs left the hitbox undefined
        /// (Dash hitbox, Forte arc, etc.): the attack then hits nothing instead of inventing values.
        /// </summary>
        public bool TryBuildShape(in AttackStep step, out HitboxShape shape)
        {
            shape = default;
            switch (step.Shape)
            {
                case AttackHitShape.Sector:
                case AttackHitShape.Cone:
                    if (step.Radius <= 0f || step.ArcDegrees <= 0f) return Warn(SectorWarned, "sector/cone without radius or arc");
                    shape = HitboxShape.Sector(step.Radius, step.ArcDegrees, step.Offset);
                    return true;
                case AttackHitShape.Circle:
                    if (step.Radius <= 0f) return Warn(SectorWarned, "circle without radius");
                    shape = HitboxShape.Sector(step.Radius, 360f, step.Offset);
                    return true;
                case AttackHitShape.Box:
                    if (step.Length <= 0f || step.Width <= 0f) return Warn(BoxWarned, "box without length or width");
                    shape = HitboxShape.Box(step.Length, step.Width, step.Offset);
                    return true;
                case AttackHitShape.Capsule:
                    if (step.Radius <= 0f) return Warn(CapsuleWarned, "capsule without radius");
                    shape = HitboxShape.Capsule(step.Radius, step.Length, step.Offset);
                    return true;
                default:
                    return Warn(NoneWarned, "no hitbox defined in the data (AttackHitShape.None)");
            }
        }

        private bool Warn(int flag, string reason)
        {
            if ((_warned & flag) == 0)
            {
                _warned |= flag;
                Debug.LogWarning("[HitboxEvaluator] Attack treated as having no hitbox: " + reason + ".");
            }
            return false;
        }

        /// <summary>Evaluates one tick. Returns the number of hits landed (see <see cref="GetRecord"/>).</summary>
        public int Evaluate(in HitboxRequest r)
        {
            HitCount = 0;
            if (r.Attacker == null || !r.Attacker.IsAlive) return 0;
            AttackStep step = r.Step;
            if (!TryBuildShape(step, out HitboxShape shape)) return 0;

            int targetMask = r.Attacker.Team == Team.Player ? PhysicsLayers.PlayerAttackTargetsMask : PhysicsLayers.EnemyAttackTargetsMask;
            bool sweep = r.ForceSweep || r.AttackSpeed > SweepAttackSpeedThreshold;
            Vector3 prevO = sweep ? r.PrevOrigin : r.Origin;
            Vector3 prevF = sweep ? r.PrevForward : r.Forward;

            // Broad phase: sphere centred between both poses that covers the whole swept shape + largest hurtbox.
            float reach = ShapeQueries.Reach(shape);
            Vector3 mid = (prevO + r.Origin) * 0.5f;
            float half = new Vector2(r.Origin.x - prevO.x, r.Origin.z - prevO.z).magnitude * 0.5f;
            int n = UPhysics.OverlapSphereNonAlloc(mid, reach + half + 1f, _overlaps, targetMask, QueryTriggerInteraction.Collide);

            float c = Mathf.Clamp01(r.ChargeFraction);
            float damageMult = Mathf.Lerp(step.DamageMultiplier, step.DamageMultiplierMax, c);
            float stagger = Mathf.Lerp(step.Stagger, step.StaggerMax, c);

            for (int i = 0; i < n && HitCount < _records.Length; i++)
            {
                if (!_overlaps[i].TryGetComponent(out Hurtbox hb) || hb.Receiver == null || hb.Entity == null) continue;
                ICombatEntity target = hb.Entity;
                if (!target.IsAlive || target.Id == r.Attacker.Id || target.Team == r.Attacker.Team) continue;

                Vector3 center = hb.transform.position;
                float tr = hb.EffectiveRadius;
                bool inside = sweep
                    ? ShapeQueries.OverlapsSwept(shape, prevO, prevF, r.Origin, r.Forward, center, tr)
                    : ShapeQueries.Overlaps(shape, r.Origin, r.Forward, center, tr);
                if (!inside) continue;

                if (LineOfSight.IsBlocked(r.Origin + Vector3.up * (hb.Height * 0.5f), hb.Center)) continue;
                if (!Registry.TryRegister(step.HitGroup, target.Id, r.Frame, step.RehitIntervalFrames)) continue;

                Vector3 dir = center - r.Origin;
                dir.y = 0f;
                dir = dir.sqrMagnitude < 1e-8f ? ShapeQueries.FlatForward(r.Forward) : dir.normalized;

                _hit.Reset();
                _hit.SourceId = r.Attacker.Id.Value;
                _hit.TargetId = target.Id.Value;
                _hit.Tags = r.Tags;
                _hit.Kind = HitKind.Hit;
                _hit.Generation = 0;
                _hit.ProcCoefficient = step.ProcCoefficient;
                _hit.StaggerValue = stagger;
                _hit.HitGroupId = step.HitGroup;
                _damage.Resolve(_hit, r.Attacker, hb, damageMult);

                DamageOutcome outcome = hb.Receiver.ReceiveHit(_hit, dir);
                _records[HitCount++] = new HitRecord { Target = hb, Outcome = outcome, Direction = dir };
            }
            return HitCount;
        }
    }
}
