#if UNITY_EDITOR
using EntityId = Eclipse.Gameplay.Combat.EntityId;
using System.Collections;
using System.Collections.Generic;
using Eclipse.Core.Damage;
using Eclipse.Core.Ids;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Combat.Delivery
{
    /// <summary>M05 T8: projectiles (2.000 active, 0 B GC), zones, DoT snapshot and the Burning stub.</summary>
    public class DeliveryTests
    {
        private CombatEnv _env;

        [SetUp]
        public void SetUp() => _env = new CombatEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();

        private sealed class Directory : ICombatDirectory
        {
            public readonly Dictionary<int, CombatEntity> Map = new Dictionary<int, CombatEntity>();
            public IDamageReceiver Override;

            public bool TryGet(EntityId id, out ICombatEntity entity, out IDamageReceiver receiver)
            {
                if (Map.TryGetValue(id.Value, out var e)) { entity = e; receiver = Override ?? e; return true; }
                entity = null; receiver = null; return false;
            }
        }

        private sealed class Recorder : IDamageReceiver
        {
            private readonly CombatEntity _entity;
            public readonly List<float> Damage = new List<float>();
            public Recorder(CombatEntity e) { _entity = e; }
            public EntityId Id => _entity.Id;

            public DamageOutcome ReceiveHit(DamageInstance hit, Vector3 hitDirection)
            {
                Damage.Add(hit.TotalDamage);
                return new DamageOutcome(HitResult.Hit, hit.TotalDamage, hit.TotalDamage, 0f, false);
            }
        }

        [Test]
        public void Zone_ticks_every_30_ticks_for_its_duration()
        {
            _env.MakeTarget(CombatEnv.Far + new Vector3(1f, 0f, 1f), 0.5f, out var dummy);
            var scheduler = new TickScheduler();
            var zones = new ZoneRunner(new AreaDeliverer(new PhysicsAreaQuery(), _env.Events));
            scheduler.Register(zones);
            var source = _env.NewId();
            var spec = new AreaSpec(source, Team.Player, HitboxShape.Sector(5f, 360f), CombatEnv.Far, Vector3.forward, 100, 30,
                DamageType.Ember, 2f, 0f, 1f, 0, 7, TagMask.None, HitFlags.None);
            Assert.IsTrue(zones.Spawn(in spec));

            var frames = new List<int>();
            for (int i = 0; i < 110; i++)
            {
                scheduler.StepManual(1);
                for (int k = 0; k < _env.Events.Count<OnTickDealt>(); k++) frames.Add(_env.Events.Read<OnTickDealt>()[k].Frame);
                _env.Events.EndFrame();
            }
            Assert.AreEqual(3, frames.Count, "ticks at 30, 60 and 90 of a 100-tick zone; frames " + string.Join(",", frames));
            Assert.AreEqual(30, frames[1] - frames[0]);
            Assert.AreEqual(30, frames[2] - frames[1]);
            Assert.AreEqual(0, zones.ActiveCount, "zone expired");
            Assert.AreEqual(dummy.Health.Max - 6f, dummy.Health.Current, 0.01f);
        }

        [Test]
        public void Dot_damage_is_a_snapshot_taken_when_applied()
        {
            var attacker = _env.MakeEntity(Team.Player);
            var target = _env.MakeEntity(Team.Enemy, 100000f);
            var dir = new Directory();
            dir.Map[target.Id.Value] = target;
            var rec = new Recorder(target);
            dir.Override = rec;
            var dot = new DotRunner(dir, _env.Events);
            var scheduler = new TickScheduler();
            scheduler.Register(dot);

            var req = new DotRequest(attacker.Id, target.Id, DamageType.Ember, 5f, 30, 150, 0, 0.5f, 3, TagMask.None, HitFlags.None);
            Assert.IsTrue(dot.Apply(in req));
            scheduler.StepManual(40);
            // A buff granted after the application must not change the running DoT.
            attacker.Stats.SetBase(StatId.CriticalDamage, 5f);
            attacker.Stats.SetBase(StatId.AttackSpeed, 1f);
            scheduler.StepManual(35);
            Assert.AreEqual(2, rec.Damage.Count, "ticks at 30 and 60");
            foreach (float d in rec.Damage) Assert.AreEqual(5f, d, 1e-4f, "snapshot damage per tick");

            // A weaker re-application keeps the stronger snapshot; a stronger one replaces it.
            var weak = new DotRequest(attacker.Id, target.Id, DamageType.Ember, 3f, 30, 150, 0, 0.5f, 3, TagMask.None, HitFlags.None);
            dot.Apply(in weak);
            scheduler.StepManual(30);
            Assert.AreEqual(5f, rec.Damage[rec.Damage.Count - 1], 1e-4f);
            var strong = new DotRequest(attacker.Id, target.Id, DamageType.Ember, 8f, 30, 150, 0, 0.5f, 3, TagMask.None, HitFlags.None);
            dot.Apply(in strong);
            scheduler.StepManual(30);
            Assert.AreEqual(8f, rec.Damage[rec.Damage.Count - 1], 1e-4f);
            Assert.AreEqual(1, dot.ActiveCount, "same skill and source: a single DoT");
        }

        [Test]
        public void Burning_stub_caps_at_10_stacks_and_expires()
        {
            var burning = StatusStubDefinition.BurningProvisional();
            Assert.AreEqual(10, burning.MaxStacks);
            var target = _env.NewId();
            var stub = new StatusStub(target, new[] { burning }, _env.Events);
            var id = new StableId(burning.StatusId);
            var scheduler = new TickScheduler();
            scheduler.Register(stub);

            for (int i = 0; i < 15; i++)
                Assert.IsTrue(stub.TryApply(new StatusRequest(id, _env.NewId(), target, 0, 0, 1, 1f, false, HitFlags.None)));
            Assert.AreEqual(10, stub.GetStacks(id));
            Assert.AreEqual(10 * burning.DotFractionPerStackPerSecond, stub.GetDotFractionPerSecond(id), 1e-5f);
            Assert.AreEqual(burning.DefaultDurationFrames, stub.GetRemainingFrames(id));

            scheduler.StepManual(burning.DefaultDurationFrames);
            Assert.AreEqual(0, stub.GetStacks(id), "expired after the default duration");
            Assert.AreEqual(1, _env.Events.Count<OnStatusExpired>());
            Assert.IsFalse(stub.TryApply(new StatusRequest(new StableId("status.unknown"), _env.NewId(), target, 0, 0, 1, 1f, false, HitFlags.None)));
        }

        [UnityTest]
        public IEnumerator Two_thousand_active_projectiles_allocate_nothing_per_frame()
        {
            var manager = new ProjectileManager(2048, null, null);
            try
            {
                var scheduler = new TickScheduler();
                scheduler.Register(manager);
                var source = _env.NewId();
                int n = 0;
                for (int x = 0; x < 50; x++)
                    for (int z = 0; z < 40; z++)
                    {
                        var spec = new ProjectileSpec(source, Team.Player, new Vector3(1000f + x * 2f, 500f, 1000f + z * 2f), Vector3.up, 5f, 0.1f,
                            100000f, 0, DamageType.Physical, 1f, 0f, 1f, 0, 0, TagMask.None, HitFlags.None);
                        if (manager.Spawn(in spec) >= 0) n++;
                    }
                Assert.AreEqual(2000, n);
                Assert.AreEqual(2000, manager.ActiveCount);
                Assert.AreEqual(0, manager.DroppedSpawns);

                var result = new GcResult();
                yield return GcProbe.Measure(result, () => scheduler.StepManual(1), 30, 180);
                Assert.AreEqual(2000, manager.ActiveCount, "all projectiles still flying");
                GcProbe.AssertClean(result);
            }
            finally
            {
                manager.Dispose();
            }
        }
    }
}
#endif
