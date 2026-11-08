using System;

namespace Eclipse.Data.Skills
{
    /// <summary>
    /// One thing a skill does to its targets (11 section 3.6.1: Damage, ApplyStatus, ApplyState, GainResource, ...). Data only;
    /// runtime implementations (M08 T4) derive from this class. Authored via [SerializeReference].
    /// </summary>
    [Serializable]
    public abstract class Effect { }
}
