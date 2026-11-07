using NUnit.Framework;

namespace Eclipse.Core.Attributes.Tests
{
    // 01 3.8 and 00 R01.
    public class ResourceTranslationTests
    {
        // Kael / Nyx: +X maximum, cap 150.
        [TestCase(0f, 100f)]
        [TestCase(0.12f, 112f)]   // 01 3.8 UI example: +12% -> +12 Fury
        [TestCase(0.15f, 115f)]   // 01 3.12
        [TestCase(0.50f, 150f)]
        [TestCase(0.80f, 150f)]   // capped
        public void Pool_KaelAndNyx(float maxResource, float expected)
        {
            Assert.AreEqual(expected, ResourceTranslation.Translate(AttributeFixtures.Kael().Resource, maxResource).Maximum, 0.001f);
            Assert.AreEqual(expected, ResourceTranslation.Translate(AttributeFixtures.Nyx().Resource, maxResource).Maximum, 0.001f);
        }

        // Lyra: +1 Charge per 25%, max 6 (= +75%), no override.
        [TestCase(0f, 3)]
        [TestCase(0.24f, 3)]
        [TestCase(0.25f, 4)]
        [TestCase(0.50f, 5)]
        [TestCase(0.75f, 6)]
        [TestCase(1.00f, 6)]   // R01: ceiling 6
        public void Charges_Lyra(float maxResource, int expected) =>
            Assert.AreEqual(expected, ResourceTranslation.Translate(AttributeFixtures.Lyra().Resource, maxResource).Count);

        // Orun: +1 Construct per 30%, max 6 (= +90%).
        [TestCase(0.29f, 3)]
        [TestCase(0.30f, 4)]
        [TestCase(0.60f, 5)]
        [TestCase(0.90f, 6)]   // float error guard: 0.9 / 0.3
        [TestCase(1.50f, 6)]
        public void Constructs_Orun(float maxResource, int expected) =>
            Assert.AreEqual(expected, ResourceTranslation.Translate(AttributeFixtures.Orun().Resource, maxResource).Count);

        // Rest: Lyra 0.4% Increased skill damage per 1% (01 3.8). 40% -> 1 step (25%), rest 15% -> +6%.
        [Test]
        public void Charges_RestBecomesBonus()
        {
            var r = ResourceTranslation.Translate(AttributeFixtures.Lyra().Resource, 0.40f);
            Assert.AreEqual(4, r.Count);
            Assert.AreEqual(0.15f, r.RestFraction, 0.0001f);
            Assert.AreEqual(0.06f, r.RestBonus, 0.0001f);
            Assert.AreEqual(RestBonusKind.ChargeSkillDamage, r.RestKind);
        }

        // Orun rest: 0.5% life and damage per 1%. 50% -> 1 step (30%), rest 20% -> +10%.
        [Test]
        public void Constructs_RestBecomesBonus()
        {
            var r = ResourceTranslation.Translate(AttributeFixtures.Orun().Resource, 0.50f);
            Assert.AreEqual(4, r.Count);
            Assert.AreEqual(0.20f, r.RestFraction, 0.0001f);
            Assert.AreEqual(0.10f, r.RestBonus, 0.0001f);
            Assert.AreEqual(RestBonusKind.ConstructLifeAndDamage, r.RestKind);
        }

        // R01 / 01 3.8: above the ceiling the excess still becomes Rest.
        [Test]
        public void Charges_ExcessAboveCeilingBecomesRest()
        {
            var r = ResourceTranslation.Translate(AttributeFixtures.Lyra().Resource, 1.00f);
            Assert.AreEqual(6, r.Count);
            Assert.AreEqual(0.25f, r.RestFraction, 0.0001f);
            Assert.AreEqual(0.10f, r.RestBonus, 0.0001f);
        }

        // R01 exception: ceiling 8 only when the caller passes it (Keystone/Augment/listed item).
        [Test]
        public void Charges_ExceptionRaisesCeilingToEight()
        {
            var rule = AttributeFixtures.Lyra().Resource;
            Assert.AreEqual(8, ResourceTranslation.Translate(rule, 1.25f, 8).Count);
            Assert.AreEqual(7, ResourceTranslation.Translate(rule, 1.00f, 7).Count);
            Assert.AreEqual(6, ResourceTranslation.Translate(rule, 1.25f).Count);   // no exception: stays 6
            Assert.AreEqual(8, ResourceTranslation.Translate(rule, 2.00f, 99).Count); // clamped to ExceptionCap
        }

        [Test]
        public void Constructs_ExceptionRaisesCeilingToEight()
        {
            var rule = AttributeFixtures.Orun().Resource;
            Assert.AreEqual(8, ResourceTranslation.Translate(rule, 1.50f, 8).Count);
            Assert.AreEqual(6, ResourceTranslation.Translate(rule, 1.50f).Count);
        }

        [Test]
        public void Override_BelowBaseCapIsIgnored() =>
            Assert.AreEqual(6, ResourceTranslation.Translate(AttributeFixtures.Lyra().Resource, 1.0f, 4).Count);

        [Test]
        public void NegativeMaximumResource_TreatedAsZero()
        {
            Assert.AreEqual(3, ResourceTranslation.Translate(AttributeFixtures.Lyra().Resource, -0.5f).Count);
            Assert.AreEqual(100f, ResourceTranslation.Translate(AttributeFixtures.Kael().Resource, -0.5f).Maximum, 0.001f);
        }
    }
}
