using System;
using System.Collections.Generic;
using Eclipse.Core.Damage;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Enemies
{
    /// <summary>
    /// One enemy type (11 section 3.4.2, 09 sections 3.1 and 4). HP and damage are multipliers of the Monster Level
    /// curve held by <see cref="EnemyScalingDefinition"/>; nothing here is an absolute stat.
    /// </summary>
    public sealed class EnemyDefinition : Definition
    {
        private const int DamageTypeCount = 7;

        [SerializeField] private EnemyArchetype archetype;
        [SerializeField] private string faction;
        [SerializeField] private float hpMult = 1f;
        [SerializeField] private float damageMult = 1f;
        [SerializeField] private int threatPoints;
        [SerializeField] private EnemyMassClass massClass;
        [SerializeField] private float staggerThreshold;
        [SerializeField] private float moveSpeed;
        [SerializeField] private DamageType baseElement;
        [SerializeField] private DamageType secondaryElement;
        [SerializeField] private float[] resistances = new float[DamageTypeCount];
        [SerializeField] private float statusResistance;
        [SerializeField] private EnemyLootClass lootClass;
        [SerializeField] private ulong tagBits;
        [SerializeField] private float preferredRangeMin;
        [SerializeField] private float preferredRangeMax;
        [SerializeField] private RepositionBehavior reposition;
        [SerializeField] private float retreatBelowMeters;
        [SerializeField] private float retreatCooldownSeconds;
        [SerializeField] private float frontPlateReduction;
        [SerializeField] private float frontPlateArcDegrees;
        [SerializeField] private string hfsmTemplate;
        [SerializeField] private List<AttackDefinition> attacks = new List<AttackDefinition>();

        public EnemyArchetype Archetype => archetype;
        public string Faction => faction;
        public float HpMult => hpMult;
        public float DamageMult => damageMult;
        /// <summary>Threat points (PA) in the encounter budget.</summary>
        public int ThreatPoints => threatPoints;
        public EnemyMassClass MassClass => massClass;
        /// <summary>Base Stagger threshold before the difficulty multiplier (06 section 3.8.2); 0 = no bar (Light).</summary>
        public float StaggerThreshold => staggerThreshold;
        /// <summary>Meters per second.</summary>
        public float MoveSpeed => moveSpeed;
        public DamageType BaseElement => baseElement;
        public DamageType SecondaryElement => secondaryElement;
        /// <summary>Resistance as a fraction (0.2 = 20%) to the given damage type.</summary>
        public float Resistance(DamageType type) => (int)type < resistances.Length ? resistances[(int)type] : 0f;
        public float StatusResistance => statusResistance;
        public EnemyLootClass LootClass => lootClass;
        public TagMask Tags => TagMask.FromBits(tagBits);
        public float PreferredRangeMin => preferredRangeMin;
        public float PreferredRangeMax => preferredRangeMax;
        public RepositionBehavior Reposition => reposition;
        /// <summary>Ranged retreat trigger distance (09 section 4.2); 0 = never retreats.</summary>
        public float RetreatBelowMeters => retreatBelowMeters;
        public float RetreatCooldownSeconds => retreatCooldownSeconds;
        /// <summary>Frontal damage reduction fraction (Kiln Brute plate, 0.5 = -50%); 0 = none.</summary>
        public float FrontPlateReduction => frontPlateReduction;
        public float FrontPlateArcDegrees => frontPlateArcDegrees;
        /// <summary>Id of the HFSM template (built in M07 T6).</summary>
        public string HfsmTemplate => hfsmTemplate;
        public IReadOnlyList<AttackDefinition> Attacks => attacks;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSetScalars(EnemyArchetype archetypeValue, string factionValue, float hp, float damage, int pa,
            EnemyMassClass mass, float stagger, float speed, DamageType element, DamageType secondary, float[] resist,
            float statusResist, EnemyLootClass loot, ulong tags, float rangeMin, float rangeMax, RepositionBehavior behavior,
            float retreatBelow, float retreatCooldown, float plateReduction, float plateArc, string hfsm)
        {
            archetype = archetypeValue; faction = factionValue; hpMult = hp; damageMult = damage; threatPoints = pa;
            massClass = mass; staggerThreshold = stagger; moveSpeed = speed; baseElement = element; secondaryElement = secondary;
            resistances = resist; statusResistance = statusResist; lootClass = loot; tagBits = tags;
            preferredRangeMin = rangeMin; preferredRangeMax = rangeMax; reposition = behavior;
            retreatBelowMeters = retreatBelow; retreatCooldownSeconds = retreatCooldown;
            frontPlateReduction = plateReduction; frontPlateArcDegrees = plateArc; hfsmTemplate = hfsm;
        }

        /// <summary>Editor/importer only. Returns true if the list changed.</summary>
        public bool EditorSetAttacks(List<AttackDefinition> list)
        {
            if (attacks.Count == list.Count)
            {
                bool same = true;
                for (int i = 0; i < list.Count; i++) if (attacks[i] != list[i]) { same = false; break; }
                if (same) return false;
            }
            attacks = new List<AttackDefinition>(list);
            return true;
        }
#endif
    }
}
