using System;
using Eclipse.Core.Procs;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Damage
{
    /// <summary>Direct instance (can crit, applies on-hit) or periodic tick (no crit, no on-hit) - 06 3.2.</summary>
    public enum HitKind : byte
    {
        Hit = 0,
        Tick = 1,
    }

    /// <summary>
    /// One hit/tick travelling through the damage pipeline (11 3.5.2, 06 11). Reusable and pooled: call
    /// <see cref="Reset"/> instead of allocating. Components hold at most one entry per <see cref="DamageType"/>.
    /// </summary>
    public sealed class DamageInstance
    {
        private readonly DamageComponent[] _components = new DamageComponent[DamageTypes.Count];
        private int _count;

        /// <summary>Attacker entity id (opaque to Core).</summary>
        public int SourceId;
        /// <summary>Target entity id (opaque to Core).</summary>
        public int TargetId;
        /// <summary>Source tags (ability vocabulary, 00 3.4). HitFlags are separate and not tags.</summary>
        public TagMask Tags;
        public HitFlags Flags;
        public HitKind Kind;
        public bool IsCrit;
        /// <summary>Proc Coefficient 0..1 (06 3.2).</summary>
        public float ProcCoefficient = 1f;
        /// <summary>Generation 0..2 (R07).</summary>
        public int Generation;
        public float StaggerValue;
        /// <summary>Groups the hits of one use of a skill (e.g. to avoid double-applying per-use effects).</summary>
        public int HitGroupId;
        public SourceChain SourceChain;

        public bool IsTick => Kind == HitKind.Tick;

        public int ComponentCount => _count;

        public DamageComponent GetComponent(int index)
        {
            if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
            return _components[index];
        }

        /// <summary>Sum of component values.</summary>
        public float TotalDamage
        {
            get
            {
                float t = 0f;
                for (int i = 0; i < _count; i++) t += _components[i].Value;
                return t;
            }
        }

        /// <summary>Value of the component of <paramref name="type"/>, or 0 if absent.</summary>
        public float GetValue(DamageType type)
        {
            for (int i = 0; i < _count; i++)
                if (_components[i].Type == type) return _components[i].Value;
            return 0f;
        }

        public void ClearComponents() => _count = 0;

        /// <summary>Adds to the component of the type (creating it if needed).</summary>
        public void AddComponent(DamageType type, float value)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_components[i].Type != type) continue;
                _components[i] = new DamageComponent(type, _components[i].Value + value);
                return;
            }
            _components[_count++] = new DamageComponent(type, value);
        }

        internal void SetComponentValue(int index, float value) => _components[index] = _components[index].WithValue(value);

        /// <summary>Process-context view of this hit for the Generation rules (T3).</summary>
        public ProcContext ToProcContext() => new ProcContext(Generation, Flags, SourceChain, ProcCoefficient);

        /// <summary>Back to the default state for pooling.</summary>
        public void Reset()
        {
            _count = 0;
            SourceId = 0;
            TargetId = 0;
            Tags = TagMask.None;
            Flags = HitFlags.None;
            Kind = HitKind.Hit;
            IsCrit = false;
            ProcCoefficient = 1f;
            Generation = 0;
            StaggerValue = 0f;
            HitGroupId = 0;
            SourceChain = SourceChain.Empty;
        }
    }
}
