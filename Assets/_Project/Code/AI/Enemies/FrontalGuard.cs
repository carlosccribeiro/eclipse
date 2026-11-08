using System;
using Eclipse.Core.Damage;
using Eclipse.Gameplay.Combat;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.AI.Enemies
{
    /// <summary>
    /// Frontal plate of the Tank (09 section 4.4): a hit that arrives inside the frontal arc deals <c>1 - reduction</c> of its
    /// damage (Kiln Brute: -50%), a hit from behind or the side outside the arc deals 100%. Reduction and arc come from
    /// <c>EnemyDefinition</c> (<c>front_plate_reduction_pct</c>, <c>front_plate_arc_deg</c>). Pure math.
    /// </summary>
    public readonly struct FrontalGuard
    {
        public readonly float Reduction;
        public readonly float ArcDegrees;

        public FrontalGuard(float reduction, float arcDegrees)
        {
            Reduction = reduction < 0f ? 0f : (reduction > 1f ? 1f : reduction);
            ArcDegrees = arcDegrees;
        }

        public bool Enabled => Reduction > 0f && ArcDegrees > 0f;

        /// <summary>
        /// True when a hit travelling along <paramref name="hitDirection"/> (attacker to victim, the convention of
        /// <see cref="IDamageReceiver.ReceiveHit"/>) strikes the front: the direction to the attacker is within half the arc
        /// of <paramref name="facing"/> (planar).
        /// </summary>
        public bool IsFrontal(Vector3 facing, Vector3 hitDirection)
        {
            facing.y = 0f;
            hitDirection.y = 0f;
            if (facing.sqrMagnitude < 1e-8f || hitDirection.sqrMagnitude < 1e-8f) return false;
            float angle = Vector3.Angle(facing, -hitDirection);
            return angle <= ArcDegrees * 0.5f + 1e-3f;
        }

        /// <summary>Damage multiplier for this hit: <c>1 - Reduction</c> from the front, 1 otherwise.</summary>
        public float Multiplier(Vector3 facing, Vector3 hitDirection) =>
            Enabled && IsFrontal(facing, hitDirection) ? 1f - Reduction : 1f;
    }

    /// <summary>
    /// Damage modifier at the receiving end: wraps the <see cref="IDamageReceiver"/> bound to the hurtbox
    /// (<c>CombatReceiver</c>) and scales the damage components of a frontal hit before forwarding it. The extension point is
    /// <c>Hurtbox.Bind(entity, receiver)</c> of the frozen M05 contract; nothing in Gameplay is edited. The Stagger value of the
    /// hit is left alone (the plate stops damage, not the Stagger bar; 09 4.4 lists Stagger as the counterplay).
    /// </summary>
    public sealed class FrontalGuardReceiver : IDamageReceiver
    {
        private readonly IDamageReceiver _inner;
        private readonly FrontalGuard _guard;
        private readonly Func<Vector3> _facing;

        public FrontalGuardReceiver(IDamageReceiver inner, FrontalGuard guard, Func<Vector3> facing)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _guard = guard;
            _facing = facing ?? throw new ArgumentNullException(nameof(facing));
        }

        public EntityId Id => _inner.Id;

        public DamageOutcome ReceiveHit(DamageInstance hit, Vector3 hitDirection)
        {
            float m = _guard.Multiplier(_facing(), hitDirection);
            if (m < 1f) Scale(hit, m);
            return _inner.ReceiveHit(hit, hitDirection);
        }

        // DamageInstance exposes components read-only; rebuild them scaled (no allocation: fixed array copy on the stack).
        private static void Scale(DamageInstance hit, float multiplier)
        {
            int n = hit.ComponentCount;
            Span<DamageComponent> saved = stackalloc DamageComponent[n];
            for (int i = 0; i < n; i++) saved[i] = hit.GetComponent(i);
            hit.ClearComponents();
            for (int i = 0; i < n; i++) hit.AddComponent(saved[i].Type, saved[i].Value * multiplier);
        }
    }
}
