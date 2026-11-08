namespace Eclipse.Gameplay.Combat
{
    /// <summary>Health pool. Pure C#, allocation-free.</summary>
    public sealed class HealthComponent
    {
        public HealthComponent(float max)
        {
            Max = max < 1f ? 1f : max;
            Current = Max;
        }

        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public float Fraction => Current / Max;

        /// <summary>Changes the maximum; <paramref name="keepFraction"/> scales Current with it, otherwise Current is only clamped.</summary>
        public void SetMax(float max, bool keepFraction)
        {
            if (max < 1f) max = 1f;
            float f = Fraction;
            Max = max;
            Current = keepFraction ? max * f : (Current > max ? max : Current);
        }

        /// <summary>Removes health; returns the amount actually removed. <paramref name="overkill"/> is the excess.</summary>
        public float Damage(float amount, out float overkill)
        {
            overkill = 0f;
            if (amount <= 0f || IsDead) return 0f;
            if (amount >= Current)
            {
                float applied = Current;
                overkill = amount - Current;
                Current = 0f;
                return applied;
            }
            Current -= amount;
            return amount;
        }

        /// <summary>Adds health up to Max; dead entities are not healed (use <see cref="Restore"/>). Returns the amount healed.</summary>
        public float Heal(float amount)
        {
            if (amount <= 0f || IsDead) return 0f;
            float room = Max - Current;
            float healed = amount < room ? amount : room;
            Current += healed;
            return healed;
        }

        /// <summary>Sets health to <paramref name="fraction"/> of Max (revive).</summary>
        public void Restore(float fraction)
        {
            if (fraction < 0f) fraction = 0f;
            if (fraction > 1f) fraction = 1f;
            Current = Max * fraction;
        }
    }
}
