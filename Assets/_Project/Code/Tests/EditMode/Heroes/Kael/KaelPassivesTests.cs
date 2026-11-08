using Eclipse.Core.Stats;
using Eclipse.Data.Heroes;
using Eclipse.Gameplay.Heroes.Kael;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.EditMode.Heroes.Kael
{
    /// <summary>Passives of 08 4.2.</summary>
    public sealed class KaelPassivesTests
    {
        private const float Tol = 1e-4f;

        private ResourceDefinition _def;
        private StatBlock _stats;
        private KaelPassives _p;

        [SetUp]
        public void SetUp()
        {
            _def = KaelFuryTests.MakeDefinition();
            _stats = new StatBlock(new StatRuleTable());
            _p = new KaelPassives(_stats, KaelPassiveRules.FromDefinition(_def));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_def);

        private float Inc(StatId s) => _stats.SumBucket(s, ModBucket.Increased, default, null);

        [Test]
        public void Embers_ZeroFury_NoBonus()
        {
            _p.Refresh(0f, 1000f, 1000f);
            Assert.AreEqual(0f, Inc(StatId.PhysicalDamage), Tol);
            Assert.AreEqual(0f, Inc(StatId.EmberDamage), Tol);
        }

        [Test]
        public void Embers_TwoPercentPerTenFury_OnPhysicalAndEmber()
        {
            _p.Refresh(35f, 1000f, 1000f);
            Assert.AreEqual(0.06f, Inc(StatId.PhysicalDamage), Tol);
            Assert.AreEqual(0.06f, Inc(StatId.EmberDamage), Tol);
            Assert.AreEqual(0f, Inc(StatId.FrostDamage), Tol);
        }

        [Test]
        public void Embers_20PercentAt100_30PercentAt150()
        {
            _p.Refresh(100f, 1000f, 1000f);
            Assert.AreEqual(0.20f, Inc(StatId.PhysicalDamage), Tol);
            _p.Refresh(150f, 1000f, 1000f);
            Assert.AreEqual(0.30f, Inc(StatId.PhysicalDamage), Tol);
            Assert.AreEqual(0.30f, Inc(StatId.EmberDamage), Tol);
        }

        [Test]
        public void Embers_FollowsFuryDownAndAvoidsDuplicates()
        {
            _p.Refresh(80f, 1000f, 1000f);
            _p.Refresh(80f, 1000f, 1000f);
            Assert.AreEqual(0.16f, Inc(StatId.PhysicalDamage), Tol);
            _p.Refresh(19.99999f, 1000f, 1000f);
            Assert.AreEqual(0.04f, Inc(StatId.PhysicalDamage), Tol);
            _p.Refresh(5f, 1000f, 1000f);
            Assert.AreEqual(0f, Inc(StatId.PhysicalDamage), Tol);
        }

        [Test]
        public void Embers_ChangeInvalidatesOnlyAffectedStats()
        {
            _p.Refresh(10f, 1000f, 1000f);
            _stats.Evaluate(StatId.FrostDamage);
            _stats.Evaluate(StatId.PhysicalDamage);
            _p.Refresh(20f, 1000f, 1000f);
            Assert.IsFalse(_stats.IsDirty(StatId.FrostDamage));
            Assert.IsTrue(_stats.IsDirty(StatId.PhysicalDamage));
        }

        [Test]
        public void Resolve_InactiveAtOrAbove35Percent()
        {
            _p.Refresh(0f, 350f, 1000f);
            Assert.IsFalse(_p.ResolveActive);
            Assert.AreEqual(0f, _stats.Evaluate(StatId.DamageReduction), Tol);
        }

        [Test]
        public void Resolve_Below35Percent_Gives15PercentLessDamageAnd25PercentFuryGeneration()
        {
            _p.Refresh(0f, 349f, 1000f);
            Assert.IsTrue(_p.ResolveActive);
            Assert.AreEqual(0.15f, _stats.Evaluate(StatId.DamageReduction), Tol);
            Assert.AreEqual(0.25f, _stats.Evaluate(StatId.ResourceGeneration), Tol);
        }

        [Test]
        public void Resolve_EndsWhenHealed()
        {
            _p.Refresh(0f, 100f, 1000f);
            _p.Refresh(0f, 600f, 1000f);
            Assert.IsFalse(_p.ResolveActive);
            Assert.AreEqual(0f, _stats.Evaluate(StatId.DamageReduction), Tol);
            Assert.AreEqual(0f, _stats.Evaluate(StatId.ResourceGeneration), Tol);
        }

        [Test]
        public void Resolve_BoostsFuryGain_OnlyWhileActive()
        {
            var fury = new KaelFury(new Eclipse.Gameplay.Heroes.ResourcePool(Eclipse.Gameplay.Heroes.ResourceRules.FromDefinition(_def)),
                FuryGainRules.FromDefinition(_def), _stats);
            _p.Refresh(0f, 100f, 1000f);
            Assert.AreEqual(6.25f, fury.OnBasicHit(1), Tol);
            _p.Refresh(6.25f, 900f, 1000f);
            Assert.AreEqual(5f, fury.OnBasicHit(1), Tol);
        }

        [Test]
        public void Passives_AreIndependentSources()
        {
            _p.Refresh(50f, 100f, 1000f);
            _p.Refresh(0f, 100f, 1000f);
            Assert.IsTrue(_p.ResolveActive);
            Assert.AreEqual(0.15f, _stats.Evaluate(StatId.DamageReduction), Tol);
            Assert.AreEqual(0f, Inc(StatId.PhysicalDamage), Tol);
        }
    }
}
