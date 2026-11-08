using Eclipse.Gameplay.Combat;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Gameplay.Feedback
{
    /// <summary>Visual hook ids (VFX_*), published as events for M06/M09; never played here.</summary>
    public enum VfxHookId : byte
    {
        VFX_HitImpact = 0,
        VFX_CritImpact = 1,
        VFX_WallImpact = 2,
        VFX_Kill = 3,
        VFX_GlobalHitStop = 4,
    }

    /// <summary>Audio hook ids (SFX_*).</summary>
    public enum SfxHookId : byte
    {
        SFX_Hit = 0,
        SFX_Crit = 1,
        SFX_WallImpact = 2,
        SFX_Kill = 3,
        SFX_GlobalHitStop = 4,
    }

    public readonly struct OnVfxHook
    {
        public readonly int Frame;
        public readonly VfxHookId Hook;
        public readonly EntityId Target;
        public readonly Vector3 Position;
        public OnVfxHook(int frame, VfxHookId hook, EntityId target, Vector3 position)
        { Frame = frame; Hook = hook; Target = target; Position = position; }
    }

    public readonly struct OnSfxHook
    {
        public readonly int Frame;
        public readonly SfxHookId Hook;
        public readonly EntityId Target;
        public readonly Vector3 Position;
        public OnSfxHook(int frame, SfxHookId hook, EntityId target, Vector3 position)
        { Frame = frame; Hook = hook; Target = target; Position = position; }
    }
}
