using System;

namespace Eclipse.Editor.Validation
{
    /// <summary>Marks an <see cref="IValidationRule"/> implementation for automatic discovery.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ValidationRuleAttribute : Attribute
    {
    }
}
