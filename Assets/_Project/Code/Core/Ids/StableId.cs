using System;

namespace Eclipse.Core.Ids
{
    /// <summary>
    /// Immutable slug identifying a definition: <c>type.scope.name</c> (2 to 4 segments, snake_case).
    /// Equivalent to regex <c>^[a-z][a-z0-9_]*(\.[a-z0-9][a-z0-9_]*){1,3}$</c>, validated without allocation.
    /// </summary>
    public readonly struct StableId : IEquatable<StableId>, IComparable<StableId>
    {
        private readonly string _value;

        /// <summary>Creates an id; throws <see cref="ArgumentException"/> if invalid.</summary>
        public StableId(string value)
        {
            if (!IsValid(value))
                throw new ArgumentException("Invalid StableId: '" + (value ?? "<null>") + "'", nameof(value));
            _value = value;
        }

        /// <summary>The slug, or empty string for <c>default</c>.</summary>
        public string Value => _value ?? string.Empty;

        /// <summary>True for <c>default(StableId)</c>.</summary>
        public bool IsEmpty => _value == null;

        /// <summary>Parses an id; throws <see cref="ArgumentException"/> if invalid.</summary>
        public static StableId Parse(string value) => new StableId(value);

        /// <summary>Parses an id without throwing.</summary>
        public static bool TryParse(string value, out StableId id)
        {
            if (IsValid(value))
            {
                id = new StableId(value);
                return true;
            }
            id = default;
            return false;
        }

        /// <summary>Allocation-free validity check.</summary>
        public static bool IsValid(string s)
        {
            if (s == null || s.Length == 0) return false;
            int segments = 1;
            bool segStart = true;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '.')
                {
                    if (segStart) return false; // empty segment / leading dot / trailing handled below
                    segments++;
                    if (segments > 4) return false;
                    segStart = true;
                    continue;
                }
                bool lower = c >= 'a' && c <= 'z';
                bool digit = c >= '0' && c <= '9';
                if (segStart)
                {
                    // first segment must start with a letter; later ones may start with a digit
                    if (!(lower || (digit && segments > 1))) return false;
                    segStart = false;
                }
                else if (!(lower || digit || c == '_'))
                {
                    return false;
                }
            }
            return !segStart && segments >= 2;
        }

        public bool Equals(StableId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StableId o && Equals(o);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public int CompareTo(StableId other) => string.CompareOrdinal(_value, other._value);
        public override string ToString() => Value;

        public static bool operator ==(StableId a, StableId b) => a.Equals(b);
        public static bool operator !=(StableId a, StableId b) => !a.Equals(b);
    }
}
