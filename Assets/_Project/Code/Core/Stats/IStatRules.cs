namespace Eclipse.Core.Stats
{
    /// <summary>
    /// Cap / diminishing-returns rule of one stat (01 3.1, 3.6). All members optional; <see cref="None"/>
    /// means uncapped. Values come from data, never from runtime code.
    /// </summary>
    public readonly struct StatRule
    {
        // 01 3.1: the diminishing-returns formula itself fixes the factor above the soft cap at 0.5 for every stat
        // (structural rule of the contract); callers may still override it per stat from data.
        public const float DefaultAboveSoftFactor = 0.5f;

        public StatRule(float? softCap, float? hardCap, float aboveSoftFactor = DefaultAboveSoftFactor, float? minimum = null)
        {
            SoftCap = softCap;
            HardCap = hardCap;
            AboveSoftFactor = aboveSoftFactor;
            Minimum = minimum;
        }

        /// <summary>Raw value up to which the stat counts 1:1.</summary>
        public float? SoftCap { get; }
        /// <summary>Maximum final value.</summary>
        public float? HardCap { get; }
        /// <summary>Fraction of the excess above <see cref="SoftCap"/> that counts (01 3.1 uses 0.5).</summary>
        public float AboveSoftFactor { get; }
        /// <summary>Minimum final value.</summary>
        public float? Minimum { get; }

        /// <summary>No soft cap, no hard cap, no minimum.</summary>
        public static StatRule None => new StatRule(null, null, 1f, null);
    }

    /// <summary>Source of per-stat rules (typically fed by StatDefinition data).</summary>
    public interface IStatRules
    {
        StatRule Get(StatId stat);
    }

    /// <summary>Array-backed <see cref="IStatRules"/>; unset stats are uncapped.</summary>
    public sealed class StatRuleTable : IStatRules
    {
        private readonly StatRule[] _rules = new StatRule[StatIds.Count];
        private readonly bool[] _set = new bool[StatIds.Count];

        public StatRuleTable Set(StatId stat, StatRule rule)
        {
            _rules[(int)stat] = rule;
            _set[(int)stat] = true;
            return this;
        }

        public StatRule Get(StatId stat)
        {
            int i = (int)stat;
            return (uint)i < (uint)_rules.Length && _set[i] ? _rules[i] : StatRule.None;
        }
    }
}
