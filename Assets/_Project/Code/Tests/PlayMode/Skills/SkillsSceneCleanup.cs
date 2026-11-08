#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Eclipse.Tests.PlayMode.Skills
{
    /// <summary>The skill tests leave the Combat Sandbox (with spawned dummies) loaded; swap to an empty scene so later fixtures start clean.</summary>
    [SetUpFixture]
    public sealed class SkillsSceneCleanup
    {
        [OneTimeTearDown]
        public void ResetScene()
        {
            Scene empty = SceneManager.CreateScene("SkillsCleanupEmpty");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s != empty && s.isLoaded) SceneManager.UnloadSceneAsync(s);
            }
        }
    }
}
#endif
