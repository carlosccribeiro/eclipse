using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Combat
{
    /// <summary>Generic combat states and Stagger tuning (06 section 3.8). Source: CombatStates.csv.</summary>
    public sealed class CombatStatesDefinition : Definition
    {
        [SerializeField] private CombatStatesValues values = new CombatStatesValues();

        public CombatStatesValues Values => values;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(CombatStatesValues v) => values = v;
#endif
    }
}
