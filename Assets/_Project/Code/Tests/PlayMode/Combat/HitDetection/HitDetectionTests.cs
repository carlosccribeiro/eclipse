#if UNITY_EDITOR
using EntityId = Eclipse.Gameplay.Combat.EntityId;
using Eclipse.Core.Tags;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.PlayMode.Combat.HitDetection
{
    /// <summary>06 3.9: sector queries, Wall line of sight, Hit Group registry and swept detection at high Attack Speed.</summary>
    public class HitDetectionTests
    {
        private CombatEnv _env;
        private CombatEntity _attacker;
        private HitboxEvaluator _eval;

        [SetUp]
        public void SetUp()
        {
            _env = new CombatEnv();
            _attacker = _env.MakeEntity(Team.Player, 1000f, Vector3.zero);
            _eval = new HitboxEvaluator(new FixedDamageResolver());
        }

        [TearDown]
        public void TearDown() => _env.Dispose();

        private static AttackStep Sector(int group = 0, int rehit = 0) => new AttackStep(20, 6, 40, AttackHitShape.Sector, 2.8f, 120f, 0f, 0f,
            Vector3.zero, 15f, 15f, 5, 1f, 1.1f, 1.1f, 0f, 0f, 60f, group, rehit, false, false, 0, 0);

        private HitboxRequest Request(AttackStep step, int frame, float attackSpeed = 1f, Vector3? forward = null, Vector3? prevForward = null)
        {
            Vector3 f = forward ?? Vector3.forward;
            return new HitboxRequest
            {
                Attacker = _attacker, PrevOrigin = Vector3.zero, Origin = Vector3.zero, Forward = f, PrevForward = prevForward ?? f,
                Step = step, AttackSpeed = attackSpeed, Tags = new TagMask(Tag.Attack).With(Tag.Melee), Frame = frame,
            };
        }

        [Test]
        public void Target_inside_the_sector_is_hit_and_outside_is_not()
        {
            _env.MakeTarget(new Vector3(0f, 0f, 2f), 0.5f, out var inFront);
            _env.MakeTarget(new Vector3(0f, 0f, -2f), 0.5f, out var behind);
            _env.MakeTarget(new Vector3(0f, 0f, 5f), 0.5f, out var tooFar);
            _env.MakeTarget(new Vector3(2.5f, 0f, 0.3f), 0.1f, out var sideOutsideArc);
            var step = Sector();
            var r = Request(step, 10);
            Assert.AreEqual(1, _eval.Evaluate(in r));
            Assert.Less(inFront.Health.Current, inFront.Health.Max, "front target damaged");
            Assert.AreEqual(behind.Health.Max, behind.Health.Current, "behind target untouched");
            Assert.AreEqual(tooFar.Health.Max, tooFar.Health.Current, "out-of-range target untouched");
            Assert.AreEqual(sideOutsideArc.Health.Max, sideOutsideArc.Health.Current, "outside the 120 degree arc");
        }

        [Test]
        public void Wall_between_attacker_and_target_blocks_the_hit()
        {
            _env.MakeTarget(new Vector3(0f, 0f, 2f), 0.5f, out var target);
            var wall = _env.MakeWall(new Vector3(0f, 1f, 1.2f), new Vector3(4f, 3f, 0.2f));
            var r = Request(Sector(), 10);
            Assert.AreEqual(0, _eval.Evaluate(in r), "Wall blocks");
            Assert.AreEqual(target.Health.Max, target.Health.Current);
            Object.DestroyImmediate(wall);
            UnityEngine.Physics.SyncTransforms();
            Assert.AreEqual(1, _eval.Evaluate(in r), "same attack lands once the Wall is gone");
        }

        [Test]
        public void One_application_per_hit_group_across_the_whole_active_window()
        {
            _env.MakeTarget(new Vector3(0f, 0f, 2f), 0.5f, out var target);
            var step = Sector(group: 0);
            int applications = 0;
            for (int frame = 100; frame < 106; frame++)
            {
                var r = Request(step, frame);
                applications += _eval.Evaluate(in r);
            }
            Assert.AreEqual(1, applications, "6 Active frames, a single application");
            Assert.AreEqual(target.Health.Max - 11f, target.Health.Current, 0.01f);

            _eval.Registry.Clear(); // new use of the attack: fresh memory
            var again = Request(step, 200);
            Assert.AreEqual(1, _eval.Evaluate(in again));
        }

        [Test]
        public void Different_hit_groups_hit_the_same_target_and_rehit_interval_re_arms()
        {
            _env.MakeTarget(new Vector3(0f, 0f, 2f), 0.5f, out var target);
            var a = Request(Sector(group: 0), 10);
            var b = Request(Sector(group: 1), 10);
            Assert.AreEqual(1, _eval.Evaluate(in a));
            Assert.AreEqual(1, _eval.Evaluate(in b), "second group is independent");

            _eval.Registry.Clear();
            int hits = 0;
            for (int frame = 0; frame < 9; frame++)
            {
                var r = Request(Sector(group: 2, rehit: 3), frame);
                hits += _eval.Evaluate(in r);
            }
            Assert.AreEqual(3, hits, "rehit every 3 frames over 9 frames");
        }

        [Test]
        public void No_tunneling_at_attack_speed_2_0_with_a_thin_target()
        {
            // The swing goes from -80 to +80 degrees in one tick; the target sits at 0 degrees, outside both end poses.
            _env.MakeTarget(new Vector3(0f, 0f, 2f), 0.05f, out var thin);
            Vector3 prev = Quaternion.Euler(0f, -80f, 0f) * Vector3.forward;
            Vector3 now = Quaternion.Euler(0f, 80f, 0f) * Vector3.forward;
            var step = Sector();

            var slow = Request(step, 10, 1.0f, now, prev);
            Assert.AreEqual(0, _eval.Evaluate(in slow), "AS 1.0 only tests the current pose");

            var fast = Request(step, 11, 2.0f, now, prev);
            Assert.AreEqual(1, _eval.Evaluate(in fast), "AS 2.0 sweeps between the previous and current pose");
            Assert.Less(thin.Health.Current, thin.Health.Max);
        }
    }
}
#endif
