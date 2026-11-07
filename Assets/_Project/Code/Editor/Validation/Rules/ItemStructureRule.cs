using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// Structure of named items (00 section 5 R25; 02 sections 3.2, 3.3, 3.12): Legendary and set piece have 4 fixed affixes +
    /// 1 Open Affix with 3 options + 2 modifiers; Unique has 5 fixed + 2 modifiers with at most 1 Signature (<c>SIG-</c>).
    /// </summary>
    [ValidationRule]
    public sealed class ItemStructureRule : IValidationRule
    {
        // R25 structural counts (the same numbers live in rarities.csv for generic items).
        public const int LegendaryFixed = 4;
        public const int OpenOptionCount = 3;
        public const int UniqueFixed = 5;
        public const int ModifierCount = 2;
        public const int MaxSignatureModifiers = 1;
        public const string SignaturePrefix = "SIG-";

        public string Name => "ItemStructure";

        public void Validate(ValidationContext context)
        {
            foreach (var d in context.Definitions)
            {
                if (!(d is NamedItemDefinition item)) continue;

                if (item.ModifierIds.Count != ModifierCount)
                    context.Report.Error(Name, item, "expected " + ModifierCount + " modifiers, found " + item.ModifierIds.Count + " (R25)");

                if (item.ItemKind == ItemKind.Unique)
                {
                    if (item.FixedAffixes.Count != UniqueFixed)
                        context.Report.Error(Name, item, "Unique expects " + UniqueFixed + " fixed affixes, found " + item.FixedAffixes.Count + " (R25)");
                    if (item.OpenOptions.Count != 0)
                        context.Report.Error(Name, item, "Unique has no Open Affix, found " + item.OpenOptions.Count + " options (R25)");
                    int signatures = 0;
                    foreach (string m in item.ModifierIds)
                        if (m != null && m.StartsWith(SignaturePrefix, System.StringComparison.Ordinal)) signatures++;
                    if (signatures > MaxSignatureModifiers)
                        context.Report.Error(Name, item, "Unique has " + signatures + " Signature modifiers, maximum is " + MaxSignatureModifiers + " (R25)");
                }
                else if (item.ItemKind == ItemKind.NamedLegendary || item.ItemKind == ItemKind.SetPiece)
                {
                    if (item.FixedAffixes.Count != LegendaryFixed)
                        context.Report.Error(Name, item, item.ItemKind + " expects " + LegendaryFixed + " fixed affixes, found " + item.FixedAffixes.Count + " (R25)");
                    if (item.OpenOptions.Count != OpenOptionCount)
                        context.Report.Error(Name, item, item.ItemKind + " expects " + OpenOptionCount + " Open Affix options, found " + item.OpenOptions.Count + " (R25)");
                }
                else
                {
                    context.Report.Error(Name, item, "named item cannot be of kind " + item.ItemKind);
                }
            }
        }
    }
}
