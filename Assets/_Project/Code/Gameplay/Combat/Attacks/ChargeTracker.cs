namespace Eclipse.Gameplay.Combat.Attacks
{
    /// <summary>
    /// Heavy charge (06 3.6.2: tap = ChargeMin frames of Startup, hold up to ChargeMax). Pure; one tick per Startup frame.
    /// Fraction 0..1 = (held - min) / (max - min) and feeds damage multiplier and Stagger interpolation.
    /// </summary>
    public sealed class ChargeTracker
    {
        public int MinTicks { get; private set; }
        public int MaxTicks { get; private set; }
        public int HeldTicks { get; private set; }
        public bool Released { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>True while the button is still held and the charge did not reach its maximum.</summary>
        public bool IsCharging => IsActive && !Released && HeldTicks < MaxTicks;

        public float Fraction
        {
            get
            {
                if (!IsActive || MaxTicks <= MinTicks) return 0f;
                float f = (HeldTicks - MinTicks) / (float)(MaxTicks - MinTicks);
                return f < 0f ? 0f : f > 1f ? 1f : f;
            }
        }

        public void Begin(int minTicks, int maxTicks)
        {
            MinTicks = minTicks;
            MaxTicks = maxTicks < minTicks ? minTicks : maxTicks;
            HeldTicks = 0;
            Released = false;
            IsActive = true;
        }

        public void Tick(bool held)
        {
            if (!IsActive || Released) return;
            if (!held) Released = true;
            else if (HeldTicks < MaxTicks) HeldTicks++;
        }

        /// <summary>Drops the charge (cancel/interrupt).</summary>
        public void Reset()
        {
            IsActive = false;
            Released = false;
            HeldTicks = 0;
            MinTicks = MaxTicks = 0;
        }
    }
}
