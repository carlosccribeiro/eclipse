using Eclipse.Data.Definitions;

namespace Eclipse.Editor.Validation
{
    /// <summary>Every reference declared by an IReferencingDefinition must resolve to an existing definition.</summary>
    [ValidationRule]
    public sealed class ReferenceResolvesRule : IValidationRule
    {
        public string Name => "ReferenceResolves";

        public void Validate(ValidationContext context)
        {
            foreach (Definition d in context.Definitions)
            {
                if (!(d is IReferencingDefinition r) || r.References == null) continue;
                foreach ((string field, string targetId) in r.References)
                {
                    if (string.IsNullOrEmpty(targetId) || !context.TryGetById(targetId, out _))
                        context.Report.Error(Name, d, "field '" + field + "': reference '" + targetId + "' does not resolve");
                }
            }
        }
    }
}
