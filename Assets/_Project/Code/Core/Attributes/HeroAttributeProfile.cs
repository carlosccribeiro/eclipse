using System;
using Eclipse.Core.Ids;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Attributes
{
    /// <summary>Base value and growth of one stat for a hero (01 3.3).</summary>
    public readonly struct StatScaling
    {
        public StatScaling(StatId stat, float baseValue, float perLevel = 0f)
        {
            Stat = stat;
            Scaling = new LevelScaling(baseValue, perLevel);
        }

        public StatId Stat { get; }
        public LevelScaling Scaling { get; }
    }

    /// <summary>
    /// A bonus granted per primary attribute point: <c>Value = PerPoint * points</c> into
    /// <see cref="Stat"/>/<see cref="Bucket"/>, optionally restricted by tags (01 3.2.1, 3.2.2).
    /// Percent values are fractions (0.0005 = 0.05%).
    /// </summary>
    public readonly struct PerPointEffect
    {
        public PerPointEffect(StatId stat, ModBucket bucket, float perPoint, TagMask requiredTags = default)
        {
            Stat = stat;
            Bucket = bucket;
            PerPoint = perPoint;
            RequiredTags = requiredTags;
        }

        public StatId Stat { get; }
        public ModBucket Bucket { get; }
        public float PerPoint { get; }
        public TagMask RequiredTags { get; }
    }

    /// <summary>
    /// Data-filled description of one hero's attributes (01 3.2.2, 3.2.3, 3.3, 3.8). Nothing here is
    /// hero-specific code: the Core/Secondary/Tertiary roles, their per-level growth, the base stats, the
    /// Secondary hero bonus and the resource model all come from the caller (DataSource).
    /// </summary>
    public sealed class HeroAttributeProfile
    {
        private static readonly StatId[] PrimaryIds = { StatId.Strength, StatId.Dexterity, StatId.Intelligence };

        private readonly StatScaling[] _stats;
        private readonly PerPointEffect[] _secondaryEffects;

        public HeroAttributeProfile(
            StableId id,
            StatId corePrimary, StatId secondaryPrimary, StatId tertiaryPrimary,
            LevelScaling coreScaling, LevelScaling secondaryScaling, LevelScaling tertiaryScaling,
            StatScaling[] stats,
            PerPointEffect[] secondaryEffects,
            ResourceTranslationRule resource)
        {
            if (!IsPrimary(corePrimary) || !IsPrimary(secondaryPrimary) || !IsPrimary(tertiaryPrimary))
                throw new ArgumentException("Core/Secondary/Tertiary must be Strength, Dexterity or Intelligence.");
            if (corePrimary == secondaryPrimary || corePrimary == tertiaryPrimary || secondaryPrimary == tertiaryPrimary)
                throw new ArgumentException("Core, Secondary and Tertiary primaries must be distinct.");
            Id = id;
            CorePrimary = corePrimary;
            SecondaryPrimary = secondaryPrimary;
            TertiaryPrimary = tertiaryPrimary;
            CoreScaling = coreScaling;
            SecondaryScaling = secondaryScaling;
            TertiaryScaling = tertiaryScaling;
            _stats = stats != null ? (StatScaling[])stats.Clone() : Array.Empty<StatScaling>();
            _secondaryEffects = secondaryEffects != null ? (PerPointEffect[])secondaryEffects.Clone() : Array.Empty<PerPointEffect>();
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        }

        public StableId Id { get; }
        public StatId CorePrimary { get; }
        public StatId SecondaryPrimary { get; }
        public StatId TertiaryPrimary { get; }
        public LevelScaling CoreScaling { get; }
        public LevelScaling SecondaryScaling { get; }
        public LevelScaling TertiaryScaling { get; }
        public ResourceTranslationRule Resource { get; }

        /// <summary>Secondary-attribute hero bonuses (01 3.2.2), per Secondary point.</summary>
        public int SecondaryEffectCount => _secondaryEffects.Length;
        public PerPointEffect GetSecondaryEffect(int index) => _secondaryEffects[index];

        /// <summary>Base points of a primary at <paramref name="level"/> (01 3.2.3), before equipment.</summary>
        public float BasePrimaryPoints(StatId primary, int level)
        {
            if (primary == CorePrimary) return CoreScaling.ValueAt(level);
            if (primary == SecondaryPrimary) return SecondaryScaling.ValueAt(level);
            if (primary == TertiaryPrimary) return TertiaryScaling.ValueAt(level);
            throw new ArgumentException("Not a primary attribute: " + primary, nameof(primary));
        }

        /// <summary>Level-resolved value of a base stat (01 3.3); 0 when the profile does not define it.</summary>
        public float BaseStatValue(StatId stat, int level)
        {
            for (int i = 0; i < _stats.Length; i++)
                if (_stats[i].Stat == stat) return _stats[i].Scaling.ValueAt(level);
            return 0f;
        }

        /// <summary>
        /// Writes the Base term of the three primaries and every listed base stat for <paramref name="level"/>.
        /// Equipment and other sources stay as mods on top.
        /// </summary>
        public void ApplyBase(StatBlock block, int level)
        {
            if (block == null) throw new ArgumentNullException(nameof(block));
            for (int i = 0; i < PrimaryIds.Length; i++)
                block.SetBase(PrimaryIds[i], BasePrimaryPoints(PrimaryIds[i], level));
            for (int i = 0; i < _stats.Length; i++)
                block.SetBase(_stats[i].Stat, _stats[i].Scaling.ValueAt(level));
        }

        private static bool IsPrimary(StatId s) =>
            s == StatId.Strength || s == StatId.Dexterity || s == StatId.Intelligence;
    }
}
