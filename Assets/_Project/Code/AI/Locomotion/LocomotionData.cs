using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Eclipse.AI.Locomotion
{
    /// <summary>
    /// Persistent native storage for all agents (struct of arrays on the XZ plane). Allocated once, grown only
    /// outside the tick (never while a job is in flight), disposed by the owner. Agent indices are stable.
    /// </summary>
    public sealed class LocomotionData : IDisposable
    {
        public const byte FlagActive = 1;
        public const byte FlagCharging = 2;
        public const byte FlagOrbitClockwise = 4;

        public NativeArray<float2> Position;
        public NativeArray<float2> Velocity;
        public NativeArray<float2> Target;
        public NativeArray<float> Radius;
        public NativeArray<float> MaxSpeed;
        public NativeArray<float> OrbitRadius;
        public NativeArray<byte> Mode;
        public NativeArray<byte> Flags;

        /// <summary>Wall rectangles as (minX, minZ, maxX, maxZ).</summary>
        public NativeArray<float4> Walls;

        int[] _free;
        int _freeCount;

        public int Capacity { get; private set; }
        /// <summary>One past the highest index ever used; jobs iterate [0, HighWater).</summary>
        public int HighWater { get; private set; }
        public int ActiveCount { get; private set; }
        public int WallCount { get; private set; }
        public bool IsCreated => Position.IsCreated;

        public LocomotionData(int capacity = 256, int wallCapacity = 64)
        {
            Capacity = Math.Max(1, capacity);
            Position = new NativeArray<float2>(Capacity, Allocator.Persistent);
            Velocity = new NativeArray<float2>(Capacity, Allocator.Persistent);
            Target = new NativeArray<float2>(Capacity, Allocator.Persistent);
            Radius = new NativeArray<float>(Capacity, Allocator.Persistent);
            MaxSpeed = new NativeArray<float>(Capacity, Allocator.Persistent);
            OrbitRadius = new NativeArray<float>(Capacity, Allocator.Persistent);
            Mode = new NativeArray<byte>(Capacity, Allocator.Persistent);
            Flags = new NativeArray<byte>(Capacity, Allocator.Persistent);
            Walls = new NativeArray<float4>(Math.Max(1, wallCapacity), Allocator.Persistent);
            _free = new int[Capacity];
        }

        /// <summary>Registers an agent and returns its stable index. May grow storage: call outside the tick.</summary>
        public int Register(float2 position, float radius, float maxSpeed)
        {
            int index;
            if (_freeCount > 0) index = _free[--_freeCount];
            else
            {
                if (HighWater == Capacity) Grow(Capacity * 2);
                index = HighWater++;
            }
            Position[index] = position;
            Velocity[index] = float2.zero;
            Target[index] = position;
            Radius[index] = radius;
            MaxSpeed[index] = maxSpeed;
            OrbitRadius[index] = 0f;
            Mode[index] = (byte)SteeringMode.Hold;
            Flags[index] = FlagActive;
            ActiveCount++;
            return index;
        }

        public void Unregister(int index)
        {
            if (!IsActive(index)) return;
            Flags[index] = 0;
            Velocity[index] = float2.zero;
            if (_freeCount == _free.Length) Array.Resize(ref _free, _free.Length * 2);
            _free[_freeCount++] = index;
            ActiveCount--;
        }

        public bool IsActive(int index) => index >= 0 && index < HighWater && (Flags[index] & FlagActive) != 0;

        public void SetSteering(int index, SteeringMode mode, float2 target, float orbitRadius = 0f, bool orbitClockwise = false)
        {
            Mode[index] = (byte)mode;
            Target[index] = target;
            OrbitRadius[index] = orbitRadius;
            SetFlag(index, FlagOrbitClockwise, orbitClockwise);
        }

        /// <summary>While charging, the agent may exceed the global speed cap (up to its own MaxSpeed).</summary>
        public void SetCharging(int index, bool charging) => SetFlag(index, FlagCharging, charging);

        public void SetMaxSpeed(int index, float maxSpeed) => MaxSpeed[index] = maxSpeed;

        public void Teleport(int index, float2 position)
        {
            Position[index] = position;
            Velocity[index] = float2.zero;
        }

        public float2 GetPosition(int index) => Position[index];
        public float2 GetVelocity(int index) => Velocity[index];

        public void ClearWalls() => WallCount = 0;

        public void AddWall(float4 rect)
        {
            if (WallCount == Walls.Length)
            {
                var bigger = new NativeArray<float4>(Walls.Length * 2, Allocator.Persistent);
                NativeArray<float4>.Copy(Walls, bigger, WallCount);
                Walls.Dispose();
                Walls = bigger;
            }
            Walls[WallCount++] = rect;
        }

        /// <summary>
        /// Counts active pairs closer than 2 * radius (both agents modelled with the same radius).
        /// Allocation free; used by the 200-agent overlap test (T10).
        /// </summary>
        public int CountOverlaps(float radius)
        {
            float minSq = 4f * radius * radius;
            int count = 0;
            for (int i = 0; i < HighWater; i++)
            {
                if ((Flags[i] & FlagActive) == 0) continue;
                for (int j = i + 1; j < HighWater; j++)
                {
                    if ((Flags[j] & FlagActive) == 0) continue;
                    if (math.distancesq(Position[i], Position[j]) < minSq) count++;
                }
            }
            return count;
        }

        /// <summary>Counts pairs whose own radii overlap (distance below the sum of radii).</summary>
        public int CountOverlaps()
        {
            int count = 0;
            for (int i = 0; i < HighWater; i++)
            {
                if ((Flags[i] & FlagActive) == 0) continue;
                for (int j = i + 1; j < HighWater; j++)
                {
                    if ((Flags[j] & FlagActive) == 0) continue;
                    float r = Radius[i] + Radius[j];
                    if (math.distancesq(Position[i], Position[j]) < r * r) count++;
                }
            }
            return count;
        }

        void SetFlag(int index, byte flag, bool on)
        {
            Flags[index] = on ? (byte)(Flags[index] | flag) : (byte)(Flags[index] & ~flag);
        }

        void Grow(int newCapacity)
        {
            Grow(ref Position, newCapacity);
            Grow(ref Velocity, newCapacity);
            Grow(ref Target, newCapacity);
            Grow(ref Radius, newCapacity);
            Grow(ref MaxSpeed, newCapacity);
            Grow(ref OrbitRadius, newCapacity);
            Grow(ref Mode, newCapacity);
            Grow(ref Flags, newCapacity);
            Array.Resize(ref _free, newCapacity);
            Capacity = newCapacity;
        }

        void Grow<T>(ref NativeArray<T> array, int newCapacity) where T : struct
        {
            var bigger = new NativeArray<T>(newCapacity, Allocator.Persistent);
            NativeArray<T>.Copy(array, bigger, HighWater);
            array.Dispose();
            array = bigger;
        }

        public void Dispose()
        {
            if (!IsCreated) return;
            Position.Dispose();
            Velocity.Dispose();
            Target.Dispose();
            Radius.Dispose();
            MaxSpeed.Dispose();
            OrbitRadius.Dispose();
            Mode.Dispose();
            Flags.Dispose();
            Walls.Dispose();
        }
    }
}
