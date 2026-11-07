using System;
using System.Collections.Generic;
using Eclipse.Data.Loot;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// loot_global.csv: once the table has rows, every key of <see cref="LootGlobalKeys.AllRequired"/> must exist and keys must be unique.
    /// (Catalog constants never fall back to defaults.)
    /// </summary>
    [ValidationRule]
    public sealed class LootGlobalKeysRule : IValidationRule
    {
        public string Name => "LootGlobalKeys";

        public void Validate(ValidationContext context)
        {
            var seen = new Dictionary<string, LootGlobalDefinition>(StringComparer.Ordinal);
            foreach (var d in context.Definitions)
            {
                if (!(d is LootGlobalDefinition g)) continue;
                if (seen.ContainsKey(g.Key ?? string.Empty))
                    context.Report.Error(Name, g, "duplicate key '" + g.Key + "'");
                else
                    seen[g.Key ?? string.Empty] = g;
            }
            if (seen.Count == 0) return;

            foreach (string key in LootGlobalKeys.AllRequired())
                if (!seen.ContainsKey(key))
                    context.Report.Error(Name, null, "loot_global.csv is missing required key '" + key + "'");
        }
    }
}
