using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Loot
{
    /// <summary>
    /// Home region of a set (row kind Set) or dominant affinities of a region (row kind Region); 05 sections 9.1, 9.2, 9.4.
    /// Fields of the other kind stay at their defaults. Source: home_regions.csv.
    /// </summary>
    public sealed class HomeRegionDefinition : Definition
    {
        [SerializeField] private HomeRegionRowKind rowKind;
        [SerializeField] private string setId;
        [SerializeField] private string heroId;
        [SerializeField] private string regionId;
        [SerializeField] private float homeWeight;
        [SerializeField] private float homeBossWeight;
        [SerializeField] private float attunementMultiplier;
        [SerializeField] private ulong dominantAffinityBits;
        [SerializeField] private float affinityWeight;
        [SerializeField] private float uniqueBonus;

        public HomeRegionRowKind RowKind => rowKind;
        public string SetId => setId;
        public string HeroId => heroId;
        public string RegionId => regionId;
        public float HomeWeight => homeWeight;
        public float HomeBossWeight => homeBossWeight;
        public float AttunementMultiplier => attunementMultiplier;
        public TagMask DominantAffinities => TagMask.FromBits(dominantAffinityBits);
        public float AffinityWeight => affinityWeight;
        /// <summary>Fraction added to the Unique share (Astral Citadel +0.03).</summary>
        public float UniqueBonus => uniqueBonus;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(HomeRegionRowKind kind, string set, string hero, string region, float home, float homeBoss,
            float attunement, TagMask dominant, float affinityW, float bonus)
        {
            rowKind = kind;
            setId = set;
            heroId = hero;
            regionId = region;
            homeWeight = home;
            homeBossWeight = homeBoss;
            attunementMultiplier = attunement;
            dominantAffinityBits = dominant.Bits;
            affinityWeight = affinityW;
            uniqueBonus = bonus;
        }
#endif
    }
}
