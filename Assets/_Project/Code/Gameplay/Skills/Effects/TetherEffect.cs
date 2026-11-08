using System;
using System.Collections.Generic;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>
    /// Links the caster to each target hit (Burning Chains). The link breaks when its duration ends or the target moves farther than the
    /// break distance; on break the <c>onBreak</c> effects run on that target (break damage, 08 4.4). Duration and distance are literals
    /// or skill params. Does not Root by itself: add an <see cref="ApplyStateEffect"/> next to it.
    /// </summary>
    [Serializable]
    public sealed class TetherEffect : SkillEffect
    {
        [SerializeField] private string durationParam;
        [SerializeField] private float durationSeconds;
        [SerializeField] private string breakDistanceParam;
        [SerializeField] private float breakDistance;
        [SerializeReference] private List<Effect> onBreak = new List<Effect>();

        private SkillEffect[] _resolved;

        public TetherEffect() { }

        public TetherEffect(string durationParam, float durationSeconds, string breakDistanceParam, float breakDistance,
            params SkillEffect[] onBreak)
        {
            this.durationParam = durationParam;
            this.durationSeconds = durationSeconds;
            this.breakDistanceParam = breakDistanceParam;
            this.breakDistance = breakDistance;
            this.onBreak = new List<Effect>(onBreak ?? Array.Empty<SkillEffect>());
        }

        public override void Apply(SkillExecContext ctx, SkillTarget target)
        {
            SkillSpec spec = ctx.Cast.Spec;
            if (target == null || ctx.Tethers == null || spec == null) return;
            int frames = CombatStateRules.Frames(SkillParams.Resolve(spec, durationParam, durationSeconds));
            float distance = SkillParams.Resolve(spec, breakDistanceParam, breakDistance);
            ctx.Tethers.Add(ctx, target.Id, distance, frames, Resolved());
        }

        private SkillEffect[] Resolved()
        {
            if (_resolved != null) return _resolved;
            var list = new List<SkillEffect>(onBreak.Count);
            for (int i = 0; i < onBreak.Count; i++)
                if (onBreak[i] is SkillEffect fx) list.Add(fx);
            _resolved = list.ToArray();
            return _resolved;
        }
    }
}
