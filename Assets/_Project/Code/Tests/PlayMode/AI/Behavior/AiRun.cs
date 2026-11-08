#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Eclipse.AI.Enemies;
using Eclipse.AI.Locomotion;
using Eclipse.AI.Spawning;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.PlayMode.AI.Behavior
{
    /// <summary>
    /// Deterministic harness for the M07 behaviour tests: loads the AI Sandbox, freezes real time (the real FixedUpdate must not add
    /// ticks on top of the manual ones) and steps the scheduler and the locomotion by hand, one 60 Hz tick per <see cref="Tick"/>.
    /// Collects the hits dealt to and by the player.
    /// </summary>
    public sealed class AiRun
    {
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/AISandbox.unity";
        public const int HuskIndex = 0, MarauderIndex = 1, HurlerIndex = 2, BruteIndex = 3;
        private const int UntouchableSource = 9002;

        public AiSandboxBootstrap Bootstrap;
        public CombatWorld World;
        public GameplayServices Services;
        public CombatActor Player;
        public PlayerMotor Motor;
        public LocomotionSystem Loco;
        public UnityEngine.Camera Cam;
        public readonly List<OnHitDealt> HitsOnPlayer = new List<OnHitDealt>(4096);
        public readonly List<OnHitDealt> HitsByPlayer = new List<OnHitDealt>(4096);
        public Vector3 AimPoint;
        public bool HasAim;
        private float _savedTimeScale;
        private bool _frozen;

        public EnemyServices Enemy => Bootstrap.Services;
        public int Frame => Services.Scheduler.CurrentFrame;

        public IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 10; i++) yield return null;

            Bootstrap = Object.FindFirstObjectByType<AiSandboxBootstrap>();
            Assert.IsNotNull(Bootstrap, "AiSandboxBootstrap missing");
            Assert.IsTrue(Bootstrap.IsReady, "bootstrap not ready");
            World = CombatWorld.Current;
            Assert.IsNotNull(World, "CombatWorld missing");
            Services = Object.FindFirstObjectByType<GameplayServices>();
            Player = World.Player;
            Assert.IsNotNull(Player, "player missing");
            Motor = Player.GetComponent<PlayerMotor>();
            Loco = Bootstrap.GetComponent<LocomotionSystem>();
            Cam = UnityEngine.Camera.main;
            Assert.IsNotNull(Cam, "main camera missing");

            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _frozen = true;
        }

        public void Restore()
        {
            if (_frozen) Time.timeScale = _savedTimeScale;
            _frozen = false;
        }

        public void PlacePlayer(Vector3 position, Vector3 facing)
        {
            Motor.Teleport(position);
            Player.Entity.Position = position; // the shell only mirrors it on the next tick; keep the AI and the harness from reading a stale spot
            Motor.SetFacing(facing);
            UnityEngine.Physics.SyncTransforms();
        }

        /// <summary>Permanent invulnerability window: the player never takes damage or reacts to hits (same trick as the SoakBot).</summary>
        public void MakePlayerUntouchable()
        {
            Player.InfiniteHealth = true;
            Player.Entity.Invulnerability.AddWindow(0, int.MaxValue - 1, 0, UntouchableSource);
        }

        public EnemyController Spawn(int prefabIndex, Vector3 position, int level = 1)
        {
            EnemyController c = Bootstrap.Spawner.SpawnAt(Bootstrap.EnemyPrefabs[prefabIndex], position, level);
            Assert.IsNotNull(c, "spawn failed");
            return c;
        }

        /// <summary>Lets Start run on freshly spawned objects (real frames; the simulation stays frozen).</summary>
        public IEnumerator Settle()
        {
            for (int i = 0; i < 3; i++) yield return null;
        }

        public int OwnerId(EnemyController c) => c.Entity.Id.Value;

        public float PlanarDistance(EnemyController c)
        {
            Vector3 d = c.transform.position - Player.Entity.Position;
            d.y = 0f;
            return d.magnitude;
        }

        /// <summary>One simulation tick plus the per-rendered-frame work (feedback processing and event flush).</summary>
        public void Tick()
        {
            if (HasAim) Services.InputReader.Aim = Cam.WorldToScreenPoint(AimPoint);
            Services.Scheduler.StepManual(1);
            LocomotionStepper.Step(Loco.Data, Loco.Config, SimulationFrame.FixedDeltaSeconds);
            UnityEngine.Physics.SyncTransforms();
            var hits = World.Events.Read<OnHitDealt>();
            for (int i = 0; i < hits.Count; i++)
            {
                OnHitDealt h = hits[i];
                if (h.Target == Player.Entity.Id) HitsOnPlayer.Add(h);
                if (h.Source == Player.Entity.Id) HitsByPlayer.Add(h);
            }
            World.ProcessEvents();
            World.Events.EndFrame();
        }

        public void Tick(int count)
        {
            for (int i = 0; i < count; i++) Tick();
        }

        public static EnemyController Controller(GameObject prefab) => prefab.GetComponent<EnemyController>();
    }
}
#endif
