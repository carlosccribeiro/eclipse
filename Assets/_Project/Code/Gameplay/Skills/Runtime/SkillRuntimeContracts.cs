using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Heroes;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>Outcome of <see cref="SkillController.TryCast"/>.</summary>
    public enum CastResult : byte
    {
        Started = 0,
        /// <summary>Spec is null (no skill in the slot).</summary>
        NoSkill,
        Stunned,
        Frozen,
        Silenced,
        /// <summary>Root blocks only skills tagged Movement (06 3.8.3).</summary>
        Rooted,
        /// <summary>Another action is running and the cancel table does not allow this skill yet.</summary>
        Busy,
        OnCooldown,
        InsufficientResource,
    }

    /// <summary>CC state of the caster. Adapter over <c>StateComponent</c> in <see cref="StateComponentSkillState"/>.</summary>
    public interface ISkillState
    {
        bool IsStunned(int frame);
        bool IsFrozen(int frame);
        bool IsSilenced(int frame);
        bool IsRooted(int frame);
    }

    /// <summary>Hero resource seen by the controller (pool-type resources only in M08).</summary>
    public interface ISkillResource
    {
        float Current { get; }
        /// <summary>True when the implementation already publishes OnResourceSpent (ResourceComponent does); the controller then does not.</summary>
        bool PublishesSpentEvent { get; }
        bool TrySpend(float amount);
        /// <summary>Spends everything and returns the amount.</summary>
        float SpendAll();
        /// <summary>Grants the base amount; the implementation applies Resource Generation.</summary>
        void Gain(float amount);
    }

    /// <summary>Cancel table of M05 (06 3.5): lets a skill interrupt the running basic/heavy attack.</summary>
    public interface IAttackCancelHost
    {
        bool CanCancelInto(ActionKind to);
        void CancelForAction();
    }

    /// <summary>What the Delivery runner needs to know about the running cast.</summary>
    public readonly struct SkillCastInfo
    {
        public readonly SkillSpec Spec;
        public readonly int StartFrame;
        /// <summary>Resource spent by this cast (Worldbreaker: whole Fury).</summary>
        public readonly float ResourceSpent;
        /// <summary>Base coefficient plus the per-resource part (08 4.4).</summary>
        public readonly float DamageCoefficient;

        public SkillCastInfo(SkillSpec spec, int startFrame, float resourceSpent, float damageCoefficient)
        {
            Spec = spec;
            StartFrame = startFrame;
            ResourceSpent = resourceSpent;
            DamageCoefficient = damageCoefficient;
        }
    }

    /// <summary>
    /// Boundary to Delivery/Effects (T4). The controller calls it on each phase entry; the runner creates DamageInstances
    /// (use <see cref="SkillController.PrepareDamage"/> for Generation 0) and reports hits via <see cref="SkillController.ReportHit"/>.
    /// </summary>
    public interface ISkillDeliveryRunner
    {
        void OnPhaseEnter(in SkillCastInfo cast, SkillPhase phase, int frame);
        void OnCastEnd(in SkillCastInfo cast, bool cancelled, int frame);
    }
}
