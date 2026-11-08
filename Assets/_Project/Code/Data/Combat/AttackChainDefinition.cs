using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Combat
{
    /// <summary>
    /// One link of a weapon's attack chain (a basic combo hit, the Heavy or the Dash Attack) with its
    /// <see cref="AttackStep"/>. Source: AttackSteps.csv (06 sections 3.5 and 3.6, 08 section 4.3).
    /// </summary>
    public sealed class AttackChainDefinition : Definition
    {
        [SerializeField] private string weaponId;
        [SerializeField] private AttackKind kind;
        [SerializeField] private int comboIndex;
        [SerializeField] private AttackStep step;

        /// <summary>StableId of the owning <see cref="WeaponDefinition"/>.</summary>
        public string WeaponId => weaponId;
        public AttackKind Kind => kind;
        /// <summary>1-based position in the basic combo; 0 for Heavy and Dash.</summary>
        public int ComboIndex => comboIndex;
        public AttackStep Step => step;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(string weapon, AttackKind attackKind, int index, AttackStep attackStep)
        {
            weaponId = weapon;
            kind = attackKind;
            comboIndex = index;
            step = attackStep;
        }
#endif
    }
}
