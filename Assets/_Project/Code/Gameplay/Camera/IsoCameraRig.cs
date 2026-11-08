using Eclipse.Gameplay.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Eclipse.Gameplay.Cameras
{
    /// <summary>
    /// Isometric rig (06 section 3.11): fixed pitch/yaw, perspective FOV, follow with look-ahead, zoom clamp and shake.
    /// Drives a Cinemachine 3 <see cref="CinemachineCamera"/> (no procedural body) read by the CinemachineBrain on the main camera.
    /// Cosmetic only: uses frame time, not the fixed simulation tick.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class IsoCameraRig : MonoBehaviour
    {
        public IsoCameraSettings Settings;
        public CinemachineCamera VirtualCamera;
        public Transform Target;
        /// <summary>Optional: aim point source for look-ahead.</summary>
        public PlayerAim Aim;

        public CameraShakeService Shake { get; } = new CameraShakeService();
        public float CurrentDistance { get; private set; }
        public float TargetDistance { get; private set; }
        public Vector3 LookAheadOffset { get; private set; }
        /// <summary>Yaw to give PlayerMotor/PlayerAim InputBasisYawDegrees (45).</summary>
        public float Yaw => Settings != null ? Settings.yawDegrees : 45f;

        Vector3 _focus;
        Vector3 _focusVelocity;
        float _zoomVelocity;

        void Awake()
        {
            if (VirtualCamera == null) VirtualCamera = GetComponentInChildren<CinemachineCamera>();
            if (Settings != null) Configure(Settings);
        }

        /// <summary>Applies a settings asset and snaps to the target.</summary>
        public void Configure(IsoCameraSettings settings)
        {
            Settings = settings;
            TargetDistance = CurrentDistance = settings.ClampDistance(settings.distance);
            Shake.DecayPerSecond = settings.traumaDecayPerSecond;
            Shake.Intensity = settings.defaultShakeIntensity;
            ApplyLens();
            if (Target != null) Snap();
        }

        /// <summary>Requests a zoom distance, clamped to the settings range.</summary>
        public void SetZoom(float distance) => TargetDistance = Settings.ClampDistance(distance);

        /// <summary>Jumps to the target without smoothing and updates the pose.</summary>
        public void Snap()
        {
            _focus = Target.position;
            _focusVelocity = Vector3.zero;
            LookAheadOffset = Vector3.zero;
            CurrentDistance = TargetDistance;
            ApplyPose(Vector3.zero);
        }

        void ApplyLens()
        {
            if (VirtualCamera == null) return;
            var lens = VirtualCamera.Lens;
            lens.FieldOfView = Settings.verticalFovDegrees;
            VirtualCamera.Lens = lens;
        }

        void LateUpdate()
        {
            if (Settings == null || Target == null) return;
            float dt = Time.deltaTime;

            Vector3 wantedLook = Aim != null
                ? CameraLookAhead.ComputeOffset(Target.position, Aim.AimPoint, Settings.lookAheadFraction, Settings.lookAheadMaxMeters)
                : Vector3.zero;
            float k = 1f - Mathf.Exp(-dt / Mathf.Max(1e-3f, Settings.followSmoothTime));
            LookAheadOffset = Vector3.ClampMagnitude(Vector3.Lerp(LookAheadOffset, wantedLook, k), Settings.lookAheadMaxMeters);

            _focus = Vector3.SmoothDamp(_focus, Target.position, ref _focusVelocity, Settings.followSmoothTime, float.PositiveInfinity, dt);
            CurrentDistance = Mathf.SmoothDamp(CurrentDistance, TargetDistance, ref _zoomVelocity, Settings.zoomSmoothTime, float.PositiveInfinity, dt);

            Shake.Tick(dt);
            ApplyPose(ShakeOffset(Time.time));
        }

        Vector3 ShakeOffset(float time)
        {
            float a = Shake.Amount;
            if (a <= 0f) return Vector3.zero;
            float t = time * Settings.shakeFrequency;
            float x = Mathf.PerlinNoise(t, 0.37f) * 2f - 1f;
            float y = Mathf.PerlinNoise(0.91f, t) * 2f - 1f;
            return new Vector3(x, y, 0f) * (a * Settings.shakeMaxOffsetMeters);
        }

        void ApplyPose(Vector3 shakeLocal)
        {
            Transform t = VirtualCamera != null ? VirtualCamera.transform : transform;
            Quaternion rot = CameraLookAhead.Rotation(Settings.pitchDegrees, Settings.yawDegrees);
            Vector3 pos = CameraLookAhead.Position(_focus + LookAheadOffset, Settings.pitchDegrees, Settings.yawDegrees, CurrentDistance);
            t.SetPositionAndRotation(pos + rot * shakeLocal, rot);
        }
    }
}
