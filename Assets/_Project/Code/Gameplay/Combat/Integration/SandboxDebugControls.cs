using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.Gameplay.Combat.Integration
{
    /// <summary>
    /// Sandbox-only keyboard shortcuts (not part of the game input map): F1 opens the player's Parry window
    /// (12 f, 06 3.8.3), F2 toggles infinite dummy health, F3 toggles the overlay, F4 toggles the test attackers.
    /// </summary>
    public sealed class SandboxDebugControls : MonoBehaviour
    {
        [SerializeField] private CombatDebugOverlay overlay;

        private void Update()
        {
            var kb = Keyboard.current;
            var world = CombatWorld.Current;
            if (kb == null || world == null || world.Player == null) return;
            if (kb.f1Key.wasPressedThisFrame) OpenParry(world);
            if (kb.f2Key.wasPressedThisFrame) ToggleInfiniteHealth(world);
            if (kb.f3Key.wasPressedThisFrame && overlay != null) overlay.Visible = !overlay.Visible;
            if (kb.f4Key.wasPressedThisFrame) ToggleAttackers();
        }

        public static void OpenParry(CombatWorld world) =>
            world.Player.States.OpenParry(world.Player.Entity.CurrentFrame + 1);

        public static void ToggleInfiniteHealth(CombatWorld world)
        {
            bool target = false;
            for (int i = 0; i < world.Actors.Count; i++)
                if (!world.Actors[i].IsPlayer) { target = !world.Actors[i].InfiniteHealth; break; }
            for (int i = 0; i < world.Actors.Count; i++)
                if (!world.Actors[i].IsPlayer) world.Actors[i].InfiniteHealth = target;
        }

        private static void ToggleAttackers()
        {
            var all = FindObjectsByType<TrainingAttacker>(FindObjectsSortMode.None);
            if (all.Length == 0) return;
            bool target = !all[0].AttackEnabled;
            for (int i = 0; i < all.Length; i++) all[i].AttackEnabled = target;
        }
    }
}
