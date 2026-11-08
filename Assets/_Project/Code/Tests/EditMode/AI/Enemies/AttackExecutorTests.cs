using Eclipse.AI.Enemies;
using Eclipse.AI.Telegraphs;
using Eclipse.Core.Damage;
using Eclipse.Data.Enemies;
using NUnit.Framework;
using UnityEngine;
using static Eclipse.Tests.EditMode.AI.Enemies.EnemyTestKit;

namespace Eclipse.Tests.EditMode.AI.Enemies
{
    /// <summary>Wind-up, impact, active window, combo and interruption in simulated 60 Hz time.</summary>
    public class AttackExecutorTests
    {
        private static AttackAim Aim(float distance) => new AttackAim
        {
            Origin = Vector3.zero, Forward = Vector3.forward, Target = new Vector3(0f, 0f, distance),
        };

        private static void Run(AttackExecutor ex, RecordingDelivery d, int ticks, float distance = 2f)
        {
            AttackAim aim = Aim(distance);
            for (int i = 0; i < ticks; i++)
            {
                d.Tick++;
                ex.Tick(Dt, in aim);
            }
        }

        [Test]
        public void ImpactLandsOnTheTickTheFillReachesTheBorder()
        {
            AttackPlan lunge = Blueprint(Husk).Heads[0]; // 0.6 s = 36 ticks, 6 active, no chain
            var d = new RecordingDelivery();
            var ex = new AttackExecutor(d);
            AttackAim aim = Aim(1.5f);
            Assert.IsTrue(ex.Begin(lunge, in aim));
            Assert.AreEqual(1, d.Count("start"));
            Assert.AreEqual(ExecutorPhase.Windup, ex.Phase);

            Run(ex, d, 35);
            Assert.AreEqual(0, d.Count("impact"), "no impact before the fill touches the border");
            Assert.Less(ex.TelegraphProgress, 1f);
            Run(ex, d, 1);
            Assert.AreEqual(1, d.Count("impact"));
            Assert.AreEqual(36, d.First("impact").Tick);
            Assert.AreEqual(1, d.Count("end"), "telegraph closed as completed");
            Assert.AreEqual(ExecutorPhase.Active, ex.Phase);

            Run(ex, d, 6);
            Assert.IsFalse(ex.IsBusy);
            Assert.AreEqual(ExecutorResult.Completed, ex.LastResult);
            Assert.AreEqual(1, d.Count("impact"), "one hit per strike");
        }

        [Test]
        public void ProgressRisesFromZeroToOne()
        {
            AttackPlan lunge = Blueprint(Husk).Heads[0];
            var d = new RecordingDelivery();
            var ex = new AttackExecutor(d);
            AttackAim aim = Aim(1.5f);
            ex.Begin(lunge, in aim);
            Run(ex, d, 36);
            float last = -1f;
            foreach (Recorded r in d.Log)
            {
                if (r.Kind != "progress") continue;
                Assert.GreaterOrEqual(r.Progress, last);
                last = r.Progress;
            }
            Assert.AreEqual(1f, last, 1e-5f);
        }

        [Test]
        public void ComboRunsBothStrikesWithTheRecoveryBetween()
        {
            AttackPlan combo = Blueprint(Marauder).Heads[0];
            var d = new RecordingDelivery();
            var ex = new AttackExecutor(d);
            AttackAim aim = Aim(1.5f);
            ex.Begin(combo, in aim);

            // Strike 1 impact at 36, active to 42, recovery 18 f to 60, strike 2 wind-up 36 f to 96, active to 102.
            Run(ex, d, 36);
            Assert.AreEqual(1, d.Count("impact"));
            Run(ex, d, 24); // tick 60: second wind-up starts
            Assert.AreEqual(2, d.Count("start"));
            Assert.AreSame(combo.Next, d.Nth("start", 1).Plan);
            Assert.AreEqual(1, d.Count("impact"));
            Run(ex, d, 36); // tick 96
            Assert.AreEqual(2, d.Count("impact"));
            Assert.AreSame(combo.Next, d.Nth("impact", 1).Plan);
            Run(ex, d, 6);
            Assert.IsFalse(ex.IsBusy);
            Assert.AreEqual(ExecutorResult.Completed, ex.LastResult);
            Assert.AreEqual(Mathf.RoundToInt(combo.ChainAttackSeconds * 60f), d.Tick, 1, "the brain's Attack length matches the executor");
        }

        [Test]
        public void InterruptibleWindupIsCancelledByStaggerInTheCyanWindow()
        {
            AttackDefinition def = MakeAttack("attack.test.interruptible", AttackDelivery.Melee, TelegraphShape.Circle, 1f, 6, 30, interruptible: true);
            AttackPlan plan = AttackPlan.Build(def, new EnemyStats(10f, 5f), DamageType.Physical, 0f, WindupClass.Common);
            var d = new RecordingDelivery();
            var ex = new AttackExecutor(d);
            AttackAim aim = Aim(1.5f);
            ex.Begin(plan, in aim);
            Run(ex, d, 10);
            Assert.IsTrue(ex.InInterruptWindow);
            Assert.IsTrue(ex.TryInterrupt());
            Assert.AreEqual(1, d.Count("cancel"));
            Assert.IsFalse(ex.IsBusy);
            Assert.AreEqual(ExecutorResult.Interrupted, ex.LastResult);
            Run(ex, d, 120);
            Assert.AreEqual(0, d.Count("impact"), "an interrupted strike never lands");
        }

