namespace Eclipse.Data.Skills
{
    /// <summary>Input slot of a hero skill (00 section 5 R05: LMB/RMB, Q/E/R/Shift, F).</summary>
    public enum SkillSlot : byte { Basic, Heavy, Q, E, R, Shift, F }

    /// <summary>Resource model of a hero (11 section 3.4.2 ResourceDefinition).</summary>
    public enum ResourceKind : byte { Pool, Charges, MassToConstructs }

    /// <summary>Primary attribute of a hero (08 section 3.1).</summary>
    public enum HeroPrimaryAttribute : byte { Strength, Intelligence, Dexterity }
}
