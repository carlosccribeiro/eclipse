using Eclipse.Core.Stats;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Simulation;

namespace Eclipse.Gameplay.Player
{
    /// <summary>Tuning of the Aether Flask, from data (06 3.12: 3 charges, 35% of max health, 1 s).</summary>
    public sealed class AetherFlaskConfig
    {
        public int MaxCharges;
        /// <summary>Fraction of max health healed per use (06 3.12: 0.35).</summary>
        public float HealFraction;
        /// <summary>Heal duration in frames (06 3.12: 1 s = 60).</summary>
        public int HealFrames;
    }

    /// <summary>
    /// Aether Flask (06 3.12). Heals over time (damage taken does not cancel it), cannot be used during Heavy/Ultimate,
    /// kill of an Elite refunds 1 charge (max once per room). Healing Effectiveness amplifies the heal. Pure C#; drive it
    /// with <see cref="Tick"/> once per simulation tick.
    /// </summary>
    public sealed class AetherFlask
    {
        private readonly AetherFlaskConfig _cfg;
        private readonly CombatEntity _owner;
        private int _healFramesLeft;
        private float _healPerFrame;
        private int _lastRefundRoom = int.MinValue;

        public AetherFlask(AetherFlaskConfig cfg, CombatEntity owner)
        {
            _cfg = cfg;
            _owner = owner;
            Charges = cfg.MaxCharges;
        }

        public int Charges { get; private set; }
        public int MaxCharges => _cfg.MaxCharges;
        public bool IsHealing => _healFramesLeft > 0;
        /// <summary>Set by the attack/ability system while a Heavy attack or Ultimate is active (blocks use).</summary>
        public bool HeavyOrUltimateActive { get; set; }

        public bool CanUse => Charges > 0 && !IsHealing && !HeavyOrUltimateActive && _owner.IsAlive;

        /// <summary>Consumes a charge and starts the heal-over-time. The heal amount is fixed at use time.</summary>
        public bool TryUse(int frame)
        {
            if (!CanUse) return false;
            Charges--;
            float total = _owner.Health.Max * _cfg.HealFraction * (1f + _owner.Stats.Evaluate(StatId.HealingEffectiveness));
            int frames = _cfg.HealFrames < 1 ? 1 : _cfg.HealFrames;
            _healFramesLeft = frames;
            _healPerFrame = total / frames;
            _owner.Events?.Publish(new OnFlaskUsed(frame, 0, _owner.Id, Charges));
            return true;
        }

        public void Tick()
        {
            if (_healFramesLeft <= 0) return;
            _healFramesLeft--;
            if (!_owner.IsAlive) { _healFramesLeft = 0; return; }
            _owner.Health.Heal(_healPerFrame);
        }

        /// <summary>Feed every OnKill of the owner. Refunds 1 charge on an Elite kill, at most once per room.</summary>
        public bool HandleKill(in OnKill kill, int roomIndex)
        {
            if (kill.Source != _owner.Id || !kill.TargetIsElite) return false;
            if (roomIndex == _lastRefundRoom || Charges >= _cfg.MaxCharges) return false;
            _lastRefundRoom = roomIndex;
            Charges++;
            return true;
        }

        /// <summary>Rest Room / Antechamber service (06 3.12, 10 R34).</summary>
        public void RestoreAll() => Charges = _cfg.MaxCharges;
    }
}
