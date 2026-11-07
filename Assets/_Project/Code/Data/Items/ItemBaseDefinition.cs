using Eclipse.Core.Items;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Items
{
    /// <summary>Weapon, armor, jewel or relic base (02 sections 3.4-3.8, 3.10). Source: item_bases.csv.</summary>
    public sealed class ItemBaseDefinition : Definition
    {
        [SerializeField] private EquipSlot slot;
        [SerializeField] private string category;
        [SerializeField] private ArmorFamily family;
        [SerializeField] private string heroId;
        [SerializeField] private ulong tagBits;
        [SerializeField] private string implicitAffixId;
        [SerializeField] private float implicitFactor;
        [SerializeField] private float armorFactor;
        [SerializeField] private bool droppable = true;
        // Extra columns not yet modelled by Core ItemBaseData (schema doc, item_bases lacunas).
        [SerializeField] private float dpsFactor;
        [SerializeField] private ulong implicitAffinityBits;
        [SerializeField] private string secondaryImplicitStat;
        [SerializeField] private float secondaryImplicitAt100;
        [SerializeField] private string implicitEffectId;

        public EquipSlot Slot => slot;
        public string Category => category;
        public ArmorFamily Family => family;
        public string HeroId => heroId;
        public TagMask Tags => TagMask.FromBits(tagBits);
        public string ImplicitAffixId => implicitAffixId;
        public float ImplicitFactor => implicitFactor;
        public float ArmorFactor => armorFactor;
        public bool Droppable => droppable;
        public float DpsFactor => dpsFactor;
        public TagMask ImplicitAffinity => TagMask.FromBits(implicitAffinityBits);
        public string SecondaryImplicitStat => secondaryImplicitStat;
        public float SecondaryImplicitAt100 => secondaryImplicitAt100;
        public string ImplicitEffectId => implicitEffectId;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(EquipSlot equipSlot, string categoryName, ArmorFamily armorFamily, string hero, TagMask tags,
            string implicitAffix, float implicitFactorValue, float armorFactorValue, bool canDrop,
            float dps, TagMask implicitAffinity, string secondaryStat, float secondaryAt100, string effectId)
        {
            slot = equipSlot;
            category = categoryName;
            family = armorFamily;
            heroId = hero;
            tagBits = tags.Bits;
            implicitAffixId = implicitAffix;
            implicitFactor = implicitFactorValue;
            armorFactor = armorFactorValue;
            droppable = canDrop;
            dpsFactor = dps;
            implicitAffinityBits = implicitAffinity.Bits;
            secondaryImplicitStat = secondaryStat;
            secondaryImplicitAt100 = secondaryAt100;
            implicitEffectId = effectId;
        }
#endif
    }
}
