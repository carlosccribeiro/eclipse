using System;
using Eclipse.AI.Utility;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Data.Enemies;

namespace Eclipse.AI.Enemies
{
    /// <summary>How an archetype closes in and holds position (09 section 4).</summary>
    public enum SteeringStyle : byte
    {
        /// <summary>Fodder: straight at the target as a swarm (09 4.1).</summary>
        Swarm = 0,
        /// <summary>Melee: straight approach with a slight bias to surround (09 4.1).</summary>
        Flank = 1,
        /// <summary>Ranged: holds the preferred band, backs off when too close, looks for line of sight (09 4.2).</summary>
        KeepDistance = 2,
        /// <summary>Tank: slow advance, interposes between the target and casters/supports (09 4.4).</summary>
        Escort = 3,
    }

    /// <summary>
    /// Behaviour template of one archetype (the <c>hfsm_template</c> of <c>EnemyDefinition</c>; 11 section 3.10). Ranges, speeds,
    /// retreat distance and cooldown come from the enemy data; the fields below are AI feel knobs that no doc defines, all
    /// <b>TUNED</b> placeholders (same status as the RunGen room weights) to be revisited with the soak test and by M14.
    /// </summary>
    public sealed class ArchetypeTemplate
    {
        public string Id;
        public SteeringStyle Style;

        /// <summary>TUNED: max turn rate while engaged (deg/s). Slower bodies are easier to flank (the Tank plate matters).</summary>
        public float TurnDegreesPerSecond;
        /// <summary>TUNED: an attack is usable only when the target is within this angle of the facing.</summary>
        public float AttackAngleToleranceDeg;
        /// <summary>TUNED (Flank): angular spread between the surround slots, degrees.</summary>
        public float FlankSlotSpreadDegrees;
        /// <summary>TUNED (Flank): number of distinct surround slots (centered on the direct line).</summary>
        public int FlankSlotCount = 5;
        /// <summary>TUNED (Escort): distance from the ward at which the Tank interposes, meters.</summary>
        public float EscortDistance;
        /// <summary>TUNED: nearby-ally search radius for the utility "allies" term, meters.</summary>
        public float AlliesRadius;
        /// <summary>TUNED: extra attack score with a full crowd of allies (pack pressure).</summary>
        public float AllyAttackBonus;
        public float WeightApproach = 1f;
        public float WeightReposition = 1f;
        public float WeightAttack = 1f;
        /// <summary>Retreats when the target is closer than <c>retreat_below_m</c> (09 4.2); needs the data value too.</summary>
        public bool UsesRetreat;

        /// <summary>Copies the template and the enemy's preferred range into a Utility config (attacks are added by the caller).</summary>
        public void Apply(UtilityConfig config, EnemyDefinition enemy)
        {
            config.WeightApproach = WeightApproach;
            config.WeightReposition = WeightReposition;
            config.WeightAttack = WeightAttack;
            config.AllyAttackBonus = AllyAttackBonus;
            config.DesiredRange = enemy.PreferredRangeMax;
            config.FleeHpFraction = 0f; // no archetype of M07 flees on low HP (the HFSM keeps the state for later modules)
        }
    }

    /// <summary>The four archetype templates of M07 (09 section 4): Fodder, Melee, Ranged, Tank.</summary>
    public static class ArchetypeTemplates
    {
        public const string FodderId = "hfsm.fodder";
        public const string MeleeId = "hfsm.melee";
        public const string RangedId = "hfsm.ranged";
        public const string TankId = "hfsm.tank";

        public static ArchetypeTemplate Fodder() => new ArchetypeTemplate
        {
            Id = FodderId, Style = SteeringStyle.Swarm,
            TurnDegreesPerSecond = 720f, AttackAngleToleranceDeg = 45f, AlliesRadius = 3f, AllyAttackBonus = 0.25f,
        };

        public static ArchetypeTemplate Melee() => new ArchetypeTemplate
        {
            Id = MeleeId, Style = SteeringStyle.Flank,
            TurnDegreesPerSecond = 540f, AttackAngleToleranceDeg = 45f, FlankSlotSpreadDegrees = 25f, FlankSlotCount = 5,
            AlliesRadius = 3f,
        };

        public static ArchetypeTemplate Ranged() => new ArchetypeTemplate
        {
            Id = RangedId, Style = SteeringStyle.KeepDistance,
            TurnDegreesPerSecond = 360f, AttackAngleToleranceDeg = 45f, AlliesRadius = 3f, UsesRetreat = true,
            WeightApproach = 1f, WeightReposition = 1f,
        };

        public static ArchetypeTemplate Tank() => new ArchetypeTemplate
        {
            Id = TankId, Style = SteeringStyle.Escort,
            TurnDegreesPerSecond = 180f, AttackAngleToleranceDeg = 45f, EscortDistance = 2.5f, AlliesRadius = 3f,
        };

        /// <summary>
        /// Template of an enemy: by its <c>hfsm_template</c> id, falling back to the archetype. Archetypes outside M07
        /// (Caster, Summoner, Assassin, Support) use the Melee template until their module adds one.
        /// </summary>
        public static ArchetypeTemplate For(EnemyDefinition enemy)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            switch (enemy.HfsmTemplate)
            {
                case FodderId: return Fodder();
                case MeleeId: return Melee();
                case RangedId: return Ranged();
                case TankId: return Tank();
            }
            return For(enemy.Archetype);
        }

        public static ArchetypeTemplate For(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Fodder: return Fodder();
                case EnemyArchetype.Ranged: return Ranged();
                case EnemyArchetype.Tank: return Tank();
                default: return Melee();
            }
        }
    }
}
