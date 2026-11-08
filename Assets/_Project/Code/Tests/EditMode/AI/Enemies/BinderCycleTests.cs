using Eclipse.AI.Enemies;
using Eclipse.AI.Locomotion;
using Eclipse.AI.StateMachine;
using Eclipse.Core.Random;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Data.Enemies;
using NUnit.Framework;
using UnityEngine;
using static Eclipse.Tests.EditMode.AI.Enemies.EnemyTestKit;

namespace Eclipse.Tests.EditMode.AI.Enemies
{
    /// <summary>
    /// Approach, telegraph, hit, recover for each archetype template, on the real M07 data and a simulated 60 Hz clock
    /// (binder + brain + executor + token director; the world is a recording fake).
    /// </summary>
    public class BinderCycleTests
    {
        private const int Bound = 3000;

        // ---------------------------------------------------------------- Fodder (Cinder Husk)

        [Test]
        public void FodderSwarmsInThenLungesAndRecovers()
        {
            var rig = new Rig(Blueprint(Husk), 1UL, null, 10f);
            rig.Step(30);
            Assert.AreEqual(EnemyState.Approach, rig.Output.State);
            Assert.AreEqual(SteeringMode.Seek, rig.Output.Steering.Mode, "swarm: straight at the target");
            Assert.AreEqual(0, rig.Delivery.Count("start"), "out of reach: no telegraph");

            rig.Distance(1.5f);
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound), "attack decided in reach");
            int started = rig.Tick - 1;
            Assert.AreEqual(1, rig.Delivery.Count("start"));
            Assert.AreEqual(EnemyState.Attack, rig.Output.State);
            Assert.AreEqual(SteeringMode.Hold, rig.Output.Steering.Mode, "the wind-up holds position");
            Assert.AreEqual(1, rig.Tokens.ActiveMelee, "token held during the attack");

            Assert.IsTrue(rig.StepUntil(() => rig.Delivery.Count("impact") == 1, 200));
            Assert.AreEqual(36, rig.Delivery.First("impact").Tick - started, "0.6 s wind-up at Normal");

