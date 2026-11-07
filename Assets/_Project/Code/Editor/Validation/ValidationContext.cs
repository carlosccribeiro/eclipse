using System;
using System.Collections.Generic;
using Eclipse.Data.Definitions;

namespace Eclipse.Editor.Validation
{
    /// <summary>Input of a validation run: definitions, reserved names (name to owner doc prefix) and the report.</summary>
    public sealed class ValidationContext
    {
        private readonly Dictionary<string, Definition> _byId = new Dictionary<string, Definition>(StringComparer.Ordinal);

        public IReadOnlyList<Definition> Definitions { get; }

        /// <summary>Reserved display name (case-insensitive) to owner doc prefix; "-" means no doc may use it.</summary>
        public IReadOnlyDictionary<string, string> ReservedNames { get; }

        public ValidationReport Report { get; }

        public ValidationContext(IReadOnlyList<Definition> definitions, IReadOnlyDictionary<string, string> reservedNames,
            ValidationReport report = null)
        {
            Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            ReservedNames = reservedNames ?? new Dictionary<string, string>();
            Report = report ?? new ValidationReport();
            foreach (Definition d in definitions)
            {
                if (d != null && !string.IsNullOrEmpty(d.StableIdString) && !_byId.ContainsKey(d.StableIdString))
                    _byId[d.StableIdString] = d;
            }
        }

        public bool TryGetById(string id, out Definition definition)
        {
            definition = null;
            return id != null && _byId.TryGetValue(id, out definition);
        }
    }
}
