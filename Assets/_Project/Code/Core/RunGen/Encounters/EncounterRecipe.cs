using System.Collections.Generic;

namespace Eclipse.Core.RunGen.Encounters
{
    /// <summary>
    /// One composition rule of a recipe, applied to every wave: at least <see cref="MinCount"/> units of the
    /// archetype, then more until the archetype reaches <see cref="TargetShare"/> of the wave PA or
    /// <see cref="MaxCount"/> units (0 = no cap). All of it is best effort and still subject to the limits.
    /// </summary>
    public sealed class RecipeSlot
    {
        public EnemyArchetype Archetype;
        public int MinCount;
        public int MaxCount;
        /// <summary>Fraction (0-1) of the wave PA aimed at this archetype; 0 = none beyond MinCount.</summary>
        public float TargetShare;

        public RecipeSlot(EnemyArchetype archetype, int minCount, int maxCount = 0, float targetShare = 0f)
        {
            Archetype = archetype; MinCount = minCount; MaxCount = maxCount; TargetShare = targetShare;
        }
    }

    /// <summary>Selection weight of a recipe for a region and a row range. Empty RegionId matches any region.</summary>
    public sealed class RecipeWeight
    {
        public string RegionId;
        public int MinRow;
        public int MaxRow;
        public float Weight;

        public RecipeWeight(string regionId, int minRow, int maxRow, float weight)
        {
            RegionId = regionId; MinRow = minRow; MaxRow = maxRow; Weight = weight;
        }

        public bool Matches(string region, int row) =>
            row >= MinRow && row <= MaxRow && (string.IsNullOrEmpty(RegionId) || RegionId == region);
    }

    /// <summary>
    /// Encounter Recipe template (09 section 10.4), pure data. After the slots, the wave is completed with
    /// archetypes drawn by <see cref="FillerWeights"/> (indexed by <see cref="EnemyArchetype"/>; 0 = never).
    /// </summary>
    public sealed class EncounterRecipe
    {
        public string Id;
        public List<RecipeSlot> Slots = new List<RecipeSlot>();
        /// <summary>One weight per archetype value (length 8). Null = every archetype weight 1.</summary>
        public float[] FillerWeights;
        public List<RecipeWeight> Weights = new List<RecipeWeight>();
        /// <summary>Themed override of the caster cap (09 section 10.3, themed recipes excepted); 0 = default.</summary>
        public int MaxCastersOverride;
        /// <summary>Themed override of the summoner cap; 0 = default.</summary>
        public int MaxSummonersOverride;

        public EncounterRecipe(string id) { Id = id; }

        public float WeightFor(string region, int row)
        {
            float best = 0f;
            for (int i = 0; i < Weights.Count; i++)
                if (Weights[i].Matches(region, row) && Weights[i].Weight > best) best = Weights[i].Weight;
            return best;
        }

        public float FillerWeight(EnemyArchetype a) =>
            FillerWeights == null ? 1f : ((int)a < FillerWeights.Length ? FillerWeights[(int)a] : 0f);

        // Filler order: Fodder, Standard, Ranged, Caster, Tank, Summoner, Assassin, Support.
        // GAP: 09 section 10.4 gives only qualitative compositions; the filler weights and row ranges below are
        // placeholders to be replaced by DataSource/run/*.csv (M15 T5) once design fixes them.
        public static List<EncounterRecipe> CreateDefaults()
        {
            var list = new List<EncounterRecipe>();

            // Swarm: 70% fodder + 1 summoner (09 section 10.4); no heavies (09 section 9.2 Swarm modifier).
            var swarm = new EncounterRecipe("swarm") { FillerWeights = new[] { 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f } };
            swarm.Slots.Add(new RecipeSlot(EnemyArchetype.Summoner, 1, 1));
            swarm.Slots.Add(new RecipeSlot(EnemyArchetype.Fodder, 1, 0, 0.70f));
            swarm.Weights.Add(new RecipeWeight("", 1, 13, 2f));
            list.Add(swarm);

            // Phalanx: tank + standards + 1 support.
            var phalanx = new EncounterRecipe("phalanx") { FillerWeights = new[] { 0.5f, 3f, 0f, 0f, 0f, 0f, 0f, 0f } };
            phalanx.Slots.Add(new RecipeSlot(EnemyArchetype.Tank, 1, 1));
            phalanx.Slots.Add(new RecipeSlot(EnemyArchetype.Support, 1, 1));
            phalanx.Slots.Add(new RecipeSlot(EnemyArchetype.Standard, 1, 0, 0.45f));
            phalanx.Weights.Add(new RecipeWeight("", 2, 13, 1.5f));
            list.Add(phalanx);

            // Siege: ranged/casters behind 1-2 tanks (tank cap of 09 section 10.3 still applies).
            var siege = new EncounterRecipe("siege") { FillerWeights = new[] { 0.5f, 0.5f, 3f, 1f, 0f, 0f, 0f, 0f } };
            siege.Slots.Add(new RecipeSlot(EnemyArchetype.Tank, 1, 2));
            siege.Slots.Add(new RecipeSlot(EnemyArchetype.Ranged, 1, 0, 0.35f));
            siege.Weights.Add(new RecipeWeight("", 3, 13, 1.2f));
            list.Add(siege);

            // Ambush: spawn around the player (runtime concern) + 2 assassins.
            var ambush = new EncounterRecipe("ambush") { FillerWeights = new[] { 2f, 2f, 0.5f, 0f, 0f, 0f, 0f, 0f } };
            ambush.Slots.Add(new RecipeSlot(EnemyArchetype.Assassin, 2, 2));
            ambush.Weights.Add(new RecipeWeight("", 4, 13, 1f));
            list.Add(ambush);

            // Ritual: 2 casters channeling + escort.
            var ritual = new EncounterRecipe("ritual") { FillerWeights = new[] { 1f, 2f, 0f, 0f, 0f, 0f, 0f, 0f } };
            ritual.Slots.Add(new RecipeSlot(EnemyArchetype.Caster, 2, 2));
            ritual.Weights.Add(new RecipeWeight("", 3, 13, 1f));
            list.Add(ritual);

            // Brood: 2 summoners + fodder (exceeds the 1-summoner cap, as 09 section 10.3 allows for themed recipes).
            var brood = new EncounterRecipe("brood") { FillerWeights = new[] { 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f }, MaxSummonersOverride = 2 };
            brood.Slots.Add(new RecipeSlot(EnemyArchetype.Summoner, 2, 2));
            brood.Weights.Add(new RecipeWeight("", 5, 13, 1f));
            list.Add(brood);

            // Mixed Patrol: "default distribution per archetype" (numbers are a placeholder).
            var mixed = new EncounterRecipe("mixed_patrol") { FillerWeights = new[] { 3f, 3f, 2f, 1f, 1f, 0.5f, 1f, 1f } };
            mixed.Weights.Add(new RecipeWeight("", 1, 13, 3f));
            list.Add(mixed);

            return list;
        }
    }
}
