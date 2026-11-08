using UnityEngine;

namespace Eclipse.Gameplay.Feedback
{
    /// <summary>
    /// Greybox material particles (06 3.11): a pooled ParticleSystem burst tinted per hit material. The system is
    /// reused (Emit with params), so there is no per-hit allocation. Real VFX arrive in M06/M09 through the VFX_* hooks.
    /// </summary>
    public sealed class MaterialImpactVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [Tooltip("Visual tuning only: particles per hit.")]
        [SerializeField] private int burstCount = 8;

        private ParticleSystem.EmitParams _params;

        public void Play(Vector3 position, Color tint)
        {
            if (particles == null) return;
            _params.position = position;
            _params.startColor = tint;
            particles.Emit(_params, burstCount);
        }
    }
}
