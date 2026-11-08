#if UNITY_EDITOR
using EntityId = Eclipse.Gameplay.Combat.EntityId;
using Eclipse.Core.CrowdControl;
using Eclipse.Gameplay.Combat;
using NUnit.Framework;

namespace Eclipse.Tests.PlayMode.Combat.States
{
    /// <summary>06 3.8: Stagger bar to Staggered on a Medium target, and CC diminishing returns 100/50/25/immune.</summary>
    public class StateTests
    {
        private CombatEnv _env;

        [SetUp]
        public void SetUp() => _env = new CombatEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();

        [Test]
        public void Medium_tier_fills_at_40_and_becomes_Staggered_with_vulnerability()
        {
            var id = _env.NewId();
            var states = new StateComponent(_env.Rules, id, false, null, _env.Events);
            var stagger = new StaggerComponent(_env.Rules, id, StaggerTier.Medium, false, _env.Rules.DifficultyMultiplier(DifficultyTier.Normal), _env.Events, states);
            states.SetStagger(stagger);
            stagger.Tick(0);

            Assert.AreEqual(40f, stagger.StaggerMax, 1e-3f, "Medium bar (06 3.8.2)");
            Assert.IsFalse(stagger.AddStagger(15f, new EntityId(1), 0));
            stagger.Tick(1);
            Assert.IsFalse(stagger.AddStagger(15f, new EntityId(1), 0));
            Assert.IsFalse(stagger.IsStaggered, "30 of 40");
            Assert.AreEqual(30f, stagger.StaggerCurrent, 0.5f);
            stagger.Tick(2);
            Assert.IsTrue(stagger.AddStagger(15f, new EntityId(1), 0), "45 >= 40 triggers");
            Assert.AreEqual(StaggerState.Staggered, stagger.State);
            Assert.AreEqual(_env.Rules.V.StaggeredVulnerability, stagger.VulnerabilityBonus, 1e-4f);
            Assert.Greater(stagger.VulnerabilityBonus, 0f);
            Assert.AreEqual(1, _env.Events.Count<OnStaggered>());

            int frames = CombatStateRules.Frames(_env.Rules.V.StaggeredSeconds);
            stagger.Tick(2 + frames - 1);
            Assert.IsTrue(stagger.IsStaggered, "still staggered one frame before the end");
            stagger.Tick(2 + frames);
            Assert.AreEqual(StaggerState.Normal, stagger.State, "recovers after StaggeredSeconds");
            Assert.AreEqual(0f, stagger.VulnerabilityBonus);
        }

        [Test]
        public void Stagger_bar_decays_when_not_hit()
        {
            var id = _env.NewId();
            var stagger = new StaggerComponent(_env.Rules, id, StaggerTier.Medium, false, 1f, _env.Events);
            stagger.Tick(0);
            stagger.AddStagger(20f, new EntityId(1), 0);
            stagger.Tick(60 * 10);
            Assert.AreEqual(0f, stagger.StaggerCurrent, 1e-3f, "fully decayed after 10 s");
            Assert.AreEqual(StaggerState.Normal, stagger.State);
        }

        [Test]
        public void Stun_diminishing_returns_are_100_50_25_then_immune()
        {
            var id = _env.NewId();
            var states = new StateComponent(_env.Rules, id, false, null, _env.Events);
            var src = new EntityId(1);

            CcResult r1 = states.ApplyCc(CcKind.Stun, 1f, src, 10, 0);
            CcResult r2 = states.ApplyCc(CcKind.Stun, 1f, src, 11, 0);
            CcResult r3 = states.ApplyCc(CcKind.Stun, 1f, src, 12, 0);
            CcResult r4 = states.ApplyCc(CcKind.Stun, 1f, src, 13, 0);

            Assert.IsTrue(r1.Applied);
            Assert.AreEqual(60, r1.DurationFrames, "100%");
            Assert.AreEqual(30, r2.DurationFrames, "50%");
            Assert.AreEqual(15, r3.DurationFrames, "25%");
            Assert.AreEqual(CcOutcome.ImmuneDr, r4.Outcome, "4th in the window: immune");
            Assert.AreEqual(0, r4.DurationFrames);
            Assert.AreEqual(3, _env.Events.Count<OnCCApplied>());
        }

        [Test]
        public void Stun_makes_the_target_unable_to_act_until_it_expires()
        {
            var id = _env.NewId();
            var states = new StateComponent(_env.Rules, id, false, null, _env.Events);
            states.ApplyCc(CcKind.Stun, 1f, new EntityId(1), 10, 0);
            Assert.IsFalse(states.CanAct(10 + 59));
            Assert.IsTrue(states.CanAct(10 + 60));
        }

        [Test]
        public void Colossal_style_dr_is_per_group_so_a_root_still_applies_after_stun_immunity()
        {
            var id = _env.NewId();
            var states = new StateComponent(_env.Rules, id, false, null, _env.Events);
            var src = new EntityId(1);
            for (int i = 0; i < 4; i++) states.ApplyCc(CcKind.Stun, 1f, src, 10 + i, 0);
            Assert.IsTrue(states.ApplyCc(CcKind.Root, 1f, src, 14, 0).Applied);
        }
    }
}
#endif
