using System.Collections.Generic;
using Eclipse.Data.Definitions;

namespace Eclipse.Editor.Validation
{
    public enum ValidationSeverity
    {
        Warning,
        Error,
    }

    public sealed class ValidationMessage
    {
        public ValidationSeverity Severity { get; }
        public string Rule { get; }
        public string Id { get; }
        public string AssetPath { get; }
        public string Text { get; }

        public ValidationMessage(ValidationSeverity severity, string rule, string id, string assetPath, string text)
        {
            Severity = severity;
            Rule = rule;
            Id = id;
            AssetPath = assetPath;
            Text = text;
        }

        public override string ToString()
        {
            string where = string.IsNullOrEmpty(AssetPath) ? "" : AssetPath;
            if (!string.IsNullOrEmpty(Id)) where += (where.Length > 0 ? " " : "") + "[" + Id + "]";
            return (Severity == ValidationSeverity.Error ? "ERROR " : "WARNING ") + Rule + " " + where + ": " + Text;
        }
    }

    /// <summary>Diagnostics produced by <see cref="DataValidator"/>.</summary>
    public sealed class ValidationReport
    {
        private readonly List<ValidationMessage> _messages = new List<ValidationMessage>();

        public IReadOnlyList<ValidationMessage> Messages => _messages;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public bool HasErrors => ErrorCount > 0;

        public void Add(ValidationSeverity severity, string rule, Definition definition, string text)
        {
            string id = definition != null ? definition.StableIdString : null;
            string path = definition != null ? UnityEditor.AssetDatabase.GetAssetPath(definition) : null;
            _messages.Add(new ValidationMessage(severity, rule, id, path, text));
            if (severity == ValidationSeverity.Error) ErrorCount++;
            else WarningCount++;
        }

        public void Error(string rule, Definition definition, string text) => Add(ValidationSeverity.Error, rule, definition, text);
        public void Warning(string rule, Definition definition, string text) => Add(ValidationSeverity.Warning, rule, definition, text);
    }
}
