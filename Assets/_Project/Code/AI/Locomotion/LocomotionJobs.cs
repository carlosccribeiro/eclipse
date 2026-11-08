using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Eclipse.AI.Locomotion
{
    /// <summary>Blittable copy of the config values the jobs need.</summary>
    public struct SteeringParams
    {
        public float GlobalMaxSpeed;
        public float Acceleration;
        public float ArriveSlowRadius;
        public float ArriveStopDistance;
        public float OrbitRadialWeight;
        public float AvoidanceWeight;
        public float AvoidanceMargin;
        public float DeltaTime;
        public int Count;

        public static SteeringParams From(LocomotionConfig c, float dt, int count) => new SteeringParams
        {
            GlobalMaxSpeed = c.GlobalMaxSpeed,
            Acceleration = c.Acceleration,
            ArriveSlowRadius = c.ArriveSlowRadius,
            ArriveStopDistance = c.ArriveStopDistance,
            OrbitRadialWeight = c.OrbitRadialWeight,
            AvoidanceWeight = c.AvoidanceWeight,
            AvoidanceMargin = c.AvoidanceMargin,
            DeltaTime = dt,
            Count = count,
        };
    }

    /// <summary>Seek/arrive/orbit/flee steering plus radial avoidance between agents. Writes Velocity[i] only.</summary>
    [BurstCompile]
    public struct SteeringJob : IJobParallelFor
    {
        public SteeringParams P;
        [ReadOnly] public NativeArray<float2> Position;
        [ReadOnly] public NativeArray<float2> Target;
        [ReadOnly] public NativeArray<float> Radius;
        [ReadOnly] public NativeArray<float> MaxSpeed;
        [ReadOnly] public NativeArray<float> OrbitRadius;
        [ReadOnly] public NativeArray<byte> Mode;
        [ReadOnly] public NativeArray<byte> Flags;
        public NativeArray<float2> Velocity;

        public void Execute(int i)
        {
            byte flags = Flags[i];
            if ((flags & LocomotionData.FlagActive) == 0) return;

            bool charging = (flags & LocomotionData.FlagCharging) != 0;
            float cap = charging ? MaxSpeed[i] : math.min(MaxSpeed[i], P.GlobalMaxSpeed);
            float2 pos = Position[i];
            float2 toTarget = Target[i] - pos;
            float dist = math.length(toTarget);
            float2 dir = dist > 1e-5f ? toTarget / dist : float2.zero;
            float2 desired = float2.zero;

            switch ((SteeringMode)Mode[i])
            {
                case SteeringMode.Seek:
                    desired = dir * cap;
                    break;
                case SteeringMode.Arrive:
                    if (dist > P.ArriveStopDistance)
                        desired = dir * (cap * math.saturate(dist / math.max(P.ArriveSlowRadius, 1e-4f)));
                    break;
                case SteeringMode.Flee:
                    desired = -dir * cap;
                    break;
                case SteeringMode.Orbit:
                {
                    float sign = (flags & LocomotionData.FlagOrbitClockwise) != 0 ? -1f : 1f;
                    float2 tangent = new float2(-dir.y, dir.x) * sign;
                    float error = math.clamp(dist - OrbitRadius[i], -1f, 1f);
                    float2 v = tangent + dir * (error * P.OrbitRadialWeight);
                    float len = math.length(v);
                    if (len > 1e-5f) desired = v / len * cap;
                    break;
                }
            }

            // Radial avoidance: push away from every neighbour closer than the sum of radii plus margin.
            float2 push = float2.zero;
            float ri = Radius[i];
            for (int j = 0; j < P.Count; j++)
            {
                if (j == i || (Flags[j] & LocomotionData.FlagActive) == 0) continue;
                float2 d = pos - Position[j];
                float minDist = ri + Radius[j] + P.AvoidanceMargin;
                float dSq = math.lengthsq(d);
                if (dSq >= minDist * minDist) continue;
                float dd = math.sqrt(dSq);
                // Coincident agents separate along +/-X by index so the result is deterministic and opposed.
                float2 n = dd > 1e-5f ? d / dd : new float2(i < j ? 1f : -1f, 0f);
                push += n * ((minDist - dd) / minDist);
            }
            desired += push * (P.AvoidanceWeight * cap);

            float dl = math.length(desired);
            if (dl > cap) desired *= cap / dl;

            float2 vel = Velocity[i];
            float2 delta = desired - vel;
            float maxDelta = P.Acceleration * P.DeltaTime;
            float dLen = math.length(delta);
            if (dLen > maxDelta) delta *= maxDelta / dLen;
            vel += delta;

            float speed = math.length(vel);
            if (speed > cap) vel *= cap / speed;
            Velocity[i] = vel;
        }
    }

    /// <summary>Integrates position and pushes agents out of Wall rectangles. Writes Position[i]/Velocity[i] only.</summary>
    [BurstCompile]
    public struct IntegrateJob : IJobParallelFor
    {
        public float DeltaTime;
        public int WallCount;
        [ReadOnly] public NativeArray<float> Radius;
        [ReadOnly] public NativeArray<byte> Flags;
        [ReadOnly] public NativeArray<float4> Walls;
        public NativeArray<float2> Position;
        public NativeArray<float2> Velocity;

        public void Execute(int i)
        {
            if ((Flags[i] & LocomotionData.FlagActive) == 0) return;
            float2 pos = Position[i] + Velocity[i] * DeltaTime;
            float2 vel = Velocity[i];
            float r = Radius[i];

            // Two passes handle corners where two rectangles touch.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int w = 0; w < WallCount; w++)
                {
                    float4 rect = Walls[w];
                    float2 closest = math.clamp(pos, rect.xy, rect.zw);
                    float2 d = pos - closest;
                    float dSq = math.lengthsq(d);
                    if (dSq >= r * r) continue;

                    float2 n;
                    float push;
                    if (dSq > 1e-10f)
                    {
                        float dd = math.sqrt(dSq);
                        n = d / dd;
                        push = r - dd;
                    }
                    else
                    {
                        // Centre inside the rectangle: leave through the nearest face.
                        float left = pos.x - rect.x, right = rect.z - pos.x;
                        float down = pos.y - rect.y, up = rect.w - pos.y;
                        float m = math.min(math.min(left, right), math.min(down, up));
                        if (m == left) n = new float2(-1f, 0f);
                        else if (m == right) n = new float2(1f, 0f);
                        else if (m == down) n = new float2(0f, -1f);
                        else n = new float2(0f, 1f);
                        push = m + r;
                    }
                    pos += n * push;
                    float into = math.dot(vel, n);
                    if (into < 0f) vel -= n * into;
                }
            }

            Position[i] = pos;
            Velocity[i] = vel;
        }
    }

    /// <summary>Runs one locomotion tick (steering then integration) over a <see cref="LocomotionData"/>.</summary>
    public static class LocomotionStepper
    {
        const int BatchSize = 16;

        /// <summary>When <paramref name="inline"/> is true the jobs run synchronously on the caller (tests).</summary>
        public static void Step(LocomotionData data, LocomotionConfig config, float dt, bool inline = false)
        {
            int n = data.HighWater;
            if (n == 0) return;

            var steer = new SteeringJob
            {
                P = SteeringParams.From(config, dt, n),
                Position = data.Position,
                Target = data.Target,
                Radius = data.Radius,
                MaxSpeed = data.MaxSpeed,
                OrbitRadius = data.OrbitRadius,
                Mode = data.Mode,
                Flags = data.Flags,
                Velocity = data.Velocity,
            };
            var move = new IntegrateJob
            {
                DeltaTime = dt,
                WallCount = data.WallCount,
                Radius = data.Radius,
                Flags = data.Flags,
                Walls = data.Walls,
                Position = data.Position,
                Velocity = data.Velocity,
            };

            if (inline)
            {
                steer.Run(n);
                move.Run(n);
            }
            else
            {
                JobHandle h = steer.Schedule(n, BatchSize);
                move.Schedule(n, BatchSize, h).Complete();
            }
        }
    }

    /// <summary>Reduced animation rate for distant enemies (11 section 3.14); the real hook is in the enemy controller.</summary>
    public static class AnimationLod
    {
        /// <summary>Distance beyond which animation updates at half rate (TUNED placeholder).</summary>
        public const float HalfRateDistance = 25f;
        /// <summary>Distance beyond which animation updates at quarter rate (TUNED placeholder).</summary>
        public const float QuarterRateDistance = 40f;

        public static bool ShouldAnimateThisFrame(float distance, int frame)
        {
            if (distance >= QuarterRateDistance) return (frame & 3) == 0;
            if (distance >= HalfRateDistance) return (frame & 1) == 0;
            return true;
        }
    }
}
