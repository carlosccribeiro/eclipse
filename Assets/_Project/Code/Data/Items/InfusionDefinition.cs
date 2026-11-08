using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Items
{
    /// <summary>Infusion metadata (02 section 3.9.4); ranges live in the affix. Source: infusions.csv.</summary>
    public sealed class InfusionDefinition : Definition
    {
        [SerializeField] private string affixId;
        [SerializeField] private ulong affinityBits;
        [SerializeField] private InfusionForm form;
        [SerializeField] private InfusionHitFilter hitFilter;
        [SerializeField] private bool countsAsAffinity;
        [SerializeField] private string poolRule;

        public string AffixId => affixId;
        public TagMask Affinity => TagMask.FromBits(affinityBits);
        public InfusionForm Form => form;
        public InfusionHitFilter HitFilter => hitFilter;
        public bool CountsAsAffinity => countsAsAffinity;
        public string PoolRule => poolRule;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(string affix, TagMask affinity, InfusionForm infusionForm, InfusionHitFilter filter, bool counts, string rule)
        {
            affixId = affix;
            affinityBits = affinity.Bits;
            form = infusionForm;
            hitFilter = filter;
            countsAsAffinity = counts;
            poolRule = rule;
        }
#endif
    }
}
