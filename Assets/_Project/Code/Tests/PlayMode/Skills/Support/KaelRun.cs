#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Eclipse.Core.Stats;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Heroes.Kael;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.PlayMode.Skills
{
    /// <summary>
    /// Harness of the M08 T8 tests: the loaded Combat Sandbox with the real Kael (rig, skills, Fury, passives, Ashen Guard), extra
    /// training dummies spawned far from the sandbox props (the scene is shared, see CLAUDE.md section 9) and manual 60 Hz ticks.
    /// Targets get a point-like silhouette (radius 0.05 m) so that area radii and arcs are measured at the nominal distance, no
    /// armor and infinite health. Embers of Rage (the innate passive, +2% per 10 Fury) is read per frame so expected damage stays exact.
    /// </summary>
    public sealed class KaelRun
    {
        /// <summary>Test arena origin: far outside the sandbox arena, its props and its walls.</summary>
        public static readonly Vector3 Origin = new Vector3(300f, 0f, 300f);
        public const string DummyPrefab = "Assets/_Project/Content/Shared/Prefabs/TrainingDummy_Light.prefab";
        public const float SilhouetteRadius = 0.05f;
        private const float AimHeight = 1.2f; // PlayerAim chest height

        public CombatWorld World;
        public GameplayServices Services;
        public PlayerMotor Motor;
        public KaelHeroRig Kael;
        public CombatActor Player;
        public AttackController Attack;
        public Vector3 AimPoint;

        public readonly List<CombatActor> Dummies = new List<CombatActor>();
        public readonly List<OnHitDealt> Hits = new List<OnHitDealt>(512);
        public readonly List<OnSkillCast> Casts = new List<OnSkillCast>(32);
        public readonly List<OnCCApplied> Ccs = new List<OnCCApplied>(32);
        public readonly List<OnGlobalHitStop> HitStops = new List<OnGlobalHitStop>(8);
        public readonly List<OnParry> Parries = new List<OnParry>(8);
        public readonly List<OnBlock> Blocks = new List<OnBlock>(8);

        private readonly Dictionary<int, float> _embers = new Dictionary<int, float>(4096);
        private UnityEngine.Camera _cam;
        private float _savedTimeScale;

        public int Frame => Services.Scheduler.CurrentFrame;
        public float WeaponPower => Kael.CurrentWeapon.WeaponPowerILvl1;
        public SkillExecContext Ctx => Kael.Skills.Context;

        /// <summary>Finds the sandbox rigs and neutralises everything that would act on its own (attackers, crits, debug overlay).</summary>
        public void Bind()
        {
            World = CombatWorld.Current;
            Assert.IsNotNull(World, "CombatWorld missing");
            Services = Object.FindFirstObjectByType<GameplayServices>();
            Motor = Object.FindFirstObjectByType<PlayerMotor>();
            Kael = Object.FindFirstObjectByType<KaelHeroRig>();
            Assert.IsNotNull(Kael, "KaelHeroRig missing");
            Assert.IsTrue(Kael.Skills.IsReady, "SkillCasterRig not set up");
            Player = World.Player;
            Attack = Player.GetComponent<AttackController>();
            var overlay = Object.FindFirstObjectByType<CombatDebugOverlay>();
            if (overlay != null) overlay.Visible = false;
            foreach (var a in Object.FindObjectsByType<TrainingAttacker>(FindObjectsSortMode.None)) a.AttackEnabled = false;
            Player.Entity.Stats.SetBase(StatId.CriticalChance, 0f);
            _cam = UnityEngine.Camera.main;
            Assert.IsNotNull(_cam, "main camera");
        }

        public CombatActor SpawnDummy(Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPrefab);
            Assert.IsNotNull(prefab, DummyPrefab);
            GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
            go.name = "KaelTestDummy_" + Dummies.Count;
            var attacker = go.GetComponent<TrainingAttacker>();
            if (attacker != null) attacker.AttackEnabled = false;
            var actor = go.GetComponent<CombatActor>();
            Assert.IsNotNull(actor, "CombatActor on the dummy prefab");
            go.GetComponentInChildren<Hurtbox>().Configure(SilhouetteRadius, 1.8f, false);
            actor.ArmorPercent = 0f;
            actor.InfiniteHealth = true;
            Dummies.Add(actor);
            UnityEngine.Physics.SyncTransforms();
            return actor;
        }

        /// <summary>Lets the spawned dummies run Start (they register in the world). Call before <see cref="Freeze"/>.</summary>
        public IEnumerator Settle()
        {
            yield return null;
            yield return null;
            foreach (CombatActor d in Dummies)
            {
                Assert.IsNotNull(d.Entity, d.name + " not started");
                d.ArmorPercent = 0f; // Start resets it from the config
                d.InfiniteHealth = true;
            }
        }

        /// <summary>Real FixedUpdate must not add ticks on top of the manual ones.</summary>
        public void Freeze()
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        public void Restore()
        {
            if (Services != null) Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
        }

        public void Place(Vector3 position, Vector3 facing)
        {
            Motor.Teleport(position);
            Motor.SetFacing(facing);
            AimPoint = position + facing.normalized * 5f + Vector3.up * AimHeight;
            UnityEngine.Physics.SyncTransforms();
        }

        public void AimAt(Vector3 worldPoint) => AimPoint = new Vector3(worldPoint.x, Player.Entity.Position.y + AimHeight, worldPoint.z);

        public void SetFury(float value)
        {
            var pool = Kael.Fury.Pool;
            pool.SpendAll();
            if (value > 0f) pool.Add(value);
        }

        public float Fury => Kael.Fury.Pool.Current;

        public void Press(InputActionId id) => Services.InputReader.Press(id);
        public void Release(InputActionId id) => Services.InputReader.Release(id);

        /// <summary>One simulation tick plus the per-rendered-frame work (feedback processing and event flush); collects the events.</summary>
        public void Tick()
        {
            Services.InputReader.AimIsGamepad = false;
            Services.InputReader.Aim = _cam.WorldToScreenPoint(AimPoint);
            Services.Scheduler.StepManual(1);
            _embers[Frame] = Kael.Passives.EmbersBonus;

            EntityId me = Player.Entity.Id;
            var hits = World.Events.Read<OnHitDealt>();
            for (int i = 0; i < hits.Count; i++) if (hits[i].Source == me) Hits.Add(hits[i]);
            var casts = World.Events.Read<OnSkillCast>();
            for (int i = 0; i < casts.Count; i++) Casts.Add(casts[i]);
            var ccs = World.Events.Read<OnCCApplied>();
            for (int i = 0; i < ccs.Count; i++) Ccs.Add(ccs[i]);
            var stops = World.Events.Read<OnGlobalHitStop>();
            for (int i = 0; i < stops.Count; i++) HitStops.Add(stops[i]);
            var parries = World.Events.Read<OnParry>();
            for (int i = 0; i < parries.Count; i++) Parries.Add(parries[i]);
            var blocks = World.Events.Read<OnBlock>();
            for (int i = 0; i < blocks.Count; i++) Blocks.Add(blocks[i]);

            World.ProcessEvents();
            World.Events.EndFrame();
        }

        public void Tick(int count)
        {
            for (int i = 0; i < count; i++) Tick();
        }

        /// <summary>Ticks until <paramref name="condition"/> holds (at most <paramref name="max"/> ticks); true if it did.</summary>
        public bool TickUntil(System.Func<bool> condition, int max = 400)
        {
            for (int i = 0; i < max; i++)
            {
                if (condition()) return true;
                Tick();
            }
            return condition();
        }

        public void ClearEvents()
        {
            Hits.Clear(); Casts.Clear(); Ccs.Clear(); HitStops.Clear(); Parries.Clear(); Blocks.Clear();
        }

        /// <summary>Presses the slot; returns the cast frame, or -1 if no cast started within a few ticks (read <c>Kael.Skills.LastResult</c>).</summary>
        public int PressAndCast(InputActionId slot, int maxTicks = 6)
        {
            int before = Casts.Count;
            Press(slot);
            for (int i = 0; i < maxTicks; i++)
            {
                Tick();
                if (Casts.Count > before) { Release(slot); return Casts[Casts.Count - 1].Frame; }
            }
            Release(slot);
            return -1;
        }

        /// <summary>Ticks until the running cast ends (and, if asked, the attack too).</summary>
        public void FinishCast(int max = 400)
        {
            Assert.IsTrue(TickUntil(() => !Kael.Skills.Controller.IsCasting, max), "cast did not finish");
        }

        public List<OnHitDealt> HitsOn(CombatActor target)
        {
            var list = new List<OnHitDealt>();
            foreach (OnHitDealt h in Hits) if (h.Target == target.Entity.Id) list.Add(h);
            return list;
        }

        public int CountHitsOn(CombatActor target) => HitsOn(target).Count;

        /// <summary>Embers of Rage bonus (fraction) in force when the simulation frame <paramref name="frame"/> ran (set at the end of the previous tick).</summary>
        public float EmbersBefore(int frame) => _embers.TryGetValue(frame - 1, out float e) ? e : Kael.Passives.EmbersBonus;

        /// <summary>Weapon Power x coefficient x (1 + Embers of Rage) for a hit that landed on <paramref name="hitFrame"/>.</summary>
        public float Expected(float coefficient, int hitFrame, float multiplier = 1f, float flat = 0f) =>
            (WeaponPower * coefficient * multiplier + flat) * (1f + EmbersBefore(hitFrame));

        public static void AssertDamage(float expected, float actual, string label) =>
            Assert.AreEqual(expected, actual, 1e-3f + expected * 1e-4f, label);
    }

    /// <summary>Common fixture plumbing: boots the sandbox with dummies at offsets from <see cref="KaelRun.Origin"/> and restores time.</summary>
    public abstract class KaelPlayTestBase
    {
        protected KaelRun Run;

        [NUnit.Framework.TearDown]
        public void TearDownRun() => Run?.Restore();

        /// <summary>Loads the sandbox, spawns one dummy per offset, places Kael at the origin facing +Z and freezes real time.</summary>
        protected IEnumerator Boot(params Vector3[] dummyOffsets)
        {
            yield return SandboxKitLoad();
            Run = new KaelRun();
            Run.Bind();
            foreach (Vector3 o in dummyOffsets) Run.SpawnDummy(KaelRun.Origin + o);
            yield return Run.Settle();
            Run.Freeze();
            Run.Place(KaelRun.Origin, Vector3.forward);
            Run.SetFury(0f);
            Run.Tick(2);
            Run.ClearEvents();
        }

        private static IEnumerator SandboxKitLoad() => Eclipse.Tests.PlayMode.Combat.SandboxKit.Load();
    }
}
#endif
