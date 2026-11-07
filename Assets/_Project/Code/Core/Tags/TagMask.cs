using System;
using System.Text;

namespace Eclipse.Core.Tags
{
    /// <summary>
    /// Bitmask of <see cref="Tag"/> values (bit index = enum value). Only the low 24 bits are ever set.
    /// Operations are allocation-free; the <c>params</c> constructor allocates and is for non-hot paths.
    /// </summary>
    public readonly struct TagMask : IEquatable<TagMask>
    {
        /// <summary>Number of tags in the vocabulary.</summary>
        public const int TagCount = 24;

        private const ulong AllBits = (1UL << TagCount) - 1UL;

        // Order must match the Tag enum (verified by tests).
        private static readonly string[] Names =
        {
            "Attack", "Spell", "Melee", "Ranged", "Projectile", "Area", "DoT", "Summon", "Construct",
            "Movement", "Defensive", "Ultimate", "Channel", "Trap", "Clone", "Basic", "Heavy", "Ember",
            "Frost", "Storm", "Void", "Echo", "Ruin", "Physical",
        };

        /// <summary>Raw bits.</summary>
        public readonly ulong Bits;

        private TagMask(ulong bits) { Bits = bits & AllBits; }

        /// <summary>Mask with a single tag.</summary>
        public TagMask(Tag tag) { Bits = IsValidTag(tag) ? 1UL << (int)tag : 0UL; }

        /// <summary>Mask with the given tags (allocates the array; not for hot paths).</summary>
        public TagMask(params Tag[] tags)
        {
            ulong b = 0;
            if (tags != null)
                for (int i = 0; i < tags.Length; i++)
                    if (IsValidTag(tags[i])) b |= 1UL << (int)tags[i];
            Bits = b;
        }

        public static TagMask None => default;
        public static TagMask All => new TagMask(AllBits);

        public static TagMask Of(Tag tag) => new TagMask(tag);

        /// <summary>Rebuilds a mask from raw bits (bits above the 24th are discarded).</summary>
        public static TagMask FromBits(ulong bits) => new TagMask(bits);

        public bool IsEmpty => Bits == 0;

        /// <summary>Number of tags set.</summary>
        public int Count
        {
            get
            {
                ulong v = Bits;
                int c = 0;
                while (v != 0) { v &= v - 1; c++; }
                return c;
            }
        }

        public bool Has(Tag tag) => IsValidTag(tag) && (Bits & (1UL << (int)tag)) != 0;
        /// <summary>True if every tag of <paramref name="other"/> is set (true for empty <paramref name="other"/>).</summary>
        public bool HasAll(TagMask other) => (Bits & other.Bits) == other.Bits;
        /// <summary>True if at least one tag of <paramref name="other"/> is set (false for empty <paramref name="other"/>).</summary>
        public bool HasAny(TagMask other) => (Bits & other.Bits) != 0;

        public TagMask Union(TagMask other) => new TagMask(Bits | other.Bits);
        public TagMask With(Tag tag) => new TagMask(Bits | new TagMask(tag).Bits);
        public TagMask Except(TagMask other) => new TagMask(Bits & ~other.Bits);
        public TagMask Without(Tag tag) => new TagMask(Bits & ~new TagMask(tag).Bits);

        private static bool IsValidTag(Tag tag) => (int)tag < TagCount;

        // ---- Parsing -------------------------------------------------------------------------

        /// <summary>
        /// Parses exact, case-sensitive tag names separated by '|' or ',' (optional spaces).
        /// Null/empty/whitespace-only gives <see cref="None"/>. Empty tokens ("Attack||Melee", trailing separator),
        /// unknown names and numbers are rejected. Throws <see cref="FormatException"/> on failure.
        /// </summary>
        public static TagMask Parse(string s)
        {
            if (!TryParse(s, out TagMask mask))
                throw new FormatException("Invalid TagMask: '" + (s ?? "<null>") + "'");
            return mask;
        }

        /// <summary>Non-throwing <see cref="Parse"/>; allocation-free.</summary>
        public static bool TryParse(string s, out TagMask mask)
        {
            mask = default;
            if (s == null) return true;
            int len = s.Length;
            int i = 0;
            while (i < len && char.IsWhiteSpace(s[i])) i++;
            if (i == len) return true; // blank -> None

            ulong bits = 0;
            i = 0;
            while (true)
            {
                int start = i;
                while (i < len && s[i] != '|' && s[i] != ',') i++;
                int end = i;
                while (start < end && char.IsWhiteSpace(s[start])) start++;
                while (end > start && char.IsWhiteSpace(s[end - 1])) end--;
                int idx = IndexOfName(s, start, end - start);
                if (idx < 0) return false; // empty token or unknown name
                bits |= 1UL << idx;
                if (i == len) break;
                i++; // skip separator
            }
            mask = new TagMask(bits);
            return true;
        }

        /// <summary>Parses a single exact, case-sensitive tag name (no numbers, no whitespace trimming).</summary>
        public static bool TryParseTag(string s, out Tag tag)
        {
            tag = default;
            if (s == null) return false;
            int idx = IndexOfName(s, 0, s.Length);
            if (idx < 0) return false;
            tag = (Tag)idx;
            return true;
        }

        private static int IndexOfName(string s, int start, int length)
        {
            if (length <= 0) return -1;
            for (int n = 0; n < Names.Length; n++)
            {
                string name = Names[n];
                if (name.Length == length && string.CompareOrdinal(s, start, name, 0, length) == 0) return n;
            }
            return -1;
        }

        // ---- Object plumbing -----------------------------------------------------------------

        /// <summary>Tag names in enum order joined by '|', or "None" when empty.</summary>
        public override string ToString()
        {
            if (Bits == 0) return "None";
            var sb = new StringBuilder();
            for (int n = 0; n < TagCount; n++)
            {
                if ((Bits & (1UL << n)) == 0) continue;
                if (sb.Length > 0) sb.Append('|');
                sb.Append(Names[n]);
            }
            return sb.ToString();
        }

        public bool Equals(TagMask other) => Bits == other.Bits;
        public override bool Equals(object obj) => obj is TagMask o && Equals(o);
        public override int GetHashCode() => Bits.GetHashCode();

        public static TagMask operator |(TagMask a, TagMask b) => new TagMask(a.Bits | b.Bits);
        public static TagMask operator &(TagMask a, TagMask b) => new TagMask(a.Bits & b.Bits);
        public static bool operator ==(TagMask a, TagMask b) => a.Bits == b.Bits;
        public static bool operator !=(TagMask a, TagMask b) => a.Bits != b.Bits;
    }
}
