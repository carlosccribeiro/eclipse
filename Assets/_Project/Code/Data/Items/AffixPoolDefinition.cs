using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Items
{
    /// <summary>Slot weights of one affix (02 section 3.9.3; weights 01 section 3.9.4). Source: affix_pools.csv.</summary>
    public sealed class AffixPoolDefinition : Definition
    {
        [SerializeField] private string affixId;
        /// <summary>Indexed by EquipSlot (10 entries); 0 = outside the pool.</summary>
        [SerializeField] private float[] slotWeights = new float[10];
        [SerializeField] private string restriction;

        public string AffixId => affixId;
        public float[] SlotWeights => slotWeights;
        public string Restriction => restriction;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(string affix, float[] weights, string restrictionCode)
        {
            affixId = affix;
            slotWeights = weights;
            restriction = restrictionCode;
        }
#endif
    }
}
