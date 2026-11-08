using System;
using Eclipse.Core.Stats;
using Eclipse.Data.Heroes;

namespace Eclipse.Gameplay.Heroes.Kael
{
    /// <summary>Numbers of Kael's innate passives (08 4.2), read by key from the params of resource.fury.</summary>
    public readonly struct KaelPassiveRules
    {
        /// <summary>Fury per Embers of Rage step (10).</summary>
        public readonly float EmbersFuryStep;
        /// <summary>Increased Physical/Ember Damage per step, as a fraction (0.02).</summary>
        public readonly float EmbersIncreasedPerStep;
        /// <summary>Health fraction below which Ashbound Resolve is active (0.35).</summary>
        public readonly float ResolveHealthThreshold;
        public readonly float ResolveDamageReduction;
        public readonly float ResolveResourceGeneration;

        public KaelPassiveRules(float embersFuryStep, float embersIncreasedPerStep, float resolveHealthThreshold,
            float resolveDamageReduction, float resolveResourceGeneration)
        {
            EmbersFuryStep = embersFuryStep;
            EmbersIncreasedPerStep = embersIncreasedPerStep;
            ResolveHealthThreshold = resolveHealthThreshold;
            ResolveDamageReduction = resolveDamageReduction;
            ResolveResourceGeneration = resolveResourceGeneration;
        }

        public static KaelPassiveRules FromDefinition(ResourceDefinition d)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            return new KaelPassiveRules(d.GetParam("embers_fury_step"), d.GetParam("embers_increased_pct_per_step") / 100f,
                d.GetParam("resolve_health_threshold_pct") / 100f, d.GetParam("resolve_damage_reduction_pct") / 100f,
                d.GetParam("resolve_resource_generation_pct") / 100f);
        }
    }

    /// <summary>
    /// Embers of Rage and Ashbound Resolve (08 4.2) as <see cref="StatMod"/> sets owned by one <see cref="SourceHandle"/> each.
    /// Mods are swapped only when the step or the low-health state changes, so the snapshot of skills stays valid (11 3.5.3).
    /// Call <see cref="Refresh"/> after Fury or health changes (once per tick is enough).
    /// </summary>
    public sealed class KaelPassives
    {
        // Float accumulation guard so 29.9999 Fury still counts as 3 steps.
        private const float StepEpsilon = 1e-3f;

        private readonly StatBlock _stats;
        private readonly KaelPassiveRules _rules;
        private readonly SourceHandle _embersSource;
        private readonly SourceHandle _resolveSource;
        private int _embersSteps;
        private bool _resolveActive;

        public KaelPassives(StatBlock stats, in KaelPassiveRules rules)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _rules = rules;
            _embersSource = stats.CreateSource();
            _resolveSource = stats.CreateSource();
        }

        /// <summary>Completed Embers of Rage steps (each 10 Fury).</summary>
        public int EmbersSteps => _embersSteps;
        public bool ResolveActive => _resolveActive;
        /// <summary>Current Increased Physical/Ember bonus as a fraction (0.20 at 100 Fury, 0.30 at 150).</summary>
        public float EmbersBonus => _embersSteps * _rules.EmbersIncreasedPerStep;

        public void Refresh(float currentFury, float currentHealth, float maxHealth)
        {
            int steps = _rules.EmbersFuryStep > 0f ? (int)Math.Floor(currentFury / _rules.EmbersFuryStep + StepEpsilon) : 0;
            if (steps < 0) steps = 0;
            if (steps != _embersSteps)
            {
                _embersSteps = steps;
                _stats.RemoveSource(_embersSource);
                if (steps > 0)
                {
                    float v = steps * _rules.EmbersIncreasedPerStep;
                    _stats.AddMod(_embersSource, new StatMod(StatId.PhysicalDamage, ModBucket.Increased, v));
                    _stats.AddMod(_embersSource, new StatMod(StatId.EmberDamage, ModBucket.Increased, v));
                }
            }

            bool low = maxHealth > 0f && currentHealth < maxHealth * _rules.ResolveHealthThreshold;
            if (low != _resolveActive)
            {
                _resolveActive = low;
                _stats.RemoveSource(_resolveSource);
                if (low)
                {
                    // Stats in % use Base 0 + Flat (CLAUDE.md M03); DamageReduction is the final multiplier of 01 3.4.1 (cap 40% from data).
                    _stats.AddMod(_resolveSource, new StatMod(StatId.DamageReduction, ModBucket.Flat, _rules.ResolveDamageReduction));
                    _stats.AddMod(_resolveSource, new StatMod(StatId.ResourceGeneration, ModBucket.Flat, _rules.ResolveResourceGeneration));
                }
            }
        }
    }
}
