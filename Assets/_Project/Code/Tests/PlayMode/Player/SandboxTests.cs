#if UNITY_EDITOR
using System.Collections;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Player.Sandbox
{
    public class SandboxTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox/CombatSandbox.unity";

        [UnityTest]
        public IEnumerator Scene_loads_without_errors_and_ticks()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 10; i++) yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            var services = Object.FindFirstObjectByType<GameplayServices>();
            Assert.IsNotNull(services, "GameplayServices missing");
            Assert.IsNotNull(Object.FindFirstObjectByType<PlayerMotor>(), "PlayerMotor missing");
            Assert.IsNotNull(UnityEngine.Camera.main, "Main camera missing");
            Assert.Greater(services.Scheduler.CurrentFrame, 0, "TickScheduler did not tick");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
