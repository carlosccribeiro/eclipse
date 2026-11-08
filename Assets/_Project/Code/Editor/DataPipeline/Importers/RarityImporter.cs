using Eclipse.Core.DataTables;
using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/rarities.csv into RarityDefinition assets (02 sections 3.1-3.3, R25).</summary>
    [TableImporter("Rarities", Order = 30)]
    public sealed class RarityImporter : ItemTableImporter<RarityDefinition>
    {
        public const string DefaultCsvPath = "rarities.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Items/Rarities";

        public RarityImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public RarityImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(RarityDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            int affixes = row.GetInt("random_affix_count");
            int modifiers = row.GetInt("modifier_count");
            int minILvl = row.GetInt("min_drop_ilvl");
            if (affixes < 0 || modifiers < 0 || minILvl < 1)
                throw new CsvFormatException("counts must be >= 0 and min_drop_ilvl >= 1", row.Line, "random_affix_count");
            definition.EditorSet(row.GetEnum<Rarity>("rarity"), affixes, modifiers, minILvl);
        }
    }
}
