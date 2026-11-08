using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Eclipse.Tests.PlayMode.Combat
{
    /// <summary>
    /// Combat tests load the CombatSandbox (dummies sit in the way of the M04 Player tests). After the whole Combat
    /// namespace has run, swap to an empty scene so later fixtures start from a clean world.
    /// </summary>
    [SetUpFixture]
    public sealed class CombatSceneCleanup
    {
        [OneTimeTearDown]
        public void ResetScene()
        {
            Scene empty = SceneManager.CreateScene("CombatCleanupEmpty");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s != empty && s.isLoaded) SceneManager.UnloadSceneAsync(s);
            }
        }
    }
}
