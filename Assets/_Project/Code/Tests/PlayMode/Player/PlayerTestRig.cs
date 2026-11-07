#if UNITY_EDITOR
using System.Collections.Generic;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Tests.PlayMode.Player
{
    /// <summary>Deterministic test harness: motor + dodge on a manual TickScheduler with synthetic input (no real time).</summary>
    public sealed class PlayerTestRig
    {
        public const string KaelProfilePath = "Assets/_Project/Content/Heroes/Kael/Data/Kael_MovementProfile.asset";
        public const string DodgeConfigPath = "Assets/_Project/Content/Heroes/Shared/Data/DodgeConfig_Universal.asset";

        private readonly List<Object> _owned = new List<Object>();

        public TickScheduler Scheduler { get; } = new TickScheduler();
        public PlayerInputReader Input { get; } = new PlayerInputReader();
        public PlayerMotor Motor { get; private set; }
        public DodgeController Dodge { get; private set; }
        public DodgeConfig Config { get; private set; }

        public PlayerTestRig(bool withDodge)
        {
            Scheduler.Register(Input, GameplayServices.InputTickPriority);
            var go = Track(new GameObject("TestPlayer"));
            Motor = go.AddComponent<PlayerMotor>();
            Motor.InputBasisYawDegrees = 0f;
            var profile = AssetDatabase.LoadAssetAtPath<MovementProfile>(KaelProfilePath);
            Assert.IsNotNull(profile, "Kael MovementProfile asset missing");
            Motor.Initialize(Scheduler, Input, profile, null);
            if (withDodge)
            {
                Config = AssetDatabase.LoadAssetAtPath<DodgeConfig>(DodgeConfigPath);
                Assert.IsNotNull(Config, "DodgeConfig asset missing");
                Dodge = go.AddComponent<DodgeController>();
                Dodge.Initialize(Scheduler, Input, Motor, Config);
            }
        }

        public T Track<T>(T obj) where T : Object { _owned.Add(obj); return obj; }

        public GameObject Box(Vector3 center, Vector3 size, int layer, MassClass? mass = null)
        {
            var go = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.transform.position = center;
            go.transform.localScale = size;
            go.layer = layer;
            if (mass.HasValue) go.AddComponent<MassBodyMarker>().MassClass = mass.Value;
            UnityEngine.Physics.SyncTransforms();
            return go;
        }

        public void PressDodge() => Input.Press(InputActionId.Dodge);

        public void Dispose()
        {
            for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) Object.DestroyImmediate(_owned[i]); // reverse: camera before its RenderTexture
            _owned.Clear();
        }
    }
}
#endif
