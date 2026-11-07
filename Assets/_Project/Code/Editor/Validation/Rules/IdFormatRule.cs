using Eclipse.Core.Ids;
using Eclipse.Data.Definitions;

namespace Eclipse.Editor.Validation
{
    /// <summary>The serialized StableId must be a valid slug; empty is an error.</summary>
    [ValidationRule]
    public sealed class IdFormatRule : IValidationRule
    {
        public string Name => "IdFormat";

        public void Validate(ValidationContext context)
        {
            foreach (Definition d in context.Definitions)
            {
                string id = d.StableIdString;
                if (string.IsNullOrEmpty(id)) context.Report.Error(Name, d, "StableId is empty");
                else if (!StableId.IsValid(id)) context.Report.Error(Name, d, "invalid StableId '" + id + "'");
            }
        }
    }
}
