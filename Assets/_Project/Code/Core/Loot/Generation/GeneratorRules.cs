namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// Structural per-item limits of affix generation. They are rules of the docs, not balance numbers:
    /// 02 section 3.9.3 (rule 1: max 1 primary, Amulet 2, max 2 affinity affixes including Infusions) and 05 section 6.2
    /// (max 1 comfort affix, max 1 single-category resistance in Common/Rare). Overridable for tests.
    /// </summary>
    public sealed class GeneratorRules
    {
        public int MaxPrimary = 1;
        public int MaxPrimaryAmulet = 2;
        public int MaxAffinity = 2;
        public int MaxComfort = 1;
        public int MaxSingleResistanceCommonRare = 1;
    }
}
