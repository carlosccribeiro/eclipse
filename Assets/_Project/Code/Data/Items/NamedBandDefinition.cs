using Eclipse.Core.Items;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Items
{
    /// <summary>Named Range band as fractions of the tier max (02 section 3.9.1, R24). Source: named_bands.csv.</summary>
    public sealed class NamedBandDefinition : Definition
    {
        [SerializeField] private NamedBand band;
        [SerializeField] private float lo;
        [SerializeField] private float hi;

        public NamedBand Band => band;
        public float Lo => lo;
        public float Hi => hi;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(NamedBand value, float low, float high)
        {
            band = value;
            lo = low;
            hi = high;
        }
#endif
    }
}
