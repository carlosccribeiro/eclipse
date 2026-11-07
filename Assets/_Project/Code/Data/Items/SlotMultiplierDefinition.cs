using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Items
{
    /// <summary>Slot multiplier rule (01 section 3.9.3). Source: slot_multipliers.csv.</summary>
    public sealed class SlotMultiplierDefinition : Definition
    {
        [SerializeField] private EquipSlot slot;
        [SerializeField] private List<string> affixIds = new List<string>();
        [SerializeField] private string baseId;
        [SerializeField] private float multiplier;
        [SerializeField] private bool appliesToImplicit;

        public EquipSlot Slot => slot;
        public IReadOnlyList<string> AffixIds => affixIds;
        /// <summary>Empty = any base of the slot.</summary>
        public string BaseId => baseId;
        public float Multiplier => multiplier;
        public bool AppliesToImplicit => appliesToImplicit;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(EquipSlot equipSlot, List<string> affixes, string baseIdValue, float mult, bool toImplicit)
        {
            slot = equipSlot;
            affixIds = affixes;
            baseId = baseIdValue;
            multiplier = mult;
            appliesToImplicit = toImplicit;
        }
#endif
    }
}
