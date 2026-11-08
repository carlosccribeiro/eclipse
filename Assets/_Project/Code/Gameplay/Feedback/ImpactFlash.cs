using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Feedback
{
    /// <summary>Frame countdown of the white flash (06 3.11: 2 f, from the profile). Pure.</summary>
    public struct FlashTimer
    {
        public int FramesRemaining { get; private set; }
        public bool Active => FramesRemaining > 0;
        public void Start(int frames) { if (frames > FramesRemaining) FramesRemaining = frames; }
        /// <summary>Consumes one tick; true while the flash is visible this tick.</summary>
        public bool Tick() { if (FramesRemaining <= 0) return false; FramesRemaining--; return true; }
    }

    /// <summary>Tints renderers white for N ticks via MaterialPropertyBlock (no material instances). Register with <see cref="Bind"/>.</summary>
    public sealed class ImpactFlash : MonoBehaviour, IFixedTickable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Color flashColor = Color.white;

        private MaterialPropertyBlock _block;
        private FlashTimer _timer;
        private bool _applied;

        public void Bind(TickScheduler scheduler, int priority = 100) => scheduler.Register(this, priority);

        public void Flash(int frames) => _timer.Start(frames);

        public void Tick(in SimulationFrame frame)
        {
            bool on = _timer.Tick();
            if (on == _applied) return;
            Apply(on);
        }

        private void Apply(bool on)
        {
            _applied = on;
            if (renderers == null) return;
            _block ??= new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (on)
                {
                    renderers[i].GetPropertyBlock(_block);
                    _block.SetColor(BaseColorId, flashColor);
                    renderers[i].SetPropertyBlock(_block);
                }
                else renderers[i].SetPropertyBlock(null);
            }
        }
    }
}
