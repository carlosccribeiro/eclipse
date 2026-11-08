using System.Collections.Generic;
using Eclipse.Core.Damage;
using Eclipse.Core.Ids;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Physics;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.EditMode.Gameplay.Delivery
{
    // Fixtures mirror doc values (06 3.2: tick 0.5 s = 30 ticks; 07 4: Burning 4 s, 10 stacks, 8% per stack per second).
    // Runtime code receives them as injected data.
    internal sealed class FakeEntity : ICombatEntity
    {
        public EntityId Id { get; }
        public Team Team { get; }
        public bool IsAlive { get; set; } = true;
        public Vector3 Position { get; set; }
        public EntityClock Clock { get; } = new EntityClock();
        public StatBlock Stats { get; } = new StatBlock(new StatRuleTable());
        public FakeEntity(int id, Team team, Vector3 pos = default) { Id = new EntityId(id); Team = team; Position = pos; }
    }

    internal sealed class FakeReceiver : IDamageReceiver
    {
        public EntityId Id { get; }
        public int Hits;
        public float Total;
        public float LastDamage;
        public HitKind LastKind;
        public bool LastCrit;
        public int LastGeneration;
        public FakeReceiver(int id) { Id = new EntityId(id); }

        public DamageOutcome ReceiveHit(DamageInstance hit, Vector3 hitDirection)
        {
            Hits++;
            LastDamage = hit.TotalDamage;
            Total += LastDamage;
            LastKind = hit.Kind;
            LastCrit = hit.IsCrit;
            LastGeneration = hit.Generation;
            return new DamageOutcome(HitResult.Hit, LastDamage, LastDamage, 0f, false);
        }
    }

    internal sealed class FakeDirectory : ICombatDirectory
    {
        private readonly Dictionary<int, FakeEntity> _entities = new Dictionary<int, FakeEntity>();
        private readonly Dictionary<int, FakeReceiver> _receivers = new Dictionary<int, FakeReceiver>();

        public FakeReceiver Add(FakeEntity e)
        {
            var r = new FakeReceiver(e.Id.Value);
            _entities[e.Id.Value] = e;
            _receivers[e.Id.Value] = r;
            return r;
        }

        public bool TryGet(EntityId id, out ICombatEntity entity, out IDamageReceiver receiver)
        {
            entity = null; receiver = null;
            if (!_entities.TryGetValue(id.Value, out FakeEntity e)) return false;
            entity = e; receiver = _receivers[id.Value];
            return true;
        }
    }

    internal static class Run
    {
        public static void Frames(IFixedTickable t, int frames, ref int frame)
        {
            for (int i = 0; i < frames; i++) { frame++; var f = new SimulationFrame(frame); t.Tick(in f); }
        }
    }

    public class DotRunnerTests
    {
        private static readonly EntityId Src = new EntityId(10);
        private static readonly EntityId Tgt = new EntityId(20);

        private static DotRequest Dot(float perTick, int duration = 240, int group = 5, int generation = 0)
            => new DotRequest(Src, Tgt, DamageType.Ember, perTick, 30, duration, generation, 0.5f, group, TagMask.None, HitFlags.None);

        private static (DotRunner runner, FakeReceiver rx, FakeEntity ent) Setup()
        {
            var dir = new FakeDirectory();
            var ent = new FakeEntity(20, Team.Enemy);
            var rx = dir.Add(ent);
            return (new DotRunner(dir), rx, ent);
        }

        [Test]
        public void TicksEvery30FramesForTheDuration()
        {
            var (r, rx, _) = Setup();
            int f = 0;
            Assert.IsTrue(r.Apply(Dot(10f)));
            Run.Frames(r, 29, ref f);
            Assert.AreEqual(0, rx.Hits);
            Run.Frames(r, 1, ref f);
            Assert.AreEqual(1, rx.Hits, "first tick at frame 30");
            Run.Frames(r, 30, ref f);
            Assert.AreEqual(2, rx.Hits);
            Run.Frames(r, 180, ref f);
            Assert.AreEqual(8, rx.Hits, "240 / 30 ticks, last one on the final frame");
            Assert.AreEqual(0, r.ActiveCount);
            Run.Frames(r, 60, ref f);
            Assert.AreEqual(8, rx.Hits);
        }

        [Test]
        public void TicksNeverCritAndAreTickKind()
        {
            var (r, rx, _) = Setup();
            int f = 0;
            r.Apply(Dot(10f, generation: 1));
            Run.Frames(r, 30, ref f);
            Assert.AreEqual(HitKind.Tick, rx.LastKind);
            Assert.IsFalse(rx.LastCrit);
            Assert.AreEqual(1, rx.LastGeneration);
            Assert.AreEqual(10f, rx.LastDamage);
        }

        [Test]
        public void SnapshotDoesNotChangeWhenBuffAppliedLater()
        {
            var (r, rx, ent) = Setup();
            int f = 0;
            r.Apply(Dot(10f));
            ent.Stats.AddMod(ent.Stats.CreateSource(), new StatMod(StatId.DamageOverTime, ModBucket.Increased, 1.0f)); // +100% later
            Run.Frames(r, 60, ref f);
            Assert.AreEqual(2, rx.Hits);
            Assert.AreEqual(20f, rx.Total, 0.0001f, "damage per tick stays the snapshot (10)");
        }

        [Test]
        public void SameSkillAndSourceDoesNotStackStrongerWinsAndRenews()
        {
            var (r, rx, _) = Setup();
            int f = 0;
            r.Apply(Dot(10f));
            Run.Frames(r, 120, ref f);                 // 4 ticks done
            Assert.IsTrue(r.Apply(Dot(6f)));           // weaker: ignored value, duration renewed
            Assert.AreEqual(1, r.ActiveCount);
            Run.Frames(r, 30, ref f);
            Assert.AreEqual(10f, rx.LastDamage);
            Assert.IsTrue(r.Apply(Dot(15f)));          // stronger replaces
            Assert.AreEqual(1, r.ActiveCount);
            Run.Frames(r, 30, ref f);
            Assert.AreEqual(15f, rx.LastDamage);
            Assert.AreEqual(6, rx.Hits, "one tick per interval, never two");
            Run.Frames(r, 240, ref f);
            Assert.AreEqual(0, r.ActiveCount, "renewed duration ended");
        }

        [Test]
        public void DifferentSkillOrSourceStacksIndependently()
        {
            var (r, rx, _) = Setup();
            int f = 0;
            r.Apply(Dot(10f, group: 5));
            r.Apply(Dot(10f, group: 6));
            r.Apply(new DotRequest(new EntityId(11), Tgt, DamageType.Ember, 10f, 30, 240, 0, 1f, 5, TagMask.None, HitFlags.None));
            Assert.AreEqual(3, r.ActiveCount);
            Run.Frames(r, 30, ref f);
            Assert.AreEqual(3, rx.Hits);
        }

        [Test]
        public void StopsWhenTargetDiesOrIsGone()
        {
            var (r, rx, ent) = Setup();
            int f = 0;
            r.Apply(Dot(10f));
            ent.IsAlive = false;
            Run.Frames(r, 30, ref f);
            Assert.AreEqual(0, rx.Hits);
            Assert.AreEqual(0, r.ActiveCount);
        }

        [Test]
        public void InvalidRequestsAreRejected()
        {
            var (r, _, _) = Setup();
            Assert.IsFalse(r.Apply(new DotRequest(Src, Tgt, DamageType.Ember, 1f, 0, 240, 0, 1f, 1, TagMask.None, HitFlags.None)));
            Assert.IsFalse(r.Apply(Dot(1f, duration: 0)));
        }
    }

    public class StatusStubTests
    {
        private static readonly StableId Burning = new StableId("status.burning");
        private static StatusRequest Req(int stacks, int duration = 0, StableId? id = null)
            => new StatusRequest(id ?? Burning, new EntityId(1), new EntityId(2), 0, duration, stacks, 1f, true, HitFlags.None);

        private static StatusStub Make() => new StatusStub(new EntityId(2), new[] { StatusStubDefinition.BurningProvisional() });

        [Test]
        public void BurningProvisionalMatches07()
        {
            StatusStubDefinition d = StatusStubDefinition.BurningProvisional();
            Assert.AreEqual(10, d.MaxStacks);
            Assert.AreEqual(240, d.DefaultDurationFrames);
            Assert.AreEqual(0.08f, d.DotFractionPerStackPerSecond, 1e-6f);
        }

        [Test]
        public void StacksAreCappedAtTen()
        {
            var s = Make();
            Assert.IsTrue(s.TryApply(Req(7)));
            Assert.IsTrue(s.TryApply(Req(7)));
            Assert.AreEqual(10, s.GetStacks(Burning));
            Assert.AreEqual(0.8f, s.GetDotFractionPerSecond(Burning), 1e-5f, "80% DRef/s at 10 stacks");
        }

        [Test]
        public void DurationRenewsAndExpiresAfterFourSeconds()
        {
            var s = Make();
            int f = 0;
            s.TryApply(Req(1));
            Run.Frames(s, 200, ref f);
            Assert.AreEqual(1, s.GetStacks(Burning));
            s.TryApply(Req(1));                       // renews to 240
            Run.Frames(s, 239, ref f);
            Assert.AreEqual(2, s.GetStacks(Burning));
            Run.Frames(s, 1, ref f);
            Assert.AreEqual(0, s.GetStacks(Burning));
        }

        [Test]
        public void UnknownStatusOrZeroStacksRejectedAndRemoveClears()
        {
            var s = Make();
            Assert.IsFalse(s.TryApply(Req(1, id: new StableId("status.chill"))));
            Assert.IsFalse(s.TryApply(Req(0)));
            s.TryApply(Req(3));
            s.Remove(Burning);
            Assert.AreEqual(0, s.GetStacks(Burning));
        }

        [Test]
        public void RequestDurationOverridesDefault()
        {
            var s = Make();
            int f = 0;
            s.TryApply(Req(1, duration: 10));
            Run.Frames(s, 10, ref f);
            Assert.AreEqual(0, s.GetStacks(Burning));
        }
    }

    public class TimedModifierTests
    {
        private static StatMod Vuln(float v) => new StatMod(StatId.MovementSpeed, ModBucket.More, v);

        [Test]
        public void AppliesForDurationThenExpires()
        {
            var stats = new StatBlock(new StatRuleTable());
            stats.SetBase(StatId.MovementSpeed, 10f);
            var t = new TimedModifierSet(stats);
            int f = 0;
            t.Apply(Vuln(0.5f), 60);
            Assert.AreEqual(15f, stats.Evaluate(StatId.MovementSpeed), 1e-4f);
            Run.Frames(t, 59, ref f);
            Assert.AreEqual(15f, stats.Evaluate(StatId.MovementSpeed), 1e-4f);
            Run.Frames(t, 1, ref f);
            Assert.AreEqual(10f, stats.Evaluate(StatId.MovementSpeed), 1e-4f);
            Assert.AreEqual(0, t.ActiveCount);
        }

        [Test]
        public void SourceCanBeRemovedEarly()
        {
            var stats = new StatBlock(new StatRuleTable());
            stats.SetBase(StatId.MovementSpeed, 10f);
            var t = new TimedModifierSet(stats);
            var h = t.Apply(Vuln(0.5f), 600);
            Assert.IsTrue(t.Remove(h));
            Assert.AreEqual(10f, stats.Evaluate(StatId.MovementSpeed), 1e-4f);
            Assert.IsFalse(t.Remove(h));
        }

        [Test]
        public void RefreshKeyReplacesAndRenewsInsteadOfStacking()
        {
            var stats = new StatBlock(new StatRuleTable());
            stats.SetBase(StatId.MovementSpeed, 10f);
            var t = new TimedModifierSet(stats);
            int f = 0;
            t.Apply(Vuln(0.5f), 60, refreshKey: 7);
            Run.Frames(t, 40, ref f);
            t.Apply(Vuln(0.2f), 60, refreshKey: 7);
            Assert.AreEqual(1, t.ActiveCount);
            Assert.AreEqual(12f, stats.Evaluate(StatId.MovementSpeed), 1e-4f);
            Run.Frames(t, 59, ref f);
            Assert.AreEqual(12f, stats.Evaluate(StatId.MovementSpeed), 1e-4f);
            Run.Frames(t, 1, ref f);
            Assert.AreEqual(10f, stats.Evaluate(StatId.MovementSpeed), 1e-4f);
        }
    }

    internal sealed class FakeAreaQuery : IAreaTargetQuery
    {
        public readonly List<AreaCandidate> Candidates = new List<AreaCandidate>();
        public int Collect(in AreaSpec spec, AreaCandidate[] buffer)
        {
            int n = System.Math.Min(Candidates.Count, buffer.Length);
            for (int i = 0; i < n; i++) buffer[i] = Candidates[i];
            return n;
        }
    }

    public class AreaTests
    {
        private static AreaSpec Spec(int duration, int group = 3, int interval = 30, float dmg = 20f)
            => new AreaSpec(new EntityId(1), Team.Player, HitboxShape.Sector(3f, 360f), Vector3.zero, Vector3.forward, duration, interval,
                DamageType.Ember, dmg, 0f, 0.5f, 0, group, TagMask.None, HitFlags.None);

        private static AreaCandidate Cand(FakeReceiver r) => new AreaCandidate { Id = r.Id, Receiver = r, Position = new Vector3(1, 0, 0) };

        [Test]
        public void InstantAreaHitsEachCandidateOnceAsHit()
        {
            var q = new FakeAreaQuery();
            var a = new FakeReceiver(2); var b = new FakeReceiver(3);
            q.Candidates.Add(Cand(a)); q.Candidates.Add(Cand(b));
            var d = new AreaDeliverer(q);
            Assert.AreEqual(2, d.Detonate(Spec(0), 1));
            Assert.AreEqual(1, a.Hits);
            Assert.AreEqual(HitKind.Hit, a.LastKind);
            Assert.AreEqual(20f, a.LastDamage);
        }

        [Test]
        public void ZoneTicksEvery30FramesAsTickKind()
        {
            var q = new FakeAreaQuery();
            var a = new FakeReceiver(2);
            q.Candidates.Add(Cand(a));
            var z = new ZoneRunner(new AreaDeliverer(q));
            int f = 0;
            Assert.IsTrue(z.Spawn(Spec(120)));
            Run.Frames(z, 29, ref f);
            Assert.AreEqual(0, a.Hits);
            Run.Frames(z, 1, ref f);
            Assert.AreEqual(1, a.Hits);
            Assert.AreEqual(HitKind.Tick, a.LastKind);
            Assert.IsFalse(a.LastCrit);
            Run.Frames(z, 90, ref f);
            Assert.AreEqual(4, a.Hits);
            Assert.AreEqual(0, z.ActiveCount);
            Run.Frames(z, 60, ref f);
            Assert.AreEqual(4, a.Hits);
        }

        [Test]
        public void ZonesOfSameSkillAndSourceDoNotStackOnTarget()
        {
            var q = new FakeAreaQuery();
            var a = new FakeReceiver(2);
            q.Candidates.Add(Cand(a));
            var z = new ZoneRunner(new AreaDeliverer(q));
            int f = 0;
            z.Spawn(Spec(60, group: 3));
            z.Spawn(Spec(60, group: 3));
            Run.Frames(z, 30, ref f);
            Assert.AreEqual(1, a.Hits, "two overlapping zones, one tick");
            Run.Frames(z, 30, ref f);
            Assert.AreEqual(2, a.Hits);
        }

        [Test]
        public void ZonesOfDifferentSkillsBothApply()
        {
            var q = new FakeAreaQuery();
            var a = new FakeReceiver(2);
            q.Candidates.Add(Cand(a));
            var z = new ZoneRunner(new AreaDeliverer(q));
            int f = 0;
            z.Spawn(Spec(60, group: 3));
            z.Spawn(Spec(60, group: 4));
            Run.Frames(z, 30, ref f);
            Assert.AreEqual(2, a.Hits);
        }

        [Test]
        public void NonPersistentSpecIsNotAZone()
        {
            var z = new ZoneRunner(new AreaDeliverer(new FakeAreaQuery()));
            Assert.IsFalse(z.Spawn(Spec(0)));
            Assert.IsFalse(z.Spawn(Spec(60, interval: 0)));
        }
    }

    public class ProjectileMathTests
    {
        [Test]
        public void ArcPeaksAtApexAndLandsOnTarget()
        {
            var s = new Vector3(0, 1, 0); var e = new Vector3(0, 1, 10);
            Assert.AreEqual(1f + 4f, ProjectileMath.ArcPosition(s, e, 4f, 0.5f).y, 1e-4f);
            Assert.AreEqual(5f, ProjectileMath.ArcPosition(s, e, 4f, 0.5f).z, 1e-4f);
            Assert.AreEqual(e, ProjectileMath.ArcPosition(s, e, 4f, 1f));
            Assert.AreEqual(s, ProjectileMath.ArcPosition(s, e, 4f, 0f));
            Assert.AreEqual(10f / 14f, ProjectileMath.ArcDuration(s, e, 14f), 1e-5f);
        }

        [Test]
        public void ForkDirectionsAreSymmetric()
        {
            Vector3 a = ProjectileMath.ForkDirection(Vector3.forward, 0, 2, 30f);
            Vector3 b = ProjectileMath.ForkDirection(Vector3.forward, 1, 2, 30f);
            Assert.AreEqual(-a.x, b.x, 1e-5f);
            Assert.AreEqual(a.z, b.z, 1e-5f);
            Assert.AreEqual(Vector3.forward, ProjectileMath.ForkDirection(Vector3.forward, 1, 3, 30f).normalized);
        }

        [Test]
        public void TurnTowardsIsRateLimited()
        {
            Vector3 d = ProjectileMath.TurnTowards(Vector3.forward, Vector3.right, 10f);
            Assert.AreEqual(10f, Vector3.Angle(Vector3.forward, d), 1e-3f);
            Assert.AreEqual(0f, Vector3.Angle(Vector3.right, ProjectileMath.TurnTowards(Vector3.forward, Vector3.right, 180f)), 1e-3f);
        }
    }

    public class ProjectileManagerTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private ProjectileManager _pm;

        [TearDown]
        public void TearDown()
        {
            _pm?.Dispose();
            _pm = null;
            foreach (GameObject g in _spawned) if (g != null) Object.DestroyImmediate(g);
            _spawned.Clear();
        }

        private static ProjectileSpec Spec(Vector3 origin, Team team = Team.Player, float range = 20f, int pierce = 0, float speed = 60f,
            TagMask tags = default, int group = 1)
            => new ProjectileSpec(new EntityId(1), team, origin, Vector3.forward, speed, 0.2f, range, pierce, DamageType.Physical, 10f,
                0f, 1f, 0, group, tags, HitFlags.None);

        private GameObject Block(int layer, Vector3 pos)
        {
            var g = new GameObject("block");
            g.layer = layer;
            g.transform.position = pos;
            g.AddComponent<BoxCollider>().size = new Vector3(4f, 4f, 0.5f);
            _spawned.Add(g);
            return g;
        }

        private (FakeReceiver rx, FakeEntity ent) Enemy(int id, Vector3 pos)
        {
            var g = new GameObject("enemy" + id);
            g.transform.position = pos;
            var hb = g.AddComponent<Hurtbox>();
            hb.Configure(0.5f, 1.8f, false);
            var ent = new FakeEntity(id, Team.Enemy, pos);
            var rx = new FakeReceiver(id);
            hb.Bind(ent, rx);
            _spawned.Add(g);
            return (rx, ent);
        }

        private static void Step(ProjectileManager pm, int n, ref int frame)
        {
            UnityEngine.Physics.SyncTransforms();
            for (int i = 0; i < n; i++) { frame++; var f = new SimulationFrame(frame); pm.Tick(in f); }
        }

        [Test]
        public void FliesAndExpiresAtMaxRange()
        {
            _pm = new ProjectileManager(8);
            int f = 0;
            int h = _pm.Spawn(Spec(new Vector3(0, 1, 0), range: 5f, speed: 60f)); // 1 m per tick
            Assert.GreaterOrEqual(h, 0);
            Step(_pm, 2, ref f);
            Assert.AreEqual(2f, _pm.GetPosition(h).z, 1e-3f);
            Step(_pm, 3, ref f);
            Assert.IsFalse(_pm.IsActive(h));
            Assert.AreEqual(0, _pm.ActiveCount);
        }

        [Test]
        public void WallBlocksAndLowCoverBlocksOnlyWithoutAreaTag()
        {
            _pm = new ProjectileManager(8);
            int f = 0;
            Block(PhysicsLayers.Wall, new Vector3(0, 1, 5));
            int a = _pm.Spawn(Spec(new Vector3(0, 1, 0), group: 1));
            Step(_pm, 10, ref f);
            Assert.IsFalse(_pm.IsActive(a), "stopped by Wall");

            var cover = Block(PhysicsLayers.LowCover, new Vector3(0, 1, 12));
            int plain = _pm.Spawn(Spec(new Vector3(0, 1, 6.5f), group: 2));
            int area = _pm.Spawn(Spec(new Vector3(0, 1, 6.5f), tags: new TagMask(Tag.Area), group: 3));
            Step(_pm, 8, ref f);
            Assert.IsFalse(_pm.IsActive(plain), "LowCover blocks a non-Area skill");
            Assert.IsTrue(_pm.IsActive(area), "Area skill flies over LowCover");
            Assert.Greater(_pm.GetPosition(area).z, 12.5f);
        }

        [Test]
        public void HitsTargetOnceAndPierceContinues()
        {
            var ev = new CombatEventStream();
            _pm = new ProjectileManager(8, ev);
            int f = 0;
            var (rx1, _) = Enemy(21, new Vector3(0, 0, 4));
            var (rx2, _) = Enemy(22, new Vector3(0, 0, 8));
            int stop = _pm.Spawn(Spec(new Vector3(0, 1, 0), pierce: 0, group: 1));
            Step(_pm, 15, ref f);
            Assert.AreEqual(1, rx1.Hits);
            Assert.AreEqual(0, rx2.Hits, "no pierce: stops at the first");
            Assert.IsFalse(_pm.IsActive(stop));

            rx1.Hits = 0;
            int pierce = _pm.Spawn(Spec(new Vector3(0, 1, 0), pierce: 1, group: 2));
            Step(_pm, 20, ref f);
            Assert.AreEqual(1, rx1.Hits, "same target is never hit twice");
            Assert.AreEqual(1, rx2.Hits);
            Assert.IsFalse(_pm.IsActive(pierce));
            Assert.AreEqual(3, ev.Count<OnHitDealt>());
        }

        [Test]
        public void DoesNotHitSameTeamOrSource()
        {
            _pm = new ProjectileManager(8);
            int f = 0;
            var (rx, _) = Enemy(21, new Vector3(0, 0, 4));
            _pm.Spawn(Spec(new Vector3(0, 1, 0), team: Team.Enemy)); // enemy projectile ignores enemy hurtbox layer
            Step(_pm, 10, ref f);
            Assert.AreEqual(0, rx.Hits);
        }

        [Test]
        public void ReturnComesBackAndMayHitAgain()
        {
            _pm = new ProjectileManager(8);
            int f = 0;
            var (rx, _) = Enemy(21, new Vector3(0, 0, 3));
            int h = _pm.Spawn(Spec(new Vector3(0, 1, 0), range: 6f, pierce: 5, group: 1), ProjectileBehavior.Boomerang());
            Step(_pm, 6, ref f);
            Assert.AreEqual(1, rx.Hits);
            Step(_pm, 8, ref f);
            Assert.AreEqual(2, rx.Hits, "Return may hit the same target again");
            Step(_pm, 10, ref f);
            Assert.IsFalse(_pm.IsActive(h));
        }

        private sealed class Listener : IProjectileListener
        {
            public int Landed;
            public Vector3 At;
            public void OnArcLanded(in ProjectileSpec spec, Vector3 position, int frame) { Landed++; At = position; }
        }

        [Test]
        public void ArtilleryArcLandsOnTargetAndReports()
        {
            var l = new Listener();
            _pm = new ProjectileManager(8, null, null, l);
            int f = 0;
            Block(PhysicsLayers.Wall, new Vector3(0, 1, 5)); // arcs fly over walls
            Vector3 target = new Vector3(0, 0, 10);
            int h = _pm.Spawn(Spec(new Vector3(0, 1, 0), speed: 10f), ProjectileBehavior.Artillery(target, 3f));
            Step(_pm, 30, ref f);
            Assert.AreEqual(0, l.Landed);
            Assert.Greater(_pm.GetPosition(h).y, 3f, "near the apex at mid flight");
            Step(_pm, 40, ref f);
            Assert.AreEqual(1, l.Landed);
            Assert.AreEqual(target, l.At);
            Assert.IsFalse(_pm.IsActive(h));
        }

        [Test]
        public void ForkSpawnsChildrenOnFirstHit()
        {
            _pm = new ProjectileManager(16);
            int f = 0;
            Enemy(21, new Vector3(0, 0, 4));
            _pm.Spawn(Spec(new Vector3(0, 1, 0), group: 1), ProjectileBehavior.Forking(2, 40f));
            Step(_pm, 5, ref f);
            Assert.AreEqual(2, _pm.ActiveCount, "parent ended, two children flying");
        }

        [Test]
        public void SupportsTwoThousandActiveAndRefusesBeyondCapacity()
        {
            _pm = new ProjectileManager(2048);
            int f = 0;
            for (int i = 0; i < 2000; i++)
                Assert.GreaterOrEqual(_pm.Spawn(Spec(new Vector3(i % 50, 1, 0), speed: 6f, group: i)), 0);
            Step(_pm, 3, ref f);
            Assert.AreEqual(2000, _pm.ActiveCount);
            for (int i = 0; i < 48; i++) _pm.Spawn(Spec(Vector3.up, group: 9999));
            Assert.AreEqual(-1, _pm.Spawn(Spec(Vector3.up)));
            Assert.AreEqual(1, _pm.DroppedSpawns);
        }

        [Test]
        public void InvalidSpecIsRefused()
        {
            _pm = new ProjectileManager(4);
            Assert.AreEqual(-1, _pm.Spawn(Spec(Vector3.zero, speed: 0f)));
            Assert.AreEqual(-1, _pm.Spawn(Spec(Vector3.zero, range: 0f)));
        }
    }
}
