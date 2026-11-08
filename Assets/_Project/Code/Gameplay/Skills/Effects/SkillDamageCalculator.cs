using System;
using Eclipse.Core.Damage;
using Eclipse.Core.Random;
using Eclipse.Core.Stats;
using UnityEngine;

namespace Eclipse.Gameplay.Skills
{
    /// <summary>"Gain X% of From as To" (06 3.2 step 2a) or "convert X% of From to To" (2b), authored on a Damage effect.</summary>
    [Serializable]
    public struct DamageTransfer
    {
        [SerializeField] private DamageType from;
        [SerializeField] private DamageType to;
        /// <summary>Fraction, 0.4 = 40%.</summary>
        [SerializeField] private float fraction;

        public DamageTransfer(DamageType from, DamageType to, float fraction)
        {
            this.from = from; this.to = to; this.fraction = fraction;
        }

        public DamageType From => from;
        public DamageType To => to;
        public float Fraction => fraction;
    }

    /// <summary>
    /// Skill side of the offensive pipeline (06 3.2): Base = Weapon Power x coefficient (+ flat), conversion / gain-as-extra, then the
    /// Core <see cref="OffensiveDamageResolver"/> does Increased (Skill/Area/type), More, crit, target mitigation, Vulnerability and the
    /// minimum of 1. Owns the scratch objects, so resolving allocates nothing. Not thread-safe.
    /// </summary>
    public sealed class SkillDamageCalculator
    {
        private readonly OffensiveDamageResolver _resolver;
        private readonly OffensiveRequest _request = new OffensiveRequest();
        private readonly TargetDefense _defense = new TargetDefense();
        private StatBlock _sourceStats;
        private StatBlockDamageSource _source;

        public SkillDamageCalculator(OffensiveRules rules)
        {
            _resolver = new OffensiveDamageResolver(rules);
        }

        /// <summary>
        /// Resolves <paramref name="hit"/> (Tags/Kind/Flags already set). <paramref name="baseDamage"/> = Weapon Power x coefficient.
        /// Returns the total damage; the components are written on the hit.
        /// </summary>
        public float Resolve(DamageInstance hit, StatBlock attacker, DamageType type, float baseDamage, float flatDamage,
            DamageTransfer[] extras, DamageTransfer[] conversions, float armorPercent, float vulnerability, RngStreams rng)
        {
            _request.Clear();
            _request.AddBase(type, baseDamage);
            if (flatDamage > 0f) _request.AddFlat(type, flatDamage);
            if (extras != null)
                for (int i = 0; i < extras.Length; i++) _request.AddGainAsExtra(extras[i].From, extras[i].To, extras[i].Fraction);
            if (conversions != null)
                for (int i = 0; i < conversions.Length; i++) _request.AddConversion(conversions[i].From, conversions[i].To, conversions[i].Fraction);

            _defense.ArmorPercent = armorPercent;
            _defense.Vulnerability = vulnerability;
            return _resolver.Resolve(_request, hit, attacker, SourceFor(attacker), _defense, rng);
        }

        private StatBlockDamageSource SourceFor(StatBlock stats)
        {
            if (_source == null || !ReferenceEquals(_sourceStats, stats))
            {
                _sourceStats = stats;
                _source = new StatBlockDamageSource(stats);
            }
            return _source;
        }
    }
}
