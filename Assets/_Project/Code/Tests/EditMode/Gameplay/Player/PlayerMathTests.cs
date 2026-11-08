using Eclipse.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.EditMode.Gameplay.Player
{
    public class PlayerMathTests
    {
        private static MovementProfile NewProfile(float accel = MovementProfile.Instant)
        {
            var p = ScriptableObject.CreateInstance<MovementProfile>();
            p.acceleration = accel;
            p.deceleration = accel;
            return p;
        }

        [Test]
        public void Speed_IsBaseTimesMultiplier()
        {
            var p = NewProfile();
            Assert.AreEqual(6.0f, p.ComputeSpeed(1f), 1e-5f);
            Assert.AreEqual(9.0f, p.ComputeSpeed(1.5f), 1e-5f);
        }

        [Test]
        public void Speed_FlooredAt40PercentOfBase()
        {
            var p = NewProfile();
            Assert.AreEqual(2.4f, p.ComputeSpeed(0.1f), 1e-5f);
            Assert.AreEqual(2.4f, p.ComputeSpeed(-1f), 1e-5f);
        }

        [Test]
        public void InstantAcceleration_SnapsToTarget()
        {
            var p = NewProfile();
            Vector3 v = PlayerMotor.StepVelocity(Vector3.zero, new Vector3(0, 0, 6f), p, 1f / 60f);
            Assert.AreEqual(6f, v.z, 1e-5f);
        }

        [Test]
        public void FiniteAcceleration_RampsLinearly()
        {
            var p = NewProfile(60f);
            Vector3 v = PlayerMotor.StepVelocity(Vector3.zero, new Vector3(0, 0, 6f), p, 1f / 60f);
            Assert.AreEqual(1f, v.z, 1e-5f);
        }

        [Test]
        public void RayPlane_HitsGroundAtChestHeight()
        {
            // Camera at (0,10,-10) looking at the origin; plane y = 1.2.
            Vector3 origin = new Vector3(0, 10, -10);
            Vector3 dir = (new Vector3(0, 1.2f, 0) - origin).normalized;
            Assert.IsTrue(PlayerAim.RayPlane(origin, dir, 1.2f, out Vector3 p));
            Assert.AreEqual(0f, p.x, 1e-4f);
            Assert.AreEqual(1.2f, p.y, 1e-4f);
            Assert.AreEqual(0f, p.z, 1e-4f);
        }

        [Test]
        public void RayPlane_FailsWhenParallelOrBehind()
        {
            Assert.IsFalse(PlayerAim.RayPlane(new Vector3(0, 5, 0), Vector3.forward, 1f, out _));
            Assert.IsFalse(PlayerAim.RayPlane(new Vector3(0, 5, 0), Vector3.up, 1f, out _));
        }

        [Test]
        public void DirectionTo_IgnoresHeightAndNormalizes()
        {
            Assert.IsTrue(PlayerAim.DirectionTo(Vector3.zero, new Vector3(3, 9, 4), out Vector3 d));
            Assert.AreEqual(0.6f, d.x, 1e-5f);
            Assert.AreEqual(0f, d.y, 1e-5f);
            Assert.AreEqual(0.8f, d.z, 1e-5f);
            Assert.IsFalse(PlayerAim.DirectionTo(Vector3.one, Vector3.one, out _));
        }

        [Test]
        public void Gamepad_StickWinsThenMoveThenFallback()
        {
            Vector3 fb = Vector3.left;
            Assert.AreEqual(Vector3.forward, PlayerAim.ResolveGamepadDirection(Vector3.forward, Vector3.right, fb));
            Assert.AreEqual(Vector3.right, PlayerAim.ResolveGamepadDirection(new Vector3(0.05f, 0, 0), Vector3.right, fb));
            Assert.AreEqual(fb, PlayerAim.ResolveGamepadDirection(Vector3.zero, Vector3.zero, fb));
        }

        [Test]
        public void StickToWorld_RotatesByBasisYaw()
        {
            Vector3 w = PlayerAim.StickToWorld(new Vector2(0, 1), 90f);
            Assert.AreEqual(1f, w.x, 1e-5f);
            Assert.AreEqual(0f, w.z, 1e-5f);
        }
    }
}
