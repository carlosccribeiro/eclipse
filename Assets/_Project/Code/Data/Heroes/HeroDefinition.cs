using System;
using System.Collections.Generic;
using Eclipse.Data.Definitions;
using Eclipse.Data.Skills;
using UnityEngine;

namespace Eclipse.Data.Heroes
{
    /// <summary>One skill bound to an input slot.</summary>
    [Serializable]
    public struct HeroSkillSlot
    {
        public SkillSlot slot;
        public SkillDefinition skill;
    }

    /// <summary>
    /// Minimal hero definition (11 section 3.4.2, 08 section 3). Source: Heroes.csv. The importer owns the numbers and references by id;
    /// <see cref="MovementProfile"/> is assigned by hand in the asset (the type lives in Gameplay, which Data cannot reference).
    /// </summary>
    public sealed class HeroDefinition : Definition
    {
        [SerializeField] private HeroPrimaryAttribute primaryAttribute;
        [SerializeField] private float healthRelative;
        [SerializeField] private float armorRelative;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float engagementMin;
        [SerializeField] private float engagementMax;
        [SerializeField] private string offHandCategory;
        [SerializeField] private List<string> weaponCategories = new List<string>();
        [SerializeField] private List<string> passiveIds = new List<string>();
        [SerializeField] private ResourceDefinition resource;
        [SerializeField] private List<HeroSkillSlot> skills = new List<HeroSkillSlot>();
        [SerializeField] private ScriptableObject movementProfile;

        public HeroPrimaryAttribute PrimaryAttribute => primaryAttribute;
        /// <summary>Base life relative to the reference (1.2 = 120%, 08 section 3.1).</summary>
        public float HealthRelative => healthRelative;
        public float ArmorRelative => armorRelative;
        /// <summary>Base Movement Speed in m/s (01 via 08 section 3.1).</summary>
        public float MoveSpeed => moveSpeed;
        public float EngagementMinMeters => engagementMin;
        public float EngagementMaxMeters => engagementMax;
        public string OffHandCategory => offHandCategory;
        /// <summary>Main Hand weapon categories allowed (08 section 4.6).</summary>
        public IReadOnlyList<string> WeaponCategories => weaponCategories;
        /// <summary>Innate passive ids (08 section 4.2); passives are not definitions yet (M08 T2).</summary>
        public IReadOnlyList<string> PassiveIds => passiveIds;
        public ResourceDefinition Resource => resource;
        public IReadOnlyList<HeroSkillSlot> Skills => skills;
        /// <summary>Gameplay.Player.MovementProfile asset (typed as ScriptableObject to keep Data below Gameplay).</summary>
        public ScriptableObject MovementProfile => movementProfile;

        public SkillDefinition GetSkill(SkillSlot slot)
        {
            for (int i = 0; i < skills.Count; i++)
                if (skills[i].slot == slot) return skills[i].skill;
            return null;
        }

#if UNITY_EDITOR
        /// <summary>Editor/importer only: scalar columns.</summary>
        public void EditorSetScalars(HeroPrimaryAttribute attribute, float health, float armor, float speed, float engageMin,
            float engageMax, string offHand, List<string> weapons, List<string> passives)
        {
            primaryAttribute = attribute;
            healthRelative = health;
            armorRelative = armor;
            moveSpeed = speed;
            engagementMin = engageMin;
            engagementMax = engageMax;
            offHandCategory = offHand;
            weaponCategories = weapons;
            passiveIds = passives;
        }

        /// <summary>Editor/importer only: resolved references. Returns true if anything changed.</summary>
        public bool EditorSetReferences(ResourceDefinition res, List<HeroSkillSlot> slots)
        {
            bool same = resource == res && skills.Count == slots.Count;
            if (same)
                for (int i = 0; i < slots.Count; i++)
                    if (skills[i].slot != slots[i].slot || skills[i].skill != slots[i].skill) { same = false; break; }
            if (same) return false;
            resource = res;
            skills = slots;
            return true;
        }
#endif
    }
}
