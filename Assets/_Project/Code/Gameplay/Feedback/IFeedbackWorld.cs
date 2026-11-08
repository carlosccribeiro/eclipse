using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Simulation;

namespace Eclipse.Gameplay.Feedback
{
    /// <summary>Visual side of an entity (flash/squash).</summary>
    public interface IImpactVisual
    {
        void Flash(int frames);
        void Squash(float seconds);
    }

    /// <summary>What <see cref="FeedbackSystem"/> needs from the scene; implemented by the sandbox/spawner (T10).</summary>
    public interface IFeedbackWorld
    {
        bool TryGetClock(EntityId id, out EntityClock clock);
        /// <summary>Freezes every entity clock for n ticks.</summary>
        void FreezeAll(int frames);
        bool IsBoss(EntityId id);
        bool TryGetVisual(EntityId id, out IImpactVisual visual);
    }

    /// <summary>Resolves the attack data behind a hit (weight-class profile, step hit-stop, projectile/ultimate flags).</summary>
    public interface IHitFeedbackSource
    {
        /// <summary>False when the hit has no feedback (e.g. enemy attacker without profile).</summary>
        bool TryGet(in OnHitDealt hit, out HitFeedbackInfo info);
    }
}
