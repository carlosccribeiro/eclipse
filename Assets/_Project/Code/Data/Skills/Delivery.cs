using System;

namespace Eclipse.Data.Skills
{
    /// <summary>
    /// How a skill reaches its targets (11 section 3.6.1: MeleeArc, Leap, AreaAtSelf, ...). Data only; the runtime
    /// implementations (M08 T4) derive from this class and live in Gameplay. Authored in the SkillDefinition asset via [SerializeReference].
    /// </summary>
    [Serializable]
    public abstract class Delivery { }
}
