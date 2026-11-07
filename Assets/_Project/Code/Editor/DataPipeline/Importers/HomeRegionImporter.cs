using Eclipse.Core.DataTables;
using Eclipse.Core.Tags;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>
    /// Imports DataSource/home_regions.csv (one table, <c>row_kind</c> = Set or Region) into HomeRegionDefinition assets
    /// (05 sections 9.1, 9.2, 9.4, R32, R39).
    /// </summary>
    [TableImporter("HomeRegions", Order = 60)]
    public sealed class HomeRegionImporter : ItemTableImporter<HomeRegionDefinition>
    {
        public const string DefaultCsvPath = "home_regions.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Loot/HomeRegions";

        public HomeRegionImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public HomeRegionImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(HomeRegionDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            HomeRegionRowKind kind = row.GetEnum<HomeRegionRowKind>("row_kind");
            string region = row.GetString("region_id");

            if (kind == HomeRegionRowKind.Set)
            {
                definition.EditorSet(kind, row.GetString("set_id"), row.GetString("hero_id"), region,
                    row.GetFloat("home_weight"), row.GetFloat("home_boss_weight"), row.GetFloat("attunement_mult"),
                    TagMask.None, 0f, 0f);
            }
            else
            {
                // Empty dominant_affinities is valid: the Astral Citadel has none (05 section 9.2, uniform pool).
                TagMask dominant = Mask(row, "dominant_affinities");
                definition.EditorSet(kind, string.Empty, string.Empty, region, 0f, 0f, 0f, dominant,
                    row.GetFloat("affinity_weight"), Pct(row, "unique_bonus_pp", 0f));
            }
        }
    }
}
