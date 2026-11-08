using System;
using System.Collections.Generic;
using Eclipse.AI.Director;
using Eclipse.AI.Enemies;
using Eclipse.AI.Lod;
using Eclipse.AI.Telegraphs;
using Eclipse.Core.Damage;
using Eclipse.Core.Random;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using Eclipse.Data.Difficulties;
using Eclipse.Data.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Tests.EditMode.AI.Enemies
{
    /// <summary>Fixtures: the imported M07 assets (09 section 5.1) plus synthetic attacks for cases the data does not cover.</summary>
    internal static class EnemyTestKit
    {
        public const string Root = "Assets/_Project/Content";
        public const float Dt = 1f / 60f;

        public static T Load<T>(string id, string folder) where T : Definition
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
            {
                var d = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null && d.StableIdString == id) return d;
            }
            Assert.Fail("asset not found: " + id + " (run the data import)");
            return null;
        }

        public static EnemyDefinition Husk => Load<EnemyDefinition>("enemy.ashen.cinder_husk", Root + "/Enemies");
        public static EnemyDefinition Marauder => Load<EnemyDefinition>("enemy.ashen.marauder", Root + "/Enemies");
        public static EnemyDefinition Hurler => Load<EnemyDefinition>("enemy.ashen.slag_hurler", Root + "/Enemies");
        public static EnemyDefinition Brute => Load<EnemyDefinition>("enemy.ashen.kiln_brute", Root + "/Enemies");
        public static DifficultyDefinition Normal => Load<DifficultyDefinition>("difficulty.normal", Root + "/Shared/Difficulty");

        public static EnemyScalingDefinition Scaling
        {
            get
            {
                string[] guids = AssetDatabase.FindAssets("t:EnemyScalingDefinition", new[] { Root + "/Enemies" });
                Assert.IsNotEmpty(guids, "EnemyScaling asset missing (run the data import)");
                return AssetDatabase.LoadAssetAtPath<EnemyScalingDefinition>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        public static EnemyStats StatsOf(EnemyDefinition enemy, int level = 1)
        {
            Assert.IsTrue(EnemyStatsBuilder.TryBuild(enemy, Scaling, level, Normal, out EnemyStats stats));
            return stats;
        }

        public static EnemyBlueprint Blueprint(EnemyDefinition enemy, int level = 1, float windupReduction = 0f) =>
            EnemyBlueprint.Build(enemy, StatsOf(enemy, level), windupReduction);

        public static TokenPolicy Policy(int melee = 3, int ranged = 2, float interval = 1.2f) => new TokenPolicy
        {
            MeleeTokens = melee, RangedTokens = ranged, AmbushTokens = 1, MinIntervalSeconds = interval, Orbit = TokenOrbitConfig.Default,
        };

        // ---- synthetic data (test inputs, not balance)

        public static AttackDefinition MakeAttack(string id, AttackDelivery delivery, TelegraphShape shape, float windup, int active,
            int recovery, bool interruptible = false, TokenKind kind = TokenKind.Melee, float range = 2f, float cooldown = 0f,
            string condition = "", float speed = 0f, AttackDefinition chain = null)
        {
            var a = ScriptableObject.CreateInstance<AttackDefinition>();
            a.EditorSetStableId(id);
            a.EditorSetScalars(AttackTrigger.Action, delivery, cooldown, range, 1f, condition, TelegraphType.Red, shape, 2f, 90f, 3f, 1f,
                0f, windup, active, recovery, 1f, 5f, HitFlags.None, 1, kind, interruptible, 1f, speed, string.Empty, 0f);
            if (chain != null) a.EditorSetChainNext(chain);
            return a;
        }

        public static EnemyDefinition MakeEnemy(string id, Eclipse.Core.RunGen.Encounters.EnemyArchetype archetype, string hfsm,
            float rangeMin, float rangeMax, float retreatBelow, float retreatCooldown, params AttackDefinition[] attacks)
        {
            var e = ScriptableObject.CreateInstance<EnemyDefinition>();
            e.EditorSetStableId(id);
            e.EditorSetScalars(archetype, "Hollow", 1f, 1f, 3, EnemyMassClass.Medium, 40f, 4f, DamageType.Physical, DamageType.Physical,
                new float[7], 0f, EnemyLootClass.Standard, 0UL, rangeMin, rangeMax, RepositionBehavior.Approach, retreatBelow,
                retreatCooldown, 0f, 0f, hfsm);
            e.EditorSetAttacks(new List<AttackDefinition>(attacks));
            return e;
        }

        public sealed class Recorded
        {
            public string Kind;
            public int Tick;
            public AttackPlan Plan;
            public float Progress;
        }

        /// <summary>Records what the executor asked for, stamped with the simulated tick.</summary>
        public sealed class RecordingDelivery : IAttackDelivery
        {
            public int Tick;
            public readonly List<Recorded> Log = new List<Recorded>();

            public void TelegraphStart(AttackPlan plan, in AttackAim aim) => Add("start", plan);
            public void TelegraphProgress(float progress) => Log.Add(new Recorded { Kind = "progress", Tick = Tick, Progress = progress });
            public void TelegraphEnd(bool completed) => Add(completed ? "end" : "cancel", null);
            public void Launch(AttackPlan plan, in AttackAim aim) => Add("launch", plan);
            public void Impact(AttackPlan plan, in AttackAim aim) => Add("impact", plan);

            private void Add(string kind, AttackPlan plan) => Log.Add(new Recorded { Kind = kind, Tick = Tick, Plan = plan });

            public int Count(string kind)
            {
                int n = 0;
                foreach (Recorded r in Log) if (r.Kind == kind) n++;
                return n;
            }

            public Recorded First(string kind)
            {
                foreach (Recorded r in Log) if (r.Kind == kind) return r;
                return null;
            }

            public Recorded Nth(string kind, int index)
            {
                int n = 0;
                foreach (Recorded r in Log) if (r.Kind == kind && n++ == index) return r;
                return null;
            }
        }

        /// <summary>Binder plus a simulated 60 Hz clock, a real token director and a recording delivery.</summary>
        public sealed class Rig
        {
            public readonly EnemyBrainBinder Binder;
            public readonly RecordingDelivery Delivery = new RecordingDelivery();
            public readonly AttackTokenDirector Tokens;
            public BinderInput Input;
            public BinderOutput Output;
            public int Tick;
            public const int OwnerId = 7;

            public Rig(EnemyBlueprint blueprint, ulong seed = 1UL, TokenPolicy? policy = null, float targetDistance = 1.5f)
            {
                Tokens = new AttackTokenDirector(policy ?? Policy());
                Tokens.Register(OwnerId, TokenClass.Normal);
                Binder = new EnemyBrainBinder(blueprint, Tokens, new RngStreams(seed), OwnerId,
                    new DecisionLodConfig { NearRadius = 20f }, TokenOrbitConfig.Default, Delivery);
                Input = new BinderInput
                {
                    HasTarget = true, OnScreen = true, CanAct = true, HasLineOfSight = true, HpFraction = 1f,
                    Self = Vector3.zero, Forward = Vector3.forward, Target = new Vector3(0f, 0f, targetDistance),
                };
            }

            public void Distance(float meters) => Input.Target = new Vector3(0f, 0f, meters);

            public void Step(int ticks = 1)
            {
                for (int i = 0; i < ticks; i++)
                {
                    Tokens.Now = Tick / 60.0;
                    Delivery.Tick = Tick;
                    Binder.Tick(in Input, Dt, ref Output);
                    Tick++;
                }
            }

            /// <summary>Steps until the condition holds (checked after each tick). False when <paramref name="maxTicks"/> pass first.</summary>
            public bool StepUntil(Func<bool> condition, int maxTicks)
            {
                for (int i = 0; i < maxTicks; i++)
                {
                    Step();
                    if (condition()) return true;
                }
                return false;
            }
        }
    }
}
