using Eclipse.Core.Loot;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Loot
{
    /// <summary>iLvl range of a difficulty/region (05 sections 4.1, 4.2, R38). Source: ilvl_ranges.csv.</summary>
    public sealed class IlvlRangeDefinition : Definition
    {
        [SerializeField] private Difficulty difficulty;
        [SerializeField] private string regionId;
        [SerializeField] private int ilvlMin;
        [SerializeField] private int ilvlMax;

        public Difficulty Difficulty => difficulty;
        /// <summary>Empty = every region.</summary>
        public string RegionId => regionId;
        public int ILvlMin => ilvlMin;
        public int ILvlMax => ilvlMax;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(Difficulty value, string region, int min, int max)
        {
            difficulty = value;
            regionId = region;
            ilvlMin = min;
            ilvlMax = max;
        }
#endif
    }
}
