namespace Eclipse.Gameplay.Combat
{
    /// <summary>Shield/Barrier points that absorb before health (06 3.3 step 6). Pure C#.</summary>
    public sealed class ShieldComponent
    {
        public float Points { get; private set; }

        /// <summary>Grants a shield of <paramref name="basePoints"/> amplified by Shield Strength (fraction; 06 3.3 step 6).</summary>
        public void Grant(float basePoints, float shieldStrength = 0f)
        {
            if (basePoints <= 0f) return;
            Points += basePoints * (1f + (shieldStrength > 0f ? shieldStrength : 0f));
        }

        /// <summary>Removes up to <paramref name="amount"/> points; <paramref name="broken"/> is true when this emptied a non-empty shield.</summary>
        public float Absorb(float amount, out bool broken)
        {
            broken = false;
            if (amount <= 0f || Points <= 0f) return 0f;
            float taken = amount < Points ? amount : Points;
            Points -= taken;
            if (Points <= 0f) { Points = 0f; broken = true; }
            return taken;
        }

        public void Clear() => Points = 0f;
    }
}
