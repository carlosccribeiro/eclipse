using System.Collections.Generic;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Combat
{
    /// <summary>
    /// Weapon moveset data: weight class, APS, Weapon Power at iLvl 1 and the attack chain
    /// (02 section 3.4/3.5, 06 section 3.6, 08 section 4.3). Source: Weapons.csv.
    /// </summary>
    public sealed class WeaponDefinition : Definition
    {
        [SerializeField] private string heroId;
        [SerializeField] private string baseId;
        [SerializeField] private WeaponWeightClass weightClass;
        [SerializeField] private float attacksPerSecond;
        [SerializeField] private float weaponPowerILvl1;
        [SerializeField] private float comboResetSeconds;
        [SerializeField] private List<AttackChainDefinition> basicChain = new List<AttackChainDefinition>();
        [SerializeField] private AttackChainDefinition heavy;
        [SerializeField] private AttackChainDefinition dashAttack;
        [SerializeField] private HitFeedbackProfile feedback;

        public string HeroId => heroId;
        /// <summary>StableId of the matching ItemBaseDefinition (02).</summary>
        public string BaseId => baseId;
        public WeaponWeightClass WeightClass => weightClass;
        /// <summary>Attacks per second at Attack Speed 1.0 (08).</summary>
        public float AttacksPerSecond => attacksPerSecond;
        /// <summary>Reference DPS at iLvl 1 (02 section 3.4: 10 x FatorArma); the in-game Weapon Power at iLvl 1.</summary>
        public float WeaponPowerILvl1 => weaponPowerILvl1;
        /// <summary>Damage per hit at iLvl 1 = DPS_ref / APS (02 section 3.4); the damage multiplier of each step applies on top.</summary>
        public float DamagePerHitILvl1 => attacksPerSecond > 0f ? weaponPowerILvl1 / attacksPerSecond : 0f;
        /// <summary>Seconds without input before the combo resets (06 section 3.5).</summary>
        public float ComboResetSeconds => comboResetSeconds;
        public IReadOnlyList<AttackChainDefinition> BasicChain => basicChain;
        public AttackChainDefinition Heavy => heavy;
        public AttackChainDefinition DashAttack => dashAttack;
        public HitFeedbackProfile Feedback => feedback;

#if UNITY_EDITOR
        /// <summary>Editor/importer only: scalar columns.</summary>
        public void EditorSetScalars(string hero, string itemBase, WeaponWeightClass weight, float aps, float weaponPower,
            float comboReset)
        {
            heroId = hero;
            baseId = itemBase;
            weightClass = weight;
            attacksPerSecond = aps;
            weaponPowerILvl1 = weaponPower;
            comboResetSeconds = comboReset;
        }

        /// <summary>Editor/importer only: resolved references. Returns true if anything changed.</summary>
        public bool EditorSetReferences(List<AttackChainDefinition> basic, AttackChainDefinition heavyAttack,
            AttackChainDefinition dash, HitFeedbackProfile feedbackProfile)
        {
            bool same = heavy == heavyAttack && dashAttack == dash && feedback == feedbackProfile
                        && basicChain.Count == basic.Count;
            if (same)
                for (int i = 0; i < basic.Count; i++)
                    if (basicChain[i] != basic[i]) { same = false; break; }
            if (same) return false;
            basicChain = basic;
            heavy = heavyAttack;
            dashAttack = dash;
            feedback = feedbackProfile;
            return true;
        }
#endif
    }
}
