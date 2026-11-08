using Eclipse.Gameplay.Physics;
using Unity.Mathematics;
using UnityEngine;
using UPhysics = UnityEngine.Physics;

namespace Eclipse.AI.Locomotion
{
    /// <summary>
    /// Thin driver: runs the locomotion jobs once per fixed step (60 Hz) and exposes agent registration.
    /// Rules live in LocomotionJobs / LocomotionData. Agents live on the XZ plane.
    /// </summary>
    public sealed class LocomotionSystem : MonoBehaviour
    {
        [SerializeField] LocomotionConfig _config = new LocomotionConfig();
        [SerializeField] int _initialCapacity = 256;

        readonly Collider[] _wallBuffer = new Collider[256];
        LocomotionData _data;

        public LocomotionData Data => _data ??= new LocomotionData(_initialCapacity);
        public LocomotionConfig Config => _config;

        public int Register(Vector3 position, float radius, float maxSpeed) =>
            Data.Register(new float2(position.x, position.z), radius, maxSpeed);

        public void Unregister(int index) => Data.Unregister(index);

        public void SetSteering(int index, SteeringMode mode, Vector3 target, float orbitRadius = 0f, bool orbitClockwise = false) =>
            Data.SetSteering(index, mode, new float2(target.x, target.z), orbitRadius, orbitClockwise);

        public Vector3 GetPosition(int index, float y = 0f)
        {
            float2 p = Data.Position[index];
            return new Vector3(p.x, y, p.y);
        }

        public Vector3 GetVelocity(int index)
        {
            float2 v = Data.Velocity[index];
            return new Vector3(v.x, 0f, v.y);
        }

        public int CountOverlaps(float radius) => Data.CountOverlaps(radius);

        /// <summary>Rebuilds the wall rectangles from colliders on the Wall layer inside the area (call per room, not per frame).</summary>
        public int CollectWalls(Bounds area)
        {
            Data.ClearWalls();
            int hits = UPhysics.OverlapBoxNonAlloc(area.center, area.extents, _wallBuffer, Quaternion.identity,
                PhysicsLayers.WallMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
            {
                Bounds b = _wallBuffer[i].bounds;
                Data.AddWall(new float4(b.min.x, b.min.z, b.max.x, b.max.z));
            }
            return hits;
        }

        void FixedUpdate()
        {
            if (_data == null) return;
            using (Eclipse.AI.Diagnostics.AiMarkers.Locomotion.Auto())
                LocomotionStepper.Step(_data, _config, Time.fixedDeltaTime);
        }

        void OnDestroy()
        {
            _data?.Dispose();
            _data = null;
        }
    }
}
