using UnityEngine;

namespace Eclipse.Gameplay.Combat.Integration
{
    /// <summary>Clears the combat event stream at the very end of LateUpdate (after every reader, 11 3.7).</summary>
    [DefaultExecutionOrder(32000)]
    public sealed class CombatStreamFlusher : MonoBehaviour
    {
        private CombatWorld _world;

        public void Bind(CombatWorld world) => _world = world;

        private void LateUpdate()
        {
            if (_world != null && _world.Events != null) _world.Events.EndFrame();
        }
    }
}
