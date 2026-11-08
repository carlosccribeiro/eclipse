#if UNITY_EDITOR
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Input;
using NUnit.Framework;

namespace Eclipse.Tests.PlayMode.Combat.Attacks
{
    /// <summary>06 3.7 cancel table, one assertion group per row (rules) plus the live sequencer on the Greatsword.</summary>
    public class CancelTableTests
    {
        private static CancelContext Ctx(ActionKind cur, AttackPhase phase = AttackPhase.None, int phaseFrame = 1) =>
            new CancelContext { Current = cur, Phase = phase, PhaseFrame = phaseFrame, FramesSinceParrySuccess = -1 };

        [Test]
        public void Idle_can_start_anything()
        {
            foreach (ActionKind to in new[] { ActionKind.Basic, ActionKind.Heavy, ActionKind.Dodge, ActionKind.Skill, ActionKind.Ultimate, ActionKind.Parry })
                Assert.IsTrue(CancelRules.Allows(Ctx(ActionKind.None), to), to.ToString());
        }

        [Test]
        public void Basic_startup_to_dodge_needs_light_weight_or_override_and_the_first_4_frames()
        {
            var c = Ctx(ActionKind.Basic, AttackPhase.Startup, 3);
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge), "Heavy-class weapon cannot startup-cancel");
            c.StartupDodgeCancelAllowed = true;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Dodge));
            c.PhaseFrame = CancelRules.StartupDodgeCancelFrames + 1;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge), "after frame 4");
            c.PhaseFrame = 2;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic));
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Skill));
        }

        [Test]
        public void Basic_active_cancels_nothing()
        {
            var c = Ctx(ActionKind.Basic, AttackPhase.Active, 2);
            c.StartupDodgeCancelAllowed = true;
            foreach (ActionKind to in new[] { ActionKind.Dodge, ActionKind.Basic, ActionKind.Heavy, ActionKind.Skill })
                Assert.IsFalse(CancelRules.Allows(c, to), to.ToString());
        }

        [Test]
        public void Basic_recovery_cancels_into_dodge_next_hit_and_skill()
        {
            var c = Ctx(ActionKind.Basic, AttackPhase.Recovery, 1);
            foreach (ActionKind to in new[] { ActionKind.Dodge, ActionKind.Basic, ActionKind.Skill })
                Assert.IsTrue(CancelRules.Allows(c, to), to.ToString());
        }

        [Test]
        public void Heavy_charging_only_cancels_into_dodge()
        {
            var c = Ctx(ActionKind.Heavy, AttackPhase.Startup, 20);
            c.Charging = true;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Dodge));
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic));
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Skill));
        }

        [Test]
        public void Skill_to_dodge_only_in_recovery_and_without_commit()
        {
            var c = Ctx(ActionKind.Skill, AttackPhase.Recovery);
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Dodge));
            c.Commit = true;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge), "Commit skills never cancel");
            c.Commit = false;
            c.Phase = AttackPhase.Active;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge));
            c.Phase = AttackPhase.Recovery;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic));
        }

        [Test]
        public void Ultimate_never_cancels()
        {
            var c = Ctx(ActionKind.Ultimate, AttackPhase.Recovery);
            foreach (ActionKind to in new[] { ActionKind.Dodge, ActionKind.Basic, ActionKind.Skill })
                Assert.IsFalse(CancelRules.Allows(c, to), to.ToString());
        }

        [Test]
        public void Parry_cancels_only_for_12_frames_after_a_success()
        {
            var c = Ctx(ActionKind.Parry);
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge), "no success");
            c.FramesSinceParrySuccess = 0;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Dodge));
            c.FramesSinceParrySuccess = CancelRules.ParrySuccessCancelFrames - 1;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Basic));
            c.FramesSinceParrySuccess = CancelRules.ParrySuccessCancelFrames;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge));
            c.FramesSinceParrySuccess = 0;
            c.ParryWindowOpen = true;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge), "window still open");
        }

        [Test]
        public void Dodge_to_basic_only_in_the_last_8_frames_and_flinch_to_dodge_after_6()
        {
            var c = Ctx(ActionKind.Dodge);
            c.DodgeFramesRemaining = 8;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Basic));
            c.DodgeFramesRemaining = 9;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic));
            c.DodgeFramesRemaining = 0;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic));
            c.DodgeCanCancel = true;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Skill));
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge));

            var f = Ctx(ActionKind.Flinch);
            f.FramesSinceFlinch = CancelRules.FlinchDodgeCancelFrames - 1;
            Assert.IsFalse(CancelRules.Allows(f, ActionKind.Dodge));
            f.FramesSinceFlinch = CancelRules.FlinchDodgeCancelFrames;
            Assert.IsTrue(CancelRules.Allows(f, ActionKind.Dodge));
            Assert.IsFalse(CancelRules.Allows(f, ActionKind.Basic));
        }

        [Test]
        public void Live_greatsword_sequencer_follows_the_table_phase_by_phase()
        {
            var w = CombatEnv.LoadWeapon();
            var seq = new AttackSequencer(AttackController.BuildSet(w, 2f, false));
            var buf = new InputBuffer();
            buf.Push(InputActionId.Basic, 1);
            bool sawStartup = false, sawActive = false, sawRecovery = false;
            for (int f = 1; f < 80; f++)
            {
                seq.Tick(f, buf, false, false, 1f, AttackExternalState.Free);
                switch (seq.State.Phase)
                {
                    case AttackPhase.Startup:
                        sawStartup = true;
                        Assert.IsFalse(seq.CanCancelInto(ActionKind.Dodge), "Heavy-class startup is not cancellable, frame " + f);
                        break;
                    case AttackPhase.Active:
                        sawActive = true;
                        Assert.IsFalse(seq.CanCancelInto(ActionKind.Dodge), "active frame " + f);
                        Assert.IsFalse(seq.CanCancelInto(ActionKind.Basic), "active frame " + f);
                        break;
                    case AttackPhase.Recovery:
                        sawRecovery = true;
                        Assert.IsTrue(seq.CanCancelInto(ActionKind.Dodge), "recovery frame " + f);
                        Assert.IsTrue(seq.CanCancelInto(ActionKind.Basic), "recovery frame " + f);
                        break;
                }
            }
            Assert.IsTrue(sawStartup && sawActive && sawRecovery);
        }

        [Test]
        public void Dodge_pressed_in_recovery_interrupts_the_attack()
        {
            var w = CombatEnv.LoadWeapon();
            var seq = new AttackSequencer(AttackController.BuildSet(w, 2f, false));
            var buf = new InputBuffer();
            buf.Push(InputActionId.Basic, 1);
            for (int f = 1; f < 40; f++) seq.Tick(f, buf, false, false, 1f, AttackExternalState.Free); // frame 39 is in Recovery
            Assert.AreEqual(AttackPhase.Recovery, seq.State.Phase);
            buf.Push(InputActionId.Dodge, 40);
            seq.Tick(40, buf, false, false, 1f, AttackExternalState.Free);
            Assert.IsTrue(seq.DodgeCancelledThisTick);
            Assert.AreEqual(AttackPhase.None, seq.State.Phase);
        }
    }
}
#endif
