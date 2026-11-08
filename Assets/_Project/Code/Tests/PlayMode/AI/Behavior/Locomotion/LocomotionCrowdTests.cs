#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Eclipse.AI.Locomotion;
using Eclipse.Gameplay.Physics;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.AI.Behavior.Locomotion
{
    /// <summary>
    /// M07 T4 acceptance (11 section 3.10): 200 agents moving in a 40 x 40 m room, real Wall-layer colliders collected by the
    /// <see cref="LocomotionSystem"/>, no pair overlapping for more than 0.5 s in a row and nobody inside a Wall.
    /// Two scenarios: roaming (every agent walks between its own waypoints, crossing flows: the criterion) and a swarm on one target
    /// (stress: walls must still hold; the overlap numbers are logged because 200 bodies cannot fit around one point).
    /// The room sits far from the origin so it cannot touch whatever scene a previous test left loaded.
    /// </summary>
    public class LocomotionCrowdTests
    {
        private const int Agents = 200;
        private const float AgentRadius = 0.4f;     // Hurtbox-sized agent (Marauder silhouette)
        private const float Half = 20f;             // 40 x 40 m room
        private const float Seconds = 30f;
        private const int Hz = 60;
        private const float OverlapTolerance = 0.9f; // contact tolerance: 10% of the body diameter is not an overlap
        private static readonly Vector3 Center = new Vector3(4000f, 0f, 4000f);

        private sealed class Result
        {
            public int MaxPairRun, MaxRunLoose, MaxRunStrict, PeakLoose, PeakStrict, WallViolations, OutOfRoom;
            public float WorstWall, MeanTravel;
        }

        private readonly List<Object> _owned = new List<Object>();
        private float _savedTimeScale;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _savedTimeScale;
            for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
            _owned.Clear();
        }

        private void Wall(string name, Vector3 local, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = PhysicsLayers.Wall;
            go.transform.position = Center + local;
            go.transform.localScale = size;
            _owned.Add(go);
        }

        // Closest approach of a point to a rectangle (0 when inside).
        private static float DistanceToRect(float2 p, float4 r)
        {
            float cx = math.clamp(p.x, r.x, r.z);
            float cz = math.clamp(p.y, r.y, r.w);
            return math.length(p - new float2(cx, cz));
        }

        private sealed class Lcg
        {
            private uint _s;
            public Lcg(uint seed) { _s = seed; }
            public float Next() { _s = _s * 1664525u + 1013904223u; return (_s >> 8) / 16777216f; }
        }

        private IEnumerator Simulate(bool roam, Result res)
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f; // the system's own FixedUpdate must not step on top of the manual steps

            var host = new GameObject("LocomotionCrowdTest");
            _owned.Add(host);
            var loco = host.AddComponent<LocomotionSystem>();

            // Perimeter (inner faces at +-20 m) and two pillars.
            Wall("Wall_N", new Vector3(0f, 2f, Half + 0.5f), new Vector3(Half * 2f + 2f, 4f, 1f));
            Wall("Wall_S", new Vector3(0f, 2f, -Half - 0.5f), new Vector3(Half * 2f + 2f, 4f, 1f));
            Wall("Wall_E", new Vector3(Half + 0.5f, 2f, 0f), new Vector3(1f, 4f, Half * 2f + 2f));
            Wall("Wall_W", new Vector3(-Half - 0.5f, 2f, 0f), new Vector3(1f, 4f, Half * 2f + 2f));
            Wall("Pillar_A", new Vector3(-8f, 2f, 8f), new Vector3(4f, 4f, 4f));
            Wall("Pillar_B", new Vector3(8f, 2f, -8f), new Vector3(4f, 4f, 4f));
            UnityEngine.Physics.SyncTransforms();
            int walls = loco.CollectWalls(new Bounds(Center, new Vector3(Half * 2f + 8f, 10f, Half * 2f + 8f)));
            Assert.AreEqual(6, walls, "the six Wall-layer colliders must be collected");
            LocomotionData data = loco.Data;
            Assert.AreEqual(6, data.WallCount);

            // Deterministic scatter: start without any overlap or wall contact.
            var rng = new Lcg(20260101u);
            var starts = new List<float2>(Agents);
            var ids = new int[Agents];
            int guard = 0;
            while (starts.Count < Agents && guard++ < 100000)
            {
                var p = new float2(Center.x + (rng.Next() * 2f - 1f) * (Half - 1f), Center.z + (rng.Next() * 2f - 1f) * (Half - 1f));
                bool ok = true;
                for (int w = 0; w < data.WallCount && ok; w++) ok = DistanceToRect(p, data.Walls[w]) > AgentRadius + 0.5f;
                for (int i = 0; i < starts.Count && ok; i++) ok = math.distance(starts[i], p) > AgentRadius * 2f + 0.2f;
                if (ok) starts.Add(p);
            }
            Assert.AreEqual(Agents, starts.Count, "could not scatter the agents");
            for (int i = 0; i < Agents; i++) ids[i] = data.Register(starts[i], AgentRadius, 4.2f); // Standard speed, 09 section 4
            Assert.AreEqual(Agents, data.ActiveCount);

            var cfg = loco.Config;
            float dt = 1f / Hz;
            int ticks = (int)(Seconds * Hz);
            int runLoose = 0, runStrict = 0;
            var travelled = new float[Agents];
            var last = new float2[Agents];
            var waypoint = new float2[Agents];
            var centre2 = new float2(Center.x, Center.z);
            var pairRun = new int[Agents * Agents]; // consecutive overlapping ticks of the pair (i < j)
            float pairLimitSq = 4f * AgentRadius * AgentRadius * OverlapTolerance * OverlapTolerance;
            for (int i = 0; i < Agents; i++)
            {
                last[i] = starts[i];
                waypoint[i] = RandomPoint(rng, centre2);
            }

            for (int t = 0; t < ticks; t++)
            {
                float time = t * dt;
                if (roam)
                {
                    for (int i = 0; i < Agents; i++)
                    {
                        if (math.distance(last[i], waypoint[i]) < 1.5f) waypoint[i] = RandomPoint(rng, centre2);
                        data.SetSteering(ids[i], SteeringMode.Seek, waypoint[i]);
                    }
                }
                else
                {
                    // The "player" circles the room; half the crowd chases it, half orbits it (the two modes enemies use around a target).
                    float2 player = centre2 + new float2(math.cos(time * 0.35f), math.sin(time * 0.35f)) * 9f;
                    for (int i = 0; i < Agents; i++)
                    {
                        if (i % 2 == 0) data.SetSteering(ids[i], SteeringMode.Seek, player);
                        else data.SetSteering(ids[i], SteeringMode.Orbit, player, 4f + (i % 5), (i & 2) != 0);
                    }
                }
                LocomotionStepper.Step(data, cfg, dt);

                int loose = data.CountOverlaps(AgentRadius * OverlapTolerance);
                int strict = data.CountOverlaps(AgentRadius);
                res.PeakLoose = math.max(res.PeakLoose, loose);
                res.PeakStrict = math.max(res.PeakStrict, strict);
                // Per pair (the criterion): count consecutive ticks below the contact distance. Only walked when CountOverlaps saw something.
                if (loose > 0 || runLoose > 0)
                {
                    for (int i = 0; i < Agents; i++)
                    {
                        float2 pi = data.GetPosition(ids[i]);
                        for (int j = i + 1; j < Agents; j++)
                        {
                            int k = i * Agents + j;
                            if (math.distancesq(pi, data.GetPosition(ids[j])) < pairLimitSq) res.MaxPairRun = math.max(res.MaxPairRun, ++pairRun[k]);
                            else pairRun[k] = 0;
                        }
                    }
                }
                runLoose = loose > 0 ? runLoose + 1 : 0;
                runStrict = strict > 0 ? runStrict + 1 : 0;
                res.MaxRunLoose = math.max(res.MaxRunLoose, runLoose);
                res.MaxRunStrict = math.max(res.MaxRunStrict, runStrict);

                for (int i = 0; i < Agents; i++)
                {
                    float2 p = data.GetPosition(ids[i]);
                    travelled[i] += math.distance(p, last[i]);
                    last[i] = p;
                    float2 local = p - centre2;
                    if (math.abs(local.x) > Half || math.abs(local.y) > Half) res.OutOfRoom++;
                    for (int w = 0; w < data.WallCount; w++)
                    {
                        float d = DistanceToRect(p, data.Walls[w]);
                        if (d < AgentRadius - 0.03f)
                        {
                            res.WallViolations++;
                            res.WorstWall = math.max(res.WorstWall, AgentRadius - d);
                        }
                    }
                }
                if (t % 120 == 0) yield return null; // keep the Editor responsive
            }

            for (int i = 0; i < Agents; i++) res.MeanTravel += travelled[i];
            res.MeanTravel /= Agents;
            Debug.Log("[M07 T10] 200 agents, 40x40 m, " + Seconds + " s, " + (roam ? "roaming" : "swarm") + ": mean travel " + res.MeanTravel.ToString("F1")
                + " m | overlap (0.9 radius) peak pairs " + res.PeakLoose + ", longest streak (any pair) " + res.MaxRunLoose + " ticks, longest same-pair streak " + res.MaxPairRun + " ticks | strict (full radius) peak pairs "
                + res.PeakStrict + ", longest streak " + res.MaxRunStrict + " ticks | wall violations " + res.WallViolations + " (worst "
                + res.WorstWall.ToString("F3") + " m), outside room " + res.OutOfRoom);
        }

        private static float2 RandomPoint(Lcg rng, float2 centre) =>
            centre + new float2((rng.Next() * 2f - 1f) * (Half - 2f), (rng.Next() * 2f - 1f) * (Half - 2f));

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Two_hundred_roaming_agents_do_not_overlap_persistently_and_never_cross_a_wall()
        {
            var r = new Result();
            yield return Simulate(true, r);
            Assert.Greater(r.MeanTravel, 20f, "agents barely moved: the crowd test would be vacuous");
            Assert.LessOrEqual(r.MaxPairRun, Hz / 2, "the same pair overlapped for more than 0.5 s in a row (" + r.MaxPairRun + " ticks, peak " + r.PeakLoose + " pairs)");
            Assert.AreEqual(0, r.WallViolations, "agents entered a Wall (worst penetration " + r.WorstWall + " m)");
            Assert.AreEqual(0, r.OutOfRoom, "agents left the 40 x 40 m room");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Two_hundred_agents_swarming_one_target_never_cross_a_wall()
        {
            var r = new Result();
            yield return Simulate(false, r);
            Assert.Greater(r.MeanTravel, 20f, "agents barely moved: the crowd test would be vacuous");
            Assert.AreEqual(0, r.WallViolations, "agents entered a Wall (worst penetration " + r.WorstWall + " m)");
            Assert.AreEqual(0, r.OutOfRoom, "agents left the 40 x 40 m room");
        }
    }
}
#endif
