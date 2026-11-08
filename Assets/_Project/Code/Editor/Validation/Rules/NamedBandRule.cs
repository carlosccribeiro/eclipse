using Eclipse.Core.Items;
using Eclipse.Data.Items;

namespace Eclipse.Editor.Validation
{
    /// <summary>
    /// Named Range bands stay inside the 70%-115% envelope (00 section 5 R24; 02 section 3.9.1), and every band used by a named item
    /// has a row in named_bands.csv.
    /// </summary>
    [ValidationRule]
    public sealed class NamedBandRule : IValidationRule
    {
        /// <summary>Envelope of the Named Range as fractions of the tier max (R24: 70%-115%).</summary>
        public const float EnvelopeMin = 0.70f;
        public const float EnvelopeMax = 1.15f;
        private const float Epsilon = 0.0001f;

        public string Name => "NamedBand";

        public void Validate(ValidationContext context)
        {
            var index = new ItemRuleIndex(context);
            foreach (var kv in index.Bands)
            {
                NamedBandDefinition band = kv.Value;
                if (band.Lo < EnvelopeMin - Epsilon || band.Hi > EnvelopeMax + Epsilon)
                    context.Report.Error(Name, band, band.Band + " band " + band.Lo + "-" + band.Hi + " is outside the envelope "
                        + EnvelopeMin + "-" + EnvelopeMax + " (R24)");
                if (band.Lo > band.Hi)
                    context.Report.Error(Name, band, band.Band + " band has lo greater than hi");
            }

            foreach (NamedItemDefinition item in index.NamedItems)
            {
                foreach (NamedAffixEntry e in ItemRuleIndex.AllEntries(item))
                {
                    if (e.band == NamedBand.None || !index.Bands.ContainsKey(e.band))
                        context.Report.Error(Name, item, "affix '" + e.affixId + "' uses band " + e.band + " which has no row in named_bands");
                }
            }
        }
    }
}
