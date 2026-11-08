using System;
using Eclipse.Data.Heroes;

namespace Eclipse.Gameplay.Heroes
{
    /// <summary>
    /// Numbers of a pool-type hero resource (08 3.2 / 4.1). Built from <see cref="ResourceDefinition"/> (Resources.csv); no values live in code.
    /// </summary>
    public readonly struct ResourceRules
    {
        public readonly float BaseMaximum;
        /// <summary>Hard ceiling after Maximum Resource (00 R01; Fury 150).</summary>
        public readonly float AbsoluteCap;
        /// <summary>Maximum added per 1% of the Maximum Resource attribute (08 3.2).</summary>
        public readonly float MaximumPerPercent;
        public readonly float DecayDelaySeconds;
        public readonly float DecayPerSecond;
        public readonly float StartOfRoomValue;

        public ResourceRules(float baseMaximum, float absoluteCap, float maximumPerPercent, float decayDelaySeconds,
            float decayPerSecond, float startOfRoomValue)
        {
            BaseMaximum = baseMaximum;
            AbsoluteCap = absoluteCap;
            MaximumPerPercent = maximumPerPercent;
            DecayDelaySeconds = decayDelaySeconds;
            DecayPerSecond = decayPerSecond;
            StartOfRoomValue = startOfRoomValue;
        }

        public static ResourceRules FromDefinition(ResourceDefinition d)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            return new ResourceRules(d.BaseMaximum, d.AbsoluteCap, d.MaximumPerMaxResourcePercent,
                d.DecayDelaySeconds, d.DecayPerSecond, d.StartOfRoomValue);
        }
    }

    /// <summary>Receives resource transitions from a <see cref="ResourcePool"/> (published as OnResource* combat events by the component).</summary>
    public interface IResourceSink
    {
        void Gained(float amount);
        void Spent(float amount);
        void Full();
        void Empty();
    }

    /// <summary>
    /// Pure numeric pool: current value, dynamic maximum, delayed decay, spend and room reset (08 4.1). No Unity types, no allocation per call.
    /// Gain multipliers (Resource Generation, Proc Coefficient) are applied by the caller; <see cref="Add"/> takes the final amount.
    /// </summary>
    public sealed class ResourcePool
    {
        // Tolerance for float accumulation when comparing against the maximum or zero.
        private const float Epsilon = 1e-4f;

        private readonly ResourceRules _rules;
        private readonly IResourceSink _sink;
        private double _idleSeconds;

        public ResourcePool(in ResourceRules rules, IResourceSink sink = null)
        {
            _rules = rules;
            _sink = sink;
            Maximum = Math.Min(rules.BaseMaximum, rules.AbsoluteCap);
            Current = rules.StartOfRoomValue;
        }

        public float Current { get; private set; }
        public float Maximum { get; private set; }
        public bool IsFull => Current >= Maximum - Epsilon;
        public bool IsEmpty => Current <= Epsilon;
        /// <summary>Seconds since the last damage dealt or received.</summary>
        public double IdleSeconds => _idleSeconds;

        /// <summary>
        /// Recomputes the maximum from the Maximum Resource stat (fraction: 0.30 = +30%): base + perPercent * percent, clamped to the cap (08 3.2).
        /// Current is clamped silently if the maximum shrinks.
        /// </summary>
        public void SetMaximumResourceStat(float maximumResourceFraction)
        {
            float bonus = Math.Max(0f, maximumResourceFraction) * 100f * _rules.MaximumPerPercent;
            Maximum = Math.Min(_rules.AbsoluteCap, _rules.BaseMaximum + bonus);
            if (Current > Maximum) Current = Maximum;
        }

        /// <summary>Marks combat activity (damage dealt or received), restarting the decay delay.</summary>
        public void MarkActivity() => _idleSeconds = 0;

        /// <summary>Adds <paramref name="amount"/> (already multiplied), clamped to the maximum. Returns the amount actually gained.</summary>
        public float Add(float amount)
        {
            if (amount <= 0f) return 0f;
            bool wasFull = IsFull;
            float before = Current;
            Current = Math.Min(Maximum, Current + amount);
            float gained = Current - before;
            if (gained > 0f)
            {
                _sink?.Gained(gained);
                if (!wasFull && IsFull) _sink?.Full();
            }
            return gained;
        }

        /// <summary>Spends exactly <paramref name="amount"/> if available; otherwise changes nothing.</summary>
        public bool TrySpend(float amount)
        {
            if (amount < 0f || amount > Current + Epsilon) return false;
            if (amount <= 0f) return true;
            RemoveInternal(Math.Min(amount, Current), spent: true);
            return true;
        }

        /// <summary>Spends everything (Worldbreaker, 08 4.4). Returns the amount spent.</summary>
        public float SpendAll()
        {
            float amount = Current;
            if (amount <= 0f) return 0f;
            RemoveInternal(amount, spent: true);
            return amount;
        }

        /// <summary>Advances decay by <paramref name="deltaSeconds"/> (08 4.1: after the delay without damage, -rate per second).</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            double before = _idleSeconds;
            _idleSeconds += deltaSeconds;
            if (Current <= 0f || _rules.DecayPerSecond <= 0f) return;
            double decaySeconds = _idleSeconds - Math.Max(before, _rules.DecayDelaySeconds);
            if (decaySeconds <= 0) return;
            decaySeconds = Math.Min(decaySeconds, deltaSeconds);
            float loss = (float)(decaySeconds * _rules.DecayPerSecond);
            RemoveInternal(Math.Min(loss, Current), spent: false);
        }

        /// <summary>Start of room: back to the configured value (0 for Fury) and decay timer cleared. Rest/Shrine never call this (08 4.1).</summary>
        public void ResetForRoom()
        {
            _idleSeconds = 0;
            float before = Current;
            Current = Math.Min(_rules.StartOfRoomValue, Maximum);
            if (before > 0f && Current <= 0f) _sink?.Empty();
        }

        private void RemoveInternal(float amount, bool spent)
        {
            Current -= amount;
            if (Current < Epsilon) Current = 0f;
            if (spent) _sink?.Spent(amount);
            if (Current <= 0f) _sink?.Empty();
        }
    }
}
