using Eclipse.Gameplay.Cameras;
using Eclipse.Gameplay.Input;
using UnityEngine;

namespace Eclipse.Gameplay.Simulation
{
    /// <summary>
    /// Scene composition root for gameplay services (lives in GameplayBootstrap.prefab): owns the 60 Hz
    /// <see cref="TickScheduler"/> (driven from FixedUpdate), the generated input wrapper and the <see cref="PlayerInputReader"/>.
    /// Holds references to the camera rig pieces so the player bootstrap can wire them.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameplayServices : MonoBehaviour
    {
        /// <summary>Input is ticked before motor (-100) and dodge (-90).</summary>
        public const int InputTickPriority = -200;
        private const int MaxTicksPerFixedUpdate = 2;

        public Camera MainCamera;
        public IsoCameraRig CameraRig;
        public OcclusionFader OcclusionFader;

        private EclipseInput _input;

        public TickScheduler Scheduler { get; } = new TickScheduler();
        public PlayerInputReader InputReader { get; } = new PlayerInputReader();

        private void Awake()
        {
            _input = new EclipseInput();
            InputReader.Bind(_input);
            _input.Enable();
            Scheduler.Register(InputReader, InputTickPriority);
        }

        private void FixedUpdate()
        {
            Scheduler.Advance(Time.fixedDeltaTime, MaxTicksPerFixedUpdate);
        }

        private void OnDestroy()
        {
            if (_input == null) return;
            _input.Disable();
            _input.Dispose();
            _input = null;
        }
    }
}
