using Eclipse.Data.Combat;

namespace Eclipse.Gameplay.Combat.Attacks
{
    /// <summary>Kind of action in the cancel table of 06 section 3.5 (also the argument of CanCancelInto, used by M08).</summary>
    public enum ActionKind : byte
    {
        /// <summary>Idle (nothing to cancel).</summary>
        None = 0,
        /// <summary>Basic combo hit (a Dash Attack counts as Basic for cancel purposes).</summary>
        Basic = 1,
        Heavy = 2,
        Dodge = 3,
        /// <summary>Hero ability slot 1-4 (M08).</summary>
        Skill = 4,
        Ultimate = 5,
        Parry = 6,
        /// <summary>Player flinch reaction (06 section 3.5 last row).</summary>
        Flinch = 7,
    }

    /// <summary>Phase of an attack (06 section 3.5).</summary>
    public enum AttackPhase : byte
    {
        None = 0,
        Startup = 1,
        Active = 2,
        Recovery = 3,
    }

    /// <summary>Pure mirror of Player.MovementLock (Locked 0, Reduced 60%, Free 100%) so the core stays testable without the motor.</summary>
    public enum MovementLockKind : byte
    {
        Locked = 0,
        Reduced = 1,
        Free = 2,
    }

    /// <summary>Snapshot read by <see cref="CancelRules.Allows"/>. All frame counters are real simulation ticks.</summary>
    public struct CancelContext
    {
        public ActionKind Current;
        public AttackPhase Phase;
        /// <summary>1-based tick inside <see cref="Phase"/> (1 = first frame of the phase).</summary>
        public int PhaseFrame;
        /// <summary>Heavy still being charged (button held, not released).</summary>
        public bool Charging;
        /// <summary>The current Skill carries the Commit flag (08).</summary>
        public bool Commit;
        /// <summary>Light weight, Chakrams or Twin Wands: may dodge-cancel the first frames of Startup.</summary>
        public bool StartupDodgeCancelAllowed;
        public bool ParryWindowOpen;
        /// <summary>Frames since a successful Parry (0 = the success frame); -1 when none.</summary>
        public int FramesSinceParrySuccess;
        /// <summary>Frames left in the current dodge, counting the current one (1 = last frame); 0 when not dodging.</summary>
        public int DodgeFramesRemaining;
        /// <summary>Dodge cancel window per DodgeState.CanCancel (used for Dodge to non-Basic).</summary>
        public bool DodgeCanCancel;
        /// <summary>Frames since the player flinch began (0 = first frame).</summary>
        public int FramesSinceFlinch;
    }

    /// <summary>
    /// Cancel table of 06 section 3.5, one branch per row. The numbers below are structural constants of the table
    /// (not balance data): they live here with the doc reference instead of in assets.
    /// </summary>
    public static class CancelRules
    {
        /// <summary>06 3.5 row 1: Startup dodge-cancel only in the first 4 frames.</summary>
        public const int StartupDodgeCancelFrames = 4;
        /// <summary>06 3.5 row "Esquiva -> Ataque": Basic in the last 8 frames of the dodge becomes Dash Attack.</summary>
        public const int DodgeToDashAttackFrames = 8;
        /// <summary>06 3.5 Parry row: cancellable into anything for 12 frames after a successful Parry.</summary>
        public const int ParrySuccessCancelFrames = 12;
        /// <summary>06 3.5 last row: Flinch -> Dodge after 6 frames.</summary>
        public const int FlinchDodgeCancelFrames = 6;

        /// <summary>True when <paramref name="to"/> may start now, cancelling <see cref="CancelContext.Current"/>.</summary>
        public static bool Allows(in CancelContext c, ActionKind to)
        {
            if (to == ActionKind.None) return false;
            switch (c.Current)
            {
                case ActionKind.None:
                    return true;

                case ActionKind.Basic:
                case ActionKind.Heavy:
                    return AllowsFromAttack(c, to);

                case ActionKind.Skill:
                    // Skill -> Dodge: only in Recovery and without Commit.
                    return to == ActionKind.Dodge && c.Phase == AttackPhase.Recovery && !c.Commit;

                case ActionKind.Ultimate:
                    return false; // Ultimate -> anything: never.

                case ActionKind.Parry:
                    if (c.ParryWindowOpen) return false;
                    return c.FramesSinceParrySuccess >= 0 && c.FramesSinceParrySuccess < ParrySuccessCancelFrames;

                case ActionKind.Dodge:
                    if (to == ActionKind.Basic)
                        return c.DodgeFramesRemaining >= 1 && c.DodgeFramesRemaining <= DodgeToDashAttackFrames;
                    return c.DodgeCanCancel && to != ActionKind.Dodge;

                case ActionKind.Flinch:
                    return to == ActionKind.Dodge && c.FramesSinceFlinch >= FlinchDodgeCancelFrames;

                default:
                    return false;
            }
        }

        private static bool AllowsFromAttack(in CancelContext c, ActionKind to)
        {
            // Heavy (charging) -> Dodge: always; the charge is lost.
            if (c.Charging) return to == ActionKind.Dodge;

            switch (c.Phase)
            {
                case AttackPhase.Startup:
                    return to == ActionKind.Dodge && c.StartupDodgeCancelAllowed && c.PhaseFrame <= StartupDodgeCancelFrames;
                case AttackPhase.Recovery:
                    // Basic -> Dodge: whole Recovery. Basic -> Skill: from Recovery frame 1.
                    // Basic -> next hit: combo window = whole Recovery. Heavy after Basic/Heavy is not in the table:
                    // treated like the combo window (assumption logged in the M05 Log).
                    return true;
                default:
                    return false; // Active: nothing.
            }
        }

        /// <summary>Movement allowance during an attack (06 3.5): melee locked; Ranged/Caster Basic at 60%.</summary>
        public static MovementLockKind MovementFor(WeaponWeightClass weight, AttackKind kind)
        {
            bool rangedLike = weight == WeaponWeightClass.Ranged || weight == WeaponWeightClass.Caster;
            return rangedLike && kind == AttackKind.Basic ? MovementLockKind.Reduced : MovementLockKind.Locked;
        }

        /// <summary>Light weight may dodge-cancel Startup; Chakrams/Twin Wands need the explicit per-weapon flag.</summary>
        public static bool WeightAllowsStartupDodge(WeaponWeightClass weight) => weight == WeaponWeightClass.Light;
    }
}
