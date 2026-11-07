using System;
using System.Collections.Generic;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;

namespace Eclipse.Editor.Validation
{
    /// <summary>Tags must be names of the <see cref="Tag"/> enum; a HitFlag used as a tag is a specific error (R02).</summary>
    [ValidationRule]
    public sealed class TagVocabularyRule : IValidationRule
    {
        private static readonly HashSet<string> HitFlagNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Unavoidable", "Unblockable", "Execute", "Knockdown", "IsEcho", "IsResonance", "IsTriggered",
        };

        public string Name => "TagVocabulary";

        public void Validate(ValidationContext context)
        {
            foreach (Definition d in context.Definitions)
            {
                if (!(d is ITaggedDefinition tagged) || tagged.TagNames == null) continue;
                foreach (string name in tagged.TagNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        context.Report.Error(Name, d, "empty tag name");
                    else if (HitFlagNames.Contains(name))
                        context.Report.Error(Name, d, "'" + name + "' is a HitFlag, not a tag (R02)");
                    else if (!IsKnownTag(name))
                        context.Report.Error(Name, d, "unknown tag '" + name + "' (not in the Tag vocabulary, 00 section 3.4)");
                }
            }
        }

        private static bool IsKnownTag(string name)
        {
            foreach (string n in Enum.GetNames(typeof(Tag)))
                if (string.Equals(n, name, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
