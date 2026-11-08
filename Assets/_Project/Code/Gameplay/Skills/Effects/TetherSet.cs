using System;
using Eclipse.Core.Procs;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Skills
{
    public enum TetherBreakReason : byte
    {
        /// <summary>The tether lasted its full duration.</summary>
        Expired = 0,
        /// <summary>The target got farther from the caster than the break distance.</summary>
        Distance = 1,
    }

    /// <summary>
    /// Active tethers (links caster to target) of one caster. A tether breaks when its duration ends or when the planar distance exceeds
    /// the break distance; breaking runs the effects stored with it (break damage) on the target, with the Generation, coefficient and
    /// multipliers of the cast that created it. A target that dies or disappears drops the tether without breaking. Plain C#,
    /// allocation-free after the first growth.
    /// </summary>
    public sealed class TetherSet
    {
        private struct Entry
        {
            public bool Active;
            public EntityId Target;
            public float BreakDistance;
            public int EndFrame;
            public SkillEffect[] OnBreak;
            public SkillCastInfo Cast;
            public int Generation;
            public HitFlags Flags;
            public SourceChain Chain;
            public int HitGroupId;
            public float DamageMultiplier;
            public float FlatDamage;
        }

        private Entry[] _entries = new Entry[8];
        private int _used;

        public int ActiveCount { get; private set; }
        public int BreakCount { get; private set; }
        public TetherBreakReason LastBreakReason { get; private set; }

        public bool IsTethered(EntityId target)
        {
            for (int i = 0; i < _used; i++)
                if (_entries[i].Active && _entries[i].Target == target) return true;
            return false;
        }

        /// <summary>Links the caster of <paramref name="ctx"/> to <paramref name="target"/>; linking again renews the same tether.</summary>
        public void Add(SkillExecContext ctx, EntityId target, float breakDistance, int durationFrames, SkillEffect[] onBreak)
        {
            if (durationFrames <= 0) return;
            int slot = -1;
            for (int i = 0; i < _used; i++)
            {
                if (_entries[i].Active && _entries[i].Target == target) { slot = i; break; }
                if (!_entries[i].Active && slot < 0) slot = i;
            }
            if (slot < 0)
            {
                if (_used == _entries.Length) Array.Resize(ref _entries, _used * 2);
                slot = _used++;
            }
            if (!_entries[slot].Active) ActiveCount++;
            _entries[slot] = new Entry
            {
                Active = true, Target = target, BreakDistance = breakDistance, EndFrame = ctx.Frame + durationFrames, OnBreak = onBreak,
                Cast = ctx.Cast, Generation = ctx.Generation, Flags = ctx.ExtraFlags, Chain = ctx.Chain, HitGroupId = ctx.HitGroupId,
                DamageMultiplier = ctx.DamageMultiplier, FlatDamage = ctx.FlatDamage,
            };
        }

        public void Clear()
        {
            for (int i = 0; i < _used; i++) _entries[i].Active = false;
            ActiveCount = 0;
        }

        /// <summary>Checks every tether at <c>ctx.Frame</c>; call once per simulation frame, also after the cast ended.</summary>
        public void Tick(SkillExecContext ctx)
        {
            if (ActiveCount == 0) return;
            Vector3 c = ctx.CasterPosition;
            for (int i = 0; i < _used; i++)
            {
                if (!_entries[i].Active) continue;
                SkillTarget t = ctx.Resolve(_entries[i].Target);
                if (t == null || !t.IsAlive)
                {
                    Drop(i);
                    continue;
                }
                Vector3 p = t.Position;
                float dx = p.x - c.x, dz = p.z - c.z;
                bool far = dx * dx + dz * dz > _entries[i].BreakDistance * _entries[i].BreakDistance;
                if (!far && ctx.Frame < _entries[i].EndFrame) continue;
                Break(ctx, i, t, far ? TetherBreakReason.Distance : TetherBreakReason.Expired);
            }
        }

        private void Drop(int i)
        {
            _entries[i].Active = false;
            ActiveCount--;
        }

        private void Break(SkillExecContext ctx, int i, SkillTarget target, TetherBreakReason reason)
        {
            Entry e = _entries[i];
            Drop(i);
            BreakCount++;
            LastBreakReason = reason;

            SkillCastInfo cast = ctx.Cast;
            int gen = ctx.Generation, group = ctx.HitGroupId;
            HitFlags flags = ctx.ExtraFlags;
            SourceChain chain = ctx.Chain;
            float mult = ctx.DamageMultiplier, flat = ctx.FlatDamage;
            int hits = ctx.CurrentHits;

            ctx.Cast = e.Cast;
            ctx.Generation = e.Generation;
            ctx.ExtraFlags = e.Flags;
            ctx.Chain = e.Chain;
            ctx.HitGroupId = e.HitGroupId;
            ctx.DamageMultiplier = e.DamageMultiplier;
            ctx.FlatDamage = e.FlatDamage;
            ctx.CurrentHits = 1;
            try
            {
                ctx.ApplyList(e.OnBreak, target);
            }
            finally
            {
                ctx.Cast = cast;
                ctx.Generation = gen;
                ctx.ExtraFlags = flags;
                ctx.Chain = chain;
                ctx.HitGroupId = group;
                ctx.DamageMultiplier = mult;
                ctx.FlatDamage = flat;
                ctx.CurrentHits = hits;
            }
        }
    }
}
