using Eclipse.Core.Loot;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Loot
{
    /// <summary>
    /// L/E/R chances of a source at the 6 anchor points (Normal, Veteran, Nightmare, Eclipse, A1, A100; 05 sections 5.2-5.4), as fractions.
    /// A negative Epic/Rare value means the source has a floor for that rarity (empty CSV cell). Source: loot_chances.csv.
    /// </summary>
    public sealed class LootChanceDefinition : Definition
    {
        [SerializeField] private LootSourceKind source;
        [SerializeField] private float[] legendary = new float[6];
        [SerializeField] private float[] epic = new float[6];
        [SerializeField] private float[] rare = new float[6];

        public LootSourceKind Source => source;
        public float[] Legendary => legendary;
        public float[] Epic => epic;
        public float[] Rare => rare;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(LootSourceKind kind, float[] legendaryValues, float[] epicValues, float[] rareValues)
        {
            source = kind;
            legendary = legendaryValues;
            epic = epicValues;
            rare = rareValues;
        }
#endif
    }
}
