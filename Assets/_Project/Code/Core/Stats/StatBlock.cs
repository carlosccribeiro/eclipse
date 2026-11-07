using System;
using Eclipse.Core.Events;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Stats
{
    /// <summary>
    /// Per-entity stat container (11 3.5). Evaluation:
    /// v = (Base + sum Flat) * (1 + sum Increased) * prod(1 + More), then cap/DR from <see cref="IStatRules"/>.
    /// The context-free query (empty tags, no evaluator) is cached per stat with a dirty flag; filtered queries
    /// are recomputed each call. Neither path allocates.
    /// </summary>
    public sealed class StatBlock
    {
        private readonly IStatRules _rules;
        private readonly StatMod[][] _mods = new StatMod[StatIds.Count][];
        private readonly int[] _counts = new int[StatIds.Count];
        private readonly float[] _base = new float[StatIds.Count];
        private readonly float[] _cache = new float[StatIds.Count];
        private readonly bool[] _dirty = new bool[StatIds.Count];
        private int _nextSource = 1;

        public StatBlock(IStatRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            for (int i = 0; i < _dirty.Length; i++) _dirty[i] = true;
        }

        /// <summary>Bus carrying <see cref="StatsChanged"/>.</summary>
        public EventBus<StatsChanged> Changed { get; } = new EventBus<StatsChanged>();

        /// <summary>Issues a fresh handle unique within this block.</summary>
        public SourceHandle CreateSource() => new SourceHandle(_nextSource++);

        public float GetBase(StatId stat) => _base[(int)stat];

        /// <summary>Sets the Base term of a stat (hero base, per-level growth already resolved by the caller).</summary>
        public void SetBase(StatId stat, float value)
        {
            int i = (int)stat;
            if (_base[i] == value) return;
            _base[i] = value;
            _dirty[i] = true;
            Publish(StatMask.Of(stat));
        }

        /// <summary>Adds one mod owned by <paramref name="source"/>.</summary>
        public void AddMod(SourceHandle source, in StatMod mod)
        {
            Append(source, mod);
            Publish(StatMask.Of(mod.Stat));
        }

        /// <summary>Adds all mods owned by <paramref name="source"/>; one StatsChanged for the batch.</summary>
        public void AddSource(SourceHandle source, StatMod[] mods)
        {
            if (mods == null) throw new ArgumentNullException(nameof(mods));
            if (mods.Length == 0) return;
            ulong bits = 0;
            for (int k = 0; k < mods.Length; k++)
            {
                Append(source, mods[k]);
                bits |= 1UL << (int)mods[k].Stat;
            }
            Publish(new StatMask(bits));
        }

        /// <summary>Removes every mod of <paramref name="source"/>; false if it owned none.</summary>
        public bool RemoveSource(SourceHandle source)
        {
            ulong bits = 0;
            for (int s = 0; s < _mods.Length; s++)
            {
                StatMod[] arr = _mods[s];
                int n = _counts[s];
                int w = 0;
                for (int r = 0; r < n; r++)
                {
                    if (arr[r].Source == source) continue;
                    if (w != r) arr[w] = arr[r];
                    w++;
                }
                if (w == n) continue;
                Array.Clear(arr, w, n - w);
                _counts[s] = w;
                _dirty[s] = true;
                bits |= 1UL << s;
            }
            if (bits == 0) return false;
            Publish(new StatMask(bits));
            return true;
        }

        /// <summary>True if the cached context-free value of the stat is stale.</summary>
        public bool IsDirty(StatId stat) => _dirty[(int)stat];

        /// <summary>Context-free value (unfiltered mods only), cached.</summary>
        public float Evaluate(StatId stat) => Evaluate(stat, TagMask.None, null);

        /// <summary>Final value after cap/DR, using only mods that match <paramref name="context"/> and conditions.</summary>
        public float Evaluate(StatId stat, TagMask context, IConditionEvaluator evaluator)
        {
            int i = (int)stat;
            bool cacheable = context.IsEmpty && evaluator == null;
            if (cacheable && !_dirty[i]) return _cache[i];
            float v = DiminishingReturns.Apply(EvaluateRaw(stat, context, evaluator), _rules.Get(stat));
            if (cacheable)
            {
                _cache[i] = v;
                _dirty[i] = false;
            }
            return v;
        }

        /// <summary>Value before cap/DR, for callers that apply <see cref="DiminishingReturns.Apply"/> themselves.</summary>
        public float EvaluateRaw(StatId stat, TagMask context, IConditionEvaluator evaluator)
        {
            int i = (int)stat;
            float flat = 0f, inc = 0f, more = 1f;
            StatMod[] arr = _mods[i];
            int n = _counts[i];
            for (int k = 0; k < n; k++)
            {
                if (!arr[k].Matches(context, evaluator)) continue;
                switch (arr[k].Bucket)
                {
                    case ModBucket.Flat: flat += arr[k].Value; break;
                    case ModBucket.Increased: inc += arr[k].Value; break;
                    default: more *= 1f + arr[k].Value; break;
                }
            }
            return (_base[i] + flat) * (1f + inc) * more;
        }

        /// <summary>
        /// Sum of the Flat or Increased values of <paramref name="stat"/> matching the context (same filter as
        /// <see cref="EvaluateRaw"/>). For <see cref="ModBucket.More"/> use <see cref="ProductMore"/>. No allocation.
        /// </summary>
        public float SumBucket(StatId stat, ModBucket bucket, TagMask context, IConditionEvaluator evaluator)
        {
            int i = (int)stat;
            StatMod[] arr = _mods[i];
            int n = _counts[i];
            float sum = 0f;
            for (int k = 0; k < n; k++)
                if (arr[k].Bucket == bucket && arr[k].Matches(context, evaluator)) sum += arr[k].Value;
            return sum;
        }

        /// <summary>Product of (1 + More) of <paramref name="stat"/> over mods matching the context. No allocation.</summary>
        public float ProductMore(StatId stat, TagMask context, IConditionEvaluator evaluator)
        {
            int i = (int)stat;
            StatMod[] arr = _mods[i];
            int n = _counts[i];
            float p = 1f;
            for (int k = 0; k < n; k++)
                if (arr[k].Bucket == ModBucket.More && arr[k].Matches(context, evaluator)) p *= 1f + arr[k].Value;
            return p;
        }

        private void Append(SourceHandle source, in StatMod mod)
        {
            int s = (int)mod.Stat;
            if ((uint)s >= (uint)_mods.Length) throw new ArgumentOutOfRangeException(nameof(mod));
            StatMod[] arr = _mods[s];
            if (arr == null) { arr = new StatMod[4]; _mods[s] = arr; }
            else if (_counts[s] == arr.Length) { Array.Resize(ref arr, arr.Length * 2); _mods[s] = arr; }
            arr[_counts[s]++] = mod.WithSource(source);
            _dirty[s] = true;
        }

        private void Publish(StatMask mask)
        {
            var e = new StatsChanged(mask);
            Changed.Publish(in e);
        }
    }
}
