using Eclipse.AI.Enemies;
using Eclipse.Core.Damage;
using Eclipse.Gameplay.Combat;
using NUnit.Framework;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.EditMode.AI.Enemies
{
    /// <summary>Kiln Brute plate (09 section 4.4): -50% from the front arc (120 degrees in the data), 100% from behind.</summary>
    public class FrontalGuardTests
    {
        private static readonly Vector3 Facing = Vector3.forward;

        // hitDirection is attacker -> victim (the IDamageReceiver convention): an attacker in front of a +Z-facing target travels along -Z.
        private static Vector3 FromAngle(float degreesFromFront) =>
            -(Quaternion.AngleAxis(degreesFromFront, Vector3.up) * Facing);

        [Test]
        public void FrontHitIsHalved()
        {
            var g = new FrontalGuard(0.5f, 120f);
            Assert.AreEqual(0.5f, g.Multiplier(Facing, FromAngle(0f)), 1e-5f);
        }

        [Test]
        public void BackHitIsFull()
        {
            var g = new FrontalGuard(0.5f, 120f);
            Assert.AreEqual(1f, g.Multiplier(Facing, FromAngle(180f)), 1e-5f);
            Assert.AreEqual(1f, g.Multiplier(Facing, FromAngle(90f)), 1e-5f, "flank outside the 120 degree arc");
        }

        [TestCase(59f, 0.5f)]
        [TestCase(-59f, 0.5f)]
        [TestCase(61f, 1f)]
        [TestCase(-61f, 1f)]
        public void ArcEdgeIsHalfTheDataArc(float degrees, float expected)
        {
            var g = new FrontalGuard(0.5f, 120f);
            Assert.AreEqual(expected, g.Multiplier(Facing, FromAngle(degrees)), 1e-5f);
        }

        [Test]
        public void FacingRotatesTheArc()
        {
            var g = new FrontalGuard(0.5f, 120f);
            Vector3 east = Vector3.right;
            Assert.AreEqual(0.5f, g.Multiplier(east, Vector3.left), 1e-5f, "attacker east of an east-facing Brute");
            Assert.AreEqual(1f, g.Multiplier(east, Vector3.right), 1e-5f);
        }

        [Test]
        public void DisabledOrDegenerateGuardDoesNothing()
        {
            Assert.AreEqual(1f, new FrontalGuard(0f, 120f).Multiplier(Facing, FromAngle(0f)));
            Assert.AreEqual(1f, new FrontalGuard(0.5f, 0f).Multiplier(Facing, FromAngle(0f)));
            Assert.AreEqual(1f, new FrontalGuard(0.5f, 120f).Multiplier(Vector3.zero, FromAngle(0f)));
            Assert.AreEqual(0f, new FrontalGuard(2f, 120f).Multiplier(Facing, FromAngle(0f)), 1e-5f, "reduction is clamped to 100%");
        }

        [Test]
        public void MatchesTheKilnBruteData()
        {
            var e = EnemyTestKit.Brute;
            var g = new FrontalGuard(e.FrontPlateReduction, e.FrontPlateArcDegrees);
            Assert.AreEqual(0.5f, e.FrontPlateReduction, 1e-5f);
            Assert.IsTrue(g.Enabled);
            Assert.AreEqual(0.5f, g.Multiplier(Facing, FromAngle(0f)), 1e-5f);
            Assert.AreEqual(1f, g.Multiplier(Facing, FromAngle(180f)), 1e-5f);
        }

        private sealed class CaptureReceiver : IDamageReceiver
        {
            public float Total;
            public float Stagger;
            public int Calls;
            public EntityId Id => new EntityId(5);

            public DamageOutcome ReceiveHit(DamageInstance hit, Vector3 hitDirection)
            {
                Calls++;
                Total = hit.TotalDamage;
                Stagger = hit.StaggerValue;
                return new DamageOutcome(HitResult.Hit, Total, Total, 0f, false);
            }
        }

        private static DamageInstance Hit()
        {
            var h = new DamageInstance { StaggerValue = 15f };
            h.AddComponent(DamageType.Physical, 40f);
            h.AddComponent(DamageType.Ember, 20f);
            return h;
        }

        [Test]
        public void ReceiverScalesEveryComponentOfAFrontalHitAndKeepsStagger()
        {
            var inner = new CaptureReceiver();
            var r = new FrontalGuardReceiver(inner, new FrontalGuard(0.5f, 120f), () => Facing);
            DamageInstance hit = Hit();
            DamageOutcome o = r.ReceiveHit(hit, FromAngle(10f));
            Assert.AreEqual(1, inner.Calls);
            Assert.AreEqual(30f, inner.Total, 1e-4f);
            Assert.AreEqual(20f, hit.GetValue(DamageType.Physical), 1e-4f);
            Assert.AreEqual(10f, hit.GetValue(DamageType.Ember), 1e-4f);
            Assert.AreEqual(2, hit.ComponentCount);
            Assert.AreEqual(15f, inner.Stagger, 1e-5f, "the plate stops damage, not the Stagger bar");
            Assert.AreEqual(HitResult.Hit, o.Result);
            Assert.AreEqual(5, r.Id.Value);
        }

        [Test]
        public void ReceiverLeavesABackHitUntouched()
        {
            var inner = new CaptureReceiver();
            var r = new FrontalGuardReceiver(inner, new FrontalGuard(0.5f, 120f), () => Facing);
            r.ReceiveHit(Hit(), FromAngle(180f));
            Assert.AreEqual(60f, inner.Total, 1e-4f);
        }

        [Test]
        public void ReceiverReadsTheFacingAtHitTime()
        {
            var inner = new CaptureReceiver();
            Vector3 facing = Vector3.forward;
            var r = new FrontalGuardReceiver(inner, new FrontalGuard(0.5f, 120f), () => facing);
            Vector3 dir = FromAngle(0f); // attacker north of the Brute
            r.ReceiveHit(Hit(), dir);
            Assert.AreEqual(30f, inner.Total, 1e-4f);
            facing = Vector3.back; // the Brute turned around: the same hit now strikes the back
            r.ReceiveHit(Hit(), dir);
            Assert.AreEqual(60f, inner.Total, 1e-4f);
        }
    }
}
