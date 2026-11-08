using System.Collections.Generic;
using Eclipse.Core.DataTables;
using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>
    /// Imports DataSource/items_named.csv into NamedItemDefinition assets (02 sections 3.11, 3.12; 11 section 3.4.2).
    /// Modifier codes of 03 (<c>KAE-CS-01</c>) stay as text; structure checks live in the validation rules.
    /// </summary>
    [TableImporter("NamedItems", Order = 50, DependsOn = new[] { "Affixes", "ItemBases" })]
    public sealed class NamedItemImporter : ItemTableImporter<NamedItemDefinition>
    {
        public const string DefaultCsvPath = "items_named.csv";
        public const string DefaultOutputFolder = "Assets/_Project/Content/Items/Named";

        public NamedItemImporter() : base(DefaultCsvPath, DefaultOutputFolder, DefaultOutputFolder) { }
        public NamedItemImporter(string csvPath, string outputFolder) : base(csvPath, outputFolder, DefaultOutputFolder) { }

        protected override void Apply(NamedItemDefinition definition, CsvRow row, ImportContext context)
        {
            ApplyCommon(definition, row);
            ItemKind kind = row.GetEnum<ItemKind>("item_kind");
            if (kind == ItemKind.Generic)
                throw new CsvFormatException("item_kind must be NamedLegendary, SetPiece or Unique", row.Line, "item_kind");

            List<NamedAffixEntry> fixedList = NamedAffixList(row, "fixed_affixes");
            if (fixedList.Count == 0)
                throw new CsvFormatException("fixed_affixes is empty", row.Line, "fixed_affixes");
            List<NamedAffixEntry> openList = NamedAffixList(row, "open_options");
            List<string> modifiers = row.GetList("modifier_ids");

            string setId = OptString(row, "set_id");
            if (kind == ItemKind.SetPiece && setId.Length == 0)
                throw new CsvFormatException("set_id is required for SetPiece", row.Line, "set_id");

            UniqueSourceKind sourceKind = row.HasColumn("unique_source_kind")
                ? row.GetEnum("unique_source_kind", UniqueSourceKind.None) : UniqueSourceKind.None;
            string sourceRef = OptString(row, "unique_source_ref");
            // Challenge Uniques (K15, L16, N15, O15) have no single source entity, so their ref may be empty (02 section 3.12.6).
            bool refRequired = sourceKind != UniqueSourceKind.Challenge;
            if (kind == ItemKind.Unique && (sourceKind == UniqueSourceKind.None || (refRequired && sourceRef.Length == 0)))
                throw new CsvFormatException("Unique requires unique_source_kind and (except Challenge) unique_source_ref", row.Line, "unique_source_kind");

            definition.EditorSet(row.GetString("catalog_no"), kind, row.GetString("base_id"), OptString(row, "hero_id"),
                OptString(row, "effect_id"), fixedList, openList, modifiers, Mask(row, "affinity"), setId, sourceKind,
                sourceRef, row.GetInt("min_drop_ilvl"));
            definition.EditorSetExtras(OptString(row, "subtitle_pt"), OptString(row, "subtitle_en"), OptString(row, "lore_pt"),
                OptString(row, "lore_en"), OptString(row, "build"), OptFloat(row, "effect_more_declared"));
        }

        protected override bool ResolveReferences(NamedItemDefinition definition, CsvRow row, ImportContext context)
        {
            string owner = definition.StableIdString;
            context.Require<ItemBaseDefinition>(definition.BaseId, row.Line, owner, "base_id");
            foreach (NamedAffixEntry e in definition.FixedAffixes)
                context.Require<AffixDefinition>(e.affixId, row.Line, owner, "fixed_affixes");
            foreach (NamedAffixEntry e in definition.OpenOptions)
                context.Require<AffixDefinition>(e.affixId, row.Line, owner, "open_options");
            return false;
        }
    }
}
