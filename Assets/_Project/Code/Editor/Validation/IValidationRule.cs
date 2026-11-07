namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// A data validation rule. Implement it in any Editor assembly, add <see cref="ValidationRuleAttribute"/> and a
    /// public parameterless constructor; <see cref="DataValidator"/> discovers it through TypeCache.
    /// </summary>
    public interface IValidationRule
    {
        string Name { get; }
        void Validate(ValidationContext context);
    }
}
