using Eclipse.Data.Skills;
using Eclipse.Gameplay.Skills;

namespace Eclipse.Gameplay.Heroes.Kael
{
    /// <summary>
    /// Reads the phase frames of a skill (params windup_f, active_f, recovery_f of Skills.csv; 60 Hz, 06 3.5) and builds the
    /// <see cref="SkillSpecBuilder"/> from the asset. Values the docs do not define are marked TUNED in the CSV notes.
    /// </summary>
    public static class KaelSkillFrames
    {
        public static SkillFrames From(SkillDefinition d) =>
            new SkillFrames((int)d.GetParam("windup_f"), (int)d.GetParam("active_f"), (int)d.GetParam("recovery_f"));

        public static SkillSpecBuilder Builder(SkillDefinition d) => SkillSpecBuilder.FromDefinition(d, From(d));
    }
}
