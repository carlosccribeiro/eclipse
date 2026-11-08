using System;
using Eclipse.Core.Damage;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Generic damage over time (06 3.2, R19). The per-tick damage is a snapshot taken by the caller at application
    /// (buffs applied later never change it). Ticks are <see cref="HitKind.Tick"/> (no crit, no on-hit) and go through the
    /// target's <see cref="IDamageReceiver"/> (incoming pipeline of the Core). A DoT with the same skill + source +
    /// target does not stack: the strongest per-tick value stays and the duration is renewed. Plain C#, no allocation per tick.
    /// </summary>
    public sealed class DotRunner : IFixedTickable
    {
        private struct Slot
        {
            public bool Active;
            public EntityId Source, Target;
            public int SkillKey;
            public DamageType Type;
            public float Damage;
            public int Interval, Remaining, UntilTick, Generation, HitGroup;
            public float Proc;
            public TagMask Tags;
            public HitFlags Flags;
        }

        private readonly ICombatDirectory _directory;
        private readonly CombatEventStream _events;
        private readonly DamageInstance _hit = new DamageInstance();
        private Slot[] _slots;
        private int _used;
        private int _active;

        public DotRunner(ICombatDirectory directory, CombatEventStream events = null, int capacity = 64)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _events = events;
            _slots = new Slot[Math.Max(capacity, 4)];
        }

        public int ActiveCount => _active;

        /// <summary>
        /// Applies using <see cref="DotRequest.HitGroupId"/> as the skill key (DotRequest carries no skill id: gap of the
        /// frozen contract; the HitGroup is the nearest identity of "the same ability").
        /// </summary>
        public bool Apply(in DotRequest request) => Apply(in request, request.HitGroupId);

        public bool Apply(in DotRequest r, int skillKey)
        {
            if (r.TickIntervalFrames <= 0 || r.DurationFrames <= 0 || !r.Target.IsValid) return false;

            for (int i = 0; i < _used; i++)
            {
                if (!_slots[i].Active || _slots[i].SkillKey != skillKey || _slots[i].Source != r.Source || _slots[i].Target != r.Target)
                    continue;
                if (r.DamagePerTick > _slots[i].Damage)
                {
                    Fill(ref _slots[i], in r, skillKey, keepPhase: true);
                }
                else
                {
                    _slots[i].Remaining = r.DurationFrames; // weaker or equal: keep the stronger value, renew duration
                }
                return true;
            }

            int free = -1;
            for (int i = 0; i < _used; i++)
                if (!_slots[i].Active) { free = i; break; }
            if (free < 0)
            {
                if (_used == _slots.Length) Array.Resize(ref _slots, _used * 2);
                free = _used++;
            }
            Fill(ref _slots[free], in r, skillKey, keepPhase: false);
            _active++;
            return true;
        }

        private static void Fill(ref Slot s, in DotRequest r, int skillKey, bool keepPhase)
        {
            int phase = s.UntilTick;
            s.Active = true;
            s.Source = r.Source; s.Target = r.Target; s.SkillKey = skillKey;
            s.Type = r.Type; s.Damage = r.DamagePerTick;
            s.Interval = r.TickIntervalFrames; s.Remaining = r.DurationFrames;
            s.UntilTick = keepPhase ? Math.Min(phase, r.TickIntervalFrames) : r.TickIntervalFrames;
            s.Generation = r.Generation; s.HitGroup = r.HitGroupId; s.Proc = r.ProcCoefficient;
            s.Tags = r.Tags; s.Flags = r.Flags;
        }

        /// <summary>True while a DoT of (skill, source) is running on the target.</summary>
        public bool IsActive(EntityId target, EntityId source, int skillKey)
        {
            for (int i = 0; i < _used; i++)
                if (_slots[i].Active && _slots[i].Target == target && _slots[i].Source == source && _slots[i].SkillKey == skillKey)
                    return true;
            return false;
        }

        /// <summary>Removes every DoT on the target (cleanse, death).</summary>
        public void ClearTarget(EntityId target)
        {
            for (int i = 0; i < _used; i++)
                if (_slots[i].Active && _slots[i].Target == target) Deactivate(i);
        }

        public void Clear()
        {
            for (int i = 0; i < _used; i++) _slots[i].Active = false;
            _active = 0;
        }

        private void Deactivate(int i)
        {
            if (!_slots[i].Active) return;
            _slots[i].Active = false;
            _active--;
        }

        public void Tick(in SimulationFrame frame)
        {
            int n = _used; // slots added during delivery start next tick
            for (int i = 0; i < n; i++)
            {
                if (!_slots[i].Active) continue;
                _slots[i].Remaining--;
                _slots[i].UntilTick--;
                if (_slots[i].UntilTick <= 0)
                {
                    _slots[i].UntilTick = _slots[i].Interval;
                    Deliver(i, frame.Index);
                }
                if (i < _slots.Length && _slots[i].Active && _slots[i].Remaining <= 0) Deactivate(i);
            }
        }

        private void Deliver(int i, int frame)
        {
            Slot s = _slots[i];
            if (!_directory.TryGet(s.Target, out ICombatEntity entity, out IDamageReceiver receiver) || entity == null || !entity.IsAlive)
            {
                Deactivate(i);
                return;
            }

            _hit.Reset();
            _hit.SourceId = s.Source.Value;
            _hit.TargetId = s.Target.Value;
            _hit.Kind = HitKind.Tick; // never crits, never applies on-hit (06 3.2)
            _hit.IsCrit = false;
            _hit.Tags = s.Tags;
            _hit.Flags = s.Flags;
            _hit.Generation = s.Generation;
            _hit.ProcCoefficient = s.Proc;
            _hit.HitGroupId = s.HitGroup;
            _hit.AddComponent(s.Type, s.Damage);

            DamageOutcome outcome = receiver.ReceiveHit(_hit, Vector3.zero);
            if (outcome.Result == HitResult.Hit)
                _events?.Publish(new OnTickDealt(frame, s.Generation, s.Source, s.Target, outcome.FinalDamage, s.Type));
            if (outcome.Killed) ClearTarget(s.Target);
        }
    }
}
