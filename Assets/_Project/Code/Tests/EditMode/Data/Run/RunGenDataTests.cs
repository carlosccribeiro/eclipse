using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Core.RunGen.Map;
using Eclipse.Data.Definitions;
using Eclipse.Data.Items.Tests;
using Eclipse.Data.Validation.Tests;
using Eclipse.Editor.DataPipeline;
using Eclipse.Editor.Validation;
using NUnit.Framework;
using UnityEditor;

namespace Eclipse.Data.Run.Tests
{
    /// <summary>M15 T5: real DataSource/run CSVs import into scratch folders, validate, and ToCore() equals the Core defaults.</summary>
    public class RunGenDataTests : ItemImportTestBase
    {
        private string RunRoot => Sub("Run");

        private ITableImporter[] AllImporters() => new ITableImporter[]
        {
            new RoomWeightsImporter(RunRoot), new MapRowsImporter(RunRoot), new DoorRewardsImporter(RunRoot),
            new DensityImporter(RunRoot), new EncounterLimitsImporter(RunRoot), new EncounterRecipesImporter(RunRoot),
        };

        private ImportContext ImportReal()
        {
            ImportContext ctx = DataPipeline.Run(AllImporters(), Path.Combine(RepoRoot, "DataSource"));
            AssetDatabase.SaveAssets();
            return ctx;
        }

        private static List<Definition> AllDefs(string folder) =>
            AssetDatabase.FindAssets("t:Definition", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<Definition>(AssetDatabase.GUIDToAssetPath(g))).ToList();

        [Test]
        public void RealCsvs_ImportWithoutErrors_AndPassValidation()
        {
            ImportContext ctx = ImportReal();
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));

