using System;

namespace Eclipse.Data.Skills
{
    /// <summary>
    /// (Event, Condition, Effects[], ICD) of 11 section 3.6.1. Data only; concrete triggers (M08 T4) derive from this class
    /// and only fire when Generation is below 2 (11 section 3.6.5). Authored via [SerializeReference].
    /// </summary>
    [Serializable]
    public abstract class Trigger { }
}
