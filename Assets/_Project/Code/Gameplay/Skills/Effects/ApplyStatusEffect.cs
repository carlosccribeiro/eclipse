using System;
using Eclipse.Core.Ids;
using Eclipse.Gameplay.Combat;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Applies a status through <see cref="IStatusReceiver"/> (07). M05 ships only the provisional Burning stub behind that contract; M11
    /// swaps the implementation without touching this effect. Generation travels in the request (R07); at Generation 2 the Potency is
    /// halved (11 3.6.5 "eventos de Gen 2 aplicam status a 50% de Status Potency").
    /// </summary>
    [Serializable]
    public sealed class ApplyStatusEffect : SkillEffect
    {
        /// <summary>11 3.6.5: structural multiplier for statuses applied by Generation 2 events.</summary>
        public const float Generation2PotencyMultiplier = 0.5f;

        [SerializeField] private string statusId;
        [SerializeField] private int stacks = 1;
        [SerializeField] private string durationParam;
        /// <summary>Literal duration; 0 = the status default.</summary>
        [SerializeField] private float durationSeconds;
        [SerializeField] private float potency = 1f;
        [SerializeField] private bool guaranteed = true;

        public ApplyStatusEffect() { }

        public ApplyStatusEffect(string statusId, int stacks = 1, string durationParam = null, float durationSeconds = 0f,
            float potency = 1f, bool guaranteed = true)
        {
            this.statusId = statusId;
            this.stacks = stacks;
            this.durationParam = durationParam;
            this.durationSeconds = durationSeconds;
            this.potency = potency;
            this.guaranteed = guaranteed;
        }

        public override void Apply(SkillExecContext ctx, SkillTarget target)
        {
            if (target == null || target.Status == null || string.IsNullOrEmpty(statusId)) return;
            SkillSpec spec = ctx.Cast.Spec;
            float seconds = spec != null ? SkillParams.Resolve(spec, durationParam, durationSeconds) : durationSeconds;
            int frames = seconds > 0f ? CombatStateRules.Frames(seconds) : 0;
            float p = ctx.Generation >= 2 ? potency * Generation2PotencyMultiplier : potency;
            var request = new StatusRequest(StableId.Parse(statusId), ctx.Caster.Id, target.Id, ctx.Generation, frames, stacks, p,
                guaranteed, ctx.ExtraFlags);
            target.Status.TryApply(in request);
        }
    }
}
