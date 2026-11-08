using Eclipse.AI.Enemies;
using Eclipse.AI.Locomotion;
using Eclipse.AI.StateMachine;
using NUnit.Framework;
using UnityEngine;
using static Eclipse.Tests.EditMode.AI.Enemies.EnemyTestKit;

namespace Eclipse.Tests.EditMode.AI.Enemies
{
    /// <summary>Decision (HFSM state) to locomotion steering mode per archetype, and the ranged retreat.</summary>
    public class SteeringAndRetreatTests
    {
        private static SteeringContext Ctx(ArchetypeTemplate t, float selfZ, float prefMin, float prefMax, int slot = 0)
        {
            return new SteeringContext
            {
                Self = new Vector3(0f, 0f, selfZ), Target = Vector3.zero, Template = t,
                PreferredMin = prefMin, PreferredMax = prefMax, OrbitMin = 4f, OrbitMax = 7f, Slot = slot, HasLineOfSight = true,
            };
        }

        [TestCase(EnemyState.Idle)]
        [TestCase(EnemyState.Attack)]
        [TestCase(EnemyState.Recover)]
        [TestCase(EnemyState.Dead)]
        public void NonMovingStatesHold(EnemyState state)
        {
            SteeringContext c = Ctx(ArchetypeTemplates.Melee(), 6f, 0f, 2f);
            Assert.AreEqual(SteeringMode.Hold, SteeringPlanner.Plan(state, EnemyState.Approach, in c).Mode);
        }

        [Test]
        public void FodderApproachesStraightAtTheTarget()
        {
            SteeringContext c = Ctx(ArchetypeTemplates.Fodder(), 8f, 0f, 1f);
            SteeringCommand cmd = SteeringPlanner.Plan(EnemyState.Approach, EnemyState.Approach, in c);
            Assert.AreEqual(SteeringMode.Seek, cmd.Mode);
            Assert.AreEqual(c.Target, cmd.Target);
        }

        [Test]
        public void EngageKeepsTheLastMovement()
        {
            SteeringContext c = Ctx(ArchetypeTemplates.Fodder(), 8f, 0f, 1f);
            Assert.AreEqual(SteeringMode.Seek, SteeringPlanner.Plan(EnemyState.Engage, EnemyState.Approach, in c).Mode);
            SteeringContext m = Ctx(ArchetypeTemplates.Melee(), 8f, 0f, 2f);
            Assert.AreEqual(SteeringMode.Orbit, SteeringPlanner.Plan(EnemyState.Engage, EnemyState.Reposition, in m).Mode);
        }

        [Test]
        public void MeleeArrivesAtItsReachOnASlotAroundTheTarget()
        {
            ArchetypeTemplate melee = ArchetypeTemplates.Melee();
            var points = new Vector3[melee.FlankSlotCount];
            for (int slot = 0; slot < melee.FlankSlotCount; slot++)
            {
                SteeringContext c = Ctx(melee, 8f, 0f, 2f, slot);
                SteeringCommand cmd = SteeringPlanner.Plan(EnemyState.Approach, EnemyState.Approach, in c);
                Assert.AreEqual(SteeringMode.Arrive, cmd.Mode);
                Assert.AreEqual(2f, cmd.Target.magnitude, 1e-4f, "point at the preferred max range from the target");
                points[slot] = cmd.Target;
            }
            for (int i = 1; i < points.Length; i++)
                Assert.Greater(Vector3.Distance(points[i], points[i - 1]), 0.1f, "slots spread around the target");
            // The middle slot is the direct line.
            Assert.AreEqual(0f, points[melee.FlankSlotCount / 2].x, 1e-4f);
        }

