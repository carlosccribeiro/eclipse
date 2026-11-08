using Eclipse.Core.DataTables;
using Eclipse.Core.Loot;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/loot_chances.csv into LootChanceDefinition assets (05 sections 5.2-5.4).</summary>
    [TableImporter("LootChances", Order = 60)]
    public sealed class LootChanceImporter : ItemTableImporter<LootChanceDefinition>
    {
        public const string DefaultCsvPath = "loot_chances.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Loot/Chances";

        /// <summary>Anchor suffixes: Normal, Veteran, Nightmare, Eclipse, A1, A100.</summary>
        public static readonly string[] Anchors = { "normal", "veteran", "nightmare", "eclipse", "a1", "a100" };

        /// <summary>Value stored for an empty Epic/Rare cell (the source has a floor); the convention of LootChanceData.</summary>
        public const float FloorMarker = -1f;

        public LootChanceImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public LootChanceImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(LootChanceDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            var legendary = new float[Anchors.Length];
            var epic = new float[Anchors.Length];
            var rare = new float[Anchors.Length];
            for (int i = 0; i < Anchors.Length; i++)
            {
                legendary[i] = Pct(row, "leg_" + Anchors[i]);
                epic[i] = OptionalPct(row, "epic_" + Anchors[i]);
                rare[i] = OptionalPct(row, "rare_" + Anchors[i]);
            }
            definition.EditorSet(row.GetEnum<LootSourceKind>("source"), legendary, epic, rare);
        }

        private static float OptionalPct(CsvRow row, string column) =>
            !row.HasColumn(column) || row.IsEmpty(column) ? FloorMarker : row.GetFloat(column) / 100f;
    }
}
