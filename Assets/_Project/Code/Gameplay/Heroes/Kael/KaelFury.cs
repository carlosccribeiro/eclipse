using System;
using Eclipse.Core.Stats;
using Eclipse.Data.Heroes;
using Eclipse.Gameplay.Combat;

namespace Eclipse.Gameplay.Heroes.Kael
{
    /// <summary>Fury gain numbers (08 4.1), read by key from the params of resource.fury (Resources.csv).</summary>
    public readonly struct FuryGainRules
    {
        public readonly float BasicFirstTarget, BasicExtraTarget, BasicMaxPerHit;
        public readonly float HeavyTap, HeavyCharged;
        public readonly float DamageTakenPerPercentHealth, DamageTakenMaxPerSecond;
        public readonly float Kill;

        public FuryGainRules(float basicFirstTarget, float basicExtraTarget, float basicMaxPerHit, float heavyTap, float heavyCharged,
            float damageTakenPerPercentHealth, float damageTakenMaxPerSecond, float kill)
        {
            BasicFirstTarget = basicFirstTarget;
            BasicExtraTarget = basicExtraTarget;
            BasicMaxPerHit = basicMaxPerHit;
            HeavyTap = heavyTap;
            HeavyCharged = heavyCharged;
            DamageTakenPerPercentHealth = damageTakenPerPercentHealth;
            DamageTakenMaxPerSecond = damageTakenMaxPerSecond;
            Kill = kill;
        }

        public static FuryGainRules FromDefinition(ResourceDefinition d)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            return new FuryGainRules(d.GetParam("basic_first_target"), d.GetParam("basic_extra_target"), d.GetParam("basic_max_per_hit"),
                d.GetParam("heavy_tap"), d.GetParam("heavy_charged"), d.GetParam("damage_taken_per_pct_hp"),
                d.GetParam("damage_taken_max_per_s"), d.GetParam("kill"));
        }
    }

    /// <summary>
    /// Kael's Fury rules (08 4.1) over a <see cref="ResourcePool"/>: basic/heavy/damage-taken/kill gains, Resource Generation multiplier,
    /// Maximum Resource sync and decay activity. Pure C#; the caller drives it from combat events and ticks the pool.
    /// </summary>
    public sealed class KaelFury
    {
        // "/s" in 08 4.1 (damage taken cap, 15 Fury/s) is measured over a sliding 1-second window: structural unit of the rule.
        private const double DamageTakenWindowSeconds = 1.0;

        private readonly FuryGainRules _rules;
        private readonly StatBlock _stats;
        private double _time;
        private double _windowStart = double.NegativeInfinity;
        private float _windowGained;
        private int _seenKills, _seenTaken;

        public KaelFury(ResourcePool pool, in FuryGainRules rules, StatBlock stats)
        {
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _rules = rules;
            _stats = stats;
            SyncMaximum();
        }

        public ResourcePool Pool { get; }

        /// <summary>1 + Resource Generation (08 3.2, Ashbound Resolve +25%). Applied to every Fury gain.</summary>
        public float GenerationMultiplier => _stats == null ? 1f : 1f + _stats.Evaluate(StatId.ResourceGeneration);

        /// <summary>Re-reads Maximum Resource (+1 Fury per 1%, capped) from the stats; call when equipment changes.</summary>
        public void SyncMaximum() => Pool.SetMaximumResourceStat(_stats == null ? 0f : _stats.Evaluate(StatId.MaximumResource));

        /// <summary>Advances time (damage window) and the pool decay. Call once per fixed tick.</summary>
        public void Tick(float deltaSeconds)
        {
            _time += deltaSeconds;
            Pool.Tick(deltaSeconds);
        }

        /// <summary>
        /// Basic attack swing: +first on the first target, +extra per additional target, max per hit; times Proc Coefficient (06 3.2).
        /// Call once per swing with the number of distinct targets hit. Returns Fury gained.
        /// </summary>
        public float OnBasicHit(int targetsHit, float procCoefficient = 1f)
        {
            if (targetsHit <= 0) return 0f;
            Pool.MarkActivity();
            float raw = Math.Min(_rules.BasicMaxPerHit, _rules.BasicFirstTarget + _rules.BasicExtraTarget * (targetsHit - 1));
            return Pool.Add(raw * procCoefficient * GenerationMultiplier);
        }

        /// <summary>Heavy attack that connected: tap or fully charged value, times Proc Coefficient. Returns Fury gained.</summary>
        public float OnHeavyHit(bool fullyCharged, float procCoefficient = 1f)
        {
            Pool.MarkActivity();
            float raw = fullyCharged ? _rules.HeavyCharged : _rules.HeavyTap;
            return Pool.Add(raw * procCoefficient * GenerationMultiplier);
        }

        /// <summary>Any damage dealt (including skills/DoT) keeps decay from starting (08 4.1).</summary>
        public void OnDamageDealt() => Pool.MarkActivity();

        /// <summary>Kill: flat gain, not an on-hit source so Proc Coefficient does not apply. Returns Fury gained.</summary>
        public float OnKill()
        {
            Pool.MarkActivity();
            return Pool.Add(_rules.Kill * GenerationMultiplier);
        }

        /// <summary>
        /// Damage received: +per 1% of max health lost, capped per second (before the generation multiplier). Returns Fury gained.
        /// </summary>
        public float OnDamageTaken(float healthLost, float maxHealth)
        {
            Pool.MarkActivity();
            if (healthLost <= 0f || maxHealth <= 0f) return 0f;
            if (_time - _windowStart >= DamageTakenWindowSeconds)
            {
                _windowStart = _time;
                _windowGained = 0f;
            }
            float raw = healthLost / maxHealth * 100f * _rules.DamageTakenPerPercentHealth;
            float allowed = Math.Max(0f, _rules.DamageTakenMaxPerSecond - _windowGained);
            raw = Math.Min(raw, allowed);
            _windowGained += raw;
            return Pool.Add(raw * GenerationMultiplier);
        }

        /// <summary>Worldbreaker and similar: spend all Fury (08 4.4). Returns the amount spent.</summary>
        public float SpendAll() => Pool.SpendAll();

        /// <summary>Cost-based spend after Resource Cost Reduction (08 3.2). Returns false if there is not enough Fury.</summary>
        public bool TrySpend(float baseCost)
        {
            float cost = baseCost * Math.Max(0f, 1f - (_stats == null ? 0f : _stats.Evaluate(StatId.ResourceCostReduction)));
            return Pool.TrySpend(cost);
        }

        /// <summary>Start of room: 0 Fury (08 4.1). Rest/Shrine do not call this.</summary>
        public void ResetForRoom()
        {
            Pool.ResetForRoom();
            _windowStart = double.NegativeInfinity;
            _windowGained = 0f;
        }

        /// <summary>
        /// Reads kills by <paramref name="owner"/> and hits taken by it from the frame stream (new events since the last call).
        /// Call once per tick after combat; the buffers are cleared by EndFrame, which this detects by a shrinking count.
        /// </summary>
        public void ConsumeEvents(CombatEventStream events, EntityId owner, float maxHealth)
        {
            if (events == null) return;
            var kills = events.Read<OnKill>();
            if (kills.Count < _seenKills) _seenKills = 0;
            for (; _seenKills < kills.Count; _seenKills++)
                if (kills[_seenKills].Source == owner) OnKill();

            var taken = events.Read<OnHitTaken>();
            if (taken.Count < _seenTaken) _seenTaken = 0;
            for (; _seenTaken < taken.Count; _seenTaken++)
            {
                ref readonly OnHitTaken e = ref taken[_seenTaken];
                if (e.Target == owner) OnDamageTaken(e.Value, maxHealth);
            }
        }
    }
}
