using System;
using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// Affixes outside the slot pool (<c>*</c> in the CSV). 00 section 5 R23 and 11 section 3.15.2 allow at most 1 per item and only on
    /// set pieces; 02 section 2.14 also lets named items use it. The documented exceptions (below) are items that break R23 on purpose
    /// and are tracked as Lacunas 1 and 2 of docs/data-schemas/items.md until the user decides.
    /// </summary>
    [ValidationRule]
    public sealed class OffPoolAffixRule : IValidationRule
    {
        public enum ExceptionKind : byte
        {
            /// <summary>The item marks the affix with <c>*</c> although it is not a set piece (R23 x 02 section 2.14).</summary>
            MarkedOffPool,
            /// <summary>An Open Affix option lies outside the pool without <c>*</c> (02 line 562 says no universal does).</summary>
            UnmarkedOpenOption,
        }

        public readonly struct PoolException
        {
            public readonly string CatalogNo;
            public readonly string AffixAbbrev;
            public readonly ExceptionKind Kind;

            public PoolException(string catalogNo, string affixAbbrev, ExceptionKind kind)
            {
                CatalogNo = catalogNo;
                AffixAbbrev = affixAbbrev;
                Kind = kind;
            }
        }

        /// <summary>
        /// Documented exceptions, keyed by catalog number (02 section 3.12) and affix abbreviation (02 section 3.9.1):
        /// K3 <c>HP*</c>, K4 <c>BLK*</c> (named items with <c>*</c>); U1 <c>DUR</c> and U3 <c>HEF</c> (open options outside the pool).
        /// Source of the conflict: 00 section 5 R23 x 02 section 2.14. Edit this list when the user decides.
        /// </summary>
        public static readonly PoolException[] Exceptions =
        {
            new PoolException("K3", "HP", ExceptionKind.MarkedOffPool),
            new PoolException("K4", "BLK", ExceptionKind.MarkedOffPool),
            new PoolException("U1", "DUR", ExceptionKind.UnmarkedOpenOption),
            new PoolException("U3", "HEF", ExceptionKind.UnmarkedOpenOption),
        };

        /// <summary>Maximum affixes outside the pool per item (R23).</summary>
        public const int MaxOffPoolPerItem = 1;

        public string Name => "OffPoolAffix";

        public static bool IsException(string catalogNo, string abbrev, ExceptionKind kind)
        {
            foreach (PoolException e in Exceptions)
                if (e.Kind == kind && string.Equals(e.CatalogNo, catalogNo, StringComparison.Ordinal)
                    && string.Equals(e.AffixAbbrev, abbrev, StringComparison.Ordinal))
                    return true;
            return false;
        }

        public void Validate(ValidationContext context)
        {
            var index = new ItemRuleIndex(context);
            foreach (NamedItemDefinition item in index.NamedItems)
            {
                bool isSet = item.ItemKind == ItemKind.SetPiece;
                foreach (NamedAffixEntry e in ItemRuleIndex.AllEntries(item))
                {
                    if (!e.offPool) continue;
                    if (!isSet && !IsException(item.CatalogNo, index.AbbrevOf(e.affixId), ExceptionKind.MarkedOffPool))
                        context.Report.Error(Name, item, "'" + e.affixId + "*' is outside the pool but the item is not a set piece (R23)");
                }

                int marked = ItemRuleIndex.EffectiveCount(item, e => e.offPool);
                if (marked > MaxOffPoolPerItem)
                    context.Report.Error(Name, item, "item has " + marked + " affixes outside the pool, maximum is " + MaxOffPoolPerItem + " (R23)");
            }
        }
    }
}
