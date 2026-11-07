using Eclipse.Core.DataTables;
using Eclipse.Core.Ids;

namespace Eclipse.Editor.DataPipeline.Tests
{
    /// <summary>Test importer; deliberately has no [TableImporter] so real runs never discover it.</summary>
    public class FakePipelineImporter : TableImporter<FakePipelineDef>
    {
        private readonly string _name;
        private readonly string _csv;
        private readonly string _folder;

        public FakePipelineImporter(string name, string csv, string folder)
        {
            _name = name;
            _csv = csv;
            _folder = folder;
        }

        /// <summary>Remap injected by tests; null means no migrations.</summary>
        public IdRemapTable Remap { get; set; }

        protected override IdRemapTable LoadRemap() => Remap ?? new IdRemapTable();

        public override string TableName => _name;
        public override string CsvPath => _csv;
        public override string OutputFolder => _folder;

        protected override void Apply(FakePipelineDef definition, CsvRow row, ImportContext context)
        {
            definition.Power = row.GetInt("power");
            definition.Label = row.GetString("label", string.Empty);
        }

        protected override bool ResolveReferences(FakePipelineDef definition, CsvRow row, ImportContext context)
        {
            string partnerId = row.GetString("partner", null);
            if (partnerId == null) return false;
            FakePipelineDef partner = context.Require<FakePipelineDef>(partnerId, row.Line, definition.StableIdString, "partner");
            if (partner == null || definition.Partner == partner) return false;
            definition.Partner = partner;
            return true;
        }
    }
}
