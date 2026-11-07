using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Physics;
using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Player
{
    /// <summary>Fraction of movement allowed by the current action (06 section 3.5). Values are percentages.</summary>
    public enum MovementLock
    {
        /// <summary>Movement locked (melee attacks).</summary>
        Locked = 0,
        /// <summary>60% speed (Ranged/Caster basic attacks).</summary>
        Reduced = 60,
        /// <summary>Full speed.</summary>
        Free = 100,
    }

    /// <summary>
    /// Kinematic twin-stick motor. Simulation position is advanced in <see cref="Tick"/> (60 Hz, via TickScheduler) with
    /// slide-capsule casts against Environment/Wall/LowCover/Character; the visual transform interpolates in Update.
    /// No dynamic Rigidbody. Input forward = +Z rotated by <see cref="InputBasisYawDegrees"/>.
    /// </summary>
    public sealed class PlayerMotor : MonoBehaviour, IFixedTickable
    {
        private const float SkinWidth = 0.02f;
        private const int MaxSlideIterations = 3;
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        [SerializeField] private MovementProfile profile;
        [SerializeField] private float capsuleRadius = 0.4f;
        [SerializeField] private float capsuleHeight = 1.8f;
        [Tooltip("Optional visual child interpolated between ticks; defaults to this transform.")]
        [SerializeField] private Transform visual;
        [Tooltip("Yaw (degrees) that rotates WASD/stick into world space; the camera yaw (isometric = 45).")]
        [SerializeField] private float inputBasisYawDegrees;
        [SerializeField] private int tickPriority = -100;

        private TickScheduler _scheduler;
        private PlayerInputReader _input;
        private PlayerAim _aim;
        private Vector3 _prevPos, _pos;
        private Vector3 _velocity;
        private double _lastTickTime;
        private bool _registered;

        public MovementProfile Profile { get => profile; set => profile = value; }
        public float InputBasisYawDegrees { get => inputBasisYawDegrees; set => inputBasisYawDegrees = value; }

        /// <summary>Authoritative simulation position (feet).</summary>
        public Vector3 SimPosition => _pos;
        public Vector3 Velocity => _velocity;

        /// <summary>Unit horizontal facing.</summary>
        public Vector3 Facing { get; private set; } = Vector3.forward;

        /// <summary>Movement allowance set by combat (M05) per 06 section 3.5.</summary>
        public MovementLock Lock { get; set; } = MovementLock.Free;

        /// <summary>When true the motor ignores player input (dodge in progress drives the body via <see cref="TryMove"/>).</summary>
        public bool InputSuspended { get; set; }

        /// <summary>When true, facing does not follow aim (active frames, 06 section 3.4). <see cref="SetFacing"/> still works.</summary>
        public bool RotationLocked { get; set; }

        /// <summary>(1 + Movement Speed bonus) net of slows; the profile floor (40% of base) is applied inside.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Last aim evaluation (valid after the first tick when an aim is bound).</summary>
        public PlayerAim Aim => _aim;

        /// <summary>Final speed in m/s given multiplier (with floor) and lock.</summary>
        public float CurrentSpeed => profile.ComputeSpeed(SpeedMultiplier) * ((int)Lock / 100f);

        /// <summary>Binds dependencies and registers in the scheduler. Aim is optional.</summary>
        public void Initialize(TickScheduler scheduler, PlayerInputReader input, MovementProfile movementProfile = null, PlayerAim aim = null)
        {
            Shutdown();
            _scheduler = scheduler;
            _input = input;
            _aim = aim;
            if (movementProfile != null) profile = movementProfile;
            Teleport(transform.position);
            _scheduler.Register(this, tickPriority);
            _registered = true;
        }

        private void OnDisable() => Shutdown();

        private void Shutdown()
        {
            if (_registered && _scheduler != null) _scheduler.Unregister(this);
            _registered = false;
        }

        /// <summary>Sets simulation and visual position with no sweep and no interpolation.</summary>
        public void Teleport(Vector3 position)
        {
            _pos = _prevPos = position;
            _velocity = Vector3.zero;
            ApplyVisual(1f);
        }

        /// <summary>Sets horizontal facing (used by aim, dodge and combat). Zero vectors are ignored.</summary>
        public void SetFacing(Vector3 direction)
        {
            direction.y = 0f;
            float sq = direction.sqrMagnitude;
            if (sq < 1e-8f) return;
            Facing = direction / Mathf.Sqrt(sq);
        }

        /// <summary>
        /// Moves the body by a horizontal delta respecting collision (slides along surfaces).
        /// Returns the effective displacement. For dodge/lunge/knockback; ignores <see cref="Lock"/>.
        /// </summary>
        public Vector3 TryMove(Vector3 delta)
        {
            delta.y = 0f;
            Vector3 start = _pos;
            Vector3 remaining = delta;
            for (int i = 0; i < MaxSlideIterations; i++)
            {
                float dist = remaining.magnitude;
                if (dist < 1e-5f) break;
                Vector3 dir = remaining / dist;
                if (!CastBody(_pos, dir, dist + SkinWidth, out RaycastHit hit))
                {
                    _pos += remaining;
                    break;
                }
                float allowed = Mathf.Max(0f, hit.distance - SkinWidth);
                allowed = Mathf.Min(allowed, dist);
                _pos += dir * allowed;
                Vector3 rest = remaining - dir * allowed;
                Vector3 n = hit.normal;
                n.y = 0f;
                if (n.sqrMagnitude < 1e-8f) break;
                n.Normalize();
                remaining = rest - Vector3.Dot(rest, n) * n;
            }
            return _pos - start;
        }

        public void Tick(in SimulationFrame frame)
        {
            _prevPos = _pos;
            _lastTickTime = Time.timeAsDouble;

            Vector3 moveWorld = PlayerAim.StickToWorld(_input.Move, inputBasisYawDegrees);
            if (moveWorld.sqrMagnitude > 1f) moveWorld.Normalize();

            if (!InputSuspended)
            {
                Vector3 target = moveWorld * CurrentSpeed;
                _velocity = StepVelocity(_velocity, target, profile, frame.DeltaSeconds);
                Vector3 moved = TryMove(_velocity * frame.DeltaSeconds);
                if (moved.sqrMagnitude < (_velocity * frame.DeltaSeconds).sqrMagnitude * 0.0001f) _velocity = Vector3.zero;
            }

            if (_aim != null)
            {
                _aim.InputBasisYawDegrees = inputBasisYawDegrees;
                _aim.Evaluate(_input, _pos, moveWorld, Facing);
                if (!RotationLocked) SetFacing(_aim.AimDirection);
            }
            else if (!RotationLocked && moveWorld.sqrMagnitude > 1e-4f)
            {
                SetFacing(moveWorld);
            }
        }

        /// <summary>Velocity step toward the target; infinite acceleration/deceleration snaps (testable pure math).</summary>
        public static Vector3 StepVelocity(Vector3 current, Vector3 target, MovementProfile p, float dt)
        {
            bool accelerating = target.sqrMagnitude >= current.sqrMagnitude;
            float rate = accelerating ? p.acceleration : p.deceleration;
            if (float.IsPositiveInfinity(rate)) return target;
            return Vector3.MoveTowards(current, target, rate * dt);
        }

        private void Update()
        {
            if (!_registered) return;
            float alpha = (float)((Time.timeAsDouble - _lastTickTime) / SimulationFrame.FixedDeltaSeconds);
            ApplyVisual(Mathf.Clamp01(alpha));
        }

        private void ApplyVisual(float alpha)
        {
            Transform t = visual != null ? visual : transform;
            t.position = Vector3.Lerp(_prevPos, _pos, alpha);
            if (Facing.sqrMagnitude > 0.5f) t.rotation = Quaternion.LookRotation(Facing, Vector3.up);
        }

        private bool CastBody(Vector3 feet, Vector3 dir, float distance, out RaycastHit best)
        {
            float half = Mathf.Max(capsuleHeight * 0.5f, capsuleRadius);
            Vector3 center = feet + Vector3.up * half;
            float offset = half - capsuleRadius;
            Vector3 p1 = center + Vector3.up * offset;
            Vector3 p2 = center - Vector3.up * offset;
            int n = UnityEngine.Physics.CapsuleCastNonAlloc(p1, p2, capsuleRadius, dir, Hits, distance,
                PhysicsLayers.MovementBlockersMask, QueryTriggerInteraction.Ignore);
            best = default;
            bool found = false;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Collider c = Hits[i].collider;
                if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
                if (Hits[i].distance < bestDist) { bestDist = Hits[i].distance; best = Hits[i]; found = true; }
            }
            return found;
        }
    }
}
