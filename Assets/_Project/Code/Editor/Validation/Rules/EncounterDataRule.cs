using System;
using System.Collections.Generic;
using Eclipse.Data.Run;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// Encounter planner tables (M15): once any row exists, every required key of density.csv (09 §10.1-§10.2) and
    /// encounter_limits.csv (09 §10.3) must be present exactly once with the right number of values; limits and
    /// budgets are positive, wave splits sum to 1; each recipe (09 §10.4) has 8 filler weights, a positive total
    /// weight and coherent slots.
    /// </summary>
    [ValidationRule]
    public sealed class EncounterDataRule : IValidationRule
    {
        public string Name => "EncounterData";

        public void Validate(ValidationContext context)
        {
            var density = new Dictionary<string, EncounterConfigDefinition>(StringComparer.Ordinal);
            var limits = new Dictionary<string, EncounterConfigDefinition>(StringComparer.Ordinal);
            int recipes = 0, total = 0;

            foreach (var def in context.Definitions)
            {
                if (def is EncounterConfigDefinition k)
                {
                    total++;
                    var map = k.Section == EncounterSection.Density ? density : limits;
                    if (map.ContainsKey(k.Key ?? string.Empty)) context.Report.Error(Name, k, "duplicate key '" + k.Key + "'");
                    else map[k.Key ?? string.Empty] = k;
                    CheckValues(context, k);
                }
                else if (def is EncounterRecipeDefinition r)
                {
                    total++;
                    recipes++;
                    CheckRecipe(context, r);
                }
            }
            if (total == 0) return;

            foreach (string key in EncounterConfigDefinition.DensityKeys)
                if (!density.ContainsKey(key)) context.Report.Error(Name, null, "density.csv is missing key '" + key + "'");
            foreach (string key in EncounterConfigDefinition.LimitKeys)
                if (!limits.ContainsKey(key)) context.Report.Error(Name, null, "encounter_limits.csv is missing key '" + key + "'");
            if (recipes == 0) context.Report.Error(Name, null, "encounter_recipes.csv has no recipe");
        }

        private void CheckValues(ValidationContext c, EncounterConfigDefinition k)
        {
            string key = k.Key ?? string.Empty;
            var allowed = k.Section == EncounterSection.Density ? EncounterConfigDefinition.DensityKeys : EncounterConfigDefinition.LimitKeys;
            if (Array.IndexOf(allowed, key) < 0)
            {
                c.Report.Error(Name, k, "unknown key '" + key + "'");
                return;
            }
            if (k.Values.Count != EncounterConfigDefinition.ExpectedValueCount(key))
            {
                c.Report.Error(Name, k, "key '" + key + "' needs " + EncounterConfigDefinition.ExpectedValueCount(key) + " value(s)");
                return;
            }

            float sum = 0f;
            foreach (float v in k.Values)
            {
                sum += v;
                if (v < 0f) c.Report.Error(Name, k, "'" + key + "' must not be negative");
            }
            if (k.Section == EncounterSection.Limits && sum <= 0f)
                c.Report.Error(Name, k, "limit '" + key + "' must be positive");
            if (k.Section == EncounterSection.Density && key != "density_ascension_per_level" && sum <= 0f)
                c.Report.Error(Name, k, "'" + key + "' must be positive");
            if ((key == "two_wave_split" || key == "three_wave_split") && Math.Abs(sum - 1f) > 0.001f)
                c.Report.Error(Name, k, "'" + key + "' must sum to 100% (got " + (sum * 100f) + "%)");
        }

        private void CheckRecipe(ValidationContext c, EncounterRecipeDefinition r)
        {
            if (r.FillerWeights.Length != 8)
                c.Report.Error(Name, r, "filler must have 8 weights (one per archetype), got " + r.FillerWeights.Length);
            foreach (float f in r.FillerWeights)
                if (f < 0f) c.Report.Error(Name, r, "negative filler weight");

            float share = 0f;
            foreach (RecipeSlotData s in r.Slots)
            {
                if (s.minCount < 0 || (s.maxCount != 0 && s.maxCount < s.minCount))
                    c.Report.Error(Name, r, "slot " + s.archetype + " needs 0 <= min <= max (max 0 = unlimited)");
                if (s.targetShare < 0f || s.targetShare > 1f)
                    c.Report.Error(Name, r, "slot " + s.archetype + " share must be 0-100%");
                share += s.targetShare;
            }
            if (share > 1f + 0.001f) c.Report.Error(Name, r, "slot shares exceed 100%");

            float best = 0f;
            foreach (RecipeWeightData w in r.RowWeights)
            {
                if (w.minRow < 1 || w.maxRow < w.minRow) c.Report.Error(Name, r, "weight row range invalid (" + w.minRow + ".." + w.maxRow + ")");
                if (w.weight < 0f) c.Report.Error(Name, r, "negative recipe weight");
                if (w.weight > best) best = w.weight;
            }
            if (best <= 0f) c.Report.Error(Name, r, "recipe needs at least one row weight greater than 0");
            if (r.MaxCastersOverride < 0 || r.MaxSummonersOverride < 0) c.Report.Error(Name, r, "negative cap override");
        }
    }
}
