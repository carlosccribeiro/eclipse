using Eclipse.Core.Items;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Items
{
    /// <summary>Affix with its T1..T5 ranges (01 section 3.9.2). Percent ranges are stored as fractions. Source: affixes.csv.</summary>
    public sealed class AffixDefinition : Definition
    {
        [SerializeField] private string abbrev;
        [SerializeField] private bool hasStat;
        [SerializeField] private StatId stat;
        [SerializeField] private AffixValueKind valueKind;
        [SerializeField] private AffixCategory category;
        [SerializeField] private AffixGroup group;
        [SerializeField] private ulong affinityBits;
        [SerializeField] private ulong relevanceBits;
        /// <summary>Ten values: t1 min, t1 max, ..., t5 min, t5 max.</summary>
        [SerializeField] private float[] tiers = new float[10];

        public string Abbrev => abbrev;
        public bool HasStat => hasStat;
        public StatId Stat => stat;
        public AffixValueKind ValueKind => valueKind;
        public AffixCategory Category => category;
        public AffixGroup Group => group;
        public TagMask Affinity => TagMask.FromBits(affinityBits);
        public TagMask RelevanceTags => TagMask.FromBits(relevanceBits);
        public float[] Tiers => tiers;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(string abbreviation, bool withStat, StatId statId, AffixValueKind kind, AffixCategory cat,
            AffixGroup grp, TagMask affinity, TagMask relevance, float[] tierValues)
        {
            abbrev = abbreviation;
            hasStat = withStat;
            stat = statId;
            valueKind = kind;
            category = cat;
            group = grp;
            affinityBits = affinity.Bits;
            relevanceBits = relevance.Bits;
            tiers = tierValues;
        }
#endif
    }
}
