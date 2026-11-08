using Eclipse.Core.Ids;

namespace Eclipse.Core.RunGen.Encounters
{
    /// <summary>
    /// Archetype of an enemy as listed in each region's bestiary (09 section 5). Drives every limit of
    /// 09 section 10.3. Append only (recipe weight tables are indexed by value).
    /// </summary>
    public enum EnemyArchetype : byte
    {
        Fodder,
        Standard,
        Ranged,
        Caster,
        Tank,
        Summoner,
        Assassin,
        Support
    }

    /// <summary>Coarse role derived from the archetype (loot classes of 09 section 10.5 and the frontline rule).</summary>
    public enum EnemyRole : byte
    {
        /// <summary>Melee standard: counts as front line (09 section 10.3).</summary>
        Frontline,
        Fodder,
        Tank,
        Ranged,
        Caster,
        Summoner,
        /// <summary>Support and assassin.</summary>
        Specialist
    }

    /// <summary>
    /// Pure DTO handed to the planner. Real data (PA of 09 section 5) comes from the enemy definitions (M07/M16).
    /// </summary>
    public sealed class EnemyCandidate
    {
        public StableId Id;
        public EnemyArchetype Archetype;
        /// <summary>Threat points (PA, 09 section 5), integer.</summary>
        public int Threat;
        /// <summary>Relative pick weight in the current region (0 = never picked).</summary>
        public float RegionWeight = 1f;
        /// <summary>Elite variant: only used by Elite Arena rooms (Elite pack, 09 section 10.1); excluded elsewhere.</summary>
        public bool IsElite;

        public EnemyCandidate() { }

        public EnemyCandidate(string id, EnemyArchetype archetype, int threat, float regionWeight = 1f, bool isElite = false)
        {
            Id = new StableId(id); Archetype = archetype; Threat = threat; RegionWeight = regionWeight; IsElite = isElite;
        }

        public EnemyRole Role => RoleOf(Archetype);

        /// <summary>Melee/tank unit that satisfies the "at least 1 front line unit" rule (09 section 10.3).</summary>
        public bool IsFrontline => ArchetypeIsFrontline(Archetype);

        /// <summary>Caster, support, summoner or assassin (09 section 10.3 specialist 40% rule).</summary>
        public bool IsSpecialist => ArchetypeIsSpecialist(Archetype);

        public static bool ArchetypeIsFrontline(EnemyArchetype a) => a == EnemyArchetype.Fodder || a == EnemyArchetype.Standard || a == EnemyArchetype.Tank;

        public static bool ArchetypeIsSpecialist(EnemyArchetype a) =>
            a == EnemyArchetype.Caster || a == EnemyArchetype.Support || a == EnemyArchetype.Summoner || a == EnemyArchetype.Assassin;

        public static EnemyRole RoleOf(EnemyArchetype a)
        {
            switch (a)
            {
                case EnemyArchetype.Fodder: return EnemyRole.Fodder;
                case EnemyArchetype.Standard: return EnemyRole.Frontline;
                case EnemyArchetype.Tank: return EnemyRole.Tank;
                case EnemyArchetype.Ranged: return EnemyRole.Ranged;
                case EnemyArchetype.Caster: return EnemyRole.Caster;
                case EnemyArchetype.Summoner: return EnemyRole.Summoner;
                default: return EnemyRole.Specialist;
            }
        }
    }
}
