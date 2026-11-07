using NUnit.Framework;

namespace Eclipse.Core.Stats.Tests
{
    /// <summary>Rules fixture with the values of docs/gdd/01-atributos.md section 3.6 (fractions).</summary>
    internal static class Rules01
    {
        public static StatRuleTable Create()
        {
            return new StatRuleTable()
                .Set(StatId.AttackSpeed, new StatRule(0.50f, 1.00f))
                .Set(StatId.CastSpeed, new StatRule(0.50f, 1.00f))
                .Set(StatId.CooldownReduction, new StatRule(0.25f, 0.40f))
                .Set(StatId.DodgeChance, new StatRule(0.20f, 0.40f))
                .Set(StatId.CriticalChance, new StatRule(null, 0.75f));
        }
    }

    [TestFixture]
    public class DiminishingReturnsTests
    {
        private readonly StatRuleTable _rules = Rules01.Create();

        [TestCase(0.80f, 0.65f)]   // 01 3.1: 0.5 + 0.3 * 0.5
        [TestCase(2.00f, 1.00f)]   // hard cap
        [TestCase(0.30f, 0.30f)]   // below soft
        [TestCase(0.50f, 0.50f)]   // exactly soft
        public void AttackSpeed(float raw, float expected)
        {
            Assert.AreEqual(expected, DiminishingReturns.Apply(raw, _rules.Get(StatId.AttackSpeed)), 1e-5f);
        }

        [TestCase(0.25f, 0.25f)]
        [TestCase(0.40f, 0.325f)]  // 0.25 + 0.15 * 0.5
        [TestCase(0.80f, 0.40f)]   // capped
        public void CooldownReduction(float raw, float expected)
        {
            Assert.AreEqual(expected, DiminishingReturns.Apply(raw, _rules.Get(StatId.CooldownReduction)), 1e-5f);
        }

        [TestCase(0.20f, 0.20f)]
        [TestCase(0.30f, 0.25f)]
        [TestCase(0.60f, 0.40f)]   // 0.2 + 0.4 * 0.5 = 0.4
        [TestCase(1.00f, 0.40f)]
        public void Dodge(float raw, float expected)
        {
            Assert.AreEqual(expected, DiminishingReturns.Apply(raw, _rules.Get(StatId.DodgeChance)), 1e-5f);
        }

        [Test]
        public void HardCapOnlyAndNone()
        {
            Assert.AreEqual(0.75f, DiminishingReturns.Apply(0.9f, _rules.Get(StatId.CriticalChance)), 1e-6f);
            Assert.AreEqual(0.5f, DiminishingReturns.Apply(0.5f, _rules.Get(StatId.CriticalChance)), 1e-6f);
            Assert.AreEqual(12345f, DiminishingReturns.Apply(12345f, StatRule.None));
            Assert.AreEqual(12345f, DiminishingReturns.Apply(12345f, _rules.Get(StatId.Armor)));
        }

        [Test]
        public void MinimumApplied()
        {
            var r = new StatRule(null, null, 1f, -0.5f);
            Assert.AreEqual(-0.5f, DiminishingReturns.Apply(-0.9f, r), 1e-6f);
        }
    }
}
