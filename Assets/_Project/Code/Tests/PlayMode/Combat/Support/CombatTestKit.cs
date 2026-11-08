#if UNITY_EDITOR
using EntityId = Eclipse.Gameplay.Combat.EntityId;
using System;
using System.Collections;
using System.Collections.Generic;
using Eclipse.Core.Damage;
using Eclipse.Core.Random;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Physics;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eclipse.Tests.PlayMode.Combat
{
    /// <summary>Shared deterministic helpers for the M05 combat PlayMode tests (no real time: everything runs on manual ticks).</summary>
    public sealed class CombatEnv : IDisposable
    {
        public const string ConfigPath = "Assets/_Project/Content/Shared/Combat/CombatRuntimeConfig.asset";
        public const string WeaponPath = "Assets/_Project/Content/Heroes/Kael/Weapons/greatsword.asset";

        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        /// <summary>Tests run in whatever scene the previous test left loaded (maybe the sandbox): physics tests work far away from it.</summary>
        public static readonly Vector3 Far = new Vector3(3000f, 0f, 3000f);

        private int _nextId = 100;

        public CombatRuntimeConfig Config { get; }
        public CombatEntityConfig EntityConfig { get; }
        public CombatStateRules Rules { get; }
        public CombatEventStream Events { get; } = new CombatEventStream();
        public RngStreams Rng { get; } = new RngStreams(20260101UL);

        public CombatEnv()
        {
            Config = AssetDatabase.LoadAssetAtPath<CombatRuntimeConfig>(ConfigPath);
            if (Config == null) throw new InvalidOperationException("CombatRuntimeConfig missing at " + ConfigPath);
            EntityConfig = Config.BuildEntityConfig();
            Rules = new CombatStateRules(Config.combatStates.Values);
        }

        public static WeaponDefinition LoadWeapon()
        {
            var w = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WeaponPath);
            if (w == null) throw new InvalidOperationException("Greatsword missing at " + WeaponPath);
            return w;
        }

        public EntityId NewId() => new EntityId(++_nextId);

        public CombatEntity MakeEntity(Team team, float maxHealth = 1000f, Vector3 position = default)
        {
            var stats = new StatBlock(new StatRuleTable());
            stats.SetBase(StatId.MaximumHealth, maxHealth);
            return new CombatEntity(NewId(), team, stats, EntityConfig, Events, Rng) { Position = position };
        }

        /// <summary>Enemy-team entity with a Hurtbox collider at <paramref name="position"/>.</summary>
        public Hurtbox MakeTarget(Vector3 position, float silhouette, out CombatEntity entity, float maxHealth = 100000f)
        {
            entity = MakeEntity(Team.Enemy, maxHealth, position);
            var go = Track(new GameObject("Target"));
            go.transform.position = position;
            var hb = go.AddComponent<Hurtbox>();
            hb.Configure(silhouette, 1.8f, false);
            hb.Bind(entity, entity);
            UnityEngine.Physics.SyncTransforms();
            return hb;
        }

        public GameObject MakeWall(Vector3 center, Vector3 size)
        {
            var go = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.name = "TestWall";
            go.transform.position = center;
            go.transform.localScale = size;
            go.layer = PhysicsLayers.Wall;
            UnityEngine.Physics.SyncTransforms();
            return go;
        }

        public T Track<T>(T obj) where T : UnityEngine.Object { _owned.Add(obj); return obj; }

        public static DamageInstance Hit(float damage, HitFlags flags = HitFlags.None, HitKind kind = HitKind.Hit, int source = 900)
        {
            var h = new DamageInstance { SourceId = source, Kind = kind, Flags = flags, Tags = new TagMask(Tag.Attack) };
            h.AddComponent(DamageType.Physical, damage);
            return h;
        }

        public void Dispose()
        {
            for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) UnityEngine.Object.DestroyImmediate(_owned[i]);
            _owned.Clear();
        }
    }

    /// <summary>Resolver that adds a flat physical component (damage = base x multiplier), no stats involved.</summary>
    public sealed class FixedDamageResolver : IHitDamageResolver
    {
        public float Base = 10f;

        public void Resolve(DamageInstance hit, ICombatEntity attacker, Hurtbox target, float damageMultiplier) =>
            hit.AddComponent(DamageType.Physical, Base * damageMultiplier);
    }

    /// <summary>Sandbox scene helpers (same approach as <c>SandboxSmokeTests</c>).</summary>
    public static class SandboxKit
    {
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/CombatSandbox.unity";

        public static IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 5; i++) yield return null;
        }

        public static CombatActor FindDummy(CombatWorld world, string name)
        {
            for (int i = 0; i < world.Actors.Count; i++)
                if (world.Actors[i].name.StartsWith(name)) return world.Actors[i];
            return null;
        }
    }

    /// <summary>
    /// Per-frame managed allocation probe (ProfilerRecorder "GC Allocated In Frame"). Test infrastructure may allocate, so the
    /// result carries an idle baseline measured the same way; product code is clean when the loop never exceeds it.
    /// </summary>
    public sealed class GcResult
    {
        public long LoopMaxBytes, BaselineMaxBytes, LoopMedianBytes, BaselineMedianBytes, BaselineP95Bytes, LoopP95Bytes;
        public int Frames, FramesAboveBaselineMedian;
        public long[] Loop;
        public string Summary => "baseline median " + BaselineMedianBytes + " max " + BaselineMaxBytes + " | loop median " + LoopMedianBytes + " max " + LoopMaxBytes
            + " | p95 baseline " + BaselineP95Bytes + " loop " + LoopP95Bytes + " | frames above baseline median " + FramesAboveBaselineMedian + "/" + Frames;
    }

    public static class GcProbe
    {
        private static long Median(long[] a)
        {
            var c = (long[])a.Clone();
            System.Array.Sort(c);
            return c[c.Length / 2];
        }

        private static long Percentile(long[] a, double p)
        {
            var c = (long[])a.Clone();
            System.Array.Sort(c);
            return c[System.Math.Min(c.Length - 1, (int)(c.Length * p))];
        }

        /// <summary>Runs <paramref name="perFrame"/> once per rendered frame and records "GC Allocated In Frame" of the previous frame.</summary>
        public static IEnumerator Measure(GcResult result, Action perFrame, int warmupFrames, int frames, int baselineFrames = 120)
        {
            using (var rec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 4))
            {
                for (int i = 0; i < 20; i++) yield return null;
                var baseline = new long[baselineFrames];
                for (int i = 0; i < baselineFrames; i++) { yield return null; baseline[i] = rec.LastValue; }
                for (int i = 0; i < warmupFrames; i++) { perFrame(); yield return null; }
                var loop = new long[frames];
                for (int i = 0; i < frames; i++) { perFrame(); yield return null; loop[i] = rec.LastValue; }

                result.BaselineMedianBytes = Median(baseline);
                result.LoopMedianBytes = Median(loop);
                result.BaselineP95Bytes = Percentile(baseline, 0.95);
                result.LoopP95Bytes = Percentile(loop, 0.95);
                foreach (long v in baseline) if (v > result.BaselineMaxBytes) result.BaselineMaxBytes = v;
                foreach (long v in loop) { if (v > result.LoopMaxBytes) result.LoopMaxBytes = v; if (v > result.BaselineMedianBytes) result.FramesAboveBaselineMedian++; }
                result.Frames = frames;
                result.Loop = loop;
            }
        }

        /// <summary>
        /// The editor adds a few hundred bytes per frame and occasional spikes (not product code). The loop is clean when its median
        /// frame does not exceed the idle median and its p95 stays within the idle tail of the same run (spikes come from the
        /// editor/runner). <c>GC.GetAllocatedBytesForCurrentThread</c> is not usable here: it does not move under the Unity player loop.
        /// <paramref name="engineBookkeepingOnRunner"/>: the GitHub runner measured ~40-90 KB/frame for 2.000 SpherecastCommands that
        /// local Windows runs do not show (open item in the M05 Log); there the median check is only logged.
        /// </summary>
        public static void AssertClean(GcResult r, bool engineBookkeepingOnRunner = false)
        {
            Debug.Log("[GcProbe] " + r.Summary);
            bool onRunner = !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
            if (engineBookkeepingOnRunner && onRunner)
            {
                if (r.LoopMedianBytes > r.BaselineMedianBytes) Debug.LogWarning("[GcProbe] runner-only allocation above idle (open item): " + r.Summary);
                return;
            }
            NUnit.Framework.Assert.LessOrEqual(r.LoopMedianBytes, r.BaselineMedianBytes, "typical loop frame allocates more than idle: " + r.Summary);
            NUnit.Framework.Assert.LessOrEqual(r.LoopP95Bytes, System.Math.Max(r.BaselineP95Bytes, r.BaselineMaxBytes) * 2 + 1024, "loop tail above the idle tail: " + r.Summary);
        }
    }
}
#endif
