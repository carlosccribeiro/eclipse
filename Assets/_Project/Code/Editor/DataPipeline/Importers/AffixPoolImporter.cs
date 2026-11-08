using Eclipse.Core.DataTables;
using Eclipse.Data.Items;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/affix_pools.csv into AffixPoolDefinition assets (02 section 3.9.3).</summary>
    [TableImporter("AffixPools", Order = 30, DependsOn = new[] { "Affixes" })]
    public sealed class AffixPoolImporter : ItemTableImporter<AffixPoolDefinition>
    {
        public const string DefaultCsvPath = "affix_pools.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Items/AffixPools";

        /// <summary>Weight columns in <c>EquipSlot</c> order.</summary>
        public static readonly string[] WeightColumns =
        {
            "w_main_hand", "w_off_hand", "w_head", "w_chest", "w_hands", "w_legs", "w_feet", "w_ring", "w_amulet", "w_relic",
        };

        public AffixPoolImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public AffixPoolImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(AffixPoolDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            var weights = new float[WeightColumns.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = row.GetFloat(WeightColumns[i]);
                if (weights[i] < 0f)
                    throw new CsvFormatException("weight must be >= 0", row.Line, WeightColumns[i]);
            }
            definition.EditorSet(row.GetString("affix_id"), weights, row.GetString("restriction", string.Empty));
        }

        protected override bool ResolveReferences(AffixPoolDefinition definition, CsvRow row, ImportContext context)
        {
            context.Require<AffixDefinition>(definition.AffixId, row.Line, definition.StableIdString, "affix_id");
            return false;
        }
    }
}
