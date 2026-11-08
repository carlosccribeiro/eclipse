using Eclipse.AI.Locomotion;
using NUnit.Framework;
using Unity.Mathematics;

namespace Eclipse.Tests.EditMode.AI.Locomotion
{
    public sealed class LocomotionTests
    {
        const float Dt = 1f / 60f;
        LocomotionData _data;
        LocomotionConfig _cfg;

        [SetUp]
        public void SetUp()
        {
            _data = new LocomotionData(4, 2);
            _cfg = new LocomotionConfig();
        }

        [TearDown]
        public void TearDown() => _data.Dispose();

        void Run(int ticks)
        {
            for (int t = 0; t < ticks; t++) LocomotionStepper.Step(_data, _cfg, Dt, true);
        }

        [Test]
        public void Seek_ReachesTarget()
        {
            int a = _data.Register(float2.zero, 0.4f, 4.2f);
            _data.SetSteering(a, SteeringMode.Seek, new float2(10f, 0f));
            Run(60 * 4);
            Assert.Less(math.distance(_data.GetPosition(a), new float2(10f, 0f)), 0.5f);
        }

        [Test]
        public void Arrive_SlowsDownAndStops()
        {
            int a = _data.Register(float2.zero, 0.4f, 5f);
            _data.SetSteering(a, SteeringMode.Arrive, new float2(10f, 0f));
            float maxSpeed = 0f;
            float speedNear = -1f;
            for (int t = 0; t < 60 * 8; t++)
            {
                LocomotionStepper.Step(_data, _cfg, Dt, true);
                float s = math.length(_data.GetVelocity(a));
                maxSpeed = math.max(maxSpeed, s);
                if (speedNear < 0f && math.distance(_data.GetPosition(a), new float2(10f, 0f)) < 1f) speedNear = s;
            }
            Assert.Greater(maxSpeed, 4.5f);
            Assert.Less(speedNear, maxSpeed * 0.5f);
            Assert.Less(math.length(_data.GetVelocity(a)), 0.2f);
            Assert.Less(math.distance(_data.GetPosition(a), new float2(10f, 0f)), 0.3f);
        }

        [Test]
        public void Orbit_KeepsRadius()
        {
            var center = new float2(0f, 0f);
            int a = _data.Register(new float2(8f, 0f), 0.4f, 4f);
            _data.SetSteering(a, SteeringMode.Orbit, center, 5f);
            Run(60 * 6);
            for (int t = 0; t < 60 * 4; t++)
            {
                LocomotionStepper.Step(_data, _cfg, Dt, true);
                Assert.That(math.length(_data.GetPosition(a) - center), Is.EqualTo(5f).Within(0.3f));
            }
        }

        [Test]
        public void Flee_MovesAway()
        {
            int a = _data.Register(new float2(1f, 0f), 0.4f, 4f);
            _data.SetSteering(a, SteeringMode.Flee, float2.zero);
            Run(120);
            Assert.Greater(_data.GetPosition(a).x, 5f);
        }

        [Test]
        public void Avoidance_SeparatesOverlappingAgents()
        {
            int a = _data.Register(new float2(0f, 0f), 0.4f, 4f);
            int b = _data.Register(new float2(0.1f, 0f), 0.4f, 4f);
            Assert.AreEqual(1, _data.CountOverlaps());
            Run(120);
            Assert.GreaterOrEqual(math.distance(_data.GetPosition(a), _data.GetPosition(b)), 0.8f - 0.01f);
            Assert.AreEqual(0, _data.CountOverlaps(0.4f - 0.01f));
        }

        [Test]
        public void Avoidance_CoincidentAgentsSeparate()
        {
            int a = _data.Register(float2.zero, 0.4f, 4f);
            int b = _data.Register(float2.zero, 0.4f, 4f);
            Run(120);
            Assert.Greater(math.distance(_data.GetPosition(a), _data.GetPosition(b)), 0.7f);
        }

        [Test]
        public void Speed_NeverExceedsGlobalCapOutsideCharge()
        {
            int a = _data.Register(float2.zero, 0.4f, 9f);
            _data.SetSteering(a, SteeringMode.Seek, new float2(100f, 0f));
            for (int t = 0; t < 120; t++)
            {
                LocomotionStepper.Step(_data, _cfg, Dt, true);
                Assert.LessOrEqual(math.length(_data.GetVelocity(a)), 5.8f + 1e-4f);
            }
            _data.SetCharging(a, true);
            Run(120);
            Assert.Greater(math.length(_data.GetVelocity(a)), 5.8f);
            Assert.LessOrEqual(math.length(_data.GetVelocity(a)), 9f + 1e-4f);
        }

        [Test]
        public void Speed_RespectsPerAgentMax()
        {
            int a = _data.Register(float2.zero, 0.4f, 3f);
            _data.SetSteering(a, SteeringMode.Seek, new float2(100f, 0f));
            Run(120);
            Assert.That(math.length(_data.GetVelocity(a)), Is.EqualTo(3f).Within(1e-3f));
        }

        [Test]
        public void Wall_BlocksMovement()
        {
            _data.AddWall(new float4(4f, -5f, 5f, 5f));
            int a = _data.Register(float2.zero, 0.4f, 5f);
            _data.SetSteering(a, SteeringMode.Seek, new float2(10f, 0f));
            for (int t = 0; t < 60 * 4; t++)
            {
                LocomotionStepper.Step(_data, _cfg, Dt, true);
                Assert.LessOrEqual(_data.GetPosition(a).x, 4f - 0.4f + 1e-3f);
            }
        }

        [Test]
        public void RegisterUnregister_ReusesStableIndexAndGrows()
        {
            int a = _data.Register(float2.zero, 0.4f, 4f);
            int b = _data.Register(new float2(5f, 0f), 0.4f, 4f);
            _data.Unregister(a);
            Assert.IsFalse(_data.IsActive(a));
            Assert.AreEqual(1, _data.ActiveCount);
            int c = _data.Register(new float2(9f, 0f), 0.4f, 4f);
            Assert.AreEqual(a, c);
            for (int i = 0; i < 10; i++) _data.Register(new float2(i * 3f, 20f), 0.4f, 4f);
            Assert.GreaterOrEqual(_data.Capacity, 12);
            Assert.AreEqual(5f, _data.GetPosition(b).x);
        }

        [Test]
        public void AnimationLod_ReducesRateWithDistance()
        {
            int near = 0, mid = 0, far = 0;
            for (int f = 0; f < 8; f++)
            {
                if (AnimationLod.ShouldAnimateThisFrame(5f, f)) near++;
                if (AnimationLod.ShouldAnimateThisFrame(30f, f)) mid++;
                if (AnimationLod.ShouldAnimateThisFrame(60f, f)) far++;
            }
            Assert.AreEqual(8, near);
            Assert.AreEqual(4, mid);
            Assert.AreEqual(2, far);
        }
    }
}
