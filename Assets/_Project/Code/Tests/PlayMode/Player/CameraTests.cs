#if UNITY_EDITOR
using Eclipse.Gameplay.Cameras;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Tests.PlayMode.Player.Camera
{
    public class CameraTests
    {
        private const string SettingsPath = "Assets/_Project/Settings/Camera/IsoCamera_Default.asset";
        private GameObject _root, _target, _occluder;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            if (_target != null) Object.DestroyImmediate(_target);
            if (_occluder != null) Object.DestroyImmediate(_occluder);
        }

        [Test]
        public void Rig_has_fov30_pitch50_yaw45_distance22()
        {
            var settings = AssetDatabase.LoadAssetAtPath<IsoCameraSettings>(SettingsPath);
            Assert.IsNotNull(settings);
            _root = new GameObject("Rig");
            var rig = _root.AddComponent<IsoCameraRig>();
            var vcamGo = new GameObject("VCam");
            vcamGo.transform.SetParent(_root.transform);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            rig.VirtualCamera = vcam;
            _target = new GameObject("Target");
            _target.transform.position = new Vector3(5, 0, -3);
            rig.Target = _target.transform;
            rig.Configure(settings);

            Assert.AreEqual(30f, vcam.Lens.FieldOfView, 1e-3f);
            Vector3 e = vcamGo.transform.eulerAngles;
            Assert.AreEqual(50f, e.x, 1e-2f);
            Assert.AreEqual(45f, e.y, 1e-2f);
            Assert.AreEqual(22f, rig.CurrentDistance, 1e-3f);
            Assert.AreEqual(22f, Vector3.Distance(vcamGo.transform.position, _target.transform.position), 1e-2f);
        }

        [Test]
        public void Look_ahead_never_exceeds_3_meters()
        {
            var settings = AssetDatabase.LoadAssetAtPath<IsoCameraSettings>(SettingsPath);
            Assert.AreEqual(3f, settings.lookAheadMaxMeters, 1e-4f);
            for (float d = 0.5f; d <= 200f; d += 7.3f)
            {
                var off = CameraLookAhead.ComputeOffset(Vector3.zero, new Vector3(d, 0, d * 0.5f), settings.lookAheadFraction, settings.lookAheadMaxMeters);
                Assert.LessOrEqual(off.magnitude, 3f + 1e-4f, "aim distance " + d);
            }
            var near = CameraLookAhead.ComputeOffset(Vector3.zero, new Vector3(10, 0, 0), 0.15f, 3f);
            Assert.AreEqual(1.5f, near.magnitude, 1e-4f);
        }

        [Test]
        public void Object_between_camera_and_player_receives_fader()
        {
            var settings = AssetDatabase.LoadAssetAtPath<IsoCameraSettings>(SettingsPath);
            _root = new GameObject("Fader");
            var fader = _root.AddComponent<OcclusionFader>();
            fader.Settings = settings;
            _occluder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _occluder.layer = Eclipse.Gameplay.Physics.PhysicsLayers.Wall;
            _occluder.transform.position = new Vector3(0, 1, 5);
            _occluder.transform.localScale = new Vector3(2, 4, 2);
            UnityEngine.Physics.SyncTransforms();

            var renderer = _occluder.GetComponent<Renderer>();
            fader.Step(new Vector3(0, 1, 15), new Vector3(0, 1, 0), 0.1f);
            Assert.IsTrue(fader.IsFading(renderer), "occluder must be fading");

            for (int i = 0; i < 30; i++) fader.Step(new Vector3(15, 1, 15), new Vector3(15, 1, 0), 0.1f); // line no longer crosses it
            Assert.IsFalse(fader.IsFading(renderer), "fader released after the line clears");
        }
    }
}
#endif
