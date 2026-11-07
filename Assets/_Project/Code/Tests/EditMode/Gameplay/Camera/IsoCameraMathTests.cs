using Eclipse.Gameplay.Cameras;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.EditMode.Gameplay.Camera
{
    public class IsoCameraMathTests
    {
        static IsoCameraSettings Defaults() => ScriptableObject.CreateInstance<IsoCameraSettings>();

        [Test]
        public void Defaults_MatchGdd()
        {
            var s = Defaults();
            Assert.AreEqual(30f, s.verticalFovDegrees);
            Assert.AreEqual(50f, s.pitchDegrees);
            Assert.AreEqual(45f, s.yawDegrees);
            Assert.AreEqual(22f, s.distance);
            Assert.AreEqual(0.15f, s.lookAheadFraction);
            Assert.AreEqual(3f, s.lookAheadMaxMeters);
            Assert.AreEqual(1.5f, s.traumaDecayPerSecond);
            Assert.AreEqual(0.7f, s.defaultShakeIntensity);
        }

        [Test]
        public void LookAhead_IsFractionOfDistance()
        {
            var o = CameraLookAhead.ComputeOffset(Vector3.zero, new Vector3(10f, 5f, 0f), 0.15f, 3f);
            Assert.AreEqual(1.5f, o.x, 1e-5f);
            Assert.AreEqual(0f, o.y);
        }

        [Test]
        public void LookAhead_NeverExceedsCap()
        {
            var o = CameraLookAhead.ComputeOffset(Vector3.zero, new Vector3(100f, 0f, 100f), 0.15f, 3f);
            Assert.AreEqual(3f, o.magnitude, 1e-4f);
            Assert.LessOrEqual(CameraLookAhead.ComputeOffset(Vector3.zero, new Vector3(1000f, 0f, 0f), 0.15f, 3f).magnitude, 3f + 1e-4f);
        }

        [Test]
        public void LookAhead_ZeroWhenAimOnPlayer()
        {
            Assert.AreEqual(Vector3.zero, CameraLookAhead.ComputeOffset(Vector3.one, Vector3.one, 0.15f, 3f));
        }

        [Test]
        public void Zoom_IsClampedTo18_26()
        {
            var s = Defaults();
            Assert.AreEqual(18f, s.ClampDistance(5f));
            Assert.AreEqual(26f, s.ClampDistance(99f));
            Assert.AreEqual(22f, s.ClampDistance(22f));
        }

        [Test]
        public void Pose_HasExpectedPitchYawAndDistance()
        {
            Vector3 focus = new Vector3(3f, 0f, 4f);
            var e = CameraLookAhead.Rotation(50f, 45f).eulerAngles;
            Assert.AreEqual(50f, e.x, 1e-3f);
            Assert.AreEqual(45f, e.y, 1e-3f);
            Vector3 pos = CameraLookAhead.Position(focus, 50f, 45f, 22f);
            Assert.AreEqual(22f, (pos - focus).magnitude, 1e-3f);
            Assert.Greater(pos.y, focus.y);
        }

        [Test]
        public void Shake_IsTraumaSquared()
        {
            Assert.AreEqual(0.25f, CameraShakeService.Shake(0.5f), 1e-6f);
            Assert.AreEqual(1f, CameraShakeService.Shake(3f), 1e-6f);
        }

        [Test]
        public void Trauma_DecaysAtRate_AndClamps()
        {
            Assert.AreEqual(0.5f, CameraShakeService.Decay(1f, 1f / 3f, 1.5f), 1e-5f);
            Assert.AreEqual(0f, CameraShakeService.Decay(0.1f, 1f, 1.5f));
            Assert.AreEqual(1f, CameraShakeService.AddTrauma(0.8f, 0.5f));
        }

        [Test]
        public void Service_IntensitySlider_ScalesAndClamps()
        {
            var svc = new CameraShakeService();
            Assert.AreEqual(0.7f, svc.Intensity);
            svc.AddTrauma(0.5f);
            Assert.AreEqual(0.25f * 0.7f, svc.Amount, 1e-6f);
            svc.Intensity = 2f;
            Assert.AreEqual(1f, svc.Intensity);
            svc.Intensity = 0f;
            Assert.AreEqual(0f, svc.Amount);
            svc.Tick(1f);
            Assert.AreEqual(0f, svc.Trauma);
        }
    }
}
