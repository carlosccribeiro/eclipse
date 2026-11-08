using Eclipse.Core.Damage;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.Skills.Effects
{
    /// <summary>DamageEffect with fake targets: Base = Weapon Power x coefficient, then the Core pipeline of 06 3.2. Fixture: Weapon Power 10, Cleaving 220%.</summary>
    public sealed class DamageEffectTests
    {
        private EffectRig _rig;
        private SkillTarget _enemy;
        private FakeReceiver Rx => EffectRig.Rx(_enemy);

        [SetUp]
        public void SetUp()
        {
            _rig = new EffectRig();
            _enemy = _rig.AddEnemy(0f, 2f);
        }

        private void Cast(SkillSpec spec) => _rig.Begin(spec);

        [Test]
        public void Damage_IsWeaponPowerTimesSkillCoefficient()
        {
            Cast(EffectRig.Spec(2.2f));
            new DamageEffect(DamageType.Physical).Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(1, Rx.Hits.Count);
            Assert.AreEqual(22f, Rx.Hits[0].Total, 1e-3f);
            Assert.AreEqual(22f, Rx.Hits[0].Physical, 1e-3f);
        }

        [Test]
        public void Damage_UsesTheCoefficientOfTheCastIncludingThePerResourcePart()
        {
            var spec = EffectRig.Spec(6f);
            var info = new SkillCastInfo(spec, 100, 80f, 6f + 0.06f * 80f); // Worldbreaker: 600% + 6% x 80 Fury
            _rig.Ctx.BeginCast(in info, 100, null, 1);
            new DamageEffect(DamageType.Physical).Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(EffectRig.WeaponPower * 10.8f, Rx.Hits[0].Total, 1e-2f);
        }

        [Test]
        public void Damage_AppliesSkillAndAreaIncreasedFromTheCasterStats()
        {
            var stats = _rig.CasterEntity.Stats;
            var h = stats.CreateSource();
            stats.AddMod(h, new StatMod(StatId.SkillDamage, ModBucket.Increased, 0.5f));
            stats.AddMod(h, new StatMod(StatId.AreaDamage, ModBucket.Increased, 0.2f));
            Cast(EffectRig.Spec(2.2f));
            new DamageEffect(DamageType.Physical).Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(22f * 1.7f, Rx.Hits[0].Total, 1e-2f);
        }

        [Test]
        public void Damage_NamedParamCoefficientAndContextMultiplier()
        {
            Cast(EffectRig.Spec(2.5f, ("counter_coef_pct", 250f)));
            _rig.Ctx.DamageMultiplier = 1.5f; // Parry counter +50%
            new DamageEffect(DamageType.Physical, "counter_coef_pct").Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(EffectRig.WeaponPower * 2.5f * 1.5f, Rx.Hits[0].Total, 1e-3f);
        }

        [Test]
        public void Damage_FlatDamageJoinsTheBase()
        {
            Cast(EffectRig.Spec(2.2f));
            _rig.Ctx.FlatDamage = 6f; // e.g. 30% of absorbed damage on the counter
            new DamageEffect(DamageType.Physical).Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(28f, Rx.Hits[0].Total, 1e-3f);
        }

        [Test]
        public void Damage_TargetArmorAndVulnerabilityApply()
        {
            _enemy.ArmorPercent = 0.5f;
            _enemy.Vulnerability.Apply(1, 0.2f, 1, 1, 60, 100);
            Cast(EffectRig.Spec(2.2f));
            new DamageEffect(DamageType.Physical).Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(22f * 0.5f * 1.2f, Rx.Hits[0].Total, 1e-3f);
        }

        [Test]
        public void Damage_ConversionSplitsTheComponents()
        {
            Cast(EffectRig.Spec(2.2f));
            var fx = new DamageEffect(DamageType.Physical, conversions: new[] { new DamageTransfer(DamageType.Physical, DamageType.Ruin, 0.4f) });
            fx.Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(13.2f, Rx.Hits[0].Physical, 1e-3f);
            Assert.AreEqual(8.8f, Rx.Hits[0].Ruin, 1e-3f);
        }

        [Test]
        public void Damage_GainAsExtraAddsAComponentWithoutTakingFromTheBase()
        {
            Cast(EffectRig.Spec(2.2f));
            var fx = new DamageEffect(DamageType.Physical, gainAsExtra: new[] { new DamageTransfer(DamageType.Physical, DamageType.Ember, 0.5f) });
            fx.Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(22f, Rx.Hits[0].Physical, 1e-3f);
            Assert.AreEqual(11f, Rx.Hits[0].Ember, 1e-3f);
        }

        [Test]
        public void Damage_DirectCastIsGenerationZeroWithSkillTagsAndOwnSource()
        {
            var spec = EffectRig.Spec(2.2f);
            Cast(spec);
            new DamageEffect(DamageType.Physical, procCoefficient: 0.5f).Apply(_rig.Ctx, _enemy);

            HitSnapshot s = Rx.Hits[0];
            Assert.AreEqual(0, s.Generation);
            Assert.AreEqual(spec.Tags.Bits, s.Tags.Bits);
            Assert.AreEqual(_rig.Caster.Id.Value, s.SourceId);
            Assert.AreEqual(HitKind.Hit, s.Kind);
            Assert.AreEqual(0.5f, s.ProcCoefficient);
        }

        [Test]
        public void Damage_RunByATriggerIsOneGenerationDeeperAndFlagged()
        {
            Cast(EffectRig.Spec(2.2f));
            _rig.Ctx.Generation = 1;
            _rig.Ctx.ExtraFlags = HitFlags.IsTriggered;
            new DamageEffect(DamageType.Physical).Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(1, Rx.Hits[0].Generation);
            Assert.IsTrue(Rx.Hits[0].Flags.Has(HitFlags.IsTriggered));
        }

        [Test]
        public void Damage_PublishesOnHitDealtAndAddsStaggerScaledByStaggerDamage()
        {
            var stats = _rig.CasterEntity.Stats;
            stats.AddMod(stats.CreateSource(), new StatMod(StatId.StaggerDamage, ModBucket.Flat, 0.25f));
            Cast(EffectRig.Spec(2.2f));
            new DamageEffect(DamageType.Physical, staggerValue: 8f).Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(1, _rig.Events.Count<OnHitDealt>());
            Assert.AreEqual(22f, _rig.Events.Read<OnHitDealt>()[0].Value, 1e-3f);
            Assert.AreEqual(10f, ((FakeStaggerable)_enemy.Stagger).Added, 1e-3f);
            Assert.AreEqual(1, _rig.Ctx.TargetsHit);
        }

        [Test]
        public void Damage_NegatedHitPublishesNothing()
        {
            Rx.Result = HitResult.Parried;
            Cast(EffectRig.Spec(2.2f));
            new DamageEffect(DamageType.Physical).Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(0, _rig.Events.Count<OnHitDealt>());
            Assert.AreEqual(0, _rig.Ctx.TargetsHit);
        }

        [Test]
        public void Damage_DeadOrNullTargetIsSkipped()
        {
            Cast(EffectRig.Spec(2.2f));
            var fx = new DamageEffect(DamageType.Physical);
            _enemy.Entity = new FakeEntity(99, Team.Enemy, UnityEngine.Vector3.zero) { IsAlive = false };
            fx.Apply(_rig.Ctx, _enemy);
            fx.Apply(_rig.Ctx, null);

            Assert.AreEqual(0, Rx.Hits.Count);
        }
    }
}
