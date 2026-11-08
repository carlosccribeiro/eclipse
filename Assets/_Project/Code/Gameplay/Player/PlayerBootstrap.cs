using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Player
{
    /// <summary>
    /// Composition of the player: binds motor, aim and dodge to <see cref="GameplayServices"/> and points the
    /// isometric rig and occlusion fader at the player. No gameplay rules here.
    /// </summary>
    public sealed class PlayerBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private DodgeController dodge;
        [SerializeField] private MovementProfile movementProfile;
        [SerializeField] private DodgeConfig dodgeConfig;

        public PlayerAim Aim { get; private set; }

        private void Start()
        {
            var services = FindFirstObjectByType<GameplayServices>();
            if (services == null)
            {
                Debug.LogError("PlayerBootstrap: GameplayServices not found in the scene (add GameplayBootstrap.prefab).", this);
                return;
            }

            var rig = services.CameraRig;
            float yaw = rig != null ? rig.Yaw : 45f;
            motor.InputBasisYawDegrees = yaw;

            Aim = new PlayerAim { Camera = services.MainCamera, InputBasisYawDegrees = yaw };
            motor.Initialize(services.Scheduler, services.InputReader, movementProfile, Aim);
            dodge.Initialize(services.Scheduler, services.InputReader, motor, dodgeConfig);

            if (rig != null)
            {
                rig.Target = transform;
                rig.Aim = Aim;
                if (rig.Settings != null) rig.Configure(rig.Settings);
            }
            if (services.OcclusionFader != null)
            {
                services.OcclusionFader.Target = transform;
                if (rig != null) services.OcclusionFader.Settings = rig.Settings;
            }
        }
    }
}
