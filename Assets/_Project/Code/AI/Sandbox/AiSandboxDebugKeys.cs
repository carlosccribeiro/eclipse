using Eclipse.AI.Spawning;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.AI.Sandbox
{
    /// <summary>
    /// AI Sandbox debug keys (M07 T8; the full console is M06). 1 Cinder Husk, 2 Ashborn Marauder, 3 Slag Hurler, 4 Kiln Brute,
    /// 5 wave. Lives in its own assembly because Eclipse.AI does not reference the Input System package.
    /// </summary>
    public sealed class AiSandboxDebugKeys : MonoBehaviour
    {
        [SerializeField] private AiSandboxBootstrap bootstrap;

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (bootstrap == null) bootstrap = FindFirstObjectByType<AiSandboxBootstrap>();
            if (bootstrap == null || !bootstrap.IsReady) return;
            if (kb.digit1Key.wasPressedThisFrame) bootstrap.SpawnEnemy(0);
            if (kb.digit2Key.wasPressedThisFrame) bootstrap.SpawnEnemy(1);
            if (kb.digit3Key.wasPressedThisFrame) bootstrap.SpawnEnemy(2);
            if (kb.digit4Key.wasPressedThisFrame) bootstrap.SpawnEnemy(3);
            if (kb.digit5Key.wasPressedThisFrame) bootstrap.SpawnWave();
        }
    }
}
