using Eclipse.Core.Tags;

namespace Eclipse.Core.Stats
{
    /// <summary>
    /// One modifier (11 3.5.1). Percent stats use fractions (0.15 = 15%), with Base 0 plus Flat.
    /// A mod applies when RequiredTags is a subset of the query context, ExcludedTags does not intersect it,
    /// and Condition is none or met.
    /// </summary>
    public readonly struct StatMod
    {
        public StatMod(StatId stat, ModBucket bucket, float value,
            TagMask requiredTags = default, TagMask excludedTags = default,
            ConditionId condition = default, SourceHandle source = default)
        {
            Stat = stat;
            Bucket = bucket;
            Value = value;
            RequiredTags = requiredTags;
            ExcludedTags = excludedTags;
            Condition = condition;
            Source = source;
        }

        public StatId Stat { get; }
        public ModBucket Bucket { get; }
        public float Value { get; }
        public TagMask RequiredTags { get; }
        public TagMask ExcludedTags { get; }
        public ConditionId Condition { get; }
        public SourceHandle Source { get; }

        /// <summary>Copy of this mod owned by <paramref name="source"/>.</summary>
        public StatMod WithSource(SourceHandle source) =>
            new StatMod(Stat, Bucket, Value, RequiredTags, ExcludedTags, Condition, source);

        /// <summary>True if the mod applies under the given context.</summary>
        public bool Matches(TagMask context, IConditionEvaluator evaluator)
        {
            if (!context.HasAll(RequiredTags)) return false;
            if (context.HasAny(ExcludedTags)) return false;
            if (!Condition.IsNone && (evaluator == null || !evaluator.IsMet(Condition))) return false;
            return true;
        }
    }
}
