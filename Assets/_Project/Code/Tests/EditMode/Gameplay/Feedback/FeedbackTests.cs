using System.Collections.Generic;
using Eclipse.Core.Damage;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Cameras;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Feedback;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.EditMode.Gameplay.Feedback
{
    public sealed class FeedbackTests
    {
        // Fixture values mirror DataSource/HitFeedback.csv (heavy) and AttackSteps.csv (hit-stop 5 f); 06 3.11.
        private static readonly HitFeedbackValues Heavy = new HitFeedbackValues(2, 100, 0.15f, 0.25f, 2, 0f);
        private static readonly HitFeedbackValues Medium = new HitFeedbackValues(2, 100, 0f, 0f, 2, 0.1f);
        private static readonly FeedbackRules Rules = FeedbackRules.Doc;

        private static readonly EntityId Atk = new EntityId(1), T1 = new EntityId(2), T2 = new EntityId(3), T3 = new EntityId(4);

        private sealed class World : IFeedbackWorld
        {
            public readonly Dictionary<int, EntityClock> Clocks = new Dictionary<int, EntityClock>();
            public bool BossSource;
            public World(params EntityId[] ids) { foreach (var i in ids) Clocks[i.Value] = new EntityClock(); }
            public bool TryGetClock(EntityId id, out EntityClock clock) => Clocks.TryGetValue(id.Value, out clock);
            public void FreezeAll(int frames) { foreach (var c in Clocks.Values) c.Freeze(frames); }
            public bool IsBoss(EntityId id) => BossSource;
            public bool TryGetVisual(EntityId id, out IImpactVisual visual) { visual = null; return false; }
        }

        private sealed class Source : IHitFeedbackSource
        {
            public HitFeedbackInfo Info = new HitFeedbackInfo(5, Heavy, false, false);
            public bool TryGet(in OnHitDealt hit, out HitFeedbackInfo info) { info = Info; return true; }
        }

        private static OnHitDealt Hit(int frame, EntityId target, bool crit = false) =>
            new OnHitDealt(frame, 0, Atk, target, 100f, DamageType.Physical, crit, HitFlags.None, Vector3.zero);

        private static int FrozenTicks(EntityClock c)
        {
            int n = 0;
            while (!c.Advance()) n++;
            return n;
        }

        private static (FeedbackSystem fs, CombatEventStream ev, World w, CameraShakeService shake, Source src) Make()
        {
            var ev = new CombatEventStream(32);
            var w = new World(Atk, T1, T2, T3);
            var shake = new CameraShakeService();
            var src = new Source();
            return (new FeedbackSystem(ev, w, src, shake, Rules), ev, w, shake, src);
        }

        [Test]
        public void Hit_FreezesAttackerAndTarget_5Frames_ThirdTargetUntouched()
        {
            var (fs, ev, w, _, _) = Make();
            ev.Publish(Hit(10, T1));
            ev.Publish(Hit(10, T2));
            fs.Process();
            Assert.AreEqual(5, FrozenTicks(w.Clocks[Atk.Value]));
            Assert.AreEqual(5, FrozenTicks(w.Clocks[T1.Value]));
            Assert.AreEqual(0, FrozenTicks(w.Clocks[T3.Value]), "third target not involved");
            Assert.AreEqual(0, FrozenTicks(w.Clocks[T2.Value]), "spacing: one hit-stop per attacker per 100 ms");
        }

        [Test]
        public void Crit_Freezes7Frames()
        {
            var (fs, ev, w, _, _) = Make();
            ev.Publish(Hit(10, T1, true));
            fs.Process();
            Assert.AreEqual(7, FrozenTicks(w.Clocks[Atk.Value]));
            Assert.AreEqual(7, FrozenTicks(w.Clocks[T1.Value]));
        }

        [Test]
        public void Projectile_FreezesOnlyTarget()
        {
            var (fs, ev, w, _, src) = Make();
            src.Info = new HitFeedbackInfo(5, Heavy, true, false);
            ev.Publish(Hit(10, T1));
            fs.Process();
            Assert.AreEqual(0, FrozenTicks(w.Clocks[Atk.Value]));
            Assert.AreEqual(5, FrozenTicks(w.Clocks[T1.Value]));
        }

        [Test]
        public void Global_6Frames_FreezesEveryone()
        {
            var (fs, ev, w, _, _) = Make();
            ev.Publish(new OnGlobalHitStop(20, 0, T1, 6));
            fs.Process();
            foreach (var c in w.Clocks.Values) Assert.AreEqual(6, FrozenTicks(c));
            Assert.AreEqual(1, ev.Count<OnVfxHook>());
            Assert.AreEqual(VfxHookId.VFX_GlobalHitStop, ev.Read<OnVfxHook>()[0].Hook);
        }

        [Test]
        public void Spacing_100ms_IsSixTicks()
        {
            Assert.AreEqual(6, HitStopService.MsToFrames(100));
            var svc = new HitStopService(Rules);
            var info = new HitFeedbackInfo(5, Heavy, false, false);
            Assert.IsTrue(svc.Plan(1, 10, info, false).Any);
            Assert.IsFalse(svc.Plan(1, 15, info, false).Any, "5 ticks = 83 ms");
            Assert.IsTrue(svc.Plan(1, 16, info, false).Any, "6 ticks = 100 ms");
            Assert.IsTrue(svc.Plan(2, 16, info, false).Any, "other attacker independent");
        }

        [Test]
        public void EliteKill_Freezes8Frames()
        {
            var (fs, ev, w, _, _) = Make();
            ev.Publish(new OnKill(30, 0, Atk, T1, 100f, true, Vector3.zero));
            fs.Process();
            Assert.AreEqual(8, FrozenTicks(w.Clocks[Atk.Value]));
        }

        [Test]
        public void NonEliteKill_DoesNotFreeze()
        {
            var (fs, ev, w, _, _) = Make();
            ev.Publish(new OnKill(30, 0, Atk, T1, 100f, false, Vector3.zero));
            fs.Process();
            Assert.AreEqual(0, FrozenTicks(w.Clocks[Atk.Value]));
        }

        [Test]
        public void TraumaTable_MatchesDoc()
        {
            var heavy = new HitFeedbackInfo(5, Heavy, false, false);
            var med = new HitFeedbackInfo(5, Medium, false, false);
            Assert.AreEqual(0.15f, TraumaTable.Hit(heavy, false, Rules), 1e-5f);
            Assert.AreEqual(0.25f, TraumaTable.Hit(heavy, true, Rules), 1e-5f);
            Assert.AreEqual(0f, TraumaTable.Hit(med, false, Rules), 1e-5f);
            Assert.AreEqual(0f, TraumaTable.Hit(med, true, Rules), 1e-5f);
            Assert.AreEqual(0.5f, TraumaTable.Hit(new HitFeedbackInfo(5, Heavy, false, true), false, Rules), 1e-5f);
            Assert.AreEqual(0.2f, TraumaTable.WallImpact(Rules), 1e-5f);
            Assert.AreEqual(0.4f, TraumaTable.BossHitReceived(Rules), 1e-5f);
        }

        [Test]
        public void System_AddsTrauma_ForHeavyHit_WallImpact_BossHit_AndNoneForMedium()
        {
            var (fs, ev, w, shake, src) = Make();
            ev.Publish(Hit(10, T1));
            fs.Process();
            Assert.AreEqual(0.15f, shake.Trauma, 1e-5f);

            shake.Clear();
            ev.EndFrame();
            ev.Publish(new OnWallImpact(11, 0, Atk, T1, 5f, Vector3.zero));
            fs.Process();
            Assert.AreEqual(0.2f, shake.Trauma, 1e-5f);

            shake.Clear();
            ev.EndFrame();
            w.BossSource = true;
            ev.Publish(new OnHitTaken(12, 0, T2, Atk, 10f, DamageType.Physical, HitResult.Hit, HitFlags.None));
            fs.Process();
            Assert.AreEqual(0.4f, shake.Trauma, 1e-5f);

            shake.Clear();
            ev.EndFrame();
            src.Info = new HitFeedbackInfo(5, Medium, false, false);
            ev.Publish(Hit(40, T1));
            fs.Process();
            Assert.AreEqual(0f, shake.Trauma, 1e-5f);
        }

        [Test]
        public void Hit_PublishesVfxAndSfxHooks()
        {
            var (fs, ev, _, _, _) = Make();
            ev.Publish(Hit(10, T1, true));
            fs.Process();
            Assert.AreEqual(VfxHookId.VFX_CritImpact, ev.Read<OnVfxHook>()[0].Hook);
            Assert.AreEqual(SfxHookId.SFX_Crit, ev.Read<OnSfxHook>()[0].Hook);
        }

        [Test]
        public void FlashTimer_Lasts2Frames()
        {
            var t = new FlashTimer();
            t.Start(2);
            Assert.IsTrue(t.Tick());
            Assert.IsTrue(t.Tick());
            Assert.IsFalse(t.Tick());
        }

        [Test]
        public void SquashMath_StartsAndEndsAtOne_DipsInMiddle()
        {
            Assert.AreEqual(1f, SquashMath.Scale(0f, 0.2f), 1e-5f);
            Assert.AreEqual(1f, SquashMath.Scale(1f, 0.2f), 1e-5f);
            Assert.AreEqual(0.8f, SquashMath.Scale(0.5f, 0.2f), 1e-5f);
        }
    }
}
