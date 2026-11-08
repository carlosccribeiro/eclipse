using UnityEngine;

namespace Eclipse.Gameplay.Cameras
{
    /// <summary>
    /// Isometric camera parameters (06 section 3.11). Asset: Settings/Camera/IsoCamera_Default.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Eclipse/Camera/Iso Camera Settings", fileName = "IsoCamera_Default")]
    public sealed class IsoCameraSettings : ScriptableObject
    {
        [Header("06 3.11 - camera table")]
        public float verticalFovDegrees = 30f;
        public float pitchDegrees = 50f;
        public float yawDegrees = 45f;
        public float distance = 22f;
        public float minDistance = 18f;
        public float maxDistance = 26f;

        [Header("Look-ahead (06 3.11)")]
        [Range(0f, 1f)] public float lookAheadFraction = 0.15f;
        public float lookAheadMaxMeters = 3f;

        [Header("Shake (06 3.11)")]
        public float traumaDecayPerSecond = 1.5f;
        [Range(0f, 1f)] public float defaultShakeIntensity = 0.7f;

        [Header("Feel placeholders (not in GDD; tune in playtest)")]
        public float followSmoothTime = 0.12f;
        public float zoomSmoothTime = 0.4f;
        public float shakeMaxOffsetMeters = 0.35f;
        public float shakeFrequency = 25f;

        [Header("Occlusion (06 3.11; values are placeholders)")]
        public float occlusionSphereRadius = 0.6f;
        public float occlusionFadeSpeed = 6f;
        [Range(0f, 1f)] public float occlusionMinOpacity = 0.15f;

        public float ClampDistance(float value) => Mathf.Clamp(value, minDistance, maxDistance);
    }
}
