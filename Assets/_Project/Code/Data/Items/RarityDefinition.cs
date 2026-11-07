using Eclipse.Core.Items;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Items
{
    /// <summary>Per-rarity structure (02 sections 3.1-3.3, R25). Source: rarities.csv.</summary>
    public sealed class RarityDefinition : Definition
    {
        [SerializeField] private Rarity rarity;
        [SerializeField] private int randomAffixCount;
        [SerializeField] private int modifierCount;
        [SerializeField] private int minDropILvl;

        public Rarity Rarity => rarity;
        public int RandomAffixCount => randomAffixCount;
        public int ModifierCount => modifierCount;
        public int MinDropILvl => minDropILvl;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(Rarity value, int affixes, int modifiers, int minILvl)
        {
            rarity = value;
            randomAffixCount = affixes;
            modifierCount = modifiers;
            minDropILvl = minILvl;
        }
#endif
    }
}
