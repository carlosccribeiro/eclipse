using Eclipse.AI.StateMachine;
using Eclipse.AI.Utility;
using Eclipse.Core.Random;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.AI.Brain
{
    public class EnemyBrainTests
    {
        private static EnemyBrain Make(float random, BrainTestKit.StubGate gate, float flee = 0f, ulong seed = 42UL)
            => new EnemyBrain(BrainTestKit.MakeConfig(random, flee), BrainTestKit.MakeLod(), new RngStreams(seed), gate, 1);

        /// <summary>Ticks until the brain enters <paramref name="target"/> (max 300 ticks); returns the last decision.</summary>
        private static AiDecision RunTo(EnemyBrain b, in AiPerception p, EnemyState target)
        {
            AiDecision d = default;
            for (int i = 0; i < 300; i++)
            {
                d = b.Tick(in p, BrainTestKit.Dt);
                if (b.State == target) return d;
            }
            Assert.Fail("never reached " + target + ", stuck in " + b.State);
            return d;
        }

        [Test]
        public void Chain_Idle_Engage_Approach_Attack_Recover_Engage()
        {
            var gate = new BrainTestKit.StubGate();
            var b = Make(0f, gate);
            Assert.That(b.State, Is.EqualTo(EnemyState.Idle));

            var idle = BrainTestKit.Percept(8f, target: false);
            b.Tick(in idle, BrainTestKit.Dt);
            Assert.That(b.State, Is.EqualTo(EnemyState.Idle));

            // New brain so the first decision happens right at the first target tick.
            b = Make(0f, gate);
            var far = BrainTestKit.Percept(8f);
            var d = b.Tick(in far, BrainTestKit.Dt);
            Assert.That(b.State, Is.EqualTo(EnemyState.Engage));
            Assert.That(d.StateChanged, Is.True);

            RunTo(b, in far, EnemyState.Approach);

            var close = BrainTestKit.Percept(1.5f, 0f);
            d = RunTo(b, in close, EnemyState.Attack);
            Assert.That(d.AttackStarted, Is.True);
            Assert.That(d.AttackIndex, Is.EqualTo(0));
            Assert.That(gate.Held, Is.EqualTo(1));

            d = RunTo(b, in close, EnemyState.Recover);
            Assert.That(gate.Held, Is.EqualTo(0), "token released when the attack ends");

            // While in Recover, attack cooldown would be running; keep it so we don't re-attack immediately.
            var cd = BrainTestKit.Percept(1.5f, 0f, cooldowns: new[] { 5f });
            RunTo(b, in cd, EnemyState.Engage);
        }

        [Test]
        public void Attack_DoesNotInterruptBeforeDuration()
        {
            var gate = new BrainTestKit.StubGate();
            var b = Make(0f, gate);
            var close = BrainTestKit.Percept(1.5f, 0f);
            b.Tick(in close, BrainTestKit.Dt);
            RunTo(b, in close, EnemyState.Attack);
            for (int i = 0; i < 20; i++) b.Tick(in close, BrainTestKit.Dt); // ~0.33 s < 0.5 s
            Assert.That(b.State, Is.EqualTo(EnemyState.Attack));
        }

        [Test]
        public void TokenDenied_NeverAttacks_Reposition()
        {
            var gate = new BrainTestKit.StubGate { Allow = false };
            var b = Make(0f, gate);
            var p = BrainTestKit.Percept(1.5f, 30f);
            for (int i = 0; i < 120; i++)
            {
                b.Tick(in p, BrainTestKit.Dt);
                Assert.That(b.State, Is.Not.EqualTo(EnemyState.Attack));
            }
            Assert.That(b.State, Is.EqualTo(EnemyState.Reposition));
            Assert.That(gate.Held, Is.EqualTo(0));
        }

        [Test]
        public void LowHp_EntersFlee()
        {
            var b = Make(0f, new BrainTestKit.StubGate(), flee: 0.2f);
            var p = BrainTestKit.Percept(5f, 0f, hp: 0.1f);
            RunTo(b, in p, EnemyState.Flee);
        }

        [Test]
        public void Dead_IsTerminal_AndReleasesToken()
        {
            var gate = new BrainTestKit.StubGate();
            var b = Make(0f, gate);
            var close = BrainTestKit.Percept(1.5f, 0f);
            RunTo(b, in close, EnemyState.Attack);
            Assert.That(gate.Held, Is.EqualTo(1));

            var dead = close; dead.IsDead = true;
            var d = b.Tick(in dead, BrainTestKit.Dt);
            Assert.That(b.State, Is.EqualTo(EnemyState.Dead));
            Assert.That(d.StateChanged, Is.True);
            Assert.That(gate.Held, Is.EqualTo(0));

            for (int i = 0; i < 100; i++)
            {
                d = b.Tick(in close, BrainTestKit.Dt);
                Assert.That(b.State, Is.EqualTo(EnemyState.Dead));
                Assert.That(d.StateChanged, Is.False);
            }
        }

        [Test]
        public void LosingTarget_ReturnsToIdle()
        {
            var b = Make(0f, new BrainTestKit.StubGate());
            var far = BrainTestKit.Percept(8f);
            RunTo(b, in far, EnemyState.Approach);
            var none = BrainTestKit.Percept(8f, target: false);
            RunTo(b, in none, EnemyState.Idle);
        }

        [Test]
        public void FarEnemy_DecidesLessOften()
        {
            int Evaluations(float dist, bool onScreen)
            {
                var b = Make(0f, null);
                var p = BrainTestKit.Percept(dist, 0f, onScreen: onScreen);
                int n = 0;
                for (int i = 0; i < 600; i++) if (b.Tick(in p, BrainTestKit.Dt).Evaluated) n++;
                return n;
            }
            Assert.That(Evaluations(5f, true), Is.GreaterThan(Evaluations(60f, false)));
        }

        [Test]
        public void SameSeed_SameDecisionSequence()
        {
            var a = Trace(2024UL);
            var b = Trace(2024UL);
            var c = Trace(2025UL);
            Assert.That(b, Is.EqualTo(a));
            Assert.That(c, Is.Not.EqualTo(a));
        }

        private static int[] Trace(ulong seed)
        {
            var b = Make(0.5f, new BrainTestKit.StubGate(), seed: seed);
            var res = new int[1800];
            var cd = new float[1];
            for (int i = 0; i < res.Length; i++)
            {
                float dist = 1f + (i / 90 % 6) * 1.5f;
                cd[0] = (i / 45) % 3 == 0 ? 0.5f : 0f;
                var p = BrainTestKit.Percept(dist, (i / 30 % 4) * 20f, cooldowns: cd);
                var d = b.Tick(in p, BrainTestKit.Dt);
                res[i] = (int)d.State * 100 + (int)d.Action * 10 + (d.Randomized ? 1 : 0);
            }
            return res;
        }
    }
}
