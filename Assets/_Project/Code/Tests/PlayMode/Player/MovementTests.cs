#if UNITY_EDITOR
using Eclipse.Gameplay.Cameras;
using Eclipse.Gameplay.Physics;
using Eclipse.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.PlayMode.Player.Movement
{
    public class MovementTests
    {
        private PlayerTestRig _rig;

        [SetUp] public void SetUp() => _rig = new PlayerTestRig(false);
        [TearDown] public void TearDown() => _rig.Dispose();

        [Test]
        public void Forward_60_ticks_moves_6_meters()
        {
            _rig.Input.Move = Vector2.up;
            _rig.Scheduler.StepManual(60);
            Assert.AreEqual(6.0f, _rig.Motor.SimPosition.z, 0.05f);
            Assert.AreEqual(0f, _rig.Motor.SimPosition.x, 0.01f);
        }

        [Test]
        public void Wall_blocks_movement()
        {
            _rig.Box(new Vector3(0, 1, 3f), new Vector3(10, 2, 1), PhysicsLayers.Wall); // near face z = 2.5
            _rig.Input.Move = Vector2.up;
            _rig.Scheduler.StepManual(60);
            Assert.Less(_rig.Motor.SimPosition.z, 2.5f - 0.4f + 0.01f);
            Assert.Greater(_rig.Motor.SimPosition.z, 1.9f);
        }

        [Test]
        public void Movement_speed_bonus_changes_movement()
        {
            _rig.Motor.SpeedMultiplier = 1.5f;
            _rig.Input.Move = Vector2.up;
            _rig.Scheduler.StepManual(60);
            Assert.AreEqual(9.0f, _rig.Motor.SimPosition.z, 0.05f);
        }

        [Test]
        public void Speed_floor_is_40_percent_of_base()
        {
            _rig.Motor.SpeedMultiplier = 0.1f; // heavy slow, below the floor
            _rig.Input.Move = Vector2.up;
            _rig.Scheduler.StepManual(60);
            Assert.AreEqual(2.4f, _rig.Motor.SimPosition.z, 0.05f);
        }

        [Test]
        public void Aim_projects_cursor_with_test_camera()
        {
            var rt = _rig.Track(new RenderTexture(1280, 720, 16));
            var camGo = _rig.Track(new GameObject("TestCam"));
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.targetTexture = rt;
            cam.fieldOfView = 30f;
            // Camera looks at (3, 1.2, 0): its center ray crosses the chest plane exactly there.
            Vector3 focus = new Vector3(3f, 1.2f, 0f);
            camGo.transform.SetPositionAndRotation(CameraLookAhead.Position(focus, 50f, 45f, 22f), CameraLookAhead.Rotation(50f, 45f));

            var aim = new PlayerAim { Camera = cam, InputBasisYawDegrees = 45f };
            _rig.Input.Aim = new Vector2(rt.width * 0.5f, rt.height * 0.5f);
            _rig.Input.AimIsGamepad = false;
            aim.Evaluate(_rig.Input, Vector3.zero, Vector3.zero, Vector3.forward);

            Assert.AreEqual(3f, aim.AimPoint.x, 0.05f);
            Assert.AreEqual(1.2f, aim.AimPoint.y, 0.01f);
            Assert.AreEqual(0f, aim.AimPoint.z, 0.05f);
            Assert.AreEqual(1f, aim.AimDirection.x, 0.01f);
            Assert.AreEqual(0f, aim.AimDirection.z, 0.01f);
        }
    }
}
#endif