        [Test]
        public void NonInterruptibleWindupIgnoresTheInterruptButStunCancels()
        {
            AttackPlan lunge = Blueprint(Husk).Heads[0];
            var d = new RecordingDelivery();
            var ex = new AttackExecutor(d);
            AttackAim aim = Aim(1.5f);
            ex.Begin(lunge, in aim);
            Run(ex, d, 10);
            Assert.IsFalse(ex.InInterruptWindow);
            Assert.IsFalse(ex.TryInterrupt());
            Assert.IsTrue(ex.IsBusy);
            ex.Cancel(); // stun / Staggered / death
            Assert.AreEqual(ExecutorResult.Cancelled, ex.LastResult);
            Assert.AreEqual(1, d.Count("cancel"));
            Run(ex, d, 120);
            Assert.AreEqual(0, d.Count("impact"));
        }

        [Test]
        public void CancelBetweenComboStrikesDropsTheSecondStrike()
        {
            AttackPlan combo = Blueprint(Marauder).Heads[0];
            var d = new RecordingDelivery();
            var ex = new AttackExecutor(d);
            AttackAim aim = Aim(1.5f);
            ex.Begin(combo, in aim);
            Run(ex, d, 50); // inside the recovery between strikes
            Assert.AreEqual(ExecutorPhase.StepRecovery, ex.Phase);
            ex.Cancel();
            Run(ex, d, 200);
            Assert.AreEqual(1, d.Count("impact"));
            Assert.AreEqual(1, d.Count("start"));
        }

        [Test]
        public void LobbedAttackLaunchesSoThatItLandsWithTheTelegraph()
        {
            AttackPlan slag = Blueprint(Hurler).Heads[0]; // wind-up 1.0 s = 60 ticks, 14 m/s
            var d = new RecordingDelivery();
            var ex = new AttackExecutor(d);
            AttackAim aim = Aim(7f); // flight 0.5 s: launch 0.5 s into the wind-up
            ex.Begin(slag, in aim);
            Run(ex, d, 60, 7f);
            Assert.AreEqual(1, d.Count("launch"));
            Assert.AreEqual(30, d.First("launch").Tick, 1, "launched half a second before the impact");
            Assert.AreEqual(60, d.First("impact").Tick);
        }

        [Test]
        public void LobbedAttackAtMaxRangeLaunchesImmediately()
        {
            AttackPlan slag = Blueprint(Hurler).Heads[0];
            var d = new RecordingDelivery();
            var ex = new AttackExecutor(d);
            AttackAim aim = Aim(14f); // flight 1.0 s = whole wind-up
            ex.Begin(slag, in aim);
            Run(ex, d, 2, 14f);
            Assert.AreEqual(1, d.Count("launch"));
            Assert.LessOrEqual(d.First("launch").Tick, 1);
        }

        [Test]
        public void BeginWhileBusyIsRefused()
        {
            AttackPlan lunge = Blueprint(Husk).Heads[0];
            var ex = new AttackExecutor(new RecordingDelivery());
            AttackAim aim = Aim(1.5f);
            Assert.IsTrue(ex.Begin(lunge, in aim));
            Assert.IsFalse(ex.Begin(lunge, in aim));
        }

        [Test]
        public void GeometryMatchesTheTelegraphAndTheHit()
        {
            AttackPlan lunge = Blueprint(Husk).Heads[0];
            AttackPlan overhead = Blueprint(Marauder).Heads[1];
            AttackPlan combo = Blueprint(Marauder).Heads[0];
            AttackPlan slag = Blueprint(Hurler).Heads[0];
            var aim = new AttackAim { Origin = new Vector3(1f, 0f, 1f), Forward = Vector3.right, Target = new Vector3(8f, 0f, 1f) };

            Assert.AreEqual(new Vector3(2f, 0f, 1f), AttackGeometry.ShapeOrigin(lunge, in aim), "circle centered 1 m ahead");
            Assert.AreEqual(aim.Origin, AttackGeometry.ShapeOrigin(combo, in aim));
            Assert.AreEqual(aim.Target, AttackGeometry.ShapeOrigin(slag, in aim), "lob telegraph sits at the destination");

            Assert.IsTrue(AttackGeometry.TryShape(lunge, out var circle));
            Assert.AreEqual(360f, circle.ArcDegrees);
            Assert.AreEqual(1f, circle.Radius);
            Assert.IsTrue(AttackGeometry.TryShape(combo, out var cone));
            Assert.AreEqual(90f, cone.ArcDegrees);
            Assert.AreEqual(2f, cone.Radius);
            Assert.IsTrue(AttackGeometry.TryShape(overhead, out var line));
            Assert.AreEqual(3f, line.Length);
            Assert.AreEqual(1.5f, line.Width);

            TelegraphRequest t = AttackGeometry.ToTelegraph(overhead, in aim);
            Assert.AreEqual(TelegraphShape.Line, t.shape);
            Assert.AreEqual(90f, t.yawDegrees, 1e-3f, "+X is 90 degrees from +Z");
        }
    }
}
