#if UNITY_EDITOR
using System.Collections;
using Eclipse.AI.Enemies;
using Eclipse.AI.Spawning;
using Eclipse.Gameplay.Combat.Integration;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.AI.Behavior
{
    /// <summary>M07 T8: the AI Sandbox loads, spawns one of each Ashen Wastes enemy and they stay alive and active (no console errors).</summary>
    public class AiSandboxLoadTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox/AISandbox.unity";
        private static readonly string[] Names = { "CinderHusk", "AshbornMarauder", "SlagHurler", "KilnBrute" };

        private static IEnumerator LoadSandbox()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 10; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_loads_and_spawns_one_of_each_and_they_stay_alive()
        {
            yield return LoadSandbox();

            var bootstrap = Object.FindFirstObjectByType<AiSandboxBootstrap>();
            Assert.IsNotNull(bootstrap, "AiSandboxBootstrap missing");
            Assert.IsTrue(bootstrap.IsReady, "bootstrap not ready");
            Assert.IsNotNull(CombatWorld.Current, "CombatWorld missing");
            Assert.IsNotNull(CombatWorld.Current.Player, "player missing");
            Assert.AreEqual(4, bootstrap.EnemyPrefabs.Length);

            var spawned = new EnemyController[4];
            for (int i = 0; i < 4; i++)
            {
                spawned[i] = bootstrap.SpawnEnemy(i);
                Assert.IsNotNull(spawned[i], Names[i] + " not spawned");
                StringAssert.StartsWith(Names[i], spawned[i].name);
            }

            yield return new WaitForSeconds(4f);

            Assert.AreEqual(4, bootstrap.Spawner.Alive);
            for (int i = 0; i < 4; i++)
            {
                EnemyController c = spawned[i];
                Assert.IsTrue(c != null && c.gameObject.activeInHierarchy, Names[i] + " inactive");
                Assert.IsTrue(c.IsReady, Names[i] + " not ready");
                Assert.IsTrue(c.Entity.IsAlive, Names[i] + " died");
                Assert.IsFalse(float.IsNaN(c.transform.position.x + c.transform.position.z), Names[i] + " position is NaN");
            }
        }

        [UnityTest]
        public IEnumerator Wave_spawns_within_the_live_cap()
        {
            yield return LoadSandbox();
            var bootstrap = Object.FindFirstObjectByType<AiSandboxBootstrap>();
            Assert.IsTrue(bootstrap.IsReady);

            bootstrap.SpawnWave();
            yield return new WaitForSeconds(3f);

            Assert.Greater(bootstrap.Spawner.TotalSpawned, 0, "the wave spawned nothing");
            Assert.LessOrEqual(bootstrap.Spawner.Alive, 6, "live cap exceeded");
        }
    }
}
#endif
