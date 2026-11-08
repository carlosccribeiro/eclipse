#if UNITY_EDITOR
using EntityId = Eclipse.Gameplay.Combat.EntityId;
using System.Collections.Generic;
using Eclipse.Core.Damage;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Feedback;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.PlayMode.Combat.Feedback
{
    /// <summary>06 3.11: local hit-stop on attacker + target (never a third party), +2 f on crit, 6 f global for Ultimate/Break.</summary>
    public class HitStopTests
    {
        private sealed class World : IFeedbackWorld, IHitFeedbackSource
        {
            public readonly Dictionary<int, EntityClock> Clocks = new Dictionary<int, EntityClock>();
            public HitFeedbackInfo Info;

            public EntityClock Add(int id) { var c = new EntityClock(); Clocks[id] = c; return c; }
            public bool TryGetClock(EntityId id, out EntityClock clock) => Clocks.TryGetValue(id.Value, out clock);
            public void FreezeAll(int frames) { foreach (var c in Clocks.Values) c.Freeze(frames); }
            public bool IsBoss(EntityId id) => false;
            public bool TryGetVisual(EntityId id, out IImpactVisual visual) { visual = null; return false; }
            public bool TryGet(in OnHitDealt hit, out HitFeedbackInfo info) { info = Info; return true; }
        }

        private CombatEventStream _events;
        private World _world;
        private FeedbackSystem _feedback;
        private EntityClock _attacker, _target, _third;
        private static readonly EntityId A = new EntityId(1), T = new EntityId(2), Third = new EntityId(3);

        [SetUp]
        public void SetUp()
        {
            _events = new CombatEventStream();
            _world = new World();
            _attacker = _world.Add(A.Value);
            _target = _world.Add(T.Value);
            _third = _world.Add(Third.Value);
            _feedback = new FeedbackSystem(_events, _world, _world, null, FeedbackRules.Doc);
        }

        private void Hit(int frame, bool crit = false) =>
            _events.Publish(new OnHitDealt(frame, 0, A, T, 10f, DamageType.Physical, crit, Eclipse.Core.Tags.HitFlags.None, Vector3.zero));

        [Test]
        public void Basic_hit_freezes_attacker_and_target_for_5_frames_but_not_a_third_entity()
        {
            _world.Info = new HitFeedbackInfo(5, new HitFeedbackValues(2, 100, 0f, 0f, 0, 0f), false, false);
            Hit(10);
            _feedback.Process();
            Assert.AreEqual(5, _target.FrozenFramesRemaining, "target");
            Assert.AreEqual(5, _attacker.FrozenFramesRemaining, "attacker");
            Assert.AreEqual(0, _third.FrozenFramesRemaining, "third entity keeps running");
            Assert.IsFalse(_third.IsFrozen);
        }

        [Test]
        public void Crit_adds_2_frames_for_a_total_of_7()
        {
            _world.Info = new HitFeedbackInfo(5, new HitFeedbackValues(2, 100, 0f, 0f, 0, 0f), false, false);
            Hit(10, crit: true);
            _feedback.Process();
            Assert.AreEqual(7, _target.FrozenFramesRemaining);
            Assert.AreEqual(7, _attacker.FrozenFramesRemaining);
            Assert.AreEqual(0, _third.FrozenFramesRemaining);
        }

        [Test]
        public void Projectile_hit_stop_affects_only_the_target()
        {
            _world.Info = new HitFeedbackInfo(5, default, true, false);
            Hit(10);
            _feedback.Process();
            Assert.AreEqual(5, _target.FrozenFramesRemaining);
            Assert.AreEqual(0, _attacker.FrozenFramesRemaining);
        }

        [Test]
        public void Attacker_gets_at_most_one_hit_stop_per_100_ms()
        {
            _world.Info = new HitFeedbackInfo(5, new HitFeedbackValues(2, 100, 0f, 0f, 0, 0f), false, false);
            Hit(10);
            _feedback.Process();
            _events.EndFrame();
            _attacker.Reset(); _target.Reset();
            Hit(14); // 4 f later: inside 100 ms (6 f)
            _feedback.Process();
            Assert.AreEqual(0, _target.FrozenFramesRemaining, "second hit within 100 ms is skipped");
            _events.EndFrame();
            Hit(16); // 6 f after the first
            _feedback.Process();
            Assert.AreEqual(5, _target.FrozenFramesRemaining);
        }

        [Test]
        public void Global_hit_stop_is_6_frames_for_everybody_including_the_third_entity()
        {
            _events.Publish(new OnGlobalHitStop(10, 0, A, FeedbackRules.Doc.GlobalHitStopFrames));
            _feedback.Process();
            Assert.AreEqual(6, _attacker.FrozenFramesRemaining);
            Assert.AreEqual(6, _target.FrozenFramesRemaining);
            Assert.AreEqual(6, _third.FrozenFramesRemaining, "global freezes everyone");
        }

        [Test]
        public void Ultimate_hit_requests_the_global_hit_stop()
        {
            _world.Info = new HitFeedbackInfo(5, default, false, true);
            Hit(10);
            _feedback.Process();
            Assert.AreEqual(6, _third.FrozenFramesRemaining);
        }

        [Test]
        public void A_frozen_clock_does_not_advance_for_exactly_its_frames()
        {
            _world.Info = new HitFeedbackInfo(5, default, false, false);
            Hit(10);
            _feedback.Process();
            int advanced = 0;
            for (int i = 0; i < 8; i++) if (_target.Advance()) advanced++;
            Assert.AreEqual(3, advanced, "8 ticks - 5 frozen");
            Assert.IsTrue(_third.Advance());
        }
    }
}
#endif
