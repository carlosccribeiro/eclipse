using Eclipse.Core.DataTables;
using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/slot_multipliers.csv into SlotMultiplierDefinition assets (01 section 3.9.3).</summary>
    [TableImporter("SlotMultipliers", Order = 30, DependsOn = new[] { "Affixes" })]
    public sealed class SlotMultiplierImporter : ItemTableImporter<SlotMultiplierDefinition>
    {
        public const string DefaultCsvPath = "slot_multipliers.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Items/SlotMultipliers";

        public SlotMultiplierImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public SlotMultiplierImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(SlotMultiplierDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            var affixes = row.GetList("affix_ids");
            if (affixes.Count == 0)
                throw new CsvFormatException("affix_ids must list at least one affix", row.Line, "affix_ids");
            float multiplier = row.GetFloat("multiplier");
            if (multiplier <= 0f)
                throw new CsvFormatException("multiplier must be greater than 0", row.Line, "multiplier");
            definition.EditorSet(row.GetEnum<EquipSlot>("slot"), affixes, row.GetString("base_id", string.Empty), multiplier,
                row.GetBool("applies_to_implicit"));
        }

        protected override bool ResolveReferences(SlotMultiplierDefinition definition, CsvRow row, ImportContext context)
        {
            foreach (string id in definition.AffixIds)
                context.Require<AffixDefinition>(id, row.Line, definition.StableIdString, "affix_ids");
            if (!string.IsNullOrEmpty(definition.BaseId))
                context.Require<ItemBaseDefinition>(definition.BaseId, row.Line, definition.StableIdString, "base_id");
            return false;
        }
    }
}
