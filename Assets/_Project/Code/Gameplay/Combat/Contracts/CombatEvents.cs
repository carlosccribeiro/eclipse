using Eclipse.Core.CrowdControl;
using Eclipse.Core.Damage;
using Eclipse.Core.Ids;
using Eclipse.Core.Tags;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    // Canonical combat hooks of 06 3.11 (names identical, R47) plus the complementary ones of 11 3.6.4.
    // Every event carries the Generation of its originating event (R07/06 3.2). Frame = simulation tick (60 Hz).
    // OnDodge and OnPerfectDodge already exist in Eclipse.Gameplay.Events (M04) and are reused: they are always
    // direct player actions (Generation 0), so they carry no Generation field.
    // Frozen contract (M05 T1): M07/M08 add new event structs in new files, never edit this one.

    /// <summary>Hit landed by the source. <see cref="Value"/> = final damage; <see cref="Type"/> = dominant damage type.</summary>
    public readonly struct OnHitDealt
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly float Value;
        public readonly DamageType Type;
        public readonly bool IsCrit;
        public readonly HitFlags Flags;
        public readonly Vector3 Position;
        public OnHitDealt(int frame, int generation, EntityId source, EntityId target, float value, DamageType type, bool isCrit, HitFlags flags, Vector3 position)
        { Frame = frame; Generation = generation; Source = source; Target = target; Value = value; Type = type; IsCrit = isCrit; Flags = flags; Position = position; }
    }

    /// <summary>Critical hit landed.</summary>
    public readonly struct OnCritDealt
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly float Value;
        public readonly DamageType Type;
        public OnCritDealt(int frame, int generation, EntityId source, EntityId target, float value, DamageType type)
        { Frame = frame; Generation = generation; Source = source; Target = target; Value = value; Type = type; }
    }

    /// <summary>Periodic (DoT/zone) damage tick dealt.</summary>
    public readonly struct OnTickDealt
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly float Value;
        public readonly DamageType Type;
        public OnTickDealt(int frame, int generation, EntityId source, EntityId target, float value, DamageType type)
        { Frame = frame; Generation = generation; Source = source; Target = target; Value = value; Type = type; }
    }

    public readonly struct OnKill
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly float Value;
        public readonly bool TargetIsElite;
        public readonly Vector3 Position;
        public OnKill(int frame, int generation, EntityId source, EntityId target, float value, bool targetIsElite, Vector3 position)
        { Frame = frame; Generation = generation; Source = source; Target = target; Value = value; TargetIsElite = targetIsElite; Position = position; }
    }

    /// <summary>Kill where damage exceeded remaining health. <see cref="Overkill"/> = excess damage.</summary>
    public readonly struct OnOverkill
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly float Overkill;
        public OnOverkill(int frame, int generation, EntityId source, EntityId target, float overkill)
        { Frame = frame; Generation = generation; Source = source; Target = target; Overkill = overkill; }
    }

    public readonly struct OnExecute
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public OnExecute(int frame, int generation, EntityId source, EntityId target)
        { Frame = frame; Generation = generation; Source = source; Target = target; }
    }

    /// <summary>Stagger value added to a target. <see cref="Value"/> = amount added.</summary>
    public readonly struct OnStaggerApplied
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly float Value;
        public OnStaggerApplied(int frame, int generation, EntityId source, EntityId target, float value)
        { Frame = frame; Generation = generation; Source = source; Target = target; Value = value; }
    }

    /// <summary>Target entered Staggered (bar filled).</summary>
    public readonly struct OnStaggered
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public OnStaggered(int frame, int generation, EntityId source, EntityId target)
        { Frame = frame; Generation = generation; Source = source; Target = target; }
    }

    /// <summary>Boss Stagger bar broken (Broken state, 00 R11). <see cref="BreakCount"/> = Breaks so far.</summary>
    public readonly struct OnStaggerBreak
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly int BreakCount;
        public OnStaggerBreak(int frame, int generation, EntityId source, EntityId target, int breakCount)
        { Frame = frame; Generation = generation; Source = source; Target = target; BreakCount = breakCount; }
    }

    /// <summary>Crowd control applied (after DR). <see cref="DurationFrames"/> = effective duration.</summary>
    public readonly struct OnCCApplied
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly CcKind Kind;
        public readonly int DurationFrames;
        public OnCCApplied(int frame, int generation, EntityId source, EntityId target, CcKind kind, int durationFrames)
        { Frame = frame; Generation = generation; Source = source; Target = target; Kind = kind; DurationFrames = durationFrames; }
    }

    /// <summary>Target had an action (attack/cast) interrupted.</summary>
    public readonly struct OnInterrupt
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public OnInterrupt(int frame, int generation, EntityId source, EntityId target)
        { Frame = frame; Generation = generation; Source = source; Target = target; }
    }

    /// <summary>Entity (receiver) parried an incoming hit from <see cref="Source"/>.</summary>
    public readonly struct OnParry
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly bool IsPerfect;
        public OnParry(int frame, int generation, EntityId source, EntityId target, bool isPerfect)
        { Frame = frame; Generation = generation; Source = source; Target = target; IsPerfect = isPerfect; }
    }

    /// <summary>Receiver blocked an incoming hit. <see cref="Value"/> = damage prevented.</summary>
    public readonly struct OnBlock
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly float Value;
        public OnBlock(int frame, int generation, EntityId source, EntityId target, float value)
        { Frame = frame; Generation = generation; Source = source; Target = target; Value = value; }
    }

    /// <summary>Receiver evaded an incoming hit (Dodge Chance).</summary>
    public readonly struct OnEvade
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public OnEvade(int frame, int generation, EntityId source, EntityId target)
        { Frame = frame; Generation = generation; Source = source; Target = target; }
    }

    /// <summary>Receiver took damage. <see cref="Value"/> = health lost; <see cref="Result"/> = pipeline outcome.</summary>
    public readonly struct OnHitTaken
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly float Value;
        public readonly DamageType Type;
        public readonly HitResult Result;
        public readonly HitFlags Flags;
        public OnHitTaken(int frame, int generation, EntityId source, EntityId target, float value, DamageType type, HitResult result, HitFlags flags)
        { Frame = frame; Generation = generation; Source = source; Target = target; Value = value; Type = type; Result = result; Flags = flags; }
    }

    public readonly struct OnShieldBroken
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public OnShieldBroken(int frame, int generation, EntityId source, EntityId target)
        { Frame = frame; Generation = generation; Source = source; Target = target; }
    }

    /// <summary>Health crossed below the low-health threshold (value comes from data). <see cref="HealthFraction"/> 0..1.</summary>
    public readonly struct OnLowHealth
    {
        public readonly int Frame, Generation;
        public readonly EntityId Target;
        public readonly float HealthFraction;
        public OnLowHealth(int frame, int generation, EntityId target, float healthFraction)
        { Frame = frame; Generation = generation; Target = target; HealthFraction = healthFraction; }
    }

    public readonly struct OnFlaskUsed
    {
        public readonly int Frame, Generation;
        public readonly EntityId Target;
        public readonly int ChargesLeft;
        public OnFlaskUsed(int frame, int generation, EntityId target, int chargesLeft)
        { Frame = frame; Generation = generation; Target = target; ChargesLeft = chargesLeft; }
    }

    /// <summary>Entity died. <see cref="Source"/> = killer (None if environmental).</summary>
    public readonly struct OnDeath
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly Vector3 Position;
        public OnDeath(int frame, int generation, EntityId source, EntityId target, Vector3 position)
        { Frame = frame; Generation = generation; Source = source; Target = target; Position = position; }
    }

    /// <summary>Only emitted by the specific Blessing of 09 (no default revive - 10).</summary>
    public readonly struct OnRevive
    {
        public readonly int Frame, Generation;
        public readonly EntityId Target;
        public OnRevive(int frame, int generation, EntityId target)
        { Frame = frame; Generation = generation; Target = target; }
    }

    /// <summary>Skill cast started. <see cref="Skill"/> = skill StableId.</summary>
    public readonly struct OnSkillCast
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source;
        public readonly StableId Skill;
        public OnSkillCast(int frame, int generation, EntityId source, StableId skill)
        { Frame = frame; Generation = generation; Source = source; Skill = skill; }
    }

    /// <summary>A skill hit a target (once per target per use).</summary>
    public readonly struct OnSkillHit
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly StableId Skill;
        public readonly float Value;
        public OnSkillHit(int frame, int generation, EntityId source, EntityId target, StableId skill, float value)
        { Frame = frame; Generation = generation; Source = source; Target = target; Skill = skill; Value = value; }
    }

    /// <summary>Hero resource spent. <see cref="Resource"/> = resource StableId.</summary>
    public readonly struct OnResourceSpent
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source;
        public readonly StableId Resource;
        public readonly float Value;
        public OnResourceSpent(int frame, int generation, EntityId source, StableId resource, float value)
        { Frame = frame; Generation = generation; Source = source; Resource = resource; Value = value; }
    }

    public readonly struct OnResourceGained
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source;
        public readonly StableId Resource;
        public readonly float Value;
        public OnResourceGained(int frame, int generation, EntityId source, StableId resource, float value)
        { Frame = frame; Generation = generation; Source = source; Resource = resource; Value = value; }
    }

    /// <summary>Knocked-back target hit a Wall (Wall Impact, 06 3.8). <see cref="Value"/> = impact damage.</summary>
    public readonly struct OnWallImpact
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly float Value;
        public readonly Vector3 Position;
        public OnWallImpact(int frame, int generation, EntityId source, EntityId target, float value, Vector3 position)
        { Frame = frame; Generation = generation; Source = source; Target = target; Value = value; Position = position; }
    }

    public readonly struct OnStealthEnter
    {
        public readonly int Frame, Generation;
        public readonly EntityId Target;
        public OnStealthEnter(int frame, int generation, EntityId target)
        { Frame = frame; Generation = generation; Target = target; }
    }

    public readonly struct OnStealthBreak
    {
        public readonly int Frame, Generation;
        public readonly EntityId Target;
        public OnStealthBreak(int frame, int generation, EntityId target)
        { Frame = frame; Generation = generation; Target = target; }
    }

    // ---- Complementary hooks of 11 3.6.4 (OnTick(interval) is a scheduler concern, not an event struct) ----

    public readonly struct OnCastEnd
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source;
        public readonly StableId Skill;
        public OnCastEnd(int frame, int generation, EntityId source, StableId skill)
        { Frame = frame; Generation = generation; Source = source; Skill = skill; }
    }

    public readonly struct OnStatusApplied
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly StableId Status;
        public readonly int Stacks;
        public OnStatusApplied(int frame, int generation, EntityId source, EntityId target, StableId status, int stacks)
        { Frame = frame; Generation = generation; Source = source; Target = target; Status = status; Stacks = stacks; }
    }

    public readonly struct OnStatusExpired
    {
        public readonly int Frame, Generation;
        public readonly EntityId Target;
        public readonly StableId Status;
        public OnStatusExpired(int frame, int generation, EntityId target, StableId status)
        { Frame = frame; Generation = generation; Target = target; Status = status; }
    }

    public readonly struct OnResonance
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Target;
        public readonly StableId Resonance;
        public OnResonance(int frame, int generation, EntityId source, EntityId target, StableId resonance)
        { Frame = frame; Generation = generation; Source = source; Target = target; Resonance = resonance; }
    }

    public readonly struct OnResourceFull
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source;
        public readonly StableId Resource;
        public OnResourceFull(int frame, int generation, EntityId source, StableId resource)
        { Frame = frame; Generation = generation; Source = source; Resource = resource; }
    }

    public readonly struct OnResourceEmpty
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source;
        public readonly StableId Resource;
        public OnResourceEmpty(int frame, int generation, EntityId source, StableId resource)
        { Frame = frame; Generation = generation; Source = source; Resource = resource; }
    }

    public readonly struct OnSummon
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Summon;
        public OnSummon(int frame, int generation, EntityId source, EntityId summon)
        { Frame = frame; Generation = generation; Source = source; Summon = summon; }
    }

    public readonly struct OnSummonDeath
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source, Summon;
        public OnSummonDeath(int frame, int generation, EntityId source, EntityId summon)
        { Frame = frame; Generation = generation; Source = source; Summon = summon; }
    }

    public readonly struct OnMovementSkill
    {
        public readonly int Frame, Generation;
        public readonly EntityId Source;
        public readonly StableId Skill;
        public OnMovementSkill(int frame, int generation, EntityId source, StableId skill)
        { Frame = frame; Generation = generation; Source = source; Skill = skill; }
    }

    public readonly struct OnStateEnter
    {
        public readonly int Frame, Generation;
        public readonly EntityId Target;
        public readonly int State;
        public OnStateEnter(int frame, int generation, EntityId target, int state)
        { Frame = frame; Generation = generation; Target = target; State = state; }
    }

    public readonly struct OnStateExit
    {
        public readonly int Frame, Generation;
        public readonly EntityId Target;
        public readonly int State;
        public OnStateExit(int frame, int generation, EntityId target, int state)
        { Frame = frame; Generation = generation; Target = target; State = state; }
    }

    public readonly struct OnRoomStart
    {
        public readonly int Frame, Generation;
        public readonly int RoomIndex;
        public OnRoomStart(int frame, int generation, int roomIndex)
        { Frame = frame; Generation = generation; RoomIndex = roomIndex; }
    }

    public readonly struct OnRoomClear
    {
        public readonly int Frame, Generation;
        public readonly int RoomIndex;
        public OnRoomClear(int frame, int generation, int roomIndex)
        { Frame = frame; Generation = generation; RoomIndex = roomIndex; }
    }
}
