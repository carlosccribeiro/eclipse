using Eclipse.Core.DataTables;
using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/item_bases.csv into ItemBaseDefinition assets (02 sections 3.4-3.8, 3.10).</summary>
    [TableImporter("ItemBases", Order = 40, DependsOn = new[] { "Affixes" })]
    public sealed class ItemBaseImporter : ItemTableImporter<ItemBaseDefinition>
    {
        public const string DefaultCsvPath = "item_bases.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Items/Bases";

        public ItemBaseImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public ItemBaseImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(ItemBaseDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            definition.EditorSet(
                row.GetEnum<EquipSlot>("slot"),
                row.GetString("category"),
                row.GetEnum<ArmorFamily>("family"),
                OptString(row, "hero_id"),
                Mask(row, "tags"),
                row.GetString("implicit_affix_id", string.Empty),
                row.GetFloat("implicit_factor"),
                row.GetFloat("armor_factor"),
                row.GetBool("droppable"),
                OptFloat(row, "dps_factor"),
                Mask(row, "implicit_affinity"),
                OptString(row, "secondary_implicit_stat"),
                OptFloat(row, "secondary_implicit_at_100"),
                OptString(row, "implicit_effect_id"));
        }

        protected override bool ResolveReferences(ItemBaseDefinition definition, CsvRow row, ImportContext context)
        {
            if (!string.IsNullOrEmpty(definition.ImplicitAffixId))
                context.Require<AffixDefinition>(definition.ImplicitAffixId, row.Line, definition.StableIdString, "implicit_affix_id");
            return false;
        }
    }
}
