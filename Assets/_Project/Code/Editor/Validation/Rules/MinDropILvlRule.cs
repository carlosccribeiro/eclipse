using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.Validation
{
    /// <summary>Minimum drop iLvl of named items: Legendary and set pieces at least 10 (02 section 3.2), Uniques at least 35 (02 section 3.12.6).</summary>
    [ValidationRule]
    public sealed class MinDropILvlRule : IValidationRule
    {
        public const int LegendaryMinDropILvl = 10;
        public const int UniqueMinDropILvl = 35;

        public string Name => "MinDropILvl";

        public void Validate(ValidationContext context)
        {
            foreach (var d in context.Definitions)
            {
                if (!(d is NamedItemDefinition item)) continue;
                int min = item.ItemKind == ItemKind.Unique ? UniqueMinDropILvl : LegendaryMinDropILvl;
                if (item.MinDropILvl < min)
                    context.Report.Error(Name, item, item.ItemKind + " needs min_drop_ilvl >= " + min + ", found " + item.MinDropILvl);
            }
        }
    }
}
