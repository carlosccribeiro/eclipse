using UnityEngine;

namespace Eclipse.Gameplay.Cameras
{
    /// <summary>
    /// Trauma model (06 section 3.11): shake = trauma^2, trauma decays 1.5/s, user intensity slider 0..1 (default 0.7).
    /// Fed by M05 (hit Heavy 0.15, crit Heavy 0.25, Wall Impact 0.2, Ultimate 0.5, boss hit received 0.4).
    /// </summary>
    public sealed class CameraShakeService
    {
        public float Trauma { get; private set; }
        public float DecayPerSecond { get; set; } = 1.5f;

        float _intensity = 0.7f;
        /// <summary>Accessibility slider, clamped to 0..1.</summary>
        public float Intensity { get => _intensity; set => _intensity = Mathf.Clamp01(value); }

        /// <summary>Shake amplitude before the intensity slider: trauma squared.</summary>
        public float RawShake => Shake(Trauma);

        /// <summary>Final amplitude 0..1 (trauma^2 times intensity).</summary>
        public float Amount => Shake(Trauma) * _intensity;

        public static float Shake(float trauma) { trauma = Mathf.Clamp01(trauma); return trauma * trauma; }
        public static float AddTrauma(float current, float amount) => Mathf.Clamp01(current + Mathf.Max(0f, amount));
        public static float Decay(float trauma, float deltaTime, float ratePerSecond) => Mathf.Max(0f, trauma - ratePerSecond * deltaTime);

        public void AddTrauma(float amount) => Trauma = AddTrauma(Trauma, amount);
        public void Tick(float deltaTime) => Trauma = Decay(Trauma, deltaTime, DecayPerSecond);
        public void Clear() => Trauma = 0f;
    }
}
