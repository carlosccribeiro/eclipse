using Eclipse.AI.StateMachine;
using Eclipse.Core.Random;

namespace Eclipse.AI.Utility
{
    /// <summary>
    /// Scores available actions (distance, angle, cooldown, nearby allies, own HP) and picks the best, with
    /// <see cref="UtilityConfig.RandomChance"/> (15%, 09 section 3.1) of a utility-weighted random pick instead.
    /// RNG: stream Combat only. Consumes one float per call with at least one candidate (plus one if randomized).
    /// Allocation free after construction.
    /// </summary>
    public sealed class UtilitySelector
    {
        private readonly UtilityConfig _config;
        private readonly RngStreams _rng;
        private readonly UtilityAction[] _actions;
        private readonly int[] _attackIdx;
        private readonly float[] _scores;
        private int _count;

        public UtilitySelector(UtilityConfig config, RngStreams rng)
        {
            _config = config;
            _rng = rng;
            int cap = config.Attacks.Length + 2;
            _actions = new UtilityAction[cap];
            _attackIdx = new int[cap];
            _scores = new float[cap];
        }

        /// <summary>Chooses an action. <paramref name="allowAttack"/> false excludes attacks (e.g. token denied).</summary>
        public void Select(in AiPerception p, bool allowAttack, out UtilityAction action, out int attackIndex,
            out float score, out bool randomized)
        {
            action = UtilityAction.None;
            attackIndex = -1;
            score = 0f;
            randomized = false;

            // Own HP: deterministic flee, no RNG consumed.
            if (_config.FleeHpFraction > 0f && p.HpFraction <= _config.FleeHpFraction)
            {
                action = UtilityAction.Flee;
                score = 1f;
                return;
            }

            _count = 0;
            bool anyReady = false;
            float crowd = _config.CrowdAllyCount > 0 ? Clamp01((float)p.AlliesNearby / _config.CrowdAllyCount) : 0f;
            var attacks = _config.Attacks;
            for (int i = 0; i < attacks.Length; i++)
            {
                var a = attacks[i];
                float cd = p.CooldownsRemaining != null && i < p.CooldownsRemaining.Length ? p.CooldownsRemaining[i] : 0f;
                if (cd > 0f || p.Distance < a.MinRange || p.Distance > a.MaxRange || p.AngleDeg > a.MaxAngleDeg) continue;
                anyReady = true;
                if (!allowAttack) continue;
                float angleFit = a.MaxAngleDeg > 0f ? 1f - p.AngleDeg / a.MaxAngleDeg : 1f;
                float s = _config.WeightAttack * a.Weight * angleFit * (1f + _config.AllyAttackBonus * crowd);
                if (s > 0f) Add(UtilityAction.Attack, i, s);
            }

            bool inRange = p.Distance <= _config.DesiredRange;
            if (!inRange && _config.ApproachFalloffDistance > 0f)
            {
                float s = _config.WeightApproach * Clamp01((p.Distance - _config.DesiredRange) / _config.ApproachFalloffDistance);
                if (s > 0f) Add(UtilityAction.Approach, -1, s);
            }

            float repoFactor = p.AngleDeg / 180f;
            if (crowd > repoFactor) repoFactor = crowd;
            if (inRange && !anyReady) repoFactor = 1f;
            float rs = _config.WeightReposition * Clamp01(repoFactor);
            if (rs > 0f) Add(UtilityAction.Reposition, -1, rs);

            if (_count == 0) return;

            ref Xoshiro128StarStar gen = ref _rng.Get(RngStream.Combat);
            float roll = gen.NextFloat01();
            int pick = 0;
            if (roll < _config.RandomChance)
            {
                randomized = true;
                float total = 0f;
                for (int i = 0; i < _count; i++) total += _scores[i];
                float r = gen.NextFloat01() * total;
                pick = _count - 1;
                for (int i = 0; i < _count; i++)
                {
                    if (r < _scores[i]) { pick = i; break; }
                    r -= _scores[i];
                }
            }
            else
            {
                for (int i = 1; i < _count; i++) if (_scores[i] > _scores[pick]) pick = i;
            }

            action = _actions[pick];
            attackIndex = _attackIdx[pick];
            score = _scores[pick];
        }

        private void Add(UtilityAction a, int attackIdx, float s)
        {
            _actions[_count] = a;
            _attackIdx[_count] = attackIdx;
            _scores[_count] = s;
            _count++;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