            var errors = DataValidator.Validate(AllDefs(RunRoot)).Messages
                .Where(m => m.Severity == ValidationSeverity.Error && m.Rule != "LocalizationKey").ToList();
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
            Assert.AreEqual(3 + 13 + 5, LoadAll<MapGenConfigDefinition>(RunRoot).Count);
            Assert.AreEqual(7, LoadAll<EncounterRecipeDefinition>(RunRoot).Count);
        }

        [Test]
        public void MapGenConfig_ToCore_EqualsDefaults()
        {
            Assert.AreEqual(0, ImportReal().ErrorCount);
            MapGenConfig actual = MapGenConfigDefinition.ToCore(LoadAll<MapGenConfigDefinition>(RunRoot));
            MapGenConfig expected = MapGenConfig.CreateDefault();

            Assert.AreEqual(expected.Rows.Length, actual.Rows.Length);
            for (int i = 0; i < expected.Rows.Length; i++)
            {
                Assert.AreEqual(expected.Rows[i].MinNodes, actual.Rows[i].MinNodes, "row " + (i + 1));
                Assert.AreEqual(expected.Rows[i].MaxNodes, actual.Rows[i].MaxNodes, "row " + (i + 1));
                Assert.AreEqual(expected.Rows[i].Kind, actual.Rows[i].Kind, "row " + (i + 1));
                Assert.AreEqual(expected.Rows[i].AllowedMask, actual.Rows[i].AllowedMask, "row " + (i + 1));
            }
            for (int s = 0; s < MapGenConfig.StratumCount; s++)
                for (int t = 0; t < MapGenConfig.WeightedTypeCount; t++)
                    Assert.AreEqual(expected.Weights[s, t], actual.Weights[s, t], "stratum " + (s + 1) + " col " + t);
            CollectionAssert.AreEqual(expected.DoorRewardWeights, actual.DoorRewardWeights);
            Assert.AreEqual(expected.RowAttempts, actual.RowAttempts);
            Assert.AreEqual(expected.EdgeAttempts, actual.EdgeAttempts);
            Assert.AreEqual(expected.RewardAttempts, actual.RewardAttempts);
            Assert.AreEqual(expected.MaxAttempts, actual.MaxAttempts);
        }

        [Test]
        public void EncounterConfig_ToCore_EqualsDefaults()
        {
            Assert.AreEqual(0, ImportReal().ErrorCount);
            EncounterConfig a = EncounterConfigDefinition.ToCore(
                LoadAll<EncounterConfigDefinition>(RunRoot), LoadAll<EncounterRecipeDefinition>(RunRoot));
            EncounterConfig e = EncounterConfig.Default();

            Assert.AreEqual(e.BaseBudget, a.BaseBudget);
            Assert.AreEqual(e.PerRowBudget, a.PerRowBudget);
            Assert.AreEqual(e.DensityNormal, a.DensityNormal);
            Assert.AreEqual(e.DensityVeteran, a.DensityVeteran);
            Assert.AreEqual(e.DensityNightmare, a.DensityNightmare);
            Assert.AreEqual(e.DensityEclipse, a.DensityEclipse);
            Assert.AreEqual(e.DensityAscensionBase, a.DensityAscensionBase);
            Assert.AreEqual(e.DensityAscensionPerLevel, a.DensityAscensionPerLevel);
            Assert.AreEqual(e.DensityAscensionCap, a.DensityAscensionCap);
            Assert.AreEqual(e.EliteArenaMult, a.EliteArenaMult);
            Assert.AreEqual(e.EliteArenaMinEliteShare, a.EliteArenaMinEliteShare);
            Assert.AreEqual(e.TwoWaveLastRow, a.TwoWaveLastRow);
            CollectionAssert.AreEqual(e.TwoWaveSplit, a.TwoWaveSplit);
            CollectionAssert.AreEqual(e.ThreeWaveSplit, a.ThreeWaveSplit);
            Assert.AreEqual(e.NextWaveAliveFraction, a.NextWaveAliveFraction);
            Assert.AreEqual(e.NextWaveMaxSeconds, a.NextWaveMaxSeconds);
            Assert.AreEqual(e.MinSpawnDistanceMeters, a.MinSpawnDistanceMeters);
            Assert.AreEqual(e.SpawnPortalSeconds, a.SpawnPortalSeconds);

            Assert.AreEqual(e.Limits.MaxLiveUnits, a.Limits.MaxLiveUnits);
            Assert.AreEqual(e.Limits.MaxLivePa, a.Limits.MaxLivePa);
            Assert.AreEqual(e.Limits.MaxCastersPerWave, a.Limits.MaxCastersPerWave);
            Assert.AreEqual(e.Limits.MaxSummonersPerWave, a.Limits.MaxSummonersPerWave);
            Assert.AreEqual(e.Limits.MaxSpecialistShare, a.Limits.MaxSpecialistShare);
            Assert.AreEqual(e.Limits.MinFrontlinePerWave, a.Limits.MinFrontlinePerWave);
            Assert.AreEqual(e.Limits.MaxTanksEarly, a.Limits.MaxTanksEarly);
            Assert.AreEqual(e.Limits.MaxTanksLate, a.Limits.MaxTanksLate);
            Assert.AreEqual(e.Limits.TankCapLastEarlyRow, a.Limits.TankCapLastEarlyRow);

            // Recipes: compare by id (defaults are not sorted by id).
            Assert.AreEqual(e.Recipes.Count, a.Recipes.Count);
            foreach (EncounterRecipe er in e.Recipes)
            {
                EncounterRecipe ar = a.Recipes.Single(r => r.Id == er.Id);
                CollectionAssert.AreEqual(er.FillerWeights, ar.FillerWeights, er.Id);
                Assert.AreEqual(er.MaxCastersOverride, ar.MaxCastersOverride, er.Id);
                Assert.AreEqual(er.MaxSummonersOverride, ar.MaxSummonersOverride, er.Id);
                Assert.AreEqual(er.Slots.Count, ar.Slots.Count, er.Id);
                for (int i = 0; i < er.Slots.Count; i++)
                {
                    Assert.AreEqual(er.Slots[i].Archetype, ar.Slots[i].Archetype, er.Id);
                    Assert.AreEqual(er.Slots[i].MinCount, ar.Slots[i].MinCount, er.Id);
                    Assert.AreEqual(er.Slots[i].MaxCount, ar.Slots[i].MaxCount, er.Id);
                    Assert.AreEqual(er.Slots[i].TargetShare, ar.Slots[i].TargetShare, er.Id);
                }
                Assert.AreEqual(er.Weights.Count, ar.Weights.Count, er.Id);
                for (int i = 0; i < er.Weights.Count; i++)
                {
                    Assert.AreEqual(er.Weights[i].RegionId, ar.Weights[i].RegionId, er.Id);
                    Assert.AreEqual(er.Weights[i].MinRow, ar.Weights[i].MinRow, er.Id);
                    Assert.AreEqual(er.Weights[i].MaxRow, ar.Weights[i].MaxRow, er.Id);
                    Assert.AreEqual(er.Weights[i].Weight, ar.Weights[i].Weight, er.Id);
                }
            }
        }

        [Test]
        public void Reimport_ChangesNothing()
        {
            Assert.AreEqual(0, ImportReal().ErrorCount);
            ImportContext second = ImportReal();
            Assert.AreEqual(0, second.Created);
            Assert.AreEqual(0, second.Updated);
        }

        [Test]
        public void Importer_RejectsUnknownRoomType()
        {
            Directory.CreateDirectory(Path.Combine(Root, "run"));
            Write("run/map_rows.csv", Csv("id,name_pt,name_en,row,min_nodes,max_nodes,kind,allowed,source_doc",
                "maprow.core.row_01,L1,R1,1,2,2,AllCombat,Bogus,09 §11.1"));
            ImportContext ctx = DataPipeline.Run(new ITableImporter[] { new MapRowsImporter(RunRoot) }, Root);
            Assert.AreEqual(1, ctx.ErrorCount);
        }
    }

    /// <summary>Validation rules MapGenData and EncounterData: one passing and one failing case each.</summary>
    public class RunGenRuleTests
    {
        private readonly List<Definition> _defs = new List<Definition>();

        [TearDown]
        public void TearDown()
        {
            foreach (Definition d in _defs) UnityEngine.Object.DestroyImmediate(d);
            _defs.Clear();
        }

        private T Add<T>(string id) where T : Definition
        {
            T d = FakeValidationDef.Make<T>(id, "n", "09 §11");
            _defs.Add(d);
            return d;
        }

        private int Errors(string rule) =>
            DataValidator.Validate(_defs).Messages.Count(m => m.Rule == rule && m.Severity == ValidationSeverity.Error);

        private string Msgs(string rule) =>
            string.Join("\n", DataValidator.Validate(_defs).Messages.Where(m => m.Rule == rule));

        private static readonly float[] ValidWeights = { 40, 10, 10, 20, 10, 10 };

        // ---- MapGenData ----

        private void ValidMap(int weightsStrata = 3, int rows = 13)
        {
            for (int s = 1; s <= weightsStrata; s++)
                Add<MapGenConfigDefinition>("roomweights.core.stratum_" + s).EditorSetWeights(s, ValidWeights);
            var def = MapGenConfig.CreateDefault();
            for (int r = 1; r <= rows; r++)
            {
                RowSpec spec = def.Rows[r - 1];
                Add<MapGenConfigDefinition>("maprow.core.row_" + r).EditorSetRow(r, spec.MinNodes, spec.MaxNodes, spec.Kind, spec.AllowedMask);
            }
            for (DoorReward k = DoorReward.Equipment; k <= DoorReward.Heal; k++)
                Add<MapGenConfigDefinition>("doorreward.core.r" + (int)k).EditorSetDoorReward(k, 10f);
        }

        [Test]
        public void MapGenData_ValidTables_Pass()
        {
            ValidMap();
            Assert.AreEqual(0, Errors("MapGenData"), Msgs("MapGenData"));
        }

        [Test]
        public void MapGenData_NoRows_IsIgnored()
        {
            Assert.AreEqual(0, Errors("MapGenData"));
        }

        [Test]
        public void MapGenData_ZeroWeightSum_Fails()
        {
            ValidMap();
            Add<MapGenConfigDefinition>("roomweights.core.zero").EditorSetWeights(1, new float[6]);
            Assert.Greater(Errors("MapGenData"), 0);
        }

        [Test]
        public void MapGenData_WrongRowCount_Fails()
        {
            ValidMap(rows: 12);
            StringAssert.Contains("row 13", Msgs("MapGenData"));
            Assert.Greater(Errors("MapGenData"), 0);
        }

        [Test]
        public void MapGenData_RowCountsIncoherent_Fails()
        {
            ValidMap();
            var bad = Add<MapGenConfigDefinition>("maprow.core.row_99");
            bad.EditorSetRow(2, 4, 3, RowKind.Weighted, 1); // duplicate row 2 and min > max
            Assert.GreaterOrEqual(Errors("MapGenData"), 2);
        }

        [Test]
        public void MapGenData_ZeroDoorWeights_Fails()
        {
            for (int s = 1; s <= 3; s++) Add<MapGenConfigDefinition>("roomweights.core.stratum_" + s).EditorSetWeights(s, ValidWeights);
            var def = MapGenConfig.CreateDefault();
            for (int r = 1; r <= 13; r++)
            {
                RowSpec spec = def.Rows[r - 1];
                Add<MapGenConfigDefinition>("maprow.core.row_" + r).EditorSetRow(r, spec.MinNodes, spec.MaxNodes, spec.Kind, spec.AllowedMask);
            }
            for (DoorReward k = DoorReward.Equipment; k <= DoorReward.Heal; k++)
                Add<MapGenConfigDefinition>("doorreward.core.r" + (int)k).EditorSetDoorReward(k, 0f);
            StringAssert.Contains("door reward", Msgs("MapGenData"));
        }

        // ---- EncounterData ----

        private void ValidEncounter()
        {
            EncounterConfig d = EncounterConfig.Default();
            void Key(EncounterSection sec, string key, params float[] v) =>
                Add<EncounterConfigDefinition>("enc.core." + key).EditorSet(sec, key, v);
            Key(EncounterSection.Density, "base_budget", d.BaseBudget);
            Key(EncounterSection.Density, "per_row_budget", d.PerRowBudget);
            Key(EncounterSection.Density, "density_normal", d.DensityNormal);
            Key(EncounterSection.Density, "density_veteran", d.DensityVeteran);
            Key(EncounterSection.Density, "density_nightmare", d.DensityNightmare);
            Key(EncounterSection.Density, "density_eclipse", d.DensityEclipse);
            Key(EncounterSection.Density, "density_ascension_base", d.DensityAscensionBase);
            Key(EncounterSection.Density, "density_ascension_per_level", d.DensityAscensionPerLevel);
            Key(EncounterSection.Density, "density_ascension_cap", d.DensityAscensionCap);
            Key(EncounterSection.Density, "elite_arena_mult", d.EliteArenaMult);
            Key(EncounterSection.Density, "elite_arena_min_elite_share", d.EliteArenaMinEliteShare);
            Key(EncounterSection.Density, "two_wave_last_row", d.TwoWaveLastRow);
            Key(EncounterSection.Density, "two_wave_split", d.TwoWaveSplit);
            Key(EncounterSection.Density, "three_wave_split", d.ThreeWaveSplit);
            Key(EncounterSection.Density, "next_wave_alive_fraction", d.NextWaveAliveFraction);
            Key(EncounterSection.Density, "next_wave_max_seconds", d.NextWaveMaxSeconds);
            Key(EncounterSection.Density, "min_spawn_distance_meters", d.MinSpawnDistanceMeters);
            Key(EncounterSection.Density, "spawn_portal_seconds", d.SpawnPortalSeconds);
            EncounterLimits l = d.Limits;
            Key(EncounterSection.Limits, "max_live_units", l.MaxLiveUnits);
            Key(EncounterSection.Limits, "max_live_pa", l.MaxLivePa);
            Key(EncounterSection.Limits, "max_casters_per_wave", l.MaxCastersPerWave);
            Key(EncounterSection.Limits, "max_summoners_per_wave", l.MaxSummonersPerWave);
            Key(EncounterSection.Limits, "max_specialist_share", l.MaxSpecialistShare);
            Key(EncounterSection.Limits, "min_frontline_per_wave", l.MinFrontlinePerWave);
            Key(EncounterSection.Limits, "max_tanks_early", l.MaxTanksEarly);
            Key(EncounterSection.Limits, "max_tanks_late", l.MaxTanksLate);
            Key(EncounterSection.Limits, "tank_cap_last_early_row", l.TankCapLastEarlyRow);

            Add<EncounterRecipeDefinition>("encrecipe.core.swarm").EditorSet(
                new[] { new RecipeSlotData { archetype = EnemyArchetype.Fodder, minCount = 1, maxCount = 0, targetShare = 0.7f } },
                new float[] { 1, 0, 0, 0, 0, 0, 0, 0 },
                new[] { new RecipeWeightData { regionId = "", minRow = 1, maxRow = 13, weight = 2f } }, 0, 0);
        }

        [Test]
        public void EncounterData_ValidTables_Pass()
        {
            ValidEncounter();
            Assert.AreEqual(0, Errors("EncounterData"), Msgs("EncounterData"));
        }

        [Test]
        public void EncounterData_NonPositiveLimit_Fails()
        {
            ValidEncounter();
            _defs.OfType<EncounterConfigDefinition>().Single(d => d.Key == "max_live_units")
                .EditorSet(EncounterSection.Limits, "max_live_units", new float[] { 0 });
            StringAssert.Contains("positive", Msgs("EncounterData"));
        }

        [Test]
        public void EncounterData_MissingKey_Fails()
        {
            ValidEncounter();
            var victim = _defs.OfType<EncounterConfigDefinition>().Single(d => d.Key == "max_live_pa");
            _defs.Remove(victim);
            UnityEngine.Object.DestroyImmediate(victim);
            StringAssert.Contains("max_live_pa", Msgs("EncounterData"));
        }

        [Test]
        public void EncounterData_WaveSplitNotHundredPercent_Fails()
        {
            ValidEncounter();
            _defs.OfType<EncounterConfigDefinition>().Single(d => d.Key == "two_wave_split")
                .EditorSet(EncounterSection.Density, "two_wave_split", new[] { 0.5f, 0.2f });
            StringAssert.Contains("100%", Msgs("EncounterData"));
        }

        [Test]
        public void EncounterData_RecipeWithoutPositiveWeight_Fails()
        {
            ValidEncounter();
            Add<EncounterRecipeDefinition>("encrecipe.core.dead").EditorSet(
                new RecipeSlotData[0], new float[8],
                new[] { new RecipeWeightData { regionId = "", minRow = 1, maxRow = 13, weight = 0f } }, 0, 0);
            StringAssert.Contains("row weight", Msgs("EncounterData"));
        }

        [Test]
        public void EncounterData_RecipeFillerWrongLength_Fails()
        {
            ValidEncounter();
            Add<EncounterRecipeDefinition>("encrecipe.core.short").EditorSet(
                new RecipeSlotData[0], new float[3],
                new[] { new RecipeWeightData { regionId = "", minRow = 1, maxRow = 13, weight = 1f } }, 0, 0);
            StringAssert.Contains("8 weights", Msgs("EncounterData"));
        }
    }
}
