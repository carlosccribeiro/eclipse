using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat;
using NUnit.Framework;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.EditMode.Gameplay.HitDetection
{
    public class ShapeQueriesTests
    {
        private static readonly Vector3 Fwd = Vector3.forward;

        [Test]
        public void Sector_InsideAndOutside()
        {
            var s = HitboxShape.Sector(3f, 90f);
            Assert.IsTrue(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, new Vector3(0, 0, 2), 0f));
            Assert.IsFalse(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, new Vector3(0, 0, 4), 0f));
            Assert.IsFalse(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, new Vector3(0, 0, -1), 0f), "behind");
            Assert.IsFalse(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, new Vector3(2, 0, 0.5f), 0f), "outside arc");
            Assert.IsTrue(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, new Vector3(1.5f, 0, 1.6f), 0f), "inside 45 deg");
        }

        [Test]
        public void Sector_TargetRadiusWidensArcAndReach()
        {
            var s = HitboxShape.Sector(3f, 90f);
            var p = new Vector3(2f, 0, 1.5f); // ~53 deg: outside a point test
            Assert.IsFalse(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, p, 0f));
            Assert.IsTrue(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, p, 0.5f));
            Assert.IsTrue(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, new Vector3(0, 0, 3.4f), 0.5f));
        }

        [Test]
        public void Sector_RespectsRotation()
        {
            var s = HitboxShape.Sector(3f, 60f);
            Assert.IsTrue(ShapeQueries.Overlaps(s, Vector3.zero, Vector3.right, new Vector3(2, 0, 0), 0f));
            Assert.IsFalse(ShapeQueries.Overlaps(s, Vector3.zero, Vector3.right, new Vector3(0, 0, 2), 0f));
        }

        [Test]
        public void FullArcIsCircle()
        {
            var s = HitboxShape.Sector(2f, 360f);
            Assert.IsTrue(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, new Vector3(0, 0, -1.5f), 0f));
            Assert.IsFalse(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, new Vector3(0, 0, -2.5f), 0f));
        }

        [Test]
        public void Box_ExtendsForwardFromOffset()
        {
            var b = HitboxShape.Box(4f, 2f);
            Assert.IsTrue(ShapeQueries.Overlaps(b, Vector3.zero, Fwd, new Vector3(0.9f, 0, 3.9f), 0f));
            Assert.IsFalse(ShapeQueries.Overlaps(b, Vector3.zero, Fwd, new Vector3(1.1f, 0, 2f), 0f));
            Assert.IsFalse(ShapeQueries.Overlaps(b, Vector3.zero, Fwd, new Vector3(0, 0, -0.1f), 0f));
            Assert.IsTrue(ShapeQueries.Overlaps(b, Vector3.zero, Fwd, new Vector3(1.3f, 0, 2f), 0.5f));
        }

        [Test]
        public void Capsule_SegmentPlusRadius()
        {
            var c = HitboxShape.Capsule(0.5f, 3f);
            Assert.IsTrue(ShapeQueries.Overlaps(c, Vector3.zero, Fwd, new Vector3(0.4f, 0, 2f), 0f));
            Assert.IsFalse(ShapeQueries.Overlaps(c, Vector3.zero, Fwd, new Vector3(0.6f, 0, 2f), 0f));
            Assert.IsTrue(ShapeQueries.Overlaps(c, Vector3.zero, Fwd, new Vector3(0, 0, 3.4f), 0f), "end cap");
            Assert.IsFalse(ShapeQueries.Overlaps(c, Vector3.zero, Fwd, new Vector3(0, 0, 3.6f), 0f));
        }

        [Test]
        public void LocalOffsetIsAppliedInAttackerSpace()
        {
            var s = HitboxShape.Sector(1f, 360f, new Vector3(0, 0, 2f));
            Assert.IsTrue(ShapeQueries.Overlaps(s, Vector3.zero, Vector3.right, new Vector3(2, 0, 0), 0f));
            Assert.IsFalse(ShapeQueries.Overlaps(s, Vector3.zero, Vector3.right, new Vector3(0, 0, 2), 0f));
        }

        [Test]
        public void Sweep_CatchesThinTargetSkippedBySinglePose()
        {
            // Thin box moving fast along z: end pose passes the target, start pose is before it.
            var b = HitboxShape.Box(0.2f, 1f);
            var target = new Vector3(0, 0, 2f);
            Vector3 prev = Vector3.zero, cur = new Vector3(0, 0, 4f);
            Assert.IsFalse(ShapeQueries.Overlaps(b, prev, Fwd, target, 0.05f));
            Assert.IsFalse(ShapeQueries.Overlaps(b, cur, Fwd, target, 0.05f));
            Assert.IsTrue(ShapeQueries.OverlapsSwept(b, prev, Fwd, cur, Fwd, target, 0.05f));
        }

        [Test]
        public void Sweep_CatchesRotationThroughTarget()
        {
            var s = HitboxShape.Sector(3f, 20f);
            var target = new Vector3(2f, 0, 0f); // 90 deg to the right
            Assert.IsFalse(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, target, 0.1f));
            var mid = new Vector3(1.4f, 0, 1.4f); // 45 deg, between poses
            Assert.IsFalse(ShapeQueries.Overlaps(s, Vector3.zero, Fwd, mid, 0.1f));
            Assert.IsFalse(ShapeQueries.Overlaps(s, Vector3.zero, Vector3.right, mid, 0.1f));
            Assert.IsTrue(ShapeQueries.OverlapsSwept(s, Vector3.zero, Fwd, Vector3.zero, Vector3.right, mid, 0.1f));
        }

        [Test]
        public void SweepSamples_AreBounded()
        {
            var s = HitboxShape.Sector(3f, 90f);
            int n = ShapeQueries.SweepSamples(s, Vector3.zero, Fwd, new Vector3(0, 0, 100f), Fwd, 0f);
            Assert.AreEqual(ShapeQueries.MaxSweepSamples, n);
            Assert.AreEqual(2, ShapeQueries.SweepSamples(s, Vector3.zero, Fwd, Vector3.zero, Fwd, 0.5f));
        }
    }

    public class HitRegistryTests
    {
        [Test]
        public void OneHitPerTargetPerGroup()
        {
            var r = new HitRegistry(1);
            var t = new EntityId(7);
            Assert.IsTrue(r.TryRegister(1, t, 10, 0));
            Assert.IsFalse(r.TryRegister(1, t, 11, 0));
            Assert.IsTrue(r.TryRegister(2, t, 11, 0), "other group");
            Assert.IsTrue(r.TryRegister(1, new EntityId(8), 11, 0), "other target (grows)");
            Assert.AreEqual(3, r.Count);
        }

        [Test]
        public void RehitAfterInterval()
        {
            var r = new HitRegistry();
            var t = new EntityId(3);
            Assert.IsTrue(r.TryRegister(1, t, 10, 12));
            Assert.IsFalse(r.TryRegister(1, t, 21, 12));
            Assert.IsTrue(r.TryRegister(1, t, 22, 12));
            Assert.IsFalse(r.TryRegister(1, t, 33, 12));
        }

        [Test]
        public void ClearAllowsNewUse()
        {
            var r = new HitRegistry();
            var t = new EntityId(3);
            r.TryRegister(1, t, 1, 0);
            r.Clear();
            Assert.IsFalse(r.Contains(1, t));
            Assert.IsTrue(r.TryRegister(1, t, 2, 0));
        }
    }

    public class HurtboxAndShapeMappingTests
    {
        [Test]
        public void PlayerHurtboxIs15PercentSmaller()
        {
            Assert.AreEqual(0.425f, Hurtbox.ComputeRadius(0.5f, true), 1e-5f);
            Assert.AreEqual(0.5f, Hurtbox.ComputeRadius(0.5f, false), 1e-5f);
        }

        private sealed class NoDamage : IHitDamageResolver
        {
            public void Resolve(Eclipse.Core.Damage.DamageInstance hit, ICombatEntity attacker, Hurtbox target, float m) { }
        }

        [Test]
        public void UndefinedShapesYieldNoHitbox()
        {
            var ev = new HitboxEvaluator(new NoDamage());
            var none = new AttackStep(1, 1, 1, AttackHitShape.None, 0, 0, 0, 0, Vector3.zero, 0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 0, false, false, 0, 0);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no hitbox"));
            Assert.IsFalse(ev.TryBuildShape(none, out _));
            var cone = new AttackStep(1, 1, 1, AttackHitShape.Cone, 3, 0, 0, 0, Vector3.zero, 0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 0, false, false, 0, 0);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("sector/cone"));
            Assert.IsFalse(ev.TryBuildShape(cone, out _));
        }

        [Test]
        public void CircleAndConeMapToSector()
        {
            var ev = new HitboxEvaluator(new NoDamage());
            var circle = new AttackStep(1, 1, 1, AttackHitShape.Circle, 2.5f, 0, 0, 0, Vector3.zero, 0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 0, false, false, 0, 0);
            Assert.IsTrue(ev.TryBuildShape(circle, out HitboxShape s));
            Assert.AreEqual(HitboxKind.Sector, s.Kind);
            Assert.AreEqual(360f, s.ArcDegrees);
            var cone = new AttackStep(1, 1, 1, AttackHitShape.Cone, 4, 60, 0, 0, Vector3.zero, 0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 0, false, false, 0, 0);
            Assert.IsTrue(ev.TryBuildShape(cone, out s));
            Assert.AreEqual(60f, s.ArcDegrees);
        }
    }
}
