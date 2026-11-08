using Eclipse.Core.DataTables;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>
    /// Imports DataSource/loot_global.csv (key/value constants) into LootGlobalDefinition assets. <c>value_kind</c> Percent is divided
    /// by 100. The presence of every required key is enforced by LootGlobalKeysRule and by the catalog builders.
    /// </summary>
    [TableImporter("LootGlobal", Order = 60)]
    public sealed class LootGlobalImporter : ItemTableImporter<LootGlobalDefinition>
    {
        public const string DefaultCsvPath = "loot_global.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Loot/Global";

        public enum GlobalValueKind { Number, Percent }

        public LootGlobalImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public LootGlobalImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(LootGlobalDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            float value = row.GetFloat("value");
            if (row.GetEnum<GlobalValueKind>("value_kind") == GlobalValueKind.Percent) value /= 100f;
            definition.EditorSet(row.GetString("key"), value);
        }
    }
}
