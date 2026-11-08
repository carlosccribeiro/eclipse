#if UNITY_EDITOR
using System.Collections;
using Eclipse.AI.Director;
using Eclipse.AI.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.AI.Behavior
{
    /// <summary>M07 criteria 1 and 2 (09 section 3.2): melee enemies approach, attack only while holding a token, and in Normal at most 3 attack at once.</summary>
    public class TokenFlowTests
    {
        private const int PhantomBase = 90001;
        private AiRun _run;

        [TearDown]
        public void TearDown() => _run?.Restore();

        [UnityTest]
        public IEnumerator Marauder_approaches_and_attacks_only_while_holding_a_token()
        {
            _run = new AiRun();
            yield return _run.Load();
            _run.PlacePlayer(Vector3.zero, Vector3.forward);
            _run.MakePlayerUntouchable();
            EnemyController m = _run.Spawn(AiRun.MarauderIndex, new Vector3(0f, 0f, 9f));
            yield return _run.Settle();
            Assert.IsTrue(m.IsReady, "marauder not ready");

            float start = _run.PlanarDistance(m);
            int id = _run.OwnerId(m);
            bool attacked = false, holdingAtStart = false, busyWithoutToken = false;
            float distAtStart = 0f;
            for (int t = 0; t < 900; t++)
            {
                _run.Tick();
                if (!m.Binder.Executor.IsBusy)
                {
                    if (attacked) break; // the whole attack ran with the token checked on every tick
                    continue;
                }
                bool holding = _run.Enemy.Tokens.IsHolding(id);
                if (!attacked)
                {
                    attacked = true;
                    holdingAtStart = holding;
                    distAtStart = _run.PlanarDistance(m);
                }
                if (!holding) busyWithoutToken = true;
            }

            Assert.IsTrue(attacked, "the marauder never attacked in 15 s");
            Assert.Less(distAtStart, start - 3f, "it must have approached before attacking");
            float maxReach = 0f;
            foreach (AttackPlan head in m.Blueprint.Heads) maxReach = Mathf.Max(maxReach, head.MaxRange);
            Assert.LessOrEqual(distAtStart, maxReach + m.HurtboxRadius + 0.6f, "attack started far outside its own range");
            Assert.IsTrue(holdingAtStart, "attack started without a token");
            Assert.IsFalse(busyWithoutToken, "the attack ran while the token was not held");
        }

        [UnityTest]
        public IEnumerator Marauder_without_a_free_token_orbits_and_does_not_attack_until_one_is_released()
        {
            _run = new AiRun();
            yield return _run.Load();
            _run.PlacePlayer(Vector3.zero, Vector3.forward);
            _run.MakePlayerUntouchable();

            // Normal allows 3 melee tokens (09 3.2, Difficulty.csv): three phantom owners take all of them.
            AttackTokenDirector tokens = _run.Enemy.Tokens;
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(tokens.Register(PhantomBase + i, TokenClass.Normal));
                Assert.IsTrue(tokens.TryAcquire(PhantomBase + i, false), "phantom " + i + " must get a token");
            }
            Assert.AreEqual(3, tokens.ActiveMelee);

            EnemyController m = _run.Spawn(AiRun.MarauderIndex, new Vector3(0f, 0f, 9f));
            yield return _run.Settle();
            float start = _run.PlanarDistance(m);
            bool everBusy = false;
            for (int t = 0; t < 600; t++)
            {
                _run.Tick();
                everBusy |= m.Binder.Executor.IsBusy;
            }
            Assert.IsFalse(everBusy, "attacked with every melee token taken");
            Assert.Less(_run.PlanarDistance(m), start - 2f, "it should still close in (and orbit) while waiting");
            Assert.AreEqual(0, _run.HitsOnPlayer.Count);

            tokens.Release(PhantomBase);
            bool attackedAfter = false;
            for (int t = 0; t < 600 && !attackedAfter; t++)
            {
                _run.Tick();
                attackedAfter = m.Binder.Executor.IsBusy && tokens.IsHolding(_run.OwnerId(m));
            }
            Assert.IsTrue(attackedAfter, "once a token is free the waiting marauder must take it and attack");
            tokens.Unregister(PhantomBase + 1);
            tokens.Unregister(PhantomBase + 2);
            tokens.Unregister(PhantomBase);
        }

        [UnityTest]
        public IEnumerator Ten_melee_in_Normal_never_have_more_than_three_attacking_in_the_same_tick()
        {
            _run = new AiRun();
            yield return _run.Load();
            _run.PlacePlayer(Vector3.zero, Vector3.forward);
            _run.MakePlayerUntouchable();

            const int count = 10;
            var group = new EnemyController[count];
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                group[i] = _run.Spawn(AiRun.MarauderIndex, new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (6f + (i % 3)));
            }
            yield return _run.Settle();
            for (int i = 0; i < count; i++) Assert.IsTrue(group[i].IsReady, "enemy " + i + " not ready");

            int maxBusy = 0, maxTokens = 0, ticksWithAttackers = 0, attackStarts = 0;
            var wasBusy = new bool[count];
            for (int t = 0; t < 1800; t++) // 30 s
            {
                _run.Tick();
                int busy = 0;
                for (int i = 0; i < count; i++)
                {
                    bool b = group[i].Binder.Executor.IsBusy;
                    if (b) busy++;
                    if (b && !wasBusy[i]) attackStarts++;
                    wasBusy[i] = b;
                }
                if (busy > 0) ticksWithAttackers++;
                maxBusy = Mathf.Max(maxBusy, busy);
                maxTokens = Mathf.Max(maxTokens, _run.Enemy.Tokens.ActiveMelee);
                Assert.LessOrEqual(busy, 3, "more than 3 attackers in tick " + t);
                Assert.LessOrEqual(_run.Enemy.Tokens.ActiveMelee, 3, "more than 3 melee tokens held in tick " + t);
            }
            Debug.Log("[M07 T10] ten melee, 30 s: max simultaneous attackers " + maxBusy + ", max melee tokens " + maxTokens
                + ", ticks with attackers " + ticksWithAttackers + ", attack starts " + attackStarts);
            Assert.GreaterOrEqual(maxBusy, 1, "nobody attacked in 30 s: the test would be vacuous");
            Assert.GreaterOrEqual(attackStarts, 5, "too few attacks for the sample to mean anything");
        }
    }
}
#endif
