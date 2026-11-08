using System.Collections.Generic;
using Eclipse.Core.Stats;
using Eclipse.Data.Definitions;
using Eclipse.Data.Heroes;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Heroes;
using Eclipse.Gameplay.Heroes.Kael;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.EditMode.Heroes.Kael
{
    /// <summary>Fury rules of 08 4.1. Fixture numbers are the doc values (08 3.2 / 4.1 / 4.2).</summary>
    public sealed class KaelFuryTests
    {
        private const float Tol = 1e-3f;
        private const float Dt = 1f / 60f;

        internal sealed class Recorder : IResourceSink
        {
            public readonly List<string> Log = new List<string>();
            public float Gained_, Spent_;
            public int Fulls, Empties;
            public void Gained(float a) { Gained_ += a; Log.Add("G"); }
            public void Spent(float a) { Spent_ += a; Log.Add("S"); }
            public void Full() { Fulls++; Log.Add("F"); }
            public void Empty() { Empties++; Log.Add("E"); }
        }

        internal static ResourceDefinition MakeDefinition()
        {
            var d = ScriptableObject.CreateInstance<ResourceDefinition>();
            d.EditorSet(ResourceKind.Pool, 100f, 150f, 1f, 4f, 8f, 0f, new List<SkillParam>
            {
                new SkillParam("basic_first_target", 5), new SkillParam("basic_extra_target", 1), new SkillParam("basic_max_per_hit", 9),
                new SkillParam("heavy_tap", 10), new SkillParam("heavy_charged", 15),
                new SkillParam("damage_taken_per_pct_hp", 1), new SkillParam("damage_taken_max_per_s", 15), new SkillParam("kill", 3),
                new SkillParam("embers_fury_step", 10), new SkillParam("embers_increased_pct_per_step", 2),
                new SkillParam("resolve_health_threshold_pct", 35), new SkillParam("resolve_damage_reduction_pct", 15),
                new SkillParam("resolve_resource_generation_pct", 25),
            });
            return d;
        }

        private ResourceDefinition _def;
        private StatBlock _stats;
        private Recorder _rec;
        private KaelFury _fury;

        [SetUp]
        public void SetUp()
        {
            _def = MakeDefinition();
            _stats = new StatBlock(new StatRuleTable());
            _rec = new Recorder();
            _fury = new KaelFury(new ResourcePool(ResourceRules.FromDefinition(_def), _rec), FuryGainRules.FromDefinition(_def), _stats);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_def);

        private void Run(float seconds)
        {
            int ticks = Mathf.RoundToInt(seconds * 60f);
            for (int i = 0; i < ticks; i++) _fury.Tick(Dt);
        }

        [Test]
        public void StartsEmptyWithBaseMaximum()
        {
            Assert.AreEqual(0f, _fury.Pool.Current);
            Assert.AreEqual(100f, _fury.Pool.Maximum);
        }

        [Test]
        public void Basic_OneTarget_Gives5()
        {
            Assert.AreEqual(5f, _fury.OnBasicHit(1), Tol);
            Assert.AreEqual(5f, _fury.Pool.Current, Tol);
        }

        [Test]
        public void Basic_ExtraTargetsAddOneEach()
        {
            Assert.AreEqual(8f, _fury.OnBasicHit(4), Tol);
        }

        [Test]
        public void Basic_CappedAtNinePerHit()
        {
            Assert.AreEqual(9f, _fury.OnBasicHit(5), Tol);
            Assert.AreEqual(9f, _fury.OnBasicHit(12), Tol);
        }

        [Test]
        public void Basic_NoTargets_GivesNothing()
        {
            Assert.AreEqual(0f, _fury.OnBasicHit(0));
        }

        [Test]
        public void Basic_ScalesWithProcCoefficient()
        {
            Assert.AreEqual(2.5f, _fury.OnBasicHit(1, 0.5f), Tol);
        }

        [Test]
        public void Heavy_TapAndCharged()
        {
            Assert.AreEqual(10f, _fury.OnHeavyHit(false), Tol);
            Assert.AreEqual(15f, _fury.OnHeavyHit(true), Tol);
            Assert.AreEqual(25f, _fury.Pool.Current, Tol);
        }

        [Test]
        public void Kill_Gives3_IgnoringProcCoefficient()
        {
            Assert.AreEqual(3f, _fury.OnKill(), Tol);
        }

        [Test]
        public void DamageTaken_OnePerPercentOfMaxHealth()
        {
            Assert.AreEqual(7f, _fury.OnDamageTaken(70f, 1000f), Tol);
        }

        [Test]
        public void DamageTaken_CappedAt15PerSecond_ThenRefillsNextWindow()
        {
            Assert.AreEqual(10f, _fury.OnDamageTaken(100f, 1000f), Tol);
            Assert.AreEqual(5f, _fury.OnDamageTaken(100f, 1000f), Tol, "only 5 left in the 1 s window");
            Assert.AreEqual(0f, _fury.OnDamageTaken(100f, 1000f), Tol);
            Run(1.0f);
            Assert.AreEqual(10f, _fury.OnDamageTaken(100f, 1000f), Tol, "new window");
        }

        [Test]
        public void ResourceGeneration_MultipliesEveryGain()
        {
            _stats.AddMod(_stats.CreateSource(), new StatMod(StatId.ResourceGeneration, ModBucket.Flat, 0.25f));
            Assert.AreEqual(6.25f, _fury.OnBasicHit(1), Tol);
            Assert.AreEqual(3.75f, _fury.OnKill(), Tol);
        }

        [Test]
        public void Ceiling_ClampsAt100_AndRaisesFullOnce()
        {
            for (int i = 0; i < 12; i++) _fury.OnHeavyHit(true);
            Assert.AreEqual(100f, _fury.Pool.Current, Tol);
            Assert.AreEqual(1, _rec.Fulls);
            Assert.AreEqual(100f, _rec.Gained_, Tol, "gain events carry only what fit under the ceiling");
            Assert.AreEqual(0f, _fury.OnKill(), "no gain when full");
        }

        [Test]
        public void MaximumResource_RaisesCeilingOnePerPercent_UpTo150()
        {
            var src = _stats.CreateSource();
            _stats.AddMod(src, new StatMod(StatId.MaximumResource, ModBucket.Flat, 0.30f));
            _fury.SyncMaximum();
            Assert.AreEqual(130f, _fury.Pool.Maximum, Tol);
            _stats.AddMod(src, new StatMod(StatId.MaximumResource, ModBucket.Flat, 0.90f));
            _fury.SyncMaximum();
            Assert.AreEqual(150f, _fury.Pool.Maximum, Tol, "absolute cap");
            for (int i = 0; i < 15; i++) _fury.OnHeavyHit(true);
            Assert.AreEqual(150f, _fury.Pool.Current, Tol);
        }

        [Test]
        public void MaximumResource_Shrinking_ClampsCurrent()
        {
            var src = _stats.CreateSource();
            _stats.AddMod(src, new StatMod(StatId.MaximumResource, ModBucket.Flat, 0.50f));
            _fury.SyncMaximum();
            for (int i = 0; i < 10; i++) _fury.OnHeavyHit(true);
            Assert.AreEqual(150f, _fury.Pool.Current, Tol);
            _stats.RemoveSource(src);
            _fury.SyncMaximum();
            Assert.AreEqual(100f, _fury.Pool.Current, Tol);
        }

        [Test]
        public void Decay_DoesNotStartBefore4Seconds()
        {
            _fury.OnHeavyHit(true);
            Run(3.9f);
            Assert.AreEqual(15f, _fury.Pool.Current, Tol);
        }

        [Test]
        public void Decay_EightPerSecondAfterFourSeconds()
        {
            _fury.OnHeavyHit(true);
            _fury.OnHeavyHit(true);
            _fury.OnHeavyHit(true); // 45
            Run(5.0f);               // 4 s idle + 1 s decay
            Assert.AreEqual(37f, _fury.Pool.Current, 0.2f);
            Run(1.0f);
            Assert.AreEqual(29f, _fury.Pool.Current, 0.2f);
        }

        [Test]
        public void Decay_ReachesZeroAndRaisesEmptyOnce()
        {
            _fury.OnHeavyHit(false);
            Run(4f + 2f);
            Assert.AreEqual(0f, _fury.Pool.Current, Tol);
            Assert.AreEqual(1, _rec.Empties);
        }

        [Test]
        public void Decay_ResetByDamageDealtOrTaken()
        {
            _fury.OnHeavyHit(true);
            Run(3.5f);
            _fury.OnDamageDealt();
            Run(3.5f);
            Assert.AreEqual(15f, _fury.Pool.Current, Tol, "dealing damage restarted the delay");
            _fury.OnDamageTaken(0f, 1000f);
            Run(3.5f);
            Assert.AreEqual(15f, _fury.Pool.Current, Tol, "receiving damage restarted the delay");
        }

        [Test]
        public void Spend_ReducesAndPublishesSpent()
        {
            _fury.OnHeavyHit(true); // 15
            Assert.IsFalse(_fury.TrySpend(20f));
            Assert.AreEqual(15f, _fury.Pool.Current, Tol);
            Assert.IsTrue(_fury.TrySpend(10f));
            Assert.AreEqual(5f, _fury.Pool.Current, Tol);
            Assert.AreEqual(10f, _rec.Spent_, Tol);
        }

        [Test]
        public void Spend_AppliesResourceCostReduction()
        {
            _stats.AddMod(_stats.CreateSource(), new StatMod(StatId.ResourceCostReduction, ModBucket.Flat, 0.2f));
            _fury.OnHeavyHit(true); // 15
            Assert.IsTrue(_fury.TrySpend(10f));
            Assert.AreEqual(7f, _fury.Pool.Current, Tol);
        }

        [Test]
        public void SpendAll_EmptiesAndRaisesEmpty()
        {
            _fury.OnHeavyHit(true);
            _fury.OnHeavyHit(true);
            Assert.AreEqual(30f, _fury.SpendAll(), Tol);
            Assert.AreEqual(0f, _fury.Pool.Current);
            Assert.AreEqual(1, _rec.Empties);
            Assert.AreEqual(0f, _fury.SpendAll());
        }

        [Test]
        public void StartOfRoom_ResetsToZero()
        {
            _fury.OnHeavyHit(true);
            _fury.ResetForRoom();
            Assert.AreEqual(0f, _fury.Pool.Current);
            Assert.AreEqual(1, _rec.Empties);
        }

        [Test]
        public void ConsumeEvents_ReadsOwnKillsAndDamageTaken_OncePerEvent()
        {
            var stream = new CombatEventStream(16);
            var me = new EntityId(1);
            var other = new EntityId(2);
            stream.Publish(new OnKill(1, 0, me, other, 10f, false, Vector3.zero));
            stream.Publish(new OnKill(1, 0, other, me, 10f, false, Vector3.zero));
            stream.Publish(new OnHitTaken(1, 0, other, me, 50f, Eclipse.Core.Damage.DamageType.Physical, default, default));
            stream.Publish(new OnHitTaken(1, 0, me, other, 50f, Eclipse.Core.Damage.DamageType.Physical, default, default));
            _fury.ConsumeEvents(stream, me, 1000f);
            _fury.ConsumeEvents(stream, me, 1000f);
            Assert.AreEqual(3f + 5f, _fury.Pool.Current, Tol);
            stream.EndFrame();
            stream.Publish(new OnKill(2, 0, me, other, 10f, false, Vector3.zero));
            _fury.ConsumeEvents(stream, me, 1000f);
            Assert.AreEqual(11f, _fury.Pool.Current, Tol);
        }

        [Test]
        public void Events_GainedSpentFullEmpty_AreOrdered()
        {
            _fury.OnHeavyHit(true);
            _fury.SpendAll();
            CollectionAssert.AreEqual(new[] { "G", "S", "E" }, _rec.Log);
        }

        [Test]
        public void Component_PublishesResourceEventsToStream()
        {
            var asset = FindFuryAsset();
            var go = new GameObject("fury-test");
            try
            {
                var stream = new CombatEventStream(16);
                var c = go.AddComponent<ResourceComponent>();
                c.Initialize(asset, new EntityId(1), stream);
                c.Tick(new SimulationFrame(7));
                c.Pool.Add(100f);
                Assert.AreEqual(1, stream.Count<OnResourceGained>());
                Assert.AreEqual(100f, stream.Read<OnResourceGained>()[0].Value, Tol);
                Assert.AreEqual(7, stream.Read<OnResourceGained>()[0].Frame);
                Assert.AreEqual("resource.fury", stream.Read<OnResourceGained>()[0].Resource.Value);
                Assert.AreEqual(1, stream.Count<OnResourceFull>());
                c.Pool.SpendAll();
                Assert.AreEqual(1, stream.Count<OnResourceSpent>());
                Assert.AreEqual(1, stream.Count<OnResourceEmpty>());
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ImportedAsset_ProvidesEveryRuleNumber()
        {
            var asset = FindFuryAsset();
            var r = ResourceRules.FromDefinition(asset);
            Assert.AreEqual(100f, r.BaseMaximum);
            Assert.AreEqual(150f, r.AbsoluteCap);
            Assert.AreEqual(4f, r.DecayDelaySeconds);
            Assert.AreEqual(8f, r.DecayPerSecond);
            var g = FuryGainRules.FromDefinition(asset);
            Assert.AreEqual(5f, g.BasicFirstTarget);
            Assert.AreEqual(9f, g.BasicMaxPerHit);
            Assert.AreEqual(15f, g.HeavyCharged);
            Assert.AreEqual(15f, g.DamageTakenMaxPerSecond);
            Assert.AreEqual(3f, g.Kill);
            var p = KaelPassiveRules.FromDefinition(asset);
            Assert.AreEqual(10f, p.EmbersFuryStep);
            Assert.AreEqual(0.02f, p.EmbersIncreasedPerStep, 1e-6f);
            Assert.AreEqual(0.35f, p.ResolveHealthThreshold, 1e-6f);
            Assert.AreEqual(0.15f, p.ResolveDamageReduction, 1e-6f);
            Assert.AreEqual(0.25f, p.ResolveResourceGeneration, 1e-6f);
        }

        private static ResourceDefinition FindFuryAsset()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ResourceDefinition", new[] { "Assets/_Project/Content/Heroes/Kael" }))
            {
                var d = AssetDatabase.LoadAssetAtPath<ResourceDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null && d.StableIdString == "resource.fury") return d;
            }
            Assert.Fail("resource.fury asset not found (run the data import)");
            return null;
        }
    }
}
