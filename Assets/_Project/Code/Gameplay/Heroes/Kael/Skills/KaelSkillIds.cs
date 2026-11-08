namespace Eclipse.Gameplay.Heroes.Kael
{
    /// <summary>StableIds of Kael's skills (08 4.4), the keys used by the Skills.csv rows and the assets.</summary>
    public static class KaelSkillIds
    {
        public const string CleavingStrike = "skill.kael.cleaving_strike";
        public const string InfernalLeap = "skill.kael.infernal_leap";
        public const string BurningChains = "skill.kael.burning_chains";
        public const string AshenGuard = "skill.kael.ashen_guard";
        public const string Worldbreaker = "skill.kael.worldbreaker";

        /// <summary>Source key of the Ruin Vulnerability stacks applied by Worldbreaker (one entry per source, 06 3.2).</summary>
        public const string WorldbreakerRuinSource = "kael.worldbreaker.ruin";
    }
}
