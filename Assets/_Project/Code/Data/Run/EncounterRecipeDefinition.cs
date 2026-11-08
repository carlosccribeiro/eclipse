using System;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Run
{
    [Serializable]
    public struct RecipeSlotData
    {
        public EnemyArchetype archetype;
        public int minCount;
        public int maxCount;
        /// <summary>Fraction 0-1.</summary>
        public float targetShare;
    }

    [Serializable]
    public struct RecipeWeightData
    {
        public string regionId;
        public int minRow;
        public int maxRow;
        public float weight;
    }

    /// <summary>One Encounter Recipe (09 §10.4, encounter_recipes.csv).</summary>
    public sealed class EncounterRecipeDefinition : Definition
    {
        [SerializeField] private RecipeSlotData[] slots = Array.Empty<RecipeSlotData>();
        [SerializeField] private float[] fillerWeights = Array.Empty<float>();
        [SerializeField] private RecipeWeightData[] rowWeights = Array.Empty<RecipeWeightData>();
        [SerializeField] private int maxCastersOverride;
        [SerializeField] private int maxSummonersOverride;

        public RecipeSlotData[] Slots => slots;
        public float[] FillerWeights => fillerWeights;
        public RecipeWeightData[] RowWeights => rowWeights;
        public int MaxCastersOverride => maxCastersOverride;
        public int MaxSummonersOverride => maxSummonersOverride;

        /// <summary>The recipe id in Core is the last segment of the StableId (e.g. "swarm").</summary>
        public string CoreId
        {
            get
            {
                string id = StableIdString ?? string.Empty;
                return id.Substring(id.LastIndexOf('.') + 1);
            }
        }

        public EncounterRecipe ToCore()
        {
            var r = new EncounterRecipe(CoreId)
            {
                FillerWeights = (float[])fillerWeights.Clone(),
                MaxCastersOverride = maxCastersOverride,
                MaxSummonersOverride = maxSummonersOverride
            };
            foreach (RecipeSlotData s in slots) r.Slots.Add(new RecipeSlot(s.archetype, s.minCount, s.maxCount, s.targetShare));
            foreach (RecipeWeightData w in rowWeights) r.Weights.Add(new RecipeWeight(w.regionId ?? string.Empty, w.minRow, w.maxRow, w.weight));
            return r;
        }

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(RecipeSlotData[] slotData, float[] filler, RecipeWeightData[] weightData, int casters, int summoners)
        {
            slots = slotData; fillerWeights = filler; rowWeights = weightData;
            maxCastersOverride = casters; maxSummonersOverride = summoners;
        }
#endif
    }
}