        [TestCase(2f, 4f)]
        [TestCase(5.5f, 5.5f)]
        [TestCase(12f, 7f)]
        public void RepositionOrbitsBetween4And7Meters(float distance, float expectedRadius)
        {
            SteeringContext c = Ctx(ArchetypeTemplates.Melee(), distance, 0f, 2f);
            SteeringCommand cmd = SteeringPlanner.Plan(EnemyState.Reposition, EnemyState.Approach, in c);
            Assert.AreEqual(SteeringMode.Orbit, cmd.Mode);
            Assert.AreEqual(expectedRadius, cmd.OrbitRadius, 1e-4f);
        }

        [Test]
        public void OrbitDirectionAlternatesBySlot()
        {
            SteeringContext a = Ctx(ArchetypeTemplates.Melee(), 5f, 0f, 2f, 0);
            SteeringContext b = Ctx(ArchetypeTemplates.Melee(), 5f, 0f, 2f, 1);
            Assert.AreNotEqual(SteeringPlanner.Plan(EnemyState.Reposition, EnemyState.Approach, in a).Clockwise,
                SteeringPlanner.Plan(EnemyState.Reposition, EnemyState.Approach, in b).Clockwise);
        }

        [Test]
        public void RangedWalksBackIntoTheBandAndStrafesInsideIt()
        {
            ArchetypeTemplate ranged = ArchetypeTemplates.Ranged();
            // Too close (3 m < 8): Arrive at the middle of 8..14 on the far side of the target.
            SteeringContext close = Ctx(ranged, 3f, 8f, 14f);
            SteeringCommand back = SteeringPlanner.Plan(EnemyState.Reposition, EnemyState.Approach, in close);
            Assert.AreEqual(SteeringMode.Arrive, back.Mode);
            Assert.AreEqual(11f, back.Target.z, 1e-4f);
            // Too far (20 m > 14): approach to the same band point.
            SteeringContext far = Ctx(ranged, 20f, 8f, 14f);
            SteeringCommand in1 = SteeringPlanner.Plan(EnemyState.Approach, EnemyState.Approach, in far);
            Assert.AreEqual(SteeringMode.Arrive, in1.Mode);
            Assert.AreEqual(11f, in1.Target.z, 1e-4f);
            // Inside the band: orbit at the current radius.
            SteeringContext inside = Ctx(ranged, 10f, 8f, 14f);
            SteeringCommand strafe = SteeringPlanner.Plan(EnemyState.Reposition, EnemyState.Approach, in inside);
            Assert.AreEqual(SteeringMode.Orbit, strafe.Mode);
            Assert.AreEqual(10f, strafe.OrbitRadius, 1e-4f);
        }

        [Test]
        public void RetreatOverridesMovementWithFlee()
        {
            SteeringContext c = Ctx(ArchetypeTemplates.Ranged(), 3f, 8f, 14f);
            c.Retreating = true;
            Assert.AreEqual(SteeringMode.Flee, SteeringPlanner.Plan(EnemyState.Reposition, EnemyState.Approach, in c).Mode);
            Assert.AreEqual(SteeringMode.Flee, SteeringPlanner.Plan(EnemyState.Engage, EnemyState.Approach, in c).Mode);
            Assert.AreEqual(SteeringMode.Hold, SteeringPlanner.Plan(EnemyState.Attack, EnemyState.Approach, in c).Mode, "a wind-up is never walked out of");
        }

        [Test]
        public void FleeStateFlees()
        {
            SteeringContext c = Ctx(ArchetypeTemplates.Melee(), 3f, 0f, 2f);
            Assert.AreEqual(SteeringMode.Flee, SteeringPlanner.Plan(EnemyState.Flee, EnemyState.Approach, in c).Mode);
        }

