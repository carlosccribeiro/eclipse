using Eclipse.Core.DataTables;
using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/infusions.csv into InfusionDefinition assets (02 section 3.9.4).</summary>
    [TableImporter("Infusions", Order = 30, DependsOn = new[] { "Affixes" })]
    public sealed class InfusionImporter : ItemTableImporter<InfusionDefinition>
    {
        public const string DefaultCsvPath = "infusions.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Items/Infusions";

        public InfusionImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public InfusionImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(InfusionDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            definition.EditorSet(row.GetString("affix_id"), Mask(row, "affinity"), row.GetEnum<InfusionForm>("form"),
                row.GetEnum("hit_filter", InfusionHitFilter.None), row.GetBool("counts_as_affinity"), row.GetString("pool_rule"));
        }

        protected override bool ResolveReferences(InfusionDefinition definition, CsvRow row, ImportContext context)
        {
            AffixDefinition affix = context.Require<AffixDefinition>(definition.AffixId, row.Line, definition.StableIdString, "affix_id");
            if (affix != null && affix.Category != AffixCategory.Infusion)
                context.Error(row.Line, definition.StableIdString, "column 'affix_id': '" + definition.AffixId + "' is not category Infusion");
            return false;
        }
    }
}
