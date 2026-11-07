using System.IO;
using System.Linq;
using Eclipse.Data.Definitions;
using Eclipse.Data.Loot;
using Eclipse.Editor.DataPipeline;
using Eclipse.Editor.Validation;
using NUnit.Framework;

namespace Eclipse.Data.Items.Tests
{
    /// <summary>Integration: the real DataSource CSVs (filled by T7/T8) import into scratch folders, validate and build both catalogs.</summary>
    public class RealDataImportTests : ItemImportTestBase
    {
        private ITableImporter[] AllImporters() => new ITableImporter[]
        {
            new AffixImporter("affixes.csv", Sub("Affixes")),
            new SlotMultiplierImporter("slot_multipliers.csv", Sub("SlotMultipliers")),
            new AffixPoolImporter("affix_pools.csv", Sub("AffixPools")),
            new InfusionImporter("infusions.csv", Sub("Infusions")),
            new RarityImporter("rarities.csv", Sub("Rarities")),
            new NamedBandImporter("named_bands.csv", Sub("NamedBands")),
            new ItemBaseImporter("item_bases.csv", Sub("Bases")),
            new NamedItemImporter("items_named.csv", Sub("Named")),
            new LootSourceImporter("loot_sources.csv", Sub("LootSources")),
            new LootChanceImporter("loot_chances.csv", Sub("LootChances")),
            new IlvlRangeImporter("ilvl_ranges.csv", Sub("IlvlRanges")),
            new LootDifficultyImporter("loot_difficulty.csv", Sub("LootDifficulty")),
            new PityImporter("pity.csv", Sub("Pity")),
            new HomeRegionImporter("home_regions.csv", Sub("HomeRegions")),
            new LootGlobalImporter("loot_global.csv", Sub("LootGlobal")),
        };

        [Test]
        public void RealCsvs_ImportValidateAndBuildCatalogs()
        {
            string dataSource = Path.Combine(RepoRoot, "DataSource");
            ImportContext ctx = DataPipeline.Run(AllImporters(), dataSource);
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));

            var defs = AssetDatabaseAll();
            Assert.Greater(defs.Count, 0);

            // LocalizationKey is skipped on purpose: scratch imports do not write the String Table.
            ValidationReport report = DataValidator.Validate(defs);
            var errors = report.Messages.Where(m => m.Severity == ValidationSeverity.Error && m.Rule != "LocalizationKey").ToList();
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors));

            var items = new ItemCatalog(defs);
            var loot = new LootCatalog(defs);
            Assert.Greater(items.Affixes.Count, 0);
            Assert.Greater(items.NamedItems.Count, 0);
            Assert.AreEqual(10, loot.SlotWeights.Weights.Length);
            Assert.Greater(loot.QuantityCap, 0f);
        }

        private System.Collections.Generic.List<Definition> AssetDatabaseAll()
        {
            var list = new System.Collections.Generic.List<Definition>();
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Definition", new[] { Folder }))
                list.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<Definition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)));
            return list;
        }
    }
}
