using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using UnityEngine;

namespace Eclipse.Gameplay.Combat.Integration
{
    /// <summary>
    /// Procedural greybox swing (M05 T10, decision D4: no external animation). Rotates the weapon pivot by a curve
    /// driven by the controller's phase and the frame inside it, so the visual is always in sync with the frame data
    /// and freezes with hit-stop (the controller does not advance a frozen entity). Pure presentation: reads the state
    /// in LateUpdate and never touches simulation. Angles are visual tuning, not balance.
    /// </summary>
    public sealed class WeaponVisualDriver : MonoBehaviour
    {
        [SerializeField] private AttackController attack;
        [SerializeField] private Transform pivot;
        [Header("Visual tuning (degrees)")]
        [SerializeField] private float restYaw = 35f;
        [SerializeField] private float restPitch = 55f;
        [SerializeField] private float basicPitch = 15f;
        [SerializeField] private float heavyWindupPitch = -120f;
        [SerializeField] private float heavyEndPitch = 60f;
        [Tooltip("Half sweep used when the step data defines no arc (Heavy/Dash are undefined in the docs).")]
        [SerializeField] private float fallbackHalfArc = 60f;

        /// <summary>Last pose applied (yaw, pitch); exposed for tests.</summary>
        public Vector2 LastPose { get; private set; }

        private void LateUpdate()
        {
            if (attack == null || pivot == null || attack.Sequencer == null) return;
            Vector2 pose = Evaluate(attack.Sequencer.State, attack.Sequencer.CurrentComboIndex);
            LastPose = pose;
            pivot.localRotation = Quaternion.Euler(pose.y, pose.x, 0f);
        }

        private Vector2 Evaluate(AttackRuntimeState s, int comboIndex)
        {
            var rest = new Vector2(restYaw, restPitch);
            if (!s.IsActive) return rest;

            bool heavy = s.Kind == AttackKind.Heavy;
            float half = s.Step.ArcDegrees > 0f ? s.Step.ArcDegrees * 0.5f : fallbackHalfArc;
            float dir = (comboIndex & 1) == 0 ? 1f : -1f;
            Vector2 windup = heavy ? new Vector2(0f, heavyWindupPitch) : new Vector2(half * dir, basicPitch);
            Vector2 end = heavy ? new Vector2(0f, heavyEndPitch) : new Vector2(-half * dir, basicPitch);

            float t = s.PhaseLength > 0 && s.PhaseLength != int.MaxValue ? Mathf.Clamp01((float)s.PhaseFrame / s.PhaseLength) : 0f;
            switch (s.Phase)
            {
                case AttackPhase.Startup:
                    // Charging: keep the wind-up pose, nudged back by the charge fraction (visual feedback of the hold).
                    if (s.PhaseLength == int.MaxValue || s.IsCharging)
                        return Vector2.Lerp(windup, windup + new Vector2(0f, heavy ? -20f : 0f), s.ChargeFraction);
                    return Vector2.Lerp(rest, windup, EaseOut(t));
                case AttackPhase.Active:
                    return Vector2.Lerp(windup, end, EaseOut(t));
                default:
                    return Vector2.Lerp(end, rest, t);
            }
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