            Assert.IsTrue(rig.StepUntil(() => rig.Output.State == EnemyState.Recover, 200));
            Assert.AreEqual(0, rig.Tokens.ActiveMelee, "token returned when the attack ends");
            int recoverFrom = rig.Tick;
            Assert.IsTrue(rig.StepUntil(() => rig.Output.State != EnemyState.Recover, 200));
            Assert.AreEqual(30, rig.Tick - recoverFrom, 2, "30 f of recovery from data");
            Assert.IsTrue(rig.Binder.CooldownRemaining(0) >= 0f);
        }

        [Test]
        public void CooldownFromTheDataSpacesTheLunges()
        {
            var rig = new Rig(Blueprint(Husk), 3UL, null, 1.5f);
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound));
            int first = rig.Tick - 1;
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound));
            int second = rig.Tick - 1;
            Assert.GreaterOrEqual(second - first, 2 * 60 - 2, "cooldown_s 2.0 counted from the start of the attack (minus float noise)");
        }

        // ---------------------------------------------------------------- Melee (Ashborn Marauder)

        [Test]
        public void MeleeComboChainsTwoStrikesThenRecovers()
        {
            var rig = new Rig(Blueprint(Marauder), 5UL, null, 1.5f);
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound), "combo chosen");
            int started = rig.Tick - 1;

            Assert.IsTrue(rig.StepUntil(() => rig.Delivery.Count("impact") == 2, 400));
            Assert.AreEqual(36, rig.Delivery.Nth("impact", 0).Tick - started);
            Assert.AreEqual(96, rig.Delivery.Nth("impact", 1).Tick - started, "second strike chained after the 18 f recovery");
            Assert.AreEqual(EnemyState.Attack, rig.Output.State, "still inside the same attack");

            Assert.IsTrue(rig.StepUntil(() => rig.Output.State == EnemyState.Recover, 200));
            int recoverFrom = rig.Tick;
            Assert.IsTrue(rig.StepUntil(() => rig.Output.State != EnemyState.Recover, 200));
            Assert.AreEqual(36, rig.Tick - recoverFrom, 2, "36 f recovery after the last strike");
        }

        [Test]
        public void MeleeApproachesToItsSlotAtReach()
        {
            var rig = new Rig(Blueprint(Marauder), 5UL, null, 12f);
            rig.Step(30);
            Assert.AreEqual(EnemyState.Approach, rig.Output.State);
            Assert.AreEqual(SteeringMode.Arrive, rig.Output.Steering.Mode);
            Assert.AreEqual(2f, Vector3.Distance(rig.Output.Steering.Target, rig.Input.Target), 1e-3f, "arrives at the preferred reach of the target");
        }

        [Test]
        public void WithoutATokenTheEnemyOrbitsInsteadOfAttacking()
        {
            var rig = new Rig(Blueprint(Marauder), 5UL, Policy(melee: 0), 1.5f);
            rig.Step(240);
            Assert.AreEqual(0, rig.Delivery.Count("start"), "no token, no attack");
            Assert.AreEqual(SteeringMode.Orbit, rig.Output.Steering.Mode);
            Assert.GreaterOrEqual(rig.Output.Steering.OrbitRadius, 4f);
            Assert.LessOrEqual(rig.Output.Steering.OrbitRadius, 7f);
        }

        [Test]
        public void TheMinimumIntervalHoldsBetweenAttacksOfTheSameEnemy()
        {
            // No cooldown in the data, so only the director's interval (1.2 s at Normal) spaces the attacks.
            AttackDefinition jab = MakeAttack("attack.test.jab", AttackDelivery.Melee, TelegraphShape.Circle, 0.6f, 6, 6, range: 3f,
                condition: "target_dist<=3.0");
            EnemyDefinition e = MakeEnemy("enemy.test.jabber", EnemyArchetype.Standard, ArchetypeTemplates.MeleeId, 0f, 2f, 0f, 0f, jab);
            var rig = new Rig(EnemyBlueprint.Build(e, new EnemyStats(30f, 10f), 0f), 9UL, Policy(interval: 1.2f), 1.5f);
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound));
            int first = rig.Tick - 1;
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound));
            Assert.GreaterOrEqual((rig.Tick - 1 - first) / 60f, 1.2f);
        }

        // ---------------------------------------------------------------- Ranged (Slag Hurler)

        [Test]
        public void RangedLobsFromTheBandWithTheLaunchBeforeTheImpact()
        {
            var rig = new Rig(Blueprint(Hurler), 2UL, null, 10f);
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound));
            int started = rig.Tick - 1;
            Assert.AreEqual(1, rig.Tokens.ActiveRanged, "ranged token");
            Assert.IsTrue(rig.StepUntil(() => rig.Delivery.Count("impact") == 1, 200));
            Assert.AreEqual(60, rig.Delivery.First("impact").Tick - started, "1.0 s wind-up");
            Recorded launch = rig.Delivery.First("launch");
            Assert.IsNotNull(launch);
            Assert.Less(launch.Tick, rig.Delivery.First("impact").Tick, "the arc is in the air before the telegraph ends");
            Assert.AreEqual(rig.Delivery.First("impact").Tick - 43, launch.Tick, 2, "10 m at 14 m/s = 0.71 s of flight");
        }

        [Test]
        public void RangedDoesNotLobOffScreen()
        {
            var rig = new Rig(Blueprint(Hurler), 2UL, null, 10f);
            rig.Input.OnScreen = false;
            rig.Step(600);
            Assert.AreEqual(0, rig.Delivery.Count("start"), "06 section 3.10: nothing is fired off-screen");
            rig.Input.OnScreen = true;
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound), "fires as soon as it is on-screen");
        }

        [Test]
        public void RangedNeedsLineOfSight()
        {
            var rig = new Rig(Blueprint(Hurler), 2UL, null, 10f);
            rig.Input.HasLineOfSight = false;
            rig.Step(600);
            Assert.AreEqual(0, rig.Delivery.Count("start"));
            rig.Input.HasLineOfSight = true;
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound));
        }

        [Test]
        public void RangedBelowTheMinimumRangeDoesNotLobAndBacksOff()
        {
            var rig = new Rig(Blueprint(Hurler), 2UL, null, 3f);
            rig.Step(12);
            Assert.AreEqual(0, rig.Delivery.Count("start"), "condition target_dist>=5");
            Assert.IsTrue(rig.Binder.Retreat.Active, "player closer than 5 m");
            Assert.AreEqual(SteeringMode.Flee, rig.Output.Steering.Mode);

            rig.Distance(9f);
            rig.Step(2);
            Assert.IsFalse(rig.Binder.Retreat.Active, "back inside 8-14 m");
            rig.Distance(3f);
            rig.Step(2);
            Assert.IsFalse(rig.Binder.Retreat.Active, "4 s cooldown");
            rig.Step(60 * 4);
            Assert.IsTrue(rig.Binder.Retreat.Active, "available again after the cooldown");
        }

        [Test]
        public void RangedStraightShotIsNotFiredThroughATankAlly()
        {
            AttackDefinition shot = MakeAttack("attack.test.shot", AttackDelivery.Projectile, TelegraphShape.Line, 0.8f, 6, 30,
                kind: TokenKind.Ranged, range: 14f, cooldown: 1f, condition: "target_dist>=5.0", speed: 14f);
            EnemyDefinition e = MakeEnemy("enemy.test.shooter", EnemyArchetype.Ranged, ArchetypeTemplates.RangedId, 8f, 14f, 5f, 4f, shot);
            var rig = new Rig(EnemyBlueprint.Build(e, new EnemyStats(30f, 10f), 0f), 2UL, null, 10f);
            rig.Input.ShotBlockedByAlly = true;
            rig.Step(600);
            Assert.AreEqual(0, rig.Delivery.Count("start"), "09 section 4.2: no shot through a Tank ally");
            rig.Input.ShotBlockedByAlly = false;
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound));
            Assert.IsTrue(rig.StepUntil(() => rig.Delivery.Count("impact") == 1, 200));
            Assert.AreEqual(0, rig.Delivery.Count("launch"), "straight projectiles spawn on impact, not as an arc");
        }

        // ---------------------------------------------------------------- Tank (Kiln Brute)

        [Test]
        public void TankSlamsThenRecoversForTwoSeconds()
        {
            var rig = new Rig(Blueprint(Brute), 4UL, null, 3f);
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound));
            int started = rig.Tick - 1;
            Assert.IsTrue(rig.StepUntil(() => rig.Delivery.Count("impact") == 1, 200));
            Assert.AreEqual(66, rig.Delivery.First("impact").Tick - started, "1.1 s wind-up");
            Assert.IsTrue(rig.Delivery.First("impact").Plan.Flags.HasFlag(Eclipse.Core.Tags.HitFlags.Unblockable));
            Assert.IsTrue(rig.StepUntil(() => rig.Output.State == EnemyState.Recover, 200));
            int recoverFrom = rig.Tick;
            Assert.IsTrue(rig.StepUntil(() => rig.Output.State != EnemyState.Recover, 400));
            Assert.AreEqual(120, rig.Tick - recoverFrom, 2, "2 s of recovery after the slam");
        }

        [Test]
        public void TankAdvancesStraightWhenFarAndEscortsAWard()
        {
            var rig = new Rig(Blueprint(Brute), 4UL, null, 15f);
            rig.Step(30);
            Assert.AreEqual(SteeringMode.Seek, rig.Output.Steering.Mode);
            rig.Input.HasEscortWard = true;
            rig.Input.EscortWard = new Vector3(0f, 0f, 20f);
            rig.Step(30);
            Assert.AreEqual(SteeringMode.Arrive, rig.Output.Steering.Mode);
        }

        // ---------------------------------------------------------------- stun, interruption, death

        [Test]
        public void StunCancelsTheWindupAndStopsNewAttacks()
        {
            var rig = new Rig(Blueprint(Marauder), 5UL, null, 1.5f);
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted >= 0, Bound));
            rig.Step(10);
            Assert.IsTrue(rig.Binder.Executor.IsBusy);
            rig.Binder.CancelAttack();
            Assert.IsFalse(rig.Binder.Executor.IsBusy);
            Assert.AreEqual(1, rig.Delivery.Count("cancel"));
            rig.Step(90); // the brain is still inside its Attack state
            Assert.AreEqual(0, rig.Delivery.Count("impact"));

            rig.Input.CanAct = false;
            int starts = rig.Delivery.Count("start");
            rig.Step(600);
            Assert.AreEqual(starts, rig.Delivery.Count("start"), "stunned: nothing new begins");
        }

        [Test]
        public void CyanWindowInterruptsAnInterruptibleStrike()
        {
            AttackDefinition cast = MakeAttack("attack.test.cast", AttackDelivery.Melee, TelegraphShape.Circle, 1f, 6, 30, interruptible: true,
                range: 3f, condition: "target_dist<=3.0");
            EnemyDefinition e = MakeEnemy("enemy.test.caster", EnemyArchetype.Standard, ArchetypeTemplates.MeleeId, 0f, 2f, 0f, 0f, cast);
            var rig = new Rig(EnemyBlueprint.Build(e, new EnemyStats(30f, 10f), 0f), 6UL, null, 1.5f);
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound));
            rig.Step(10);
            Assert.IsTrue(rig.Binder.Executor.InInterruptWindow);
            Assert.IsTrue(rig.Binder.TryInterrupt());
            rig.Step(120);
            Assert.AreEqual(0, rig.Delivery.Count("impact"));
        }

        private static ulong SeedWithDeathRoll(bool fires)
        {
            for (ulong seed = 1; seed < 500; seed++)
            {
                var r = new RngStreams(seed);
                float roll = r.Get(RngStream.Combat).NextFloat01();
                if (fires ? roll < 0.25f : roll >= 0.25f) return seed;
            }
            Assert.Fail("no seed found");
            return 0;
        }

        [Test]
        public void HuskSparkFiresOnDeathWithTheDataChance()
        {
            var rig = new Rig(Blueprint(Husk), SeedWithDeathRoll(true), null, 10f);
            rig.Step(5);
            rig.Input.IsDead = true;
            rig.Step(1);
            Assert.AreEqual(EnemyState.Dead, rig.Output.State);
            Assert.IsFalse(rig.Output.ReadyToDespawn, "the spark telegraph is still running");
            Assert.AreEqual(1, rig.Delivery.Count("start"));
            Assert.AreSame(Blueprint(Husk).DeathPlans[0].Definition, rig.Delivery.First("start").Plan.Definition);
            rig.Step(36);
            Assert.AreEqual(1, rig.Delivery.Count("impact"), "0.6 s later");
            rig.Step(10);
            Assert.IsTrue(rig.Output.ReadyToDespawn, "after the spark the object returns to the pool");
        }

        [Test]
        public void HuskWithoutTheSparkRollDespawnsAtOnce()
        {
            var rig = new Rig(Blueprint(Husk), SeedWithDeathRoll(false), null, 10f);
            rig.Step(5);
            rig.Input.IsDead = true;
            rig.Step(1);
            Assert.AreEqual(0, rig.Delivery.Count("start"));
            Assert.IsTrue(rig.Output.ReadyToDespawn);
        }

        [Test]
        public void SparkChanceIsRoughlyTwentyFivePercent()
        {
            int fired = 0;
            const int runs = 400;
            EnemyBlueprint bp = Blueprint(Husk);
            for (ulong seed = 1; seed <= runs; seed++)
            {
                var rig = new Rig(bp, seed * 7919UL, null, 10f);
                rig.Input.IsDead = true;
                rig.Step(1);
                if (rig.Delivery.Count("start") > 0) fired++;
            }
            Assert.That(fired / (float)runs, Is.EqualTo(0.25f).Within(0.07f));
        }

        [Test]
        public void DeathCancelsAnAttackInProgressAndReturnsTheToken()
        {
            var rig = new Rig(Blueprint(Marauder), 5UL, null, 1.5f);
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted >= 0, Bound));
            rig.Step(10);
            Assert.AreEqual(1, rig.Tokens.ActiveMelee);
            rig.Input.IsDead = true;
            rig.Step(1);
            Assert.AreEqual(0, rig.Tokens.ActiveMelee, "token released on death");
            Assert.AreEqual(1, rig.Delivery.Count("cancel"));
            Assert.IsTrue(rig.Output.ReadyToDespawn, "Marauder has no on-death attack");
            rig.Step(200);
            Assert.AreEqual(0, rig.Delivery.Count("impact"));
        }

        [Test]
        public void ResetReturnsToAFreshIdleEnemy()
        {
            var rig = new Rig(Blueprint(Husk), 1UL, null, 1.5f);
            Assert.IsTrue(rig.StepUntil(() => rig.Output.AttackStarted == 0, Bound));
            rig.Binder.Reset();
            Assert.AreEqual(EnemyState.Idle, rig.Binder.State);
            Assert.IsFalse(rig.Binder.Executor.IsBusy);
            Assert.AreEqual(0f, rig.Binder.CooldownRemaining(0));
        }

        [Test]
        public void NoTargetHoldsPosition()
        {
            var rig = new Rig(Blueprint(Marauder), 5UL, null, 10f);
            rig.Input.HasTarget = false;
            rig.Step(60);
            Assert.AreEqual(SteeringMode.Hold, rig.Output.Steering.Mode);
            Assert.AreEqual(EnemyState.Idle, rig.Output.State);
        }
    }
}
