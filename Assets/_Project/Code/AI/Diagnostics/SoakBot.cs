using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Player;
using UnityEngine;

namespace Eclipse.AI.Diagnostics
{
    /// <summary>
    /// Stand-in player for the soak test (M07 T9): walks a circle, attacks and dodges through the same <see cref="PlayerInputReader"/>
    /// a human uses, so the whole M04/M05 player pipeline runs. It is made untouchable with a permanent invulnerability window plus
    /// infinite health, so the 60 s run is never cut short by the player dying (the enemies still run their full attack logic).
    /// SANDBOX/diagnostic only: the numbers below shape the load, they are not balance data.
    /// </summary>
    public sealed class SoakBot : MonoBehaviour
    {
        private const int InvulnerabilitySource = 9001;

        [SerializeField] private Vector3 center = Vector3.zero;
        [SerializeField] private float circleRadius = 7f;
        [Tooltip("Seconds between attack presses (the reader repeats the combo while held).")]
        [SerializeField] private float attackInterval = 0.6f;
        [SerializeField] private float dodgeInterval = 2.2f;

        private CombatActor _player;
        private PlayerMotor _motor;
        private PlayerInputReader _input;
        private float _nextAttack, _nextDodge;
        private float _clockwise = 1f;
        private bool _armed;

        public bool Armed => _armed;

        /// <summary>Binds to the player (call once the CombatWorld has one). Returns false when it is not there yet.</summary>
        public bool TryArm()
        {
            CombatWorld world = CombatWorld.Current;
            if (world == null || world.Player == null || world.Services == null || world.Player.Entity == null) return false;
            _player = world.Player;
            _input = world.Services.InputReader;
            _motor = _player.GetComponent<PlayerMotor>();
            _player.InfiniteHealth = true;
            _player.Entity.Invulnerability.AddWindow(0, int.MaxValue - 1, 0, InvulnerabilitySource);
            _player.Entity.Health.Restore(1f);
            _armed = true;
            return true;
        }

        public void Disarm()
        {
            if (_armed && _input != null) _input.Move = Vector2.zero;
            _armed = false;
        }

        private void Update()
        {
            if (!_armed || _player == null) return;
            Vector3 pos = _player.Entity.Position;
            Vector3 radial = pos - center;
            radial.y = 0f;
            float dist = radial.magnitude;
            Vector3 outward = dist > 1e-3f ? radial / dist : Vector3.right;
            Vector3 tangent = new Vector3(-outward.z, 0f, outward.x) * _clockwise;
            // Tangent plus a spring back to the circle.
            Vector3 dir = tangent + outward * Mathf.Clamp((circleRadius - dist) * 0.5f, -1f, 1f);
            dir.Normalize();
            float yaw = _motor != null ? _motor.InputBasisYawDegrees : 0f;
            Vector3 stick3 = Quaternion.Euler(0f, -yaw, 0f) * dir;
            _input.Move = new Vector2(stick3.x, stick3.z);

            float t = Time.time;
            if (t >= _nextAttack)
            {
                _input.Press(InputActionId.Basic);
                _nextAttack = t + attackInterval;
            }
            if (t >= _nextDodge)
            {
                _input.Press(InputActionId.Dodge);
                _nextDodge = t + dodgeInterval;
                _clockwise = -_clockwise;
            }
        }

        private void OnDestroy()
        {
            if (_armed && _player != null && _player.Entity != null) _player.Entity.Invulnerability.RemoveSource(InvulnerabilitySource);
        }
    }
}
