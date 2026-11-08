using Eclipse.Core.Damage;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Skills;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Heroes.Kael
{
    /// <summary>
    /// Defensive side of Ashen Guard (08 4.4), wrapped around the player IDamageReceiver (the combat registry hands attackers this
    /// wrapper). While the stance runs:
    /// - a hit inside the Parry window (opened by the skill ParryWindow effect, 12 f, R20) goes to the inner receiver, which negates it;
    ///   the counter-attack then gets +parry_counter_bonus_pct (once per stance). Never against Unblockable/Annihilation;
    /// - a frontal hit is blocked entirely (not Unblockable);
    /// - any other hit is reduced by damage_reduction_pct before the inner pipeline.
    /// Everything absorbed (blocked damage, or the reduced part) feeds absorbed_to_counter_pct of it into
    /// <see cref="SkillExecContext.FlatDamage"/>, which the counter-attack Damage effect adds to its Base. All numbers are skill params.
    /// Outside the stance it forwards untouched. Plain C#, allocation-free after construction.
    /// </summary>
    public sealed class AshenGuardReceiver : IDamageReceiver
    {
        // Engineering cap of components one DamageInstance can carry (one per DamageType, far below 16).
        private const int MaxComponents = 16;

        private readonly IDamageReceiver _inner;
        private readonly SkillExecContext _ctx;
        private readonly DamageComponent[] _scratch = new DamageComponent[MaxComponents];
        private int _stanceStartFrame = int.MinValue;
        private bool _parryBonusApplied;

        public AshenGuardReceiver(IDamageReceiver inner, SkillExecContext ctx)
        {
            _inner = inner ?? throw new System.ArgumentNullException(nameof(inner));
            _ctx = ctx ?? throw new System.ArgumentNullException(nameof(ctx));
        }

        public EntityId Id => _inner.Id;

        /// <summary>Total damage absorbed in the current stance (blocked + reduced), before the 30% conversion.</summary>
        public float AbsorbedThisStance { get; private set; }
        public int BlockedHits { get; private set; }
        public int ParriedHits { get; private set; }

        public bool StanceActive
        {
            get
            {
                SkillSpec spec = _ctx.Cast.Spec;
                return _ctx.StanceActive && spec != null && spec.SkillId.Value == KaelSkillIds.AshenGuard;
            }
        }

        public DamageOutcome ReceiveHit(DamageInstance hit, Vector3 hitDirection)
        {
            if (!StanceActive) return _inner.ReceiveHit(hit, hitDirection);

            SkillSpec spec = _ctx.Cast.Spec;
            if (_ctx.StanceStartFrame != _stanceStartFrame)
            {
                _stanceStartFrame = _ctx.StanceStartFrame;
                _parryBonusApplied = false;
                AbsorbedThisStance = 0f;
                BlockedHits = 0;
                ParriedHits = 0;
            }

            bool unblockable = (hit.Flags & (HitFlags.Unblockable | HitFlags.Annihilation)) != 0;

            StateComponent state = _ctx.Caster.State;
            if (!unblockable && state != null && state.IsParryActive(_ctx.Frame))
            {
                DamageOutcome parried = _inner.ReceiveHit(hit, hitDirection);
                if (parried.Result == HitResult.Parried)
                {
                    ParriedHits++;
                    if (!_parryBonusApplied)
                    {
                        _parryBonusApplied = true;
                        _ctx.DamageMultiplier *= 1f + SkillParams.Get(spec, "parry_counter_bonus_pct");
                    }
                }
                return parried;
            }

            float total = hit.TotalDamage;
            bool frontal = Vector3.Dot(_ctx.Forward, -hitDirection) >= 0f; // same half-plane test as CombatEntity (06 3.3 step 4)
            if (!unblockable && frontal)
            {
                Absorb(spec, total);
                BlockedHits++;
                _ctx.Events?.Publish(new OnBlock(_ctx.Frame, hit.Generation, new EntityId(hit.SourceId), Id, total));
                return new DamageOutcome(HitResult.Blocked, 0f, 0f, 0f, false);
            }

            float reduction = SkillParams.Get(spec, "damage_reduction_pct");
            int n = hit.ComponentCount < MaxComponents ? hit.ComponentCount : MaxComponents;
            for (int i = 0; i < n; i++) _scratch[i] = hit.GetComponent(i);
            hit.ClearComponents();
            for (int i = 0; i < n; i++) hit.AddComponent(_scratch[i].Type, _scratch[i].Value * (1f - reduction));
            Absorb(spec, total * reduction);
            return _inner.ReceiveHit(hit, hitDirection);
        }

        private void Absorb(SkillSpec spec, float amount)
        {
            AbsorbedThisStance += amount;
            _ctx.FlatDamage += amount * SkillParams.Get(spec, "absorbed_to_counter_pct");
        }
    }
}
