using System;
using Eclipse.Data.Combat;

namespace Eclipse.Gameplay.Combat.Attacks
{
    /// <summary>What happened during one <see cref="AttackRuntimeState.Tick"/>.</summary>
    [Flags]
    public enum AttackTickEvents : byte
    {
        None = 0,
        /// <summary>Entered Active this tick.</summary>
        ActiveBegan = 1,
        /// <summary>Entered Recovery this tick.</summary>
        RecoveryBegan = 2,
        /// <summary>The attack finished (Recovery ended) this tick; the state is idle again.</summary>
        Ended = 4,
    }

    /// <summary>
    /// Phase machine of one attack (06 3.5): Startup, Active, Recovery. Pure C#, advanced one call per fixed tick.
    /// Attack Speed scales the three phases equally: each phase lasts <c>ceil(baseFrames / attackSpeed)</c> ticks
    /// (minimum 1; a base of 0 skips the phase). Hit-stop is NOT handled here: the controller simply does not tick
    /// a frozen entity (EntityClock), which delays every phase without scaling it.
    /// Heavy with a charge: Startup lasts until the button is released after the scaled minimum, or until the scaled
    /// maximum (06 3.6.2).
    /// Usage: <see cref="Begin"/> then <see cref="Tick"/> in the same tick, so the first call is Startup frame 1.
    /// </summary>
    public sealed class AttackRuntimeState
    {
        private const float ScaleEpsilon = 1e-4f;
        private const int Unbounded = int.MaxValue;

        private int _phaseLength;
        private int _startupBase;

        public AttackStep Step { get; private set; }
        public AttackKind Kind { get; private set; }
        public AttackPhase Phase { get; private set; }
        /// <summary>1-based tick inside the current phase (0 right after <see cref="Begin"/>).</summary>
        public int PhaseFrame { get; private set; }
        /// <summary>Scaled length of the current phase (int.MaxValue while a charge is still open).</summary>
        public int PhaseLength => _phaseLength;
        /// <summary>Effective Attack Speed (after the cap) used by this attack.</summary>
        public float AttackSpeed { get; private set; } = 1f;
        public ChargeTracker Charge { get; } = new ChargeTracker();
        /// <summary>Ticks since <see cref="Begin"/> (including the current one).</summary>
        public int ElapsedTicks { get; private set; }
        /// <summary>Scaled Startup length once known (charging Heavy: set when the charge closes).</summary>
        public int StartupTicks { get; private set; }

        public bool IsActive => Phase != AttackPhase.None;
        public bool IsCharging => Phase == AttackPhase.Startup && Charge.IsCharging;
        public float ChargeFraction => Charge.IsActive ? Charge.Fraction : 0f;
        /// <summary>Rotation is locked in the Active frames except for <c>Tracking</c> hits (06 3.4).</summary>
        public bool RotationLocked => Phase == AttackPhase.Active && !Step.Tracking;

        /// <summary>Effective Attack Speed: lower-bounded to avoid division blow-ups, capped by <paramref name="maxAttackSpeed"/> when > 0.</summary>
        public static float EffectiveAttackSpeed(float attackSpeed, float maxAttackSpeed)
        {
            float s = attackSpeed < 0.01f ? 0.01f : attackSpeed;
            if (maxAttackSpeed > 0f && s > maxAttackSpeed) s = maxAttackSpeed;
            return s;
        }

        /// <summary>Phase length in ticks at the given speed: <c>ceil(base / speed)</c>, min 1; 0 for a base of 0.</summary>
        public static int ScaleFrames(int baseFrames, float speed)
        {
            if (baseFrames <= 0) return 0;
            int n = (int)Math.Ceiling(baseFrames / speed - ScaleEpsilon);
            return n < 1 ? 1 : n;
        }

        public void Begin(in AttackStep step, AttackKind kind, float attackSpeed, float maxAttackSpeed)
        {
            Step = step;
            Kind = kind;
            AttackSpeed = EffectiveAttackSpeed(attackSpeed, maxAttackSpeed);
            PhaseFrame = 0;
            ElapsedTicks = 0;
            Phase = AttackPhase.Startup;
            Charge.Reset();
            if (step.IsChargeable)
            {
                int min = ScaleFrames(step.ChargeMinFrames, AttackSpeed);
                int max = ScaleFrames(step.ChargeMaxFrames, AttackSpeed);
                Charge.Begin(min, max);
                _startupBase = min;
                _phaseLength = Unbounded;
                StartupTicks = 0;
            }
            else
            {
                StartupTicks = ScaleFrames(step.StartupFrames, AttackSpeed);
                _phaseLength = StartupTicks;
                if (_phaseLength == 0) AdvancePhase();
            }
        }

        /// <summary>Drops the attack immediately (cancel, flinch, death). The charge is lost.</summary>
        public void Interrupt()
        {
            Phase = AttackPhase.None;
            PhaseFrame = 0;
            _phaseLength = 0;
            Charge.Reset();
        }

        /// <summary>Advances one tick. <paramref name="heavyHeld"/> is the state of the Heavy button this tick.</summary>
        public AttackTickEvents Tick(bool heavyHeld)
        {
            if (Phase == AttackPhase.None) return AttackTickEvents.None;
            AttackTickEvents ev = AttackTickEvents.None;

            if (PhaseFrame >= _phaseLength) ev |= AdvancePhase();
            if (Phase == AttackPhase.None) return ev;

            PhaseFrame++;
            ElapsedTicks++;

            if (Phase == AttackPhase.Startup && Charge.IsActive)
            {
                Charge.Tick(heavyHeld);
                bool closes = PhaseFrame >= _startupBase && (Charge.Released || PhaseFrame >= Charge.MaxTicks);
                if (closes)
                {
                    _phaseLength = PhaseFrame;
                    StartupTicks = PhaseFrame;
                }
            }
            return ev;
        }

        private AttackTickEvents AdvancePhase()
        {
            AttackTickEvents ev = AttackTickEvents.None;
            while (true)
            {
                PhaseFrame = 0;
                switch (Phase)
                {
                    case AttackPhase.Startup:
                        Phase = AttackPhase.Active;
                        _phaseLength = ScaleFrames(Step.ActiveFrames, AttackSpeed);
                        if (_phaseLength > 0) ev |= AttackTickEvents.ActiveBegan;
                        break;
                    case AttackPhase.Active:
                        Phase = AttackPhase.Recovery;
                        _phaseLength = ScaleFrames(Step.RecoveryFrames, AttackSpeed);
                        if (_phaseLength > 0) ev |= AttackTickEvents.RecoveryBegan;
                        break;
                    default:
                        Phase = AttackPhase.None;
                        _phaseLength = 0;
                        Charge.Reset();
                        return ev | AttackTickEvents.Ended;
                }
                if (_phaseLength > 0) return ev;
            }
        }
    }
}
