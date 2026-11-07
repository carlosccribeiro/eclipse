using System;
using System.Collections.Generic;

namespace Eclipse.Data.Loot
{
    /// <summary>Key/value view of the <see cref="LootGlobalDefinition"/>s. A missing key is an error, never a default (no invented numbers).</summary>
    public sealed class LootGlobalTable
    {
        private readonly Dictionary<string, float> _values = new Dictionary<string, float>(StringComparer.Ordinal);

        public LootGlobalTable(IEnumerable<LootGlobalDefinition> definitions)
        {
            if (definitions == null) return;
            foreach (LootGlobalDefinition d in definitions)
                if (d != null && !string.IsNullOrEmpty(d.Key)) _values[d.Key] = d.Value;
        }

        public bool Has(string key) => key != null && _values.ContainsKey(key);

        public bool TryGet(string key, out float value)
        {
            value = 0f;
            return key != null && _values.TryGetValue(key, out value);
        }

        /// <summary>Returns the value or throws <see cref="InvalidOperationException"/> naming the missing key.</summary>
        public float Get(string key)
        {
            if (!TryGet(key, out float v))
                throw new InvalidOperationException("loot_global.csv has no row with key '" + key + "'");
            return v;
        }
    }
}
