using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Loot
{
    /// <summary>One global loot/item constant as key/value (05 R45, R30, R31; 01 section 3.9.3). Percent values are fractions. Source: loot_global.csv.</summary>
    public sealed class LootGlobalDefinition : Definition
    {
        [SerializeField] private string key;
        [SerializeField] private float value;

        public string Key => key;
        /// <summary>Already converted: a Percent CSV value is divided by 100.</summary>
        public float Value => value;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(string keyName, float resolvedValue)
        {
            key = keyName;
            value = resolvedValue;
        }
#endif
    }
}