        [Test]
        public void TankAdvancesAndEscortsItsWard()
        {
            ArchetypeTemplate tank = ArchetypeTemplates.Tank();
            SteeringContext c = Ctx(tank, 10f, 0f, 4f);
            Assert.AreEqual(SteeringMode.Seek, SteeringPlanner.Plan(EnemyState.Approach, EnemyState.Approach, in c).Mode, "no ward: straight at the target");

            c.HasEscortWard = true;
            c.EscortWard = new Vector3(0f, 0f, 12f);
            SteeringCommand escort = SteeringPlanner.Plan(EnemyState.Approach, EnemyState.Approach, in c);
            Assert.AreEqual(SteeringMode.Arrive, escort.Mode);
            // Between the ward (z 12) and the target (z 0), one escort distance from the ward.
            Assert.AreEqual(12f - tank.EscortDistance, escort.Target.z, 1e-4f);
            Assert.AreEqual(SteeringMode.Arrive, SteeringPlanner.Plan(EnemyState.Reposition, EnemyState.Approach, in c).Mode);
        }

        [Test]
        public void ShotLineAndScreenHelpers()
        {
            Assert.IsTrue(ShotLine.SegmentHitsCircle(new Vector3(0, 0, 0), new Vector3(0, 0, 10), new Vector3(0.3f, 0, 5), 0.5f));
            Assert.IsFalse(ShotLine.SegmentHitsCircle(new Vector3(0, 0, 0), new Vector3(0, 0, 10), new Vector3(2f, 0, 5), 0.5f));
            Assert.IsFalse(ShotLine.SegmentHitsCircle(new Vector3(0, 0, 0), new Vector3(0, 0, 10), new Vector3(0f, 0, 12), 0.5f), "behind the target");

            Matrix4x4 vp = Matrix4x4.Ortho(-10f, 10f, -5f, 5f, 0.1f, 100f) * Matrix4x4.TRS(new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(1f, 1f, -1f));
            Assert.IsTrue(ShotLine.IsOnScreen(in vp, new Vector3(0f, 0f, 10f), 0f));
            Assert.IsFalse(ShotLine.IsOnScreen(in vp, new Vector3(30f, 0f, 10f), 0.05f));
            Assert.IsTrue(ShotLine.IsOnScreen(in vp, new Vector3(10.3f, 0f, 10f), 0.05f), "inside the margin");
        }

        // ---------------------------------------------------------------- retreat (09 section 4.2: below 5 m, cooldown 4 s)

        [Test]
        public void RetreatStartsBelowFiveMetersAndEndsInsideTheBand()
        {
            var r = new RetreatPlanner(5f, 4f, 8f);
            Assert.IsFalse(r.Update(9f, Dt, true));
            Assert.IsTrue(r.Update(4f, Dt, true));
            Assert.IsTrue(r.Update(6f, Dt, true), "keeps backing off until the band");
            Assert.IsFalse(r.Update(8.5f, Dt, true));
        }

        [Test]
        public void RetreatCooldownIsFourSecondsFromItsStart()
        {
            var r = new RetreatPlanner(5f, 4f, 8f);
            Assert.IsTrue(r.Update(4f, Dt, true));
            Assert.IsFalse(r.Update(9f, Dt, true));
            for (int i = 0; i < 60 * 3; i++) r.Update(4f, Dt, true);
            Assert.IsFalse(r.Active, "still cooling down after 3 s");
            for (int i = 0; i < 60 + 2; i++) r.Update(4f, Dt, true);
            Assert.IsTrue(r.Active, "available again after 4 s");
        }

        [Test]
        public void RetreatDoesNotStartWhileAttackingAndGivesUpWhenCornered()
        {
            var r = new RetreatPlanner(5f, 4f, 8f);
            Assert.IsFalse(r.Update(2f, Dt, false));
            Assert.IsTrue(r.Update(2f, Dt, true));
            bool gaveUp = false;
            for (int i = 0; i < 60 * 5 && !gaveUp; i++) gaveUp = !r.Update(2f, Dt, true);
            Assert.IsTrue(gaveUp, "the retreat span ends with the cooldown span");
        }

        [Test]
        public void DisabledRetreatNeverStarts()
        {
            var r = new RetreatPlanner(0f, 0f, 0f);
            Assert.IsFalse(r.Enabled);
            Assert.IsFalse(r.Update(0.5f, Dt, true));
        }
    }
}
