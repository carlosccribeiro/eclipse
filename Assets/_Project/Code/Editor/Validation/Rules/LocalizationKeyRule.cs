using Eclipse.Data.Definitions;

namespace Eclipse.Editor.Validation
{
    /// <summary>displayName must be filled. Warning until the String Table exists (T6 raises it to Error).</summary>
    [ValidationRule]
    public sealed class LocalizationKeyRule : IValidationRule
    {
        /// <summary>Set to Error once the localization String Table exists (T6).</summary>
        public static ValidationSeverity Severity { get; set; } = ValidationSeverity.Warning;

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
