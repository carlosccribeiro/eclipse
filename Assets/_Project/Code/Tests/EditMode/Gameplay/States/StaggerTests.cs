using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Entities;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.Gameplay.States
{
    public sealed class StaggerTests
    {
        private CombatEventStream _ev;
        private CombatStateRules _rules;
        private static readonly EntityId Target = new EntityId(1);
        private static readonly EntityId Src = new EntityId(2);

        [SetUp]
        public void SetUp()
        {
            _ev = new CombatEventStream(64);
            _rules = StatesFixtures.Rules();
        }

        private StaggerComponent Make(StaggerTier tier, bool elite = false, float diff = 1f, StateComponent states = null) =>
            new StaggerComponent(_rules, Target, tier, elite, diff, _ev, states);

        private StateComponent States(MassClass mass, bool elite = false, bool guardianOrBoss = false) =>
            new StateComponent(_rules, Target, false, new FakeMass(mass, elite), _ev, guardianOrBoss);

        [Test]
        public void Thresholds_FollowTable()
        {
            Assert.AreEqual(40f, Make(StaggerTier.Medium).StaggerMax, 1e-4f);
            Assert.AreEqual(120f, Make(StaggerTier.Heavy).StaggerMax, 1e-4f);
            Assert.AreEqual(100f, Make(StaggerTier.Medium, true).StaggerMax, 1e-4f);
            Assert.AreEqual(300f, Make(StaggerTier.Heavy, true).StaggerMax, 1e-4f);
            Assert.AreEqual(400f, Make(StaggerTier.Guardian).StaggerMax, 1e-4f);
            Assert.AreEqual(800f, Make(StaggerTier.Boss).StaggerMax, 1e-4f);
            Assert.AreEqual(0f, Make(StaggerTier.None).StaggerMax, 1e-4f);
        }

        [Test]
        public void DifficultyMultiplier_ScalesThreshold()
        {
            Assert.AreEqual(48f, Make(StaggerTier.Medium, false, _rules.DifficultyMultiplier(DifficultyTier.Veteran)).StaggerMax, 1e-3f);
            Assert.AreEqual(64f, Make(StaggerTier.Medium, false, _rules.DifficultyMultiplier(DifficultyTier.Eclipse)).StaggerMax, 1e-3f);
            Assert.AreEqual(1.65f, _rules.DifficultyMultiplier(DifficultyTier.Ascension, 5), 1e-5f);
        }

        [Test]
        public void Decay_Starts_After3Seconds_At15PercentPerSecond()
        {
            var s = Make(StaggerTier.Heavy); // 120 -> 18 per second
            s.Tick(0);
            s.AddStagger(60f, Src, 0);
            s.Tick(180); // exactly 3 s without gains: nothing decayed yet
            Assert.AreEqual(60f, s.StaggerCurrent, 1e-3f);
            s.Tick(240); // +1 s of decay
            Assert.AreEqual(60f - 18f, s.StaggerCurrent, 1e-3f);
            s.Tick(10000);
            Assert.AreEqual(0f, s.StaggerCurrent, 1e-4f);
        }

        [Test]
        public void Decay_Restarts_WhenNewStaggerArrives()
        {
            var s = Make(StaggerTier.Heavy);
            s.Tick(0);
            s.AddStagger(60f, Src, 0);
            s.Tick(200);
            s.AddStagger(10f, Src, 0);
            s.Tick(200 + 180);
            Assert.AreEqual(64f, s.StaggerCurrent, 1e-3f); // 60 - 6 decayed before the second hit + 10
        }

        [Test]
        public void FullBar_Staggered_Plus20Vulnerability_AndDuration()
        {
            var s = Make(StaggerTier.Medium);
            s.Tick(10);
            Assert.IsFalse(s.AddStagger(39f, Src, 0));
            Assert.IsTrue(s.AddStagger(1f, Src, 0));
            Assert.IsTrue(s.IsStaggered);
            Assert.AreEqual(0.2f, s.VulnerabilityBonus, 1e-5f);
            Assert.AreEqual(1, _ev.Count<OnStaggered>());
            Assert.AreEqual(2, _ev.Count<OnStaggerApplied>());
            s.Tick(10 + 89);
            Assert.IsTrue(s.IsStaggered);
            s.Tick(10 + 90); // 1.5 s
            Assert.IsFalse(s.IsStaggered);
            Assert.AreEqual(0f, s.VulnerabilityBonus);
        }

        [Test]
        public void StaggeredDuration_EliteAndGuardian()
        {
            var e = Make(StaggerTier.Medium, true);
            e.Tick(0);
            e.AddStagger(100f, Src, 0);
            e.Tick(119);
            Assert.IsTrue(e.IsStaggered);
            e.Tick(120); // 2.0 s
            Assert.IsFalse(e.IsStaggered);

            var g = Make(StaggerTier.Guardian);
            g.Tick(0);
            g.AddStagger(400f, Src, 0);
            g.Tick(179);
            Assert.IsTrue(g.IsStaggered);
            g.Tick(180); // 3.0 s
            Assert.IsFalse(g.IsStaggered);
        }

        [Test]
        public void StaggerWhileStaggered_DoesNotFillBar()
        {
            var s = Make(StaggerTier.Medium);
            s.Tick(0);
            s.AddStagger(40f, Src, 0);
            s.AddStagger(30f, Src, 0);
            Assert.AreEqual(0f, s.StaggerCurrent);
            Assert.AreEqual(1, _ev.Count<OnStaggered>());
        }

        [Test]
        public void Boss_Broken_4s_Plus30_HitStop6_AndBreakEvent()
        {
            var s = Make(StaggerTier.Boss);
            s.Tick(0);
            Assert.IsTrue(s.AddStagger(800f, Src, 0));
            Assert.IsTrue(s.IsBroken);
            Assert.AreEqual(0.3f, s.VulnerabilityBonus, 1e-5f);
            Assert.AreEqual(1, _ev.Count<OnStaggerBreak>());
            Assert.AreEqual(1, _ev.Read<OnStaggerBreak>()[0].BreakCount);
            Assert.AreEqual(0, _ev.Count<OnStaggered>());
            Assert.AreEqual(1, _ev.Count<OnGlobalHitStop>());
            Assert.AreEqual(6, _ev.Read<OnGlobalHitStop>()[0].Frames);
            s.Tick(239);
            Assert.IsTrue(s.IsBroken);
            s.Tick(240); // 4 s
            Assert.IsFalse(s.IsStaggered);
        }

        [Test]
        public void Boss_Threshold_Grows50PercentPerBreak_UpTo3Times()
        {
            var s = Make(StaggerTier.Boss);
            int frame = 0;
            float[] expected = { 800f, 1200f, 1600f, 2000f, 2000f };
            for (int i = 0; i < expected.Length; i++)
            {
                s.Tick(frame);
                Assert.AreEqual(expected[i], s.StaggerMax, 1e-3f, "before break " + i);
                Assert.IsTrue(s.AddStagger(expected[i], Src, 0));
                frame += 300; // Broken lasts 240 f
            }
            s.Tick(frame);
            Assert.AreEqual(5, s.BreakCount);
            Assert.AreEqual(2000f, s.StaggerMax, 1e-3f);
        }

        [Test]
        public void Unstoppable_FillsBar_ButDefersStaggeredUntilItEnds()
        {
            var st = States(MassClass.Medium);
            st.GrantUnstoppable(300);
            var s = Make(StaggerTier.Medium, false, 1f, st);
            s.Tick(0);
            Assert.IsFalse(s.AddStagger(40f, Src, 0));
            Assert.IsFalse(s.IsStaggered);
            Assert.IsTrue(s.HasUnstoppablePending);
            Assert.AreEqual(40f, s.StaggerCurrent, 1e-4f);
            Assert.AreEqual(1, _ev.Count<OnStaggerApplied>());
            s.Tick(299);
            Assert.IsFalse(s.IsStaggered);
            s.Tick(300);
            Assert.IsTrue(s.IsStaggered);
            Assert.AreEqual(1, _ev.Count<OnStaggered>());
            Assert.IsFalse(s.HasUnstoppablePending);
        }

        [Test]
        public void ForceStaggered_InterruptWindow_ImmediateAndBlockedByUnstoppable()
        {
            var s = Make(StaggerTier.Heavy);
            s.Tick(5);
            Assert.IsTrue(s.ForceStaggered(Src, 0));
            Assert.IsTrue(s.IsStaggered);
            Assert.AreEqual(1, _ev.Count<OnInterrupt>());

            var st = States(MassClass.Heavy);
            st.GrantUnstoppable(100);
            var u = Make(StaggerTier.Heavy, false, 1f, st);
            u.Tick(5);
            Assert.IsFalse(u.ForceStaggered(Src, 0));
        }

        [Test]
        public void Flinch_ByMassClass_UsesHitStagger()
        {
            Assert.IsTrue(States(MassClass.Light).ShouldFlinch(1f, 0));
            Assert.IsFalse(States(MassClass.Light).ShouldFlinch(0.5f, 0));
            Assert.IsFalse(States(MassClass.Medium).ShouldFlinch(9f, 0));
            Assert.IsTrue(States(MassClass.Medium).ShouldFlinch(10f, 0));
            Assert.IsFalse(States(MassClass.Medium, true).ShouldFlinch(19f, 0));
            Assert.IsTrue(States(MassClass.Medium, true).ShouldFlinch(20f, 0));
            Assert.IsFalse(States(MassClass.Heavy).ShouldFlinch(500f, 0));
            Assert.IsFalse(States(MassClass.Colossal).ShouldFlinch(500f, 0));
        }

        [Test]
        public void Flinch_PublishesInterrupt_AndSuperArmorIgnoresIt()
        {
            var st = States(MassClass.Medium);
            var s = Make(StaggerTier.Medium, false, 1f, st);
            s.Tick(0);
            s.AddStagger(10f, Src, 0);
            Assert.AreEqual(1, _ev.Count<OnInterrupt>());
            st.GrantSuperArmor(100);
            s.AddStagger(10f, Src, 0);
            Assert.AreEqual(1, _ev.Count<OnInterrupt>());
        }

        [Test]
        public void Heavy_HasSuperArmor_OnlyWhileAttacking()
        {
            var st = States(MassClass.Heavy);
            Assert.IsFalse(st.HasSuperArmor(0));
            st.SetAttacking(true);
            Assert.IsTrue(st.HasSuperArmor(0));
        }
    }
}
