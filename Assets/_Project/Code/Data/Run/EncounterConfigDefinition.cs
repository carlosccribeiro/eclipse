using System;
using System.Collections.Generic;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Run
{
    /// <summary>Which table an <see cref="EncounterConfigDefinition"/> row came from.</summary>
    public enum EncounterSection
    {
        Density,
        Limits
    }

    /// <summary>
    /// One key/value of the encounter planner numbers (09 §10.1-§10.3: density.csv, encounter_limits.csv).
    /// Percent values are already fractions. The full <see cref="EncounterConfig"/> is built by <see cref="ToCore"/>.
    /// </summary>
    public sealed class EncounterConfigDefinition : Definition
    {
        [SerializeField] private EncounterSection section;
        [SerializeField] private string key;
        [SerializeField] private float[] values = Array.Empty<float>();

        public EncounterSection Section => section;
        public string Key => key;
        public IReadOnlyList<float> Values => values;

        /// <summary>Keys required in density.csv (name = EncounterConfig field).</summary>
        public static readonly string[] DensityKeys =
        {
            "base_budget", "per_row_budget", "density_normal", "density_veteran", "density_nightmare", "density_eclipse",
            "density_ascension_base", "density_ascension_per_level", "density_ascension_cap", "elite_arena_mult",
            "elite_arena_min_elite_share", "two_wave_last_row", "two_wave_split", "three_wave_split",
            "next_wave_alive_fraction", "next_wave_max_seconds", "min_spawn_distance_meters", "spawn_portal_seconds"
        };

        /// <summary>Keys required in encounter_limits.csv (name = EncounterLimits field).</summary>
        public static readonly string[] LimitKeys =
        {
            "max_live_units", "max_live_pa", "max_casters_per_wave", "max_summoners_per_wave", "max_specialist_share",
            "min_frontline_per_wave", "max_tanks_early", "max_tanks_late", "tank_cap_last_early_row"
        };

        /// <summary>Number of values a key takes (wave splits are lists).</summary>
        public static int ExpectedValueCount(string key) => key == "two_wave_split" ? 2 : key == "three_wave_split" ? 3 : 1;

        /// <summary>Builds the Core config from all key rows plus the recipes. Throws on missing keys.</summary>
        public static EncounterConfig ToCore(IEnumerable<EncounterConfigDefinition> definitions,
            IEnumerable<EncounterRecipeDefinition> recipes)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (recipes == null) throw new ArgumentNullException(nameof(recipes));

            var density = new Dictionary<string, float[]>(StringComparer.Ordinal);
            var limits = new Dictionary<string, float[]>(StringComparer.Ordinal);
            foreach (EncounterConfigDefinition d in definitions)
            {
                if (d == null) continue;
                var target = d.section == EncounterSection.Density ? density : limits;
                if (target.ContainsKey(d.key)) throw new InvalidOperationException("duplicate key '" + d.key + "'");
                target[d.key] = d.values;
            }
            Require(density, DensityKeys, "density.csv");
            Require(limits, LimitKeys, "encounter_limits.csv");

            var c = new EncounterConfig
            {
                BaseBudget = density["base_budget"][0],
                PerRowBudget = density["per_row_budget"][0],
                DensityNormal = density["density_normal"][0],
                DensityVeteran = density["density_veteran"][0],
                DensityNightmare = density["density_nightmare"][0],
                DensityEclipse = density["density_eclipse"][0],
                DensityAscensionBase = density["density_ascension_base"][0],
                DensityAscensionPerLevel = density["density_ascension_per_level"][0],
                DensityAscensionCap = density["density_ascension_cap"][0],
                EliteArenaMult = density["elite_arena_mult"][0],
                EliteArenaMinEliteShare = density["elite_arena_min_elite_share"][0],
                TwoWaveLastRow = (int)density["two_wave_last_row"][0],
                TwoWaveSplit = (float[])density["two_wave_split"].Clone(),
                ThreeWaveSplit = (float[])density["three_wave_split"].Clone(),
                NextWaveAliveFraction = density["next_wave_alive_fraction"][0],
                NextWaveMaxSeconds = density["next_wave_max_seconds"][0],
                MinSpawnDistanceMeters = density["min_spawn_distance_meters"][0],
                SpawnPortalSeconds = density["spawn_portal_seconds"][0],
                Limits = new EncounterLimits
                {
                    MaxLiveUnits = (int)limits["max_live_units"][0],
                    MaxLivePa = (int)limits["max_live_pa"][0],
                    MaxCastersPerWave = (int)limits["max_casters_per_wave"][0],
                    MaxSummonersPerWave = (int)limits["max_summoners_per_wave"][0],
                    MaxSpecialistShare = limits["max_specialist_share"][0],
                    MinFrontlinePerWave = (int)limits["min_frontline_per_wave"][0],
                    MaxTanksEarly = (int)limits["max_tanks_early"][0],
                    MaxTanksLate = (int)limits["max_tanks_late"][0],
                    TankCapLastEarlyRow = (int)limits["tank_cap_last_early_row"][0]
                },
                Recipes = new List<EncounterRecipe>()
            };

            // Order by StableId: independent of asset enumeration order.
            var sorted = new List<EncounterRecipeDefinition>(recipes);
            sorted.RemoveAll(r => r == null);
            sorted.Sort((a, b) => string.CompareOrdinal(a.StableIdString, b.StableIdString));
            foreach (EncounterRecipeDefinition r in sorted) c.Recipes.Add(r.ToCore());
            return c;
        }

        private static void Require(Dictionary<string, float[]> map, string[] keys, string file)
        {
            foreach (string k in keys)
            {
                if (!map.TryGetValue(k, out float[] v)) throw new InvalidOperationException(file + " is missing key '" + k + "'");
                if (v.Length != ExpectedValueCount(k))
                    throw new InvalidOperationException(file + " key '" + k + "' needs " + ExpectedValueCount(k) + " value(s)");
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(EncounterSection encounterSection, string keyName, float[] resolvedValues)
        {
            section = encounterSection; key = keyName; values = resolvedValues;
        }
#endif
    }
}
