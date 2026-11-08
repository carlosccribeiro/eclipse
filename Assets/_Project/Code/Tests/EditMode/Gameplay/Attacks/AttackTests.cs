using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.EditMode.Gameplay.Attacks
{
    /// <summary>Fixtures: Greatsword frame data from 06 3.6.2 (values live here, never in runtime code).</summary>
    internal static class AttackFixtures
    {
        public static AttackStep Step(int s, int a, int r, int chargeMin = 0, int chargeMax = 0, float lungeMin = 0f,
            float lungeMax = 0f, float lungeCone = 0f, bool tracking = false) =>
            new AttackStep(s, a, r, AttackHitShape.Sector, 2.8f, 120f, 0f, 0f, Vector3.zero, 15f, chargeMax > 0 ? 100f : 15f, 5, 1f, 1.1f,
                1.1f, lungeMin, lungeMax, lungeCone, 0, 0, tracking, false, chargeMin, chargeMax);

        public static AttackSetData Greatsword(WeaponWeightClass weight = WeaponWeightClass.Heavy) => new AttackSetData
        {
            Basic = new[] { Step(20, 6, 40), Step(20, 6, 40), Step(26, 8, 34) },
            HasHeavy = true,
            Heavy = Step(36, 6, 26, 36, 72),
            HasDash = true,
            Dash = Step(10, 6, 18),
            Weight = weight,
            ComboResetFrames = 48,
        };

        /// <summary>Runs an attack to the end, returning ticks spent in Startup/Active/Recovery.</summary>
        public static void CountPhases(AttackStep step, AttackKind kind, float speed, float cap, bool heavyHeld,
            out int startup, out int active, out int recovery, int heldTicks = int.MaxValue)
        {
            var st = new AttackRuntimeState();
            st.Begin(step, kind, speed, cap);
            startup = active = recovery = 0;
            int tick = 0;
            while (true)
            {
                tick++;
                st.Tick(heavyHeld && tick <= heldTicks);
                switch (st.Phase)
                {
                    case AttackPhase.Startup: startup++; break;
                    case AttackPhase.Active: active++; break;
                    case AttackPhase.Recovery: recovery++; break;
                    default: return;
                }
                if (tick > 1000) Assert.Fail("attack did not end");
            }
        }
    }

    public class PhaseFrameTests
    {
        private static void Expect(AttackStep step, float speed, int s, int a, int r)
        {
            AttackFixtures.CountPhases(step, AttackKind.Basic, speed, 0f, false, out int ps, out int pa, out int pr);
            Assert.AreEqual(new[] { s, a, r }, new[] { ps, pa, pr });
        }

        [Test]
        public void Greatsword_AttackSpeed1_MatchesTheDocFrameData()
        {
            Expect(AttackFixtures.Step(20, 6, 40), 1f, 20, 6, 40);   // hit 1 and 2
            Expect(AttackFixtures.Step(26, 8, 34), 1f, 26, 8, 34);   // hit 3
        }

        [Test]
        public void Greatsword_AttackSpeed1_5_ScalesAllThreePhases()
        {
            Expect(AttackFixtures.Step(20, 6, 40), 1.5f, 14, 4, 27);
            Expect(AttackFixtures.Step(26, 8, 34), 1.5f, 18, 6, 23);
        }

        [Test]
        public void AttackSpeedCap_ComesFromTheArgument()
        {
            // 3.0 requested with a cap of 2.0 behaves as 2.0: 20/2=10, 6/2=3, 40/2=20.
            AttackFixtures.CountPhases(AttackFixtures.Step(20, 6, 40), AttackKind.Basic, 3f, 2f, false, out int s, out int a, out int r);
            Assert.AreEqual(new[] { 10, 3, 20 }, new[] { s, a, r });
            // Without a cap (0) the value is used as is.
            AttackFixtures.CountPhases(AttackFixtures.Step(20, 6, 40), AttackKind.Basic, 2f, 0f, false, out s, out a, out r);
            Assert.AreEqual(new[] { 10, 3, 20 }, new[] { s, a, r });
        }

        [Test]
        public void Heavy_Tap_Is36StartupFrames()
        {
            AttackFixtures.CountPhases(AttackFixtures.Step(36, 6, 26, 36, 72), AttackKind.Heavy, 1f, 0f, false, out int s, out int a, out int r);
            Assert.AreEqual(new[] { 36, 6, 26 }, new[] { s, a, r });
        }

        [Test]
        public void Heavy_HeldFully_ChargesUpTo72()
        {
            var st = new AttackRuntimeState();
            st.Begin(AttackFixtures.Step(36, 6, 26, 36, 72), AttackKind.Heavy, 1f, 0f);
            int startup = 0;
            while (true)
            {
                st.Tick(true);
                if (st.Phase != AttackPhase.Startup) break;
                startup++;
            }
            Assert.AreEqual(72, startup);
            Assert.AreEqual(1f, st.ChargeFraction, 1e-5f);
            Assert.AreEqual(AttackPhase.Active, st.Phase);
            Assert.AreEqual(1, st.PhaseFrame);
        }

        [Test]
        public void Heavy_HalfCharge_InterpolatesFraction()
        {
            var st = new AttackRuntimeState();
            st.Begin(AttackFixtures.Step(36, 6, 26, 36, 72), AttackKind.Heavy, 1f, 0f);
            for (int i = 0; i < 54; i++) st.Tick(true);
            Assert.IsTrue(st.IsCharging);
            while (st.Phase == AttackPhase.Startup) st.Tick(false);
            Assert.AreEqual(0.5f, st.ChargeFraction, 1e-5f);
        }

        [Test]
        public void Heavy_AttackSpeed1_5_ScalesTapAndMax()
        {
            var step = AttackFixtures.Step(36, 6, 26, 36, 72);
            AttackFixtures.CountPhases(step, AttackKind.Heavy, 1.5f, 0f, false, out int s, out int a, out int r);
            Assert.AreEqual(new[] { 24, 4, 18 }, new[] { s, a, r });
            AttackFixtures.CountPhases(step, AttackKind.Heavy, 1.5f, 0f, true, out s, out a, out r);
            Assert.AreEqual(48, s);
        }

        [Test]
        public void HitStop_FreezesWithoutScaling()
        {
            // The controller only ticks when EntityClock.Advance() is true: a 5-frame freeze delays by exactly 5 ticks.
            var clock = new EntityClock();
            var st = new AttackRuntimeState();
            st.Begin(AttackFixtures.Step(20, 6, 40), AttackKind.Basic, 1f, 0f);
            int global = 0, ran = 0;
            while (true)
            {
                global++;
                if (global == 10) clock.Freeze(5);
                if (!clock.Advance()) continue;
                ran++;
                st.Tick(false);
                if (!st.IsActive) break;
            }
            Assert.AreEqual(66 + 1, ran); // 66 attack ticks + the Ended tick
            Assert.AreEqual(ran + 5, global);
        }

        [Test]
        public void RotationLock_OnlyInActiveExceptTracking()
        {
            var st = new AttackRuntimeState();
            st.Begin(AttackFixtures.Step(2, 2, 2), AttackKind.Basic, 1f, 0f);
            st.Tick(false);
            Assert.IsFalse(st.RotationLocked, "startup free");
            st.Tick(false); st.Tick(false);
            Assert.AreEqual(AttackPhase.Active, st.Phase);
            Assert.IsTrue(st.RotationLocked, "active locked");
            st.Tick(false); st.Tick(false);
            Assert.AreEqual(AttackPhase.Recovery, st.Phase);
            Assert.IsFalse(st.RotationLocked, "recovery free");

            st.Begin(AttackFixtures.Step(1, 2, 2, tracking: true), AttackKind.Basic, 1f, 0f);
            st.Tick(false); st.Tick(false);
            Assert.AreEqual(AttackPhase.Active, st.Phase);
            Assert.IsFalse(st.RotationLocked, "tracking hits keep rotating");
        }

        [Test]
        public void MovementLock_MeleeLocked_RangedAndCasterBasicAt60()
        {
            Assert.AreEqual(MovementLockKind.Locked, CancelRules.MovementFor(WeaponWeightClass.Heavy, AttackKind.Basic));
            Assert.AreEqual(MovementLockKind.Locked, CancelRules.MovementFor(WeaponWeightClass.Light, AttackKind.Heavy));
            Assert.AreEqual(MovementLockKind.Reduced, CancelRules.MovementFor(WeaponWeightClass.Ranged, AttackKind.Basic));
            Assert.AreEqual(MovementLockKind.Reduced, CancelRules.MovementFor(WeaponWeightClass.Caster, AttackKind.Basic));
            Assert.AreEqual(MovementLockKind.Locked, CancelRules.MovementFor(WeaponWeightClass.Caster, AttackKind.Heavy));
        }
    }

    public class CancelRulesTests
    {
        private static CancelContext Attack(AttackPhase phase, int frame, ActionKind kind = ActionKind.Basic, bool light = false,
            bool charging = false) => new CancelContext
            {
                Current = kind, Phase = phase, PhaseFrame = frame, StartupDodgeCancelAllowed = light, Charging = charging,
                FramesSinceParrySuccess = -1,
            };

        [Test]
        public void BasicToDodge_Startup_OnlyFirst4Frames_AndOnlyLightChakramsTwinWands()
        {
            Assert.IsTrue(CancelRules.Allows(Attack(AttackPhase.Startup, 1, light: true), ActionKind.Dodge));
            Assert.IsTrue(CancelRules.Allows(Attack(AttackPhase.Startup, 4, light: true), ActionKind.Dodge));
            Assert.IsFalse(CancelRules.Allows(Attack(AttackPhase.Startup, 5, light: true), ActionKind.Dodge));
            Assert.IsFalse(CancelRules.Allows(Attack(AttackPhase.Startup, 1, light: false), ActionKind.Dodge), "heavy/medium weight");
            Assert.IsTrue(CancelRules.WeightAllowsStartupDodge(WeaponWeightClass.Light));
            Assert.IsFalse(CancelRules.WeightAllowsStartupDodge(WeaponWeightClass.Heavy));
            Assert.IsFalse(CancelRules.WeightAllowsStartupDodge(WeaponWeightClass.Medium));
        }

        [Test]
        public void BasicToDodge_WholeRecovery_NeverInActive()
        {
            Assert.IsTrue(CancelRules.Allows(Attack(AttackPhase.Recovery, 1), ActionKind.Dodge));
            Assert.IsTrue(CancelRules.Allows(Attack(AttackPhase.Recovery, 40), ActionKind.Dodge));
            Assert.IsFalse(CancelRules.Allows(Attack(AttackPhase.Active, 3, light: true), ActionKind.Dodge));
        }

        [Test]
        public void BasicToSkill_FromFirstRecoveryFrame()
        {
            Assert.IsFalse(CancelRules.Allows(Attack(AttackPhase.Startup, 2, light: true), ActionKind.Skill));
            Assert.IsFalse(CancelRules.Allows(Attack(AttackPhase.Active, 1), ActionKind.Skill));
            Assert.IsTrue(CancelRules.Allows(Attack(AttackPhase.Recovery, 1), ActionKind.Skill));
            Assert.IsTrue(CancelRules.Allows(Attack(AttackPhase.Recovery, 1), ActionKind.Ultimate));
        }

        [Test]
        public void BasicToNextHit_ComboWindowIsWholeRecovery()
        {
            Assert.IsFalse(CancelRules.Allows(Attack(AttackPhase.Startup, 1), ActionKind.Basic));
            Assert.IsFalse(CancelRules.Allows(Attack(AttackPhase.Active, 1), ActionKind.Basic));
            Assert.IsTrue(CancelRules.Allows(Attack(AttackPhase.Recovery, 1), ActionKind.Basic));
            Assert.IsTrue(CancelRules.Allows(Attack(AttackPhase.Recovery, 40), ActionKind.Basic));
        }

        [Test]
        public void HeavyCharging_ToDodge_Always_ButNothingElse()
        {
            var c = Attack(AttackPhase.Startup, 30, ActionKind.Heavy, charging: true);
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Dodge));
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Skill));
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic));
        }

        [Test]
        public void SkillToDodge_OnlyRecovery_AndNotWithCommit()
        {
            var c = new CancelContext { Current = ActionKind.Skill, Phase = AttackPhase.Startup, PhaseFrame = 1 };
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge));
            c.Phase = AttackPhase.Active;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge));
            c.Phase = AttackPhase.Recovery;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Dodge));
            c.Commit = true;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge));
        }

        [Test]
        public void Ultimate_NeverCancels()
        {
            foreach (AttackPhase p in new[] { AttackPhase.Startup, AttackPhase.Active, AttackPhase.Recovery })
                foreach (ActionKind to in new[] { ActionKind.Dodge, ActionKind.Basic, ActionKind.Skill, ActionKind.Heavy })
                    Assert.IsFalse(CancelRules.Allows(new CancelContext { Current = ActionKind.Ultimate, Phase = p, PhaseFrame = 5 }, to));
        }

        [Test]
        public void Parry_LockedInWindow_AnyActionFor12FramesAfterSuccess()
        {
            var c = new CancelContext { Current = ActionKind.Parry, ParryWindowOpen = true, FramesSinceParrySuccess = -1 };
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge));
            c.ParryWindowOpen = false;
            c.FramesSinceParrySuccess = 0;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Dodge));
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Skill));
            c.FramesSinceParrySuccess = 11;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Basic));
            c.FramesSinceParrySuccess = 12;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic));
            c.FramesSinceParrySuccess = -1;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic), "no success");
        }

        [Test]
        public void DodgeToBasic_LastEightFrames_BecomesDashAttack()
        {
            var c = new CancelContext { Current = ActionKind.Dodge, DodgeFramesRemaining = 9 };
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic));
            c.DodgeFramesRemaining = 8;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Basic));
            c.DodgeFramesRemaining = 1;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Basic));
            c.DodgeFramesRemaining = 0;
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic));
        }

        [Test]
        public void FlinchToDodge_After6Frames()
        {
            var c = new CancelContext { Current = ActionKind.Flinch, FramesSinceFlinch = 5 };
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Dodge));
            c.FramesSinceFlinch = 6;
            Assert.IsTrue(CancelRules.Allows(c, ActionKind.Dodge));
            Assert.IsFalse(CancelRules.Allows(c, ActionKind.Basic));
        }

        [Test]
        public void Idle_AllowsEverything()
        {
            Assert.IsTrue(CancelRules.Allows(new CancelContext { Current = ActionKind.None }, ActionKind.Skill));
            Assert.IsFalse(CancelRules.Allows(new CancelContext { Current = ActionKind.None }, ActionKind.None));
        }
    }

    public class SequencerTests
    {
        private InputBuffer _buf;
        private AttackSequencer _seq;
        private AttackExternalState _ext;

        [SetUp]
        public void SetUp()
        {
            _buf = new InputBuffer();
            _seq = new AttackSequencer(AttackFixtures.Greatsword());
            _ext = AttackExternalState.Free;
        }

        private void Tick(int frame, bool basicHeld = false, bool heavyHeld = false, float speed = 1f) =>
            _seq.Tick(frame, _buf, basicHeld, heavyHeld, speed, in _ext);

        private void Run(int from, int to)
        {
            for (int f = from; f <= to; f++) Tick(f);
        }

        [Test]
        public void BasicPress_StartsStartupFrame1_SameTick()
        {
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            Assert.IsTrue(_seq.StartedThisTick);
            Assert.AreEqual(AttackPhase.Startup, _seq.State.Phase);
            Assert.AreEqual(1, _seq.State.PhaseFrame);
            Assert.AreEqual(0, _seq.CurrentComboIndex);
        }

        [Test]
        public void ComboAdvances_PressedDuringRecovery_StartsWhenRecoveryEnds()
        {
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            _buf.Push(InputActionId.Basic, 30); // recovery spans frames 27..66
            Run(2, 66);
            Assert.AreEqual(0, _seq.CurrentComboIndex);
            Tick(67);
            Assert.IsTrue(_seq.StartedThisTick, "period = 20+6+40 = 66 frames");
            Assert.AreEqual(1, _seq.CurrentComboIndex);
        }

        [Test]
        public void Combo_RunsThreeHitsThenWraps()
        {
            int[] expectedPeriod = { 66, 66, 68 };
            int[] seen = new int[4];
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            seen[0] = _seq.CurrentComboIndex;
            int frame = 1;
            for (int hit = 0; hit < 3; hit++)
            {
                int start = frame;
                int guard = 0;
                do
                {
                    frame++;
                    Tick(frame, basicHeld: true); // holding Basic chains the next hit
                    if (++guard > 200) Assert.Fail("combo stuck");
                }
                while (!_seq.StartedThisTick);
                Assert.AreEqual(expectedPeriod[hit], frame - start, "hit " + hit);
                seen[hit + 1] = _seq.CurrentComboIndex;
            }
            Assert.AreEqual(new[] { 0, 1, 2, 0 }, seen);
        }

        [Test]
        public void Combo_ResetsAfter08Seconds_NotBefore()
        {
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            Run(2, 66);
            Tick(67); // attack ended at tick 67, idle
            Assert.IsFalse(_seq.IsAttacking);
            Run(68, 113);
            _buf.Push(InputActionId.Basic, 114); // 47 frames after the end
            Tick(114);
            Assert.AreEqual(1, _seq.CurrentComboIndex, "within 0.8 s the combo continues");

            var seq2 = new AttackSequencer(AttackFixtures.Greatsword());
            var buf2 = new InputBuffer();
            var ext = AttackExternalState.Free;
            buf2.Push(InputActionId.Basic, 1);
            seq2.Tick(1, buf2, false, false, 1f, in ext);
            for (int f = 2; f <= 114; f++) seq2.Tick(f, buf2, false, false, 1f, in ext);
            buf2.Push(InputActionId.Basic, 115); // 48 frames after the end
            seq2.Tick(115, buf2, false, false, 1f, in ext);
            Assert.AreEqual(0, seq2.CurrentComboIndex, "0.8 s without attacking resets to hit 1");
        }

        [Test]
        public void Buffer_PressDuringStartup_IsKeptForTheNinthFrameOnly()
        {
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            // Pressed at frame 25 (Active): nothing is consumed until Recovery (27), still inside the 9-frame window.
            _buf.Push(InputActionId.Basic, 25);
            Run(2, 26);
            Assert.AreEqual(1, _buf.Count);
            Tick(27);
            Assert.AreEqual(ActionKind.Basic, _seq.Queued);
            Assert.AreEqual(0, _buf.Count);

            // Pressed at 10 (Startup): expires at frame 19 and never starts a hit.
            var seq = new AttackSequencer(AttackFixtures.Greatsword());
            var buf = new InputBuffer();
            var ext = AttackExternalState.Free;
            buf.Push(InputActionId.Basic, 1);
            seq.Tick(1, buf, false, false, 1f, in ext);
            buf.Push(InputActionId.Basic, 10);
            for (int f = 2; f <= 67; f++) seq.Tick(f, buf, false, false, 1f, in ext);
            Assert.AreEqual(ActionKind.None, seq.Queued);
            Assert.IsFalse(seq.IsAttacking, "expired press does not chain");
        }

        [Test]
        public void Heavy_Tap_And_FullCharge_ThroughTheSequencer()
        {
            _buf.Push(InputActionId.Heavy, 1);
            Tick(1, heavyHeld: true);
            Assert.AreEqual(ActionKind.Heavy, _seq.CurrentAction);
            Assert.IsTrue(_seq.State.IsCharging);
            for (int f = 2; f <= 72; f++) Tick(f, heavyHeld: true);
            Assert.AreEqual(72, _seq.State.StartupTicks);
            Assert.AreEqual(AttackPhase.Startup, _seq.State.Phase);
            Tick(73, heavyHeld: true);
            Assert.AreEqual(AttackPhase.Active, _seq.State.Phase);
            Assert.AreEqual(1f, _seq.State.ChargeFraction, 1e-5f);
        }

        [Test]
        public void DodgePress_HeavyWeightStartup_IsHeldAndExpires_AttackContinues()
        {
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            _buf.Push(InputActionId.Dodge, 3);
            for (int f = 3; f <= 12; f++)
            {
                Tick(f);
                Assert.IsFalse(_seq.DodgeCancelledThisTick);
                Assert.IsFalse(_buf.Contains(InputActionId.Dodge, f), "held by the sequencer, not visible to the DodgeController");
            }
            Assert.IsTrue(_seq.IsAttacking);
        }

        [Test]
        public void DodgePress_InRecovery_CancelsAndReturnsToTheBuffer()
        {
            _buf.Push(InputActionId.Basic, 1);
            Run(1, 40);
            _buf.Push(InputActionId.Dodge, 41);
            Tick(41);
            Assert.IsTrue(_seq.DodgeCancelledThisTick);
            Assert.IsFalse(_seq.IsAttacking);
            Assert.IsTrue(_buf.Contains(InputActionId.Dodge, 41));
        }

        [Test]
        public void DodgePress_PendingFromStartup_FiresWhenRecoveryOpensWithinTheBuffer()
        {
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            Run(2, 20);
            _buf.Push(InputActionId.Dodge, 22);
            Run(21, 26);
            Assert.IsTrue(_seq.IsAttacking);
            Tick(27); // Recovery frame 1, press from frame 22 is still inside 9 frames
            Assert.IsTrue(_seq.DodgeCancelledThisTick);
        }

        [Test]
        public void DodgePress_LightWeapon_StartupFirst4Frames()
        {
            _seq = new AttackSequencer(AttackFixtures.Greatsword(WeaponWeightClass.Light));
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            _buf.Push(InputActionId.Dodge, 3);
            Tick(3); // startup frame 3
            Assert.IsTrue(_seq.DodgeCancelledThisTick);

            _seq = new AttackSequencer(AttackFixtures.Greatsword(WeaponWeightClass.Light));
            _buf.Clear();
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            Run(2, 5);
            _buf.Push(InputActionId.Dodge, 6);
            Tick(6); // startup frame 6
            Assert.IsFalse(_seq.DodgeCancelledThisTick);
        }

        [Test]
        public void DodgePress_WhileChargingHeavy_CancelsAndLosesTheCharge()
        {
            _buf.Push(InputActionId.Heavy, 1);
            Tick(1, heavyHeld: true);
            for (int f = 2; f <= 30; f++) Tick(f, heavyHeld: true);
            _buf.Push(InputActionId.Dodge, 31);
            Tick(31, heavyHeld: true);
            Assert.IsTrue(_seq.DodgeCancelledThisTick);
            Assert.IsFalse(_seq.IsAttacking);
            Assert.AreEqual(0f, _seq.State.ChargeFraction);
        }

        [Test]
        public void DodgePress_WithoutCharges_KeepsTheAttack()
        {
            _buf.Push(InputActionId.Basic, 1);
            Run(1, 40);
            _ext.CanDodge = false;
            _buf.Push(InputActionId.Dodge, 41);
            Tick(41);
            Assert.IsFalse(_seq.DodgeCancelledThisTick);
            Assert.IsTrue(_seq.IsAttacking);
        }

        [Test]
        public void Stun_InterruptsTheAttack()
        {
            _buf.Push(InputActionId.Basic, 1);
            Run(1, 10);
            _ext.CanAct = false;
            Tick(11);
            Assert.IsFalse(_seq.IsAttacking);
        }

        [Test]
        public void BasicDuringDodge_LastEightFrames_IsDashAttack_ElseWaits()
        {
            _ext.DodgeActive = true;
            _ext.DodgeFramesRemaining = 9;
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            Assert.IsFalse(_seq.IsAttacking);
            Assert.AreEqual(1, _buf.Count, "waits in the buffer");
            _ext.DodgeFramesRemaining = 8;
            Tick(2);
            Assert.IsTrue(_seq.IsAttacking);
            Assert.AreEqual(AttackKind.Dash, _seq.State.Kind);
            Assert.AreEqual(10, _seq.State.Step.StartupFrames);
        }

        [Test]
        public void CanCancelInto_ReflectsTheTable()
        {
            Assert.IsTrue(_seq.CanCancelInto(ActionKind.Skill), "idle");
            _buf.Push(InputActionId.Basic, 1);
            Tick(1);
            Assert.IsFalse(_seq.CanCancelInto(ActionKind.Skill));
            Assert.IsFalse(_seq.CanCancelInto(ActionKind.Dodge), "heavy weight, startup");
            Run(2, 26);
            Assert.IsFalse(_seq.CanCancelInto(ActionKind.Skill), "still active");
            Tick(27);
            Assert.IsTrue(_seq.CanCancelInto(ActionKind.Skill));
            Assert.IsTrue(_seq.CanCancelInto(ActionKind.Dodge));
        }

        [Test]
        public void HoldingBasic_RepeatsTheCombo()
        {
            Tick(1, basicHeld: true);
            Assert.IsTrue(_seq.IsAttacking);
            Run(2, 66);
            Tick(67, basicHeld: true);
            Assert.AreEqual(1, _seq.CurrentComboIndex);
        }
    }

    public class LungeSolverTests
    {
        private static readonly LungeCandidate[] None = new LungeCandidate[0];

        [Test]
        public void ZeroDistanceInTheData_MeansNoLunge()
        {
            var step = AttackFixtures.Step(20, 6, 40, lungeMin: 0f, lungeMax: 0f, lungeCone: 60f);
            var plan = LungeSolver.Plan(step, Vector3.zero, Vector3.forward, new[] { new LungeCandidate(new Vector3(0, 0, 3), 0.5f) }, 1, 0.4f, 20);
            Assert.IsFalse(plan.HasLunge);
        }

        [Test]
        public void TargetFar_AdvancesLungeMax_NearTarget_StopsAtContact()
        {
            var step = AttackFixtures.Step(20, 6, 40, lungeMin: 0.5f, lungeMax: 1.5f, lungeCone: 60f);
            var far = LungeSolver.Plan(step, Vector3.zero, Vector3.forward, new[] { new LungeCandidate(new Vector3(0, 0, 5), 0.5f) }, 1, 0.4f, 10);
            Assert.AreEqual(1.5f, far.Distance, 1e-4f);
            Assert.AreEqual(10, far.Ticks);
            Assert.AreEqual(0.15f, far.StepPerTick.z, 1e-4f);

            // gap = 2.0 - 0.5 - 0.4 = 1.1 m
            var near = LungeSolver.Plan(step, Vector3.zero, Vector3.forward, new[] { new LungeCandidate(new Vector3(0, 0, 2), 0.5f) }, 1, 0.4f, 10);
            Assert.AreEqual(1.1f, near.Distance, 1e-4f);

            // Already touching: never crosses, even below LungeMin.
            var touching = LungeSolver.Plan(step, Vector3.zero, Vector3.forward, new[] { new LungeCandidate(new Vector3(0, 0, 0.9f), 0.5f) }, 1, 0.4f, 10);
            Assert.IsFalse(touching.HasLunge);
        }

        [Test]
        public void NoTargetOrOutsideCone_AdvancesLungeMinForward()
        {
            var step = AttackFixtures.Step(20, 6, 40, lungeMin: 0.5f, lungeMax: 1.5f, lungeCone: 60f);
            var empty = LungeSolver.Plan(step, Vector3.zero, Vector3.forward, None, 0, 0.4f, 10);
            Assert.AreEqual(0.5f, empty.Distance, 1e-4f);
            Assert.AreEqual(Vector3.forward, empty.Direction);

            // 90 degrees to the side: outside the 60 degree cone (half 30) and not on the path.
            var side = LungeSolver.Plan(step, Vector3.zero, Vector3.forward, new[] { new LungeCandidate(new Vector3(3, 0, 0), 0.5f) }, 1, 0.4f, 10);
            Assert.AreEqual(0.5f, side.Distance, 1e-4f);
            Assert.AreEqual(Vector3.forward, side.Direction);
        }

        [Test]
        public void TargetInsideCone_SteersTowardIt_NearestWins()
        {
            var step = AttackFixtures.Step(20, 6, 40, lungeMin: 0.5f, lungeMax: 1.5f, lungeCone: 60f);
            var targets = new[]
            {
                new LungeCandidate(new Vector3(0, 0, 6), 0.5f),
                new LungeCandidate(new Vector3(1.5f, 0, 3f), 0.5f),  // ~26.6 degrees, nearest in the cone
            };
            var plan = LungeSolver.Plan(step, Vector3.zero, Vector3.forward, targets, 2, 0.4f, 10);
            Assert.Greater(plan.Direction.x, 0.3f);
            Assert.AreEqual(1.5f, plan.Distance, 1e-4f);
        }

        [Test]
        public void BodyOnThePathOutsideTheCone_StillBlocks()
        {
            var step = AttackFixtures.Step(20, 6, 40, lungeMin: 1.0f, lungeMax: 1.5f, lungeCone: 0f);
            var blocker = new[] { new LungeCandidate(new Vector3(0, 0, 1.2f), 0.3f) };
            var plan = LungeSolver.Plan(step, Vector3.zero, Vector3.forward, blocker, 1, 0.4f, 10);
            // reach = 1.2 - (0.3 + 0.4) = 0.5
            Assert.AreEqual(0.5f, plan.Distance, 1e-4f);
        }
    }
}
