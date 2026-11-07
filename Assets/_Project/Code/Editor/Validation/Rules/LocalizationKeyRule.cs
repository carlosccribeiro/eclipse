using Eclipse.Data.Definitions;

namespace Eclipse.Editor.Validation
{
    /// <summary>displayName must be filled (the String Table exists since T6, so it is an Error).</summary>
    [ValidationRule]
    public sealed class LocalizationKeyRule : IValidationRule
    {
        public static ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;

        public string Name => "LocalizationKey";

        public void Validate(ValidationContext context)
        {
            foreach (Definition d in context.Definitions)
            {
                if (d.DisplayName == null || d.DisplayName.IsEmpty)
                    context.Report.Add(Severity, Name, d, "displayName is not set");
            }
        }
    }
}
