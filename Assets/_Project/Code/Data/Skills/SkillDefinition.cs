using System;
using System.Collections.Generic;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Skills
{
    /// <summary>
    /// Skill definition (11 section 3.4.2/3.6.1, 08 section 4.4). Structure (Delivery, Effects, Triggers) is authored in the asset;
    /// the numbers (cost, cooldown, coefficients, <see cref="Params"/>, flags) come from Skills.csv and are applied by the importer
    /// without recreating the asset (11 section 3.15), so authored structure is never overwritten.
    /// </summary>
    public sealed class SkillDefinition : Definition
    {
        [SerializeField] private string heroId;
        [SerializeField] private SkillSlot slot;
        [SerializeField] private ulong tagBits;
        [SerializeField] private string costResourceId;
        [SerializeField] private float costAmount;
        [SerializeField] private bool costAllResource;
        [SerializeField] private float costMinimum;
        [SerializeField] private float resourceGain;
        [SerializeField] private float cooldownSeconds;
        [SerializeField] private float damageCoefficient;
        [SerializeField] private float damageCoefficientPerResource;
        [SerializeField] private int moveDuringCastPercent;
        [SerializeField] private bool commit;
        [SerializeField] private bool tracking;
        [SerializeField] private int modifierSlots;
        [SerializeField] private List<SkillParam> parameters = new List<SkillParam>();

        [SerializeReference] private Delivery delivery;
        [SerializeReference] private List<Effect> effects = new List<Effect>();
        [SerializeReference] private List<Trigger> triggers = new List<Trigger>();

        public string HeroId => heroId;
        public SkillSlot Slot => slot;
        public TagMask Tags => TagMask.FromBits(tagBits);
        /// <summary>StableId of the resource spent (empty when the skill costs nothing).</summary>
        public string CostResourceId => costResourceId;
        public float CostAmount => costAmount;
        /// <summary>True when the skill spends the whole resource (Worldbreaker), requiring at least <see cref="CostMinimum"/>.</summary>
        public bool CostAllResource => costAllResource;
        public float CostMinimum => costMinimum;
        /// <summary>Resource granted on cast (Infernal Leap +15 Fury).</summary>
        public float ResourceGain => resourceGain;
        public float CooldownSeconds => cooldownSeconds;
        /// <summary>Damage coefficient as a fraction of Weapon Power (2.2 = 220% DA, 06 section 3.2).</summary>
        public float DamageCoefficient => damageCoefficient;
        /// <summary>Extra coefficient per unit of resource consumed (Worldbreaker 0.06 per Fury).</summary>
        public float DamageCoefficientPerResource => damageCoefficientPerResource;
        /// <summary>0, 50 or 100 (06 section 3.5).</summary>
        public int MoveDuringCastPercent => moveDuringCastPercent;
        public bool Commit => commit;
        public bool Tracking => tracking;
        /// <summary>Item modifier slots accepted: 3, or 2 in Ultimate/Basic/Heavy (03, 11 section 3.6.2).</summary>
        public int ModifierSlots => modifierSlots;
        public IReadOnlyList<SkillParam> Params => parameters;
        public Delivery Delivery => delivery;
        public IReadOnlyList<Effect> Effects => effects;
        public IReadOnlyList<Trigger> Triggers => triggers;

        public bool TryGetParam(string key, out float value)
        {
            for (int i = 0; i < parameters.Count; i++)
                if (string.Equals(parameters[i].Key, key, StringComparison.Ordinal))
                {
                    value = parameters[i].Value;
                    return true;
                }
            value = 0f;
            return false;
        }

        /// <summary>Value of the named parameter; throws if missing (a missing number is a data error, never a default).</summary>
        public float GetParam(string key)
        {
            if (!TryGetParam(key, out float v))
                throw new KeyNotFoundException("Skill '" + StableIdString + "' has no param '" + key + "'");
            return v;
        }

#if UNITY_EDITOR
        /// <summary>Editor/importer only: numbers and flags. Never touches Delivery/Effects/Triggers.</summary>
        public void EditorSetNumbers(string hero, SkillSlot skillSlot, TagMask tags, string costResource, float cost, bool costAll,
            float costMin, float gain, float cooldown, float coefficient, float coefficientPerResource, int moveDuringCast,
            bool commitFlag, bool trackingFlag, int modSlots, List<SkillParam> newParams)
        {
            heroId = hero;
            slot = skillSlot;
            tagBits = tags.Bits;
            costResourceId = costResource;
            costAmount = cost;
            costAllResource = costAll;
            costMinimum = costMin;
            resourceGain = gain;
            cooldownSeconds = cooldown;
            damageCoefficient = coefficient;
            damageCoefficientPerResource = coefficientPerResource;
            moveDuringCastPercent = moveDuringCast;
            commit = commitFlag;
            tracking = trackingFlag;
            modifierSlots = modSlots;
            parameters = newParams;
        }
#endif
    }
}
