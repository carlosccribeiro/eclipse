namespace Eclipse.Core.Damage
{
    /// <summary>One typed damage amount of a hit (06 3.1: each component is mitigated on its own).</summary>
    public readonly struct DamageComponent
    {
        public DamageComponent(DamageType type, float value)
        {
            Type = type;
            Value = value;
        }

        public DamageType Type { get; }
        public float Value { get; }

        public DamageComponent WithValue(float value) => new DamageComponent(Type, value);
    }
}
