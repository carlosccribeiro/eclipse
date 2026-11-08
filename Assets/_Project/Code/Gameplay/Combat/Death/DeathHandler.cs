using UnityEngine;
using InvState = Eclipse.Gameplay.Player.InvulnerabilityState;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>Death and revive state of an entity (06 3.14). Pure C#; presentation reads <see cref="TimeScale"/>.</summary>
    public sealed class DeathHandler
    {
        /// <summary>Source id used for the revive window in <see cref="InvState"/>.</summary>
        public const int ReviveWindowSource = 9001;

        private readonly CombatEntityConfig _cfg;
        private readonly CombatEventStream _events;
        private int _revivesThisBossRoom;

        public DeathHandler(CombatEntityConfig cfg, CombatEventStream events, DeathRecap recap)
        {
            _cfg = cfg; _events = events; Recap = recap;
        }

        public DeathRecap Recap { get; }
        public bool IsDead { get; private set; }
        public int DeathFrame { get; private set; }

        /// <summary>Presentation time scale: slow motion only for the configured window after death; logic pauses regardless.</summary>
        public float TimeScale(int frame) =>
            IsDead && frame - DeathFrame < _cfg.DeathSlowMotionFrames ? _cfg.DeathSlowMotionScale : 1f;

        public void Die(EntityId self, EntityId killer, Vector3 position, int frame, int generation)
        {
            if (IsDead) return;
            IsDead = true;
            DeathFrame = frame;
            _events?.Publish(new OnDeath(frame, generation, killer, self, position));
        }

        /// <summary>
        /// Revive API (06 3.14). No built-in source: only the Blessing of 09 (R46) may call it. Fails if alive or if the
        /// per-boss-room cap is reached. Resource restore and the 6 m knockback wave are done by listeners of OnRevive.
        /// </summary>
        public bool TryRevive(HealthComponent health, InvState invulnerability, EntityId self, int frame)
        {
            if (!IsDead || _revivesThisBossRoom >= _cfg.MaxRevivesPerBossRoom) return false;
            _revivesThisBossRoom++;
            IsDead = false;
            health.Restore(_cfg.ReviveHealthFraction);
            if (_cfg.ReviveInvulnerabilityFrames > 0)
                invulnerability?.AddWindow(frame, frame + _cfg.ReviveInvulnerabilityFrames - 1, 0, ReviveWindowSource);
            _events?.Publish(new OnRevive(frame, 0, self));
            return true;
        }

        /// <summary>Call on entering a boss room (resets the revive cap).</summary>
        public void ResetBossRoom() => _revivesThisBossRoom = 0;
    }
}
