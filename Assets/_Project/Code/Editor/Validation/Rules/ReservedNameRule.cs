using System;
using Eclipse.Data.Definitions;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// R48: a reserved name may only be used by its owner doc. Compares Definition.NameEn (case-insensitive, filled by
    /// the importer from name_en) with DataSource/ReservedNames.csv. Owner "-" means no doc may use it.
    /// A definition is allowed if its SourceDoc starts with the owner prefix (e.g. "07" matches "07-resonance").
    /// </summary>
    [ValidationRule]
    public sealed class ReservedNameRule : IValidationRule
    {
        public string Name => "ReservedName";

        public void Validate(ValidationContext context)
        {
            foreach (Definition d in context.Definitions)
            {
                string name = d.NameEn?.Trim();
                if (string.IsNullOrEmpty(name)) continue;
                if (!context.ReservedNames.TryGetValue(name, out string owner)) continue;
                owner = owner?.Trim();
                bool allowed = !string.IsNullOrEmpty(owner) && owner != "-" && d.SourceDoc != null &&
                               d.SourceDoc.StartsWith(owner, StringComparison.Ordinal);
                if (!allowed)
                    context.Report.Error(Name, d, "name '" + name + "' is reserved for doc '" + owner + "' (R48), SourceDoc='" + d.SourceDoc + "'");
            }
        }
    }
}
