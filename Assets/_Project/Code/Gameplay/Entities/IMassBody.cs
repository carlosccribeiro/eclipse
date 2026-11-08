namespace Eclipse.Gameplay.Entities
{
    /// <summary>Anything that has a mass class (enemies, bosses). Consumed by dodge (M04) and combat reactions (M05).</summary>
    public interface IMassBody
    {
        /// <summary>Base mass class (06 section 3.8.1).</summary>
        MassClass MassClass { get; }

        /// <summary>Elite modifier (06 section 3.8.1): same base class, but flinch needs double Stagger and knockback x0.6.</summary>
        bool IsElite { get; }
    }
}
