using NUnit.Framework;

namespace Eclipse.Core.CrowdControl.Tests
{
    public class CrowdControlTrackerTests
    {
        [Test]
        public void Enemy_StunFreezeStun_Gives100_50_25_Then4thImmune()
        {
            var t = CcTestRules.NewTracker();
            Assert.AreEqual(120, t.TryApply(CcKind.Stun, 2f, false, 0f, 0));
            Assert.AreEqual(60, t.TryApply(CcKind.Freeze, 2f, false, 0f, 60));
            Assert.AreEqual(30, t.TryApply(CcKind.Stun, 2f, false, 0f, 120));
            Assert.AreEqual(0, t.TryApply(CcKind.Freeze, 2f, false, 0f, 180));
            Assert.IsTrue(t.IsImmune(CcGroup.Hard, false, 200));
        }

        [Test]
        public void Enemy_WindowCountsFromFirstCc_AndResetsAfter8s()
        {
            var t = CcTestRules.NewTracker();
            t.TryApply(CcKind.Stun, 1f, false, 0f, 100);
            t.TryApply(CcKind.Stun, 1f, false, 0f, 200);
            t.TryApply(CcKind.Stun, 1f, false, 0f, 300);
            Assert.AreEqual(0, t.TryApply(CcKind.Stun, 1f, false, 0f, 100 + 480 - 1));
            Assert.AreEqual(60, t.TryApply(CcKind.Stun, 1f, false, 0f, 100 + 480));
            Assert.AreEqual(30, t.TryApply(CcKind.Stun, 1f, false, 0f, 100 + 480 + 10));
        }

        [Test]
        public void Enemy_GroupsAreIndependent()
        {
            var t = CcTestRules.NewTracker();
            t.TryApply(CcKind.Stun, 1f, false, 0f, 0);
            t.TryApply(CcKind.Stun, 1f, false, 0f, 1);
            Assert.AreEqual(60, t.TryApply(CcKind.Root, 1f, false, 0f, 2));
            Assert.AreEqual(60, t.TryApply(CcKind.Silence, 1f, false, 0f, 2));
            Assert.AreEqual(60, t.TryApply(CcKind.Taunt, 1f, false, 0f, 2));
            Assert.AreEqual(30, t.TryApply(CcKind.Root, 1f, false, 0f, 3));
        }

        [Test]
        public void Enemy_ImmuneCallDoesNotExtendWindow()
        {
            var t = CcTestRules.NewTracker();
            for (int i = 0; i < 3; i++) t.TryApply(CcKind.Stun, 1f, false, 0f, i);
            Assert.AreEqual(0, t.TryApply(CcKind.Stun, 1f, false, 0f, 400));
            Assert.AreEqual(60, t.TryApply(CcKind.Stun, 1f, false, 0f, 480));
        }

        [Test]
        public void Player_FreezeCappedAt1s_ThenHardImmuneFor2s()
        {
            var t = CcTestRules.NewTracker();
            Assert.AreEqual(60, t.TryApply(CcKind.Freeze, 2f, true, 0f, 0));
            Assert.AreEqual(0, t.TryApply(CcKind.Stun, 1f, true, 0f, 60));   // Freeze still running
            Assert.AreEqual(0, t.TryApply(CcKind.Stun, 1f, true, 0f, 179));  // immune until end + 2 s
            Assert.AreEqual(90, t.TryApply(CcKind.Stun, 5f, true, 0f, 180)); // Hard cap 1.5 s
        }

        [Test]
        public void Player_Stun1_5s_ImmuneUntilEndPlus2s()
        {
            var t = CcTestRules.NewTracker();
            Assert.AreEqual(90, t.TryApply(CcKind.Stun, 1.5f, true, 0f, 0));
            Assert.AreEqual(0, t.TryApply(CcKind.Stun, 1f, true, 0f, 209));
            Assert.AreEqual(60, t.TryApply(CcKind.Stun, 1f, true, 0f, 210));
        }

        [Test]
        public void Player_HasNoSequence_OnlyCapsAndImmunity()
        {
            var t = CcTestRules.NewTracker();
            Assert.AreEqual(60, t.TryApply(CcKind.Stun, 1f, true, 0f, 0));
            Assert.AreEqual(60, t.TryApply(CcKind.Stun, 1f, true, 0f, 180));
            Assert.AreEqual(60, t.TryApply(CcKind.Stun, 1f, true, 0f, 360));
        }

        [Test]
        public void Player_RootCappedAt2s_NoImmunity_RootDoesNotBlockHard()
        {
            var t = CcTestRules.NewTracker();
            Assert.AreEqual(120, t.TryApply(CcKind.Root, 5f, true, 0f, 0));
            Assert.AreEqual(120, t.TryApply(CcKind.Root, 5f, true, 0f, 1));
            Assert.AreEqual(60, t.TryApply(CcKind.Stun, 1f, true, 0f, 2));
        }

        [Test]
        public void Player_TauntNeverApplied()
        {
            Assert.AreEqual(0, CcTestRules.NewTracker().TryApply(CcKind.Taunt, 2f, true, 0f, 0));
        }

        [Test]
        public void StatusResistance_ReducesDuration_BeforePlayerCap()
        {
            var t = CcTestRules.NewTracker();
            Assert.AreEqual(60, t.TryApply(CcKind.Root, 2f, true, 0.5f, 0)); // 1 s
            Assert.AreEqual(90, t.TryApply(CcKind.Stun, 3f, true, 0.5f, 0)); // 1.5 s, exactly the cap
            var t2 = CcTestRules.NewTracker();
            Assert.AreEqual(90, t2.TryApply(CcKind.Stun, 10f, true, 0.7f, 0)); // 3 s -> capped to 1.5 s
        }

        [Test]
        public void StatusResistance_RoundsUpToTicks()
        {
            var t = CcTestRules.NewTracker();
            // 0.1 s x 0.7 = 0.07 s = 4.2 ticks -> 5
            Assert.AreEqual(5, t.TryApply(CcKind.Silence, 0.1f, true, 0.3f, 0));
        }

        [Test]
        public void Reset_ClearsState()
        {
            var t = CcTestRules.NewTracker();
            for (int i = 0; i < 3; i++) t.TryApply(CcKind.Stun, 1f, false, 0f, i);
            t.Reset();
            Assert.AreEqual(60, t.TryApply(CcKind.Stun, 1f, false, 0f, 3));
        }
    }
}
