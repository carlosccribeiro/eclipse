using System;
using Eclipse.Core.Items;

namespace Eclipse.Data.Items
{
    /// <summary>Serializable form of <see cref="NamedAffixSpec"/> (affix id, Named band, off-pool flag).</summary>
    [Serializable]
    public struct NamedAffixEntry
    {
        public string affixId;
        public NamedBand band;
        /// <summary>The <c>*</c> flag (02 section 2.14).</summary>
        public bool offPool;

        public NamedAffixEntry(string affix, NamedBand namedBand, bool off)
        {
            affixId = affix;
            band = namedBand;
            offPool = off;
        }
    }
}
