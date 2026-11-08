using Eclipse.Core.DataTables;
using Eclipse.Core.Items;
using Eclipse.Core.Loot;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/loot_sources.csv into LootSourceDefinition assets (05 sections 3.1, 3.2, 8.1).</summary>
    [TableImporter("LootSources", Order = 60)]
    public sealed class LootSourceImporter : ItemTableImporter<LootSourceDefinition>
    {
        public const string DefaultCsvPath = "loot_sources.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Loot/Sources";

        public LootSourceImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public LootSourceImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(LootSourceDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            definition.EditorSet(
                row.GetEnum<LootSourceKind>("source"),
                Pct(row, "drop_chance_pct"),
                row.GetInt("guaranteed_items"),
                Pct(row, "extra_item_chance_pct"),
                row.GetInt("extra_items_if_all_challenges"),
                row.GetInt("choose_of"),
                row.GetInt("guaranteed_epic_plus"),
                row.GetEnum<Rarity>("rarity_floor"),
                row.GetBool("affected_by_quantity"),
                row.GetInt("ilvl_bonus"),
                row.GetBool("ilvl_from_range_top"),
                row.GetBool("consumes_hard_pity"));
        }
    }
}
