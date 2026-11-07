using NUnit.Framework;

namespace Eclipse.Core.Procs.Tests
{
    [TestFixture]
    public class ExecuteRulesTests
    {
        // R08 / 06 3.2: max 20%, Elite half, boss +25% Vulnerability below 20% HP.
        private readonly ExecuteRules _rules = new ExecuteRules(new ExecuteConfig(0.20f, 0.5f, 0.25f, 0.20f));

        [Test]
        public void Common_ThresholdCappedAt20()
        {
            Assert.AreEqual(0.20f, _rules.EffectiveThreshold(ExecuteTargetClass.Common, 0.20f), 1e-6f);
            Assert.AreEqual(0.20f, _rules.EffectiveThreshold(ExecuteTargetClass.Common, 0.35f), 1e-6f); // absolute cap
            Assert.AreEqual(0.15f, _rules.EffectiveThreshold(ExecuteTargetClass.Common, 0.15f), 1e-6f);
        }

        [Test]
        public void Elite_WithThreshold20_Gets10()
        {
            Assert.AreEqual(0.10f, _rules.EffectiveThreshold(ExecuteTargetClass.Elite, 0.20f), 1e-6f);
            Assert.AreEqual(0.10f, _rules.EffectiveThreshold(ExecuteTargetClass.Elite, 0.30f), 1e-6f);
            Assert.IsTrue(_rules.ShouldExecute(ExecuteTargetClass.Elite, 0.09f, 0.20f));
            Assert.IsFalse(_rules.ShouldExecute(ExecuteTargetClass.Elite, 0.15f, 0.20f));
        }

        [Test]
        public void GuardianAndBoss_AreImmune()
        {
            Assert.AreEqual(0f, _rules.EffectiveThreshold(ExecuteTargetClass.Guardian, 0.20f));
            Assert.AreEqual(0f, _rules.EffectiveThreshold(ExecuteTargetClass.Boss, 0.20f));
            Assert.IsFalse(_rules.ShouldExecute(ExecuteTargetClass.Boss, 0.01f, 0.20f));
            Assert.IsFalse(_rules.ShouldExecute(ExecuteTargetClass.Guardian, 0.01f, 0.20f));
        }

        [Test]
        public void Boss_Below20_GetsSingle25Vulnerability_EvenWithTwoSources()
        {
            Assert.AreEqual(0.25f, _rules.BossVulnerability(ExecuteTargetClass.Boss, 0.19f, 1), 1e-6f);
            Assert.AreEqual(0.25f, _rules.BossVulnerability(ExecuteTargetClass.Boss, 0.19f, 2), 1e-6f); // no stacking
        }

        [Test]
        public void Boss_NoConversion_AboveThreshold_OrWithoutSource_OrNonBoss()
        {
            Assert.AreEqual(0f, _rules.BossVulnerability(ExecuteTargetClass.Boss, 0.20f, 2));
            Assert.AreEqual(0f, _rules.BossVulnerability(ExecuteTargetClass.Boss, 0.50f, 2));
            Assert.AreEqual(0f, _rules.BossVulnerability(ExecuteTargetClass.Boss, 0.10f, 0));
            Assert.AreEqual(0f, _rules.BossVulnerability(ExecuteTargetClass.Common, 0.10f, 1));
        }

        [Test]
        public void Common_ExecutesStrictlyBelowThreshold()
        {
            Assert.IsTrue(_rules.ShouldExecute(ExecuteTargetClass.Common, 0.19f, 0.20f));
            Assert.IsFalse(_rules.ShouldExecute(ExecuteTargetClass.Common, 0.20f, 0.20f));
        }
    }
}
