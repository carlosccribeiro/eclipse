using Eclipse.Core.Damage;
using Eclipse.Core.Stats;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Simulation;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.EditMode.Skills.Effects
{
    /// <summary>GainResource, Shield, GroundZone and Tether with fake targets.</summary>
    public sealed class ResourceShieldZoneTetherTests
    {
        private EffectRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new EffectRig();
        }

        // ---- GainResource ----

        [Test]
        public void GainResource_GrantsTheParamAmountOnce()
        {
            _rig.Begin(EffectRig.Spec(1.8f, ("fury_gain", 15f)));
            new GainResourceEffect("fury_gain").Apply(_rig.Ctx, _rig.Caster);

            Assert.AreEqual(15f, _rig.Resource.Gained);
        }

        [Test]
        public void GainResource_ScalesByProcCoefficientWhenAsked()
        {
            _rig.Begin(EffectRig.Spec());
            new GainResourceEffect(null, 10f, scaleByProcCoefficient: true, procCoefficient: 0.5f).Apply(_rig.Ctx, _rig.Caster);

            Assert.AreEqual(5f, _rig.Resource.Gained);
        }

        [Test]
        public void GainResource_WithoutResourceDoesNothing()
        {
            _rig.Ctx.Resource = null;
            _rig.Begin(EffectRig.Spec());
            Assert.DoesNotThrow(() => new GainResourceEffect(null, 10f).Apply(_rig.Ctx, _rig.Caster));
        }

        [Test]
        public void GainResource_AsHitTargetEffectRunsOncePerTargetHit()
        {
            var a = _rig.AddEnemy(0f, 1f);
            var b = _rig.AddEnemy(0f, 2f);
            var fx = (SkillEffect)new GainResourceEffect(null, 1f).At(EffectTiming.Impact, EffectTargeting.HitTargets);
            _rig.Begin(EffectRig.Spec(), 100, new[] { fx });
            var shape = HitboxShape.Sector(5f, 180f);
            _rig.Ctx.Strike(in shape, Vector3.zero, Vector3.forward);

            Assert.AreEqual(2f, _rig.Resource.Gained);
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
        }

        // ---- Shield ----

        [Test]
        public void Shield_IsAFractionOfMaxHealthAmplifiedByShieldStrength()
        {
            var stats = _rig.CasterEntity.Stats;
            stats.SetBase(StatId.MaximumHealth, 1000f);
            _rig.Begin(EffectRig.Spec(0f, ("shield_pct", 30f)));
            new ShieldEffect(null, 0f, "shield_pct", 0f).Apply(_rig.Ctx, _rig.Caster);
            Assert.AreEqual(300f, _rig.Caster.Shield.Points, 1e-3f);

            stats.AddMod(stats.CreateSource(), new StatMod(StatId.ShieldStrength, ModBucket.Flat, 0.5f));
            _rig.Caster.Shield.Clear();
            new ShieldEffect(null, 0f, "shield_pct", 0f).Apply(_rig.Ctx, _rig.Caster);
            Assert.AreEqual(450f, _rig.Caster.Shield.Points, 1e-3f);
        }

        [Test]
        public void Shield_FlatPointsAddUp()
        {
            _rig.Begin(EffectRig.Spec());
            new ShieldEffect(null, 40f, null, 0f).Apply(_rig.Ctx, _rig.Caster);
            Assert.AreEqual(40f, _rig.Caster.Shield.Points, 1e-3f);
        }

        // ---- GroundZone ----

        private ZoneRunner SetUpZones()
        {
            var zones = new ZoneRunner(new AreaDeliverer(_rig.Query, _rig.Events));
            _rig.Ctx.Zones = zones;
            return zones;
        }

        [Test]
        public void GroundZone_SpawnsAPersistentZoneThatTicksEveryHalfSecond()
        {
            ZoneRunner zones = SetUpZones();
            var enemy = _rig.AddEnemy(1f, 0f);
            _rig.Begin(EffectRig.Spec(1.8f, ("radius_m", 3f), ("zone_s", 2f), ("zone_coef_pct", 50f)));
            _rig.Ctx.ImpactPoint = Vector3.zero;
            new GroundZoneEffect(DamageType.Ember, "radius_m", 0f, "zone_s", 0f, "zone_coef_pct").Apply(_rig.Ctx, null);

            Assert.AreEqual(1, zones.ActiveCount);
            for (int f = 101; f <= 129; f++) zones.Tick(new SimulationFrame(f));
            Assert.AreEqual(0, EffectRig.Rx(enemy).Hits.Count);
            zones.Tick(new SimulationFrame(130)); // 30 frames = 0.5 s

            var hits = EffectRig.Rx(enemy).Hits;
            Assert.AreEqual(1, hits.Count);
            Assert.AreEqual(HitKind.Tick, hits[0].Kind);
            Assert.AreEqual(EffectRig.WeaponPower * 0.5f, hits[0].Total, 1e-3f);
            Assert.AreEqual(1, hits[0].Generation, "a zone is a generated entity: origin + 1 (11 3.6.5)");
            Assert.AreEqual(0.25f, hits[0].ProcCoefficient, "persistent area tick (11 3.6.5)");
        }

        [Test]
        public void GroundZone_EndsAfterItsDuration()
        {
            ZoneRunner zones = SetUpZones();
            _rig.Begin(EffectRig.Spec(1.8f, ("radius_m", 3f), ("zone_s", 2f)));
            new GroundZoneEffect(DamageType.Ember, "radius_m", 0f, "zone_s", 0f, null).Apply(_rig.Ctx, null);
            Assert.AreEqual(1, zones.ActiveCount);

            for (int f = 101; f <= 220; f++) zones.Tick(new SimulationFrame(f)); // 2 s = 120 frames
            Assert.AreEqual(0, zones.ActiveCount);
        }

        [Test]
        public void GroundZone_WithoutDamageDoesNotSpawn()
        {
            ZoneRunner zones = SetUpZones();
            _rig.Begin(EffectRig.Spec(0f, ("radius_m", 3f), ("zone_s", 2f)));
            new GroundZoneEffect(DamageType.Ember, "radius_m", 0f, "zone_s", 0f, null).Apply(_rig.Ctx, null);
            Assert.AreEqual(0, zones.ActiveCount);
        }

        // ---- Tether ----

        private SkillSpec TetherSpec() =>
            EffectRig.Spec(1.2f, ("root_s", 2f), ("break_distance_m", 10f), ("break_damage_coef_pct", 60f));

        private TetherEffect NewTether() =>
            new TetherEffect("root_s", 0f, "break_distance_m", 0f, new DamageEffect(DamageType.Physical, "break_damage_coef_pct"));

        private (TetherSet set, SkillTarget enemy) Linked()
        {
            var set = new TetherSet();
            _rig.Ctx.Tethers = set;
            var enemy = _rig.AddEnemy(0f, 5f);
            _rig.Begin(TetherSpec());
            NewTether().Apply(_rig.Ctx, enemy);
            return (set, enemy);
        }

        [Test]
        public void Tether_LinksTheTargetAndHoldsWhileClose()
        {
            var (set, enemy) = Linked();
            Assert.IsTrue(set.IsTethered(enemy.Id));

            enemy.Entity = MoveTo(enemy, 0f, 9.9f);
            _rig.Ctx.Frame = 110;
            set.Tick(_rig.Ctx);

            Assert.IsTrue(set.IsTethered(enemy.Id));
            Assert.AreEqual(0, EffectRig.Rx(enemy).Hits.Count);
        }

        [Test]
        public void Tether_BreaksBeyondTheDistanceAndDealsTheBreakDamage()
        {
            var (set, enemy) = Linked();
            enemy.Entity = MoveTo(enemy, 0f, 10.5f);
            _rig.Ctx.Frame = 110;
            set.Tick(_rig.Ctx);

            Assert.IsFalse(set.IsTethered(enemy.Id));
            Assert.AreEqual(1, set.BreakCount);
            Assert.AreEqual(TetherBreakReason.Distance, set.LastBreakReason);
            var hits = EffectRig.Rx(enemy).Hits;
            Assert.AreEqual(1, hits.Count);
            Assert.AreEqual(EffectRig.WeaponPower * 0.6f, hits[0].Total, 1e-3f); // 60% of Weapon Power
            Assert.AreEqual(0, hits[0].Generation);
        }

        [Test]
        public void Tether_BreaksWhenTheDurationEnds()
        {
            var (set, enemy) = Linked();
            _rig.Ctx.Frame = 219;
            set.Tick(_rig.Ctx);
            Assert.IsTrue(set.IsTethered(enemy.Id));

            _rig.Ctx.Frame = 220; // root_s = 2 s from frame 100
            set.Tick(_rig.Ctx);
            Assert.IsFalse(set.IsTethered(enemy.Id));
            Assert.AreEqual(TetherBreakReason.Expired, set.LastBreakReason);
            Assert.AreEqual(1, EffectRig.Rx(enemy).Hits.Count);
        }

        [Test]
        public void Tether_DeadTargetDropsTheLinkWithoutBreakDamage()
        {
            var (set, enemy) = Linked();
            ((FakeEntity)enemy.Entity).IsAlive = false;
            _rig.Ctx.Frame = 110;
            set.Tick(_rig.Ctx);

            Assert.IsFalse(set.IsTethered(enemy.Id));
            Assert.AreEqual(0, set.BreakCount);
            Assert.AreEqual(0, EffectRig.Rx(enemy).Hits.Count);
        }

        [Test]
        public void Tether_BreakAfterTheCastKeepsTheContextOfTheNextCast()
        {
            var (set, enemy) = Linked();
            var other = EffectRig.Spec("skill.test.other", "Attack|Ember", 1f, new SkillFrames(1, 1, 1));
            _rig.Begin(other, 150);
            enemy.Entity = MoveTo(enemy, 0f, 11f);
            _rig.Ctx.Frame = 151;
            set.Tick(_rig.Ctx);

            Assert.AreEqual(1, EffectRig.Rx(enemy).Hits.Count, "break damage still uses the chains skill coefficients");
            Assert.AreSame(other, _rig.Ctx.Cast.Spec, "the running cast is restored after the break");
        }

        private static FakeEntity MoveTo(SkillTarget t, float x, float z)
        {
            var e = (FakeEntity)t.Entity;
            e.Position = new Vector3(x, 0f, z);
            return e;
        }
    }
}
