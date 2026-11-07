using System;

namespace Eclipse.Core.Attributes
{
    /// <summary>The four hero resources (01 3.8, 08).</summary>
    public enum ResourceKind { Fury, ArcaneCharges, EchoEnergy, VoidConstructs }

    /// <summary>Shape of a resource: continuous pool or discrete count.</summary>
    public enum ResourceModel { Pool, Discrete }

    /// <summary>What the "Resto" (remainder) of Maximum Resource converts into for discrete resources (01 3.8).</summary>
    public enum RestBonusKind
    {
        None,
        /// <summary>Increased damage of skills that consume Charges (Lyra).</summary>
        ChargeSkillDamage,
        /// <summary>Increased life and damage of Constructs (Orun).</summary>
        ConstructLifeAndDamage,
    }

    /// <summary>
    /// Data describing how a hero translates the generic Maximum Resource attribute (01 3.8, R01).
    /// Use <see cref="CreatePool"/> or <see cref="CreateDiscrete"/>; all numbers come from data.
    /// </summary>
    public sealed class ResourceTranslationRule
    {
        private ResourceTranslationRule() { }

        public ResourceKind Kind { get; private set; }
        public ResourceModel Model { get; private set; }
        /// <summary>Pool: base maximum. Discrete: base count.</summary>
        public float BaseValue { get; private set; }
        /// <summary>Pool only: points of maximum per 1.0 (=100%) of Maximum Resource.</summary>
        public float PoolPointsPerFraction { get; private set; }
        /// <summary>Pool only: hard cap of the maximum.</summary>
        public float PoolCap { get; private set; }
        /// <summary>Discrete only: Maximum Resource fraction per +1 unit (0.25 or 0.30).</summary>
        public float StepFraction { get; private set; }
        /// <summary>Discrete only: default count ceiling (R01: 6).</summary>
        public int BaseCap { get; private set; }
        /// <summary>Discrete only: ceiling that explicit exceptions may reach (R01: 8).</summary>
        public int ExceptionCap { get; private set; }
        public RestBonusKind RestKind { get; private set; }
        /// <summary>Discrete only: rest bonus fraction per 1 percentage point of leftover Maximum Resource.</summary>
        public float RestBonusPerPoint { get; private set; }

        public static ResourceTranslationRule CreatePool(ResourceKind kind, float baseMaximum, float pointsPerFraction, float cap)
        {
            if (cap < baseMaximum) throw new ArgumentException("cap must be >= baseMaximum.", nameof(cap));
            return new ResourceTranslationRule
            {
                Kind = kind, Model = ResourceModel.Pool, BaseValue = baseMaximum,
                PoolPointsPerFraction = pointsPerFraction, PoolCap = cap,
            };
        }

        public static ResourceTranslationRule CreateDiscrete(ResourceKind kind, int baseCount, float stepFraction,
            int baseCap, int exceptionCap, RestBonusKind restKind, float restBonusPerPoint)
        {
            if (stepFraction <= 0f) throw new ArgumentOutOfRangeException(nameof(stepFraction));
            if (baseCap < baseCount) throw new ArgumentException("baseCap must be >= baseCount.", nameof(baseCap));
            if (exceptionCap < baseCap) throw new ArgumentException("exceptionCap must be >= baseCap.", nameof(exceptionCap));
            return new ResourceTranslationRule
            {
                Kind = kind, Model = ResourceModel.Discrete, BaseValue = baseCount, StepFraction = stepFraction,
                BaseCap = baseCap, ExceptionCap = exceptionCap, RestKind = restKind, RestBonusPerPoint = restBonusPerPoint,
            };
        }
    }

    /// <summary>Result of translating Maximum Resource for one hero.</summary>
    public readonly struct ResourceMaximum
    {
        public ResourceMaximum(ResourceKind kind, float maximum, int count, float restFraction, float restBonus, RestBonusKind restKind)
        {
            Kind = kind;
            Maximum = maximum;
            Count = count;
            RestFraction = restFraction;
            RestBonus = restBonus;
            RestKind = restKind;
        }

        public ResourceKind Kind { get; }
        /// <summary>Resource maximum (pool value, or the count as float for discrete resources).</summary>
        public float Maximum { get; }
        /// <summary>Discrete count (0 for pools).</summary>
        public int Count { get; }
        /// <summary>Leftover Maximum Resource, as a fraction, not consumed by units.</summary>
        public float RestFraction { get; }
        /// <summary>Rest converted to a bonus fraction (0.04 = +4%).</summary>
        public float RestBonus { get; }
        public RestBonusKind RestKind { get; }
    }

    /// <summary>Pure translation of Maximum Resource into each hero's resource (01 3.8, R01).</summary>
    public static class ResourceTranslation
    {
        // Absorbs float error so that e.g. 0.9 / 0.3 counts as exactly 3 steps.
        private const float StepEpsilon = 1e-4f;

        /// <summary>
        /// Translates <paramref name="maximumResource"/> (fraction, 0.15 = 15%) for <paramref name="rule"/>.
        /// <paramref name="discreteCapOverride"/> raises the discrete ceiling above BaseCap (Keystones, Augments,
        /// listed items: R01); it is clamped to [BaseCap, ExceptionCap]. Pass 0 for the default ceiling.
        /// </summary>
        public static ResourceMaximum Translate(ResourceTranslationRule rule, float maximumResource, int discreteCapOverride = 0)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (maximumResource < 0f) maximumResource = 0f;

            if (rule.Model == ResourceModel.Pool)
            {
                float max = Math.Min(rule.BaseValue + maximumResource * rule.PoolPointsPerFraction, rule.PoolCap);
                return new ResourceMaximum(rule.Kind, max, 0, 0f, 0f, RestBonusKind.None);
            }

            int cap = Math.Min(Math.Max(discreteCapOverride, rule.BaseCap), rule.ExceptionCap);
            int baseCount = (int)rule.BaseValue;
            int room = Math.Max(cap - baseCount, 0);
            int steps = (int)Math.Floor(maximumResource / rule.StepFraction + StepEpsilon);
            if (steps > room) steps = room;
            float rest = Math.Max(maximumResource - steps * rule.StepFraction, 0f);
            float bonus = rest * 100f * rule.RestBonusPerPoint;
            int count = baseCount + steps;
            return new ResourceMaximum(rule.Kind, count, count, rest, bonus, rule.RestKind);
        }
    }
}
