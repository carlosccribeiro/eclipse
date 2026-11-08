using UnityEngine;

namespace Eclipse.Gameplay.Feedback
{
    /// <summary>Squash curve: scale factor along the push axis over normalized time. Pure.</summary>
    public static class SquashMath
    {
        /// <summary>1 at t=0 and t=1, dips to 1 - amplitude at t=0.5 (sine bump).</summary>
        public static float Scale(float t01, float amplitude)
        {
            t01 = Mathf.Clamp01(t01);
            return 1f - amplitude * Mathf.Sin(t01 * Mathf.PI);
        }
    }

    /// <summary>
    /// Brief visual push on Light/Medium hits (06 3.11; duration from the profile, 0.1 s). Scales the visual child
    /// only, volume-preserving on the other axes. Driven by <see cref="Update"/> (visual, not simulation).
    /// </summary>
    public sealed class SquashEffect : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [Tooltip("Visual tuning only (not balance): depth of the squash, 0..0.5.")]
        [SerializeField, Range(0f, 0.5f)] private float amplitude = 0.15f;

        private float _duration, _elapsed;
        private Vector3 _baseScale = Vector3.one;
        private bool _active;

        private void Awake()
        {
            if (visual == null) visual = transform;
            _baseScale = visual.localScale;
        }

        public void Squash(float seconds)
        {
            if (seconds <= 0f) return;
            _duration = seconds;
            _elapsed = 0f;
            _active = true;
        }

        private void Update()
        {
            if (!_active) return;
            _elapsed += Time.deltaTime;
            float t = _elapsed / _duration;
            if (t >= 1f)
            {
                _active = false;
                visual.localScale = _baseScale;
                return;
            }
            float y = SquashMath.Scale(t, amplitude);
            float xz = 1f / Mathf.Sqrt(y);
            visual.localScale = new Vector3(_baseScale.x * xz, _baseScale.y * y, _baseScale.z * xz);
        }
    }
}
