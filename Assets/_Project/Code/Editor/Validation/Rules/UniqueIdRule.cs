using System;
using System.Collections.Generic;
using Eclipse.Data.Definitions;

namespace Eclipse.Editor.Validation
{
    /// <summary>Every StableId must be used by exactly one definition.</summary>
    [ValidationRule]
    public sealed class UniqueIdRule : IValidationRule
    {
        public string Name => "UniqueId";

        public void Validate(ValidationContext context)
        {
            var seen = new Dictionary<string, Definition>(StringComparer.Ordinal);
            foreach (Definition d in context.Definitions)
            {
                string id = d.StableIdString;
                if (string.IsNullOrEmpty(id)) continue; // reported by IdFormatRule
                if (seen.TryGetValue(id, out Definition first))
                    context.Report.Error(Name, d, "duplicate StableId '" + id + "' (also used by " + first.name + ")");
                else seen[id] = d;
            }
        }
    }
}
