using System;
using Eclipse.Core.Stats;

namespace Eclipse.Core.Attributes
{
    /// <summary>Source handles used by <see cref="PrimaryAttributeRules.Refresh"/>; one per sheet line.</summary>
    public readonly struct PrimarySources
    {
        public PrimarySources(SourceHandle universal, SourceHandle secondary, SourceHandle coreAttunement)
        {
            Universal = universal;
            Secondary = secondary;
            CoreAttunement = coreAttunement;
        }

        public SourceHandle Universal { get; }
        public SourceHandle Secondary { get; }
        /// <summary>Named source shown on the sheet as "Core Attunement" (01 3.2.2).</summary>
        public SourceHandle CoreAttunement { get; }
    }

    /// <summary>
    /// Primary attribute effects as <see cref="StatMod"/>s (01 3.2). Every coefficient is injected:
    /// the universal per-point effects, the Core Attunement coefficient (01: 0.08% = 0.0008 per Core point)
    /// and the stat that receives the More bucket. The Secondary bonus comes from the hero profile.
    /// </summary>
    public sealed class PrimaryAttributeRules
    {
        private readonly PerPointEffect[] _strength;
        private readonly PerPointEffect[] _dexterity;
        private readonly PerPointEffect[] _intelligence;

        public PrimaryAttributeRules(
            PerPointEffect[] strengthEffects, PerPointEffect[] dexterityEffects, PerPointEffect[] intelligenceEffects,
            float coreAttunementPerPoint, StatId coreAttunementStat)
        {
            _strength = Copy(strengthEffects);
            _dexterity = Copy(dexterityEffects);
            _intelligence = Copy(intelligenceEffects);
            CoreAttunementPerPoint = coreAttunementPerPoint;
            CoreAttunementStat = coreAttunementStat;
        }

        /// <summary>More fraction per Core point.</summary>
        public float CoreAttunementPerPoint { get; }
        /// <summary>Stat receiving the Core Attunement More mod (damage stat chosen by data).</summary>
        public StatId CoreAttunementStat { get; }

        /// <summary>Universal per-point effects of the three primaries (01 3.2.1).</summary>
        public StatMod[] BuildUniversalMods(float strength, float dexterity, float intelligence)
        {
            int n = _strength.Length + _dexterity.Length + _intelligence.Length;
            var mods = new StatMod[n];
            int k = 0;
            k = Fill(mods, k, _strength, strength);
            k = Fill(mods, k, _dexterity, dexterity);
            Fill(mods, k, _intelligence, intelligence);
            return mods;
        }

        /// <summary>Hero Secondary bonus for the given Secondary points (01 3.2.2).</summary>
        public StatMod[] BuildSecondaryMods(HeroAttributeProfile profile, float secondaryPoints)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var mods = new StatMod[profile.SecondaryEffectCount];
            for (int i = 0; i < mods.Length; i++) mods[i] = ToMod(profile.GetSecondaryEffect(i), secondaryPoints);
            return mods;
        }

        /// <summary>Core Attunement: a More mod of <c>corePoints * CoreAttunementPerPoint</c> (01 3.2.2).</summary>
        public StatMod BuildCoreAttunement(float corePoints) =>
            new StatMod(CoreAttunementStat, ModBucket.More, corePoints * CoreAttunementPerPoint);

        /// <summary>
        /// Replaces the three attribute sources in <paramref name="block"/> using its current primary totals
        /// (Base + equipment). Call again whenever a primary changes.
        /// </summary>
        public void Refresh(StatBlock block, HeroAttributeProfile profile, in PrimarySources sources)
        {
            if (block == null) throw new ArgumentNullException(nameof(block));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            float str = block.Evaluate(StatId.Strength);
            float dex = block.Evaluate(StatId.Dexterity);
            float intl = block.Evaluate(StatId.Intelligence);

            block.RemoveSource(sources.Universal);
            block.RemoveSource(sources.Secondary);
            block.RemoveSource(sources.CoreAttunement);

            block.AddSource(sources.Universal, BuildUniversalMods(str, dex, intl));
            block.AddSource(sources.Secondary, BuildSecondaryMods(profile, Points(profile.SecondaryPrimary, str, dex, intl)));
            block.AddMod(sources.CoreAttunement, BuildCoreAttunement(Points(profile.CorePrimary, str, dex, intl)));
        }

        private static float Points(StatId primary, float str, float dex, float intl) =>
            primary == StatId.Strength ? str : primary == StatId.Dexterity ? dex : intl;

        private static int Fill(StatMod[] dest, int k, PerPointEffect[] effects, float points)
        {
            for (int i = 0; i < effects.Length; i++) dest[k++] = ToMod(effects[i], points);
            return k;
        }

        private static StatMod ToMod(in PerPointEffect e, float points) =>
            new StatMod(e.Stat, e.Bucket, e.PerPoint * points, e.RequiredTags);

        private static PerPointEffect[] Copy(PerPointEffect[] src) =>
            src != null ? (PerPointEffect[])src.Clone() : Array.Empty<PerPointEffect>();
    }
}
