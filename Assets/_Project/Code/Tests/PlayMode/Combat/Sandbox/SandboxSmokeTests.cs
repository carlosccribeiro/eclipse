#if UNITY_EDITOR
using System.Collections;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Combat.Sandbox
{
    /// <summary>M05 T10 smoke: the sandbox loads with the combat content and a simulated Greatsword combo damages a dummy.</summary>
    public class SandboxSmokeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox/CombatSandbox.unity";

        private static IEnumerator LoadSandbox()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 5; i++) yield return null;
        }

        private static CombatActor FindDummy(CombatWorld world, string name)
        {
            for (int i = 0; i < world.Actors.Count; i++)
                if (world.Actors[i].name.StartsWith(name)) return world.Actors[i];
            return null;
        }

        [UnityTest]
        public IEnumerator Scene_loads_with_combat_content()
        {
            yield return LoadSandbox();

            var world = CombatWorld.Current;
            Assert.IsNotNull(world, "CombatWorld missing");
            Assert.IsNotNull(world.Player, "player CombatActor missing");
            foreach (string n in new[] { "TrainingDummy_Light", "TrainingDummy_Medium", "TrainingDummy_Heavy", "TrainingDummy_Colossal" })
                Assert.IsNotNull(FindDummy(world, n), n + " missing");
            Assert.IsNotNull(Object.FindFirstObjectByType<PlayerCombatRig>(), "PlayerCombatRig missing");
            Assert.AreEqual(2f, Object.FindFirstObjectByType<Eclipse.Gameplay.Combat.Attacks.AttackController>().MaxAttackSpeed, 1e-4f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Simulated_combo_damages_dummy_staggers_it_and_hit_stops()
        {
            yield return LoadSandbox();

            var world = CombatWorld.Current;
            var services = Object.FindFirstObjectByType<GameplayServices>();
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            var dummy = FindDummy(world, "TrainingDummy_Medium");
            Assert.IsNotNull(dummy);
            var overlay = Object.FindFirstObjectByType<CombatDebugOverlay>();
            if (overlay != null) overlay.Visible = false;

            // Isolate the combo: no counter-attack from the test attacker, player 1.8 m from the dummy, facing it.
            foreach (var a in Object.FindObjectsByType<TrainingAttacker>(FindObjectsSortMode.None)) a.AttackEnabled = false;
            Vector3 dummyPos = dummy.transform.position;
            motor.Teleport(dummyPos + new Vector3(0f, 0f, -1.8f));
            motor.SetFacing(Vector3.forward);
            var cam = UnityEngine.Camera.main;
            Assert.IsNotNull(cam);

            var attacker = world.Player;
            int hits = 0;
            bool sawStaggered = false, sawAttackerFrozen = false, sawTargetFrozen = false;

            services.InputReader.Press(InputActionId.Basic);
            for (int i = 0; i < 260; i++)
            {
                services.InputReader.Aim = cam.WorldToScreenPoint(dummyPos);
                services.Scheduler.StepManual(1);
                hits += world.Events.Count<OnHitDealt>();
                world.ProcessEvents();
                sawAttackerFrozen |= attacker.Entity.Clock.IsFrozen;
                sawTargetFrozen |= dummy.Entity.Clock.IsFrozen;
                sawStaggered |= dummy.Stagger.IsStaggered;
                world.Events.EndFrame(); // one "rendered frame" per tick, as LateUpdate would do
            }
            services.InputReader.Release(InputActionId.Basic);

            Assert.GreaterOrEqual(hits, 3, "the 3-hit combo should land three hits");
            // 110% + 110% + 160% of 10 damage per hit (08 4.3), armor 0; crits only add.
            Assert.GreaterOrEqual(dummy.TotalDamageTaken, 37.9f);
            Assert.IsTrue(sawStaggered, "Stagger 15+15+35 must fill the Medium bar (40)");
            Assert.IsTrue(sawAttackerFrozen && sawTargetFrozen, "hit-stop must freeze attacker and target");
            Assert.IsTrue(dummy.Entity.IsAlive, "infinite-health dummy must survive");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
