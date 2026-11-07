using System;
using Eclipse.Core.Random;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Damage
{
    /// <summary>
    /// Data-fed parameters of the offensive pipeline. Balance values live in data, not in code.
    /// </summary>
    public readonly struct OffensiveRules
    {
        public OffensiveRules(float resistanceFloorUnderStatus)
        {
            ResistanceFloorUnderStatus = resistanceFloorUnderStatus;
        }

        /// <summary>Res% floor when lowered by statuses (01 3.4.2: -0.20 in the current docs).</summary>
        public float ResistanceFloorUnderStatus { get; }
    }

    /// <summary>
    /// Target-side defense for the player's outgoing damage (01 3.4.2): Armor% and Res% per affinity, status-driven
    /// resistance reductions, and the accumulated Vulnerability. Reusable; fractions (0.30 = 30%).
    /// </summary>
    public sealed class TargetDefense
    {
        private readonly float[] _res = new float[DamageTypes.Count];
        private readonly float[] _resReduction = new float[DamageTypes.Count];

        /// <summary>Armor% of the archetype (09), before Armor Penetration.</summary>
        public float ArmorPercent;

        /// <summary>Sum of Vulnerability% on the target; the resolver applies the +60% cap (R12).</summary>
        public float Vulnerability;

        /// <summary>Base Res% of an affinity (Physical slot is unused: Physical uses Armor%).</summary>
        public void SetResistance(DamageType type, float value) => _res[(int)type] = value;

        /// <summary>Sets Ember/Frost/Storm to <paramref name="elemental"/> and Void/Echo/Ruin to <paramref name="voidRes"/> (06 3.1).</summary>
        public void SetFamilyResistances(float elemental, float voidRes)
        {
            _res[(int)DamageType.Ember] = elemental;
            _res[(int)DamageType.Frost] = elemental;
            _res[(int)DamageType.Storm] = elemental;
            _res[(int)DamageType.Void] = voidRes;
            _res[(int)DamageType.Echo] = voidRes;
            _res[(int)DamageType.Ruin] = voidRes;
        }

        /// <summary>Resistance reduction applied by statuses (Corruption and similar, 07); subject to the floor.</summary>
        public void SetStatusResistanceReduction(DamageType type, float reduction) => _resReduction[(int)type] = reduction;

        public float GetResistance(DamageType type) => _res[(int)type];
        public float GetStatusResistanceReduction(DamageType type) => _resReduction[(int)type];
    }

    /// <summary>
    /// Source of the Increased sum and More product that apply to a damage query (11 3.5.2 steps 3-4).
    /// <see cref="StatBlock"/> does not expose its buckets separately, so damage queries go through this seam;
    /// <see cref="StatModDamageSource"/> is the Core implementation over <see cref="StatMod"/> lists.
    /// </summary>
    public interface IDamageModifierSource
    {
        /// <summary>Sum of Increased values of <paramref name="stat"/> whose tag filter and Condition match <paramref name="context"/>.</summary>
        float SumIncreased(StatId stat, TagMask context);

        /// <summary>Product of (1 + More) of <paramref name="stat"/> with the same filtering.</summary>
        float ProductMore(StatId stat, TagMask context);
    }

    /// <summary>Reads Increased/More straight from an attacker's <see cref="StatBlock"/>.</summary>
    public sealed class StatBlockDamageSource : IDamageModifierSource
    {
        private readonly StatBlock _stats;
        private readonly IConditionEvaluator _conditions;

        public StatBlockDamageSource(StatBlock stats, IConditionEvaluator conditions = null)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _conditions = conditions;
        }

        public float SumIncreased(StatId stat, TagMask context) =>
            _stats.SumBucket(stat, ModBucket.Increased, context, _conditions);

        public float ProductMore(StatId stat, TagMask context) => _stats.ProductMore(stat, context, _conditions);
    }

    /// <summary>Simple <see cref="IDamageModifierSource"/> over a growable <see cref="StatMod"/> array. Queries do not allocate.</summary>
    public sealed class StatModDamageSource : IDamageModifierSource
    {
        private StatMod[] _mods = new StatMod[8];
        private int _count;
        private readonly IConditionEvaluator _conditions;

        public StatModDamageSource(IConditionEvaluator conditions = null)
        {
            _conditions = conditions;
        }

        public int Count => _count;

        public void Add(in StatMod mod)
        {
            if (_count == _mods.Length) Array.Resize(ref _mods, _count * 2);
            _mods[_count++] = mod;
        }

        public void Clear()
        {
            Array.Clear(_mods, 0, _count);
            _count = 0;
        }

        public float SumIncreased(StatId stat, TagMask context)
        {
            float sum = 0f;
            for (int i = 0; i < _count; i++)
                if (_mods[i].Stat == stat && _mods[i].Bucket == ModBucket.Increased && _mods[i].Matches(context, _conditions))
                    sum += _mods[i].Value;
            return sum;
        }

        public float ProductMore(StatId stat, TagMask context)
        {
            float p = 1f;
            for (int i = 0; i < _count; i++)
                if (_mods[i].Stat == stat && _mods[i].Bucket == ModBucket.More && _mods[i].Matches(context, _conditions))
                    p *= 1f + _mods[i].Value;
            return p;
        }
    }

    /// <summary>Reusable input of one offensive resolution (06 3.2 steps 1-2). Fractions are 0..1.</summary>
    public sealed class OffensiveRequest
    {
        private DamageComponent[] _base = new DamageComponent[4];
        private DamageComponent[] _flat = new DamageComponent[4];
        private Transfer[] _extra = new Transfer[4];
        private Transfer[] _convert = new Transfer[4];
        private int _baseCount, _flatCount, _extraCount, _convertCount;

        internal readonly struct Transfer
        {
            public readonly DamageType From, To;
            public readonly float Fraction;
            public Transfer(DamageType from, DamageType to, float fraction) { From = from; To = to; Fraction = fraction; }
        }

        internal int BaseCount => _baseCount;
        internal int FlatCount => _flatCount;
        internal int ExtraCount => _extraCount;
        internal int ConvertCount => _convertCount;
        internal DamageComponent BaseAt(int i) => _base[i];
        internal DamageComponent FlatAt(int i) => _flat[i];
        internal Transfer ExtraAt(int i) => _extra[i];
        internal Transfer ConvertAt(int i) => _convert[i];

        public void Clear() { _baseCount = _flatCount = _extraCount = _convertCount = 0; }

        /// <summary>Step 1: Base (weapon damage x strike coefficient, or WeaponPower x skill coefficient).</summary>
        public void AddBase(DamageType type, float value) => Push(ref _base, ref _baseCount, new DamageComponent(type, value));

        /// <summary>Step 1: flat "adds X damage". Joins the Base of its type (06 3.2 step 1): convertible and part of the gain-as-extra base.</summary>
        public void AddFlat(DamageType type, float value) => Push(ref _flat, ref _flatCount, new DamageComponent(type, value));

        /// <summary>Step 2a: "gain X% of <paramref name="from"/> as <paramref name="to"/>", over the pre-conversion Base.</summary>
        public void AddGainAsExtra(DamageType from, DamageType to, float fraction) =>
            Push(ref _extra, ref _extraCount, new Transfer(from, to, fraction));

        /// <summary>Step 2b: "convert X% of <paramref name="from"/> to <paramref name="to"/>".</summary>
        public void AddConversion(DamageType from, DamageType to, float fraction) =>
            Push(ref _convert, ref _convertCount, new Transfer(from, to, fraction));

        private static void Push<T>(ref T[] arr, ref int count, T item)
        {
            if (count == arr.Length) Array.Resize(ref arr, count * 2);
            arr[count++] = item;
        }
    }

    /// <summary>
    /// Offensive damage pipeline of 06 3.2 / 11 3.5.2, evaluated per component. Not thread-safe (owns scratch
    /// buffers); resolution allocates nothing.
    /// </summary>
    public sealed class OffensiveDamageResolver
    {
        /// <summary>Total Vulnerability cap +60% (00 5 R12; owner 07). Structural contract constant.</summary>
        public const float MaxVulnerability = 0.60f;

        /// <summary>Minimum damage of a hit (06 3.2 step 8). Structural contract constant.</summary>
        public const float MinimumDamagePerHit = 1f;

        private readonly OffensiveRules _rules;
        private readonly float[] _baseAmt = new float[DamageTypes.Count];
        private readonly float[] _keepAmt = new float[DamageTypes.Count];
        private readonly float[] _final = new float[DamageTypes.Count];
        private readonly float[] _moved = new float[DamageTypes.Count];
        private readonly float[] _convSum = new float[DamageTypes.Count];

        public OffensiveDamageResolver(OffensiveRules rules)
        {
            _rules = rules;
        }

        /// <summary>
        /// Resolves <paramref name="hit"/>'s components from <paramref name="request"/>. Uses hit.Tags/Kind for filtering,
        /// sets hit.IsCrit, and returns the total. <paramref name="attacker"/> supplies Critical Chance/Damage and
        /// Armor Penetration (caps from its IStatRules); <paramref name="mods"/> supplies Increased/More.
        /// </summary>
        public float Resolve(OffensiveRequest request, DamageInstance hit, StatBlock attacker, IDamageModifierSource mods,
            TargetDefense target, RngStreams rng)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (hit == null) throw new ArgumentNullException(nameof(hit));
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (mods == null) throw new ArgumentNullException(nameof(mods));
            if (target == null) throw new ArgumentNullException(nameof(target));

            // Step 1: Base (pre-conversion) and flat added damage, per type.
            Array.Clear(_baseAmt, 0, _baseAmt.Length);
            Array.Clear(_final, 0, _final.Length);
            for (int i = 0; i < request.BaseCount; i++)
            {
                DamageComponent c = request.BaseAt(i);
                _baseAmt[(int)c.Type] += c.Value;
            }
            // 06 3.2 step 1: added flat damage is part of the Base of its type (convertible, feeds gain-as-extra).
            for (int i = 0; i < request.FlatCount; i++)
            {
                DamageComponent c = request.FlatAt(i);
                _baseAmt[(int)c.Type] += c.Value;
            }

            // Step 2a: gain-as-extra over the pre-conversion Base; extras are new components, never converted.
            for (int i = 0; i < request.ExtraCount; i++)
            {
                OffensiveRequest.Transfer t = request.ExtraAt(i);
                _final[(int)t.To] += _baseAmt[(int)t.From] * t.Fraction;
            }

            // Step 2b: conversions, per source type; sum > 100% is normalized; no chain (only Base converts).
            Array.Clear(_convSum, 0, _convSum.Length);
            for (int i = 0; i < request.ConvertCount; i++)
            {
                OffensiveRequest.Transfer t = request.ConvertAt(i);
                if (t.From != t.To) _convSum[(int)t.From] += t.Fraction;
            }
            Array.Copy(_baseAmt, _keepAmt, _keepAmt.Length);
            for (int i = 0; i < request.ConvertCount; i++)
            {
                OffensiveRequest.Transfer t = request.ConvertAt(i);
                if (t.From == t.To) continue;
                float sum = _convSum[(int)t.From];
                float f = sum > 1f ? t.Fraction / sum : t.Fraction;
                float moved = _baseAmt[(int)t.From] * f;
                _final[(int)t.To] += moved;
                _keepAmt[(int)t.From] -= moved;
            }
            for (int i = 0; i < _final.Length; i++) _final[i] += _keepAmt[i];

            // Step 5 roll: once per Hit, never for Ticks. Always consumes exactly one value for Hits.
            bool crit = false;
            float critMult = 1f;
            if (hit.Kind == HitKind.Hit)
            {
                float chance = attacker.Evaluate(StatId.CriticalChance, hit.Tags, null);
                crit = rng.Get(RngStream.Combat).Chance(chance);
                if (crit) critMult = 1f + attacker.Evaluate(StatId.CriticalDamage, hit.Tags, null);
            }
            hit.IsCrit = crit;

            float penetration = attacker.Evaluate(StatId.ArmorPenetration, hit.Tags, null);
            float vuln = target.Vulnerability;
            if (vuln > MaxVulnerability) vuln = MaxVulnerability;
            if (vuln < 0f) vuln = 0f;

            hit.ClearComponents();
            TagMask sourceTags = hit.Tags.Except(DamageTypes.AllTypeTags);
            bool isSkill = !hit.Tags.Has(Tag.Basic) && !hit.Tags.Has(Tag.Heavy);
            bool isTick = hit.Kind == HitKind.Tick;

            for (int t = 0; t < DamageTypes.Count; t++)
            {
                float amount = _final[t];
                if (amount <= 0f) continue;
                var type = (DamageType)t;

                // Steps 3-4: filter by final type + source tags (source type tags are dropped).
                TagMask ctx = sourceTags.With(type.ToTag());
                float inc = mods.SumIncreased(type.ToDamageStat(), ctx);
                float more = mods.ProductMore(type.ToDamageStat(), ctx);
                AccumulateScope(mods, StatId.GeneralDamage, true, ctx, ref inc, ref more); // all hits, all types (Core Attunement)
                AccumulateScope(mods, StatId.SkillDamage, isSkill, ctx, ref inc, ref more);
                AccumulateScope(mods, StatId.AreaDamage, hit.Tags.Has(Tag.Area), ctx, ref inc, ref more);
                AccumulateScope(mods, StatId.ProjectileDamage, hit.Tags.Has(Tag.Projectile), ctx, ref inc, ref more);
                AccumulateScope(mods, StatId.DamageOverTime, isTick || hit.Tags.Has(Tag.DoT), ctx, ref inc, ref more);
                AccumulateScope(mods, StatId.SummonDamage, hit.Tags.Has(Tag.Summon) || hit.Tags.Has(Tag.Construct), ctx, ref inc, ref more);

                float value = amount * (1f + inc) * more * critMult;

                // Step 6: target mitigation by family.
                float mitigation = Mitigation(type, penetration, target);
                value *= mitigation >= 1f ? 0f : 1f - mitigation;

                // Step 7: Vulnerability (capped sum).
                value *= 1f + vuln;

                hit.AddComponent(type, value);
            }

            // Step 8: minimum 1 per hit (also for Ticks); the shortfall goes to the largest component.
            float total = hit.TotalDamage;
            if (total < MinimumDamagePerHit)
            {
                if (hit.ComponentCount == 0)
                {
                    hit.AddComponent(DamageType.Physical, MinimumDamagePerHit);
                }
                else
                {
                    int best = 0;
                    for (int i = 1; i < hit.ComponentCount; i++)
                        if (hit.GetComponent(i).Value > hit.GetComponent(best).Value) best = i;
                    hit.SetComponentValue(best, hit.GetComponent(best).Value + (MinimumDamagePerHit - total));
                }
                total = hit.TotalDamage;
            }
            return total;
        }

        private static void AccumulateScope(IDamageModifierSource mods, StatId stat, bool applies, TagMask ctx,
            ref float inc, ref float more)
        {
            if (!applies) return;
            inc += mods.SumIncreased(stat, ctx);
            more *= mods.ProductMore(stat, ctx);
        }

        /// <summary>01 3.4.2: Physical = Armor% x (1 - ArmorPen); Elemental/Void = Res% of the affinity, status floor applied.</summary>
        private float Mitigation(DamageType type, float armorPenetration, TargetDefense target)
        {
            if (type.Family() == MitigationFamily.Physical)
                return target.ArmorPercent * (1f - armorPenetration);

            float res = target.GetResistance(type);
            float reduction = target.GetStatusResistanceReduction(type);
            if (reduction == 0f) return res;
            float floor = _rules.ResistanceFloorUnderStatus;
            if (res <= floor) return res; // already weaker than the floor: statuses do not lower it further
            float lowered = res - reduction;
            return lowered < floor ? floor : lowered;
        }
    }
}
