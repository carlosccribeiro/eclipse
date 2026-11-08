using Eclipse.AI.StateMachine;
using Eclipse.AI.Utility;
using Eclipse.Core.Random;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.AI.Brain
{
    public class UtilitySelectorTests
    {
        [Test]
        public void Randomness_IsFifteenPercent_Within2Points()
        {
            var sel = new UtilitySelector(BrainTestKit.MakeConfig(UtilityConfig.DesignRandomChance), new RngStreams(1234UL));
            var p = BrainTestKit.Percept(2f, 30f); // Attack + Reposition candidates
            int randomized = 0, nonBest = 0;
            const int n = 10000;
            for (int i = 0; i < n; i++)
            {
                sel.Select(in p, true, out var a, out _, out _, out bool r);
                if (r) randomized++;
                if (a != UtilityAction.Attack) nonBest++;
            }
            double pct = 100.0 * randomized / n;
            Assert.That(pct, Is.InRange(13.0, 17.0), "randomized pct " + pct);
            Assert.That(nonBest, Is.GreaterThan(0), "weighted pick must sometimes choose a lesser action");
            Assert.That(nonBest, Is.LessThanOrEqualTo(randomized));
        }

        [Test]
        public void SameSeed_SameSequence_DifferentSeed_Differs()
        {
            var p = BrainTestKit.Percept(2f, 30f);
            var a = Sequence(77UL, p);
            var b = Sequence(77UL, p);
            var c = Sequence(78UL, p);
            Assert.That(b, Is.EqualTo(a));
            Assert.That(c, Is.Not.EqualTo(a));
        }

        private static int[] Sequence(ulong seed, AiPerception p)
        {
            var sel = new UtilitySelector(BrainTestKit.MakeConfig(0.5f), new RngStreams(seed));
            var res = new int[200];
            for (int i = 0; i < res.Length; i++)
            {
                sel.Select(in p, true, out var act, out _, out _, out bool r);
                res[i] = (int)act * 2 + (r ? 1 : 0);
            }
            return res;
        }

        [Test]
        public void UsesOnlyCombatStream()
        {
            var rng = new RngStreams(5UL);
            var map0 = rng.Get(RngStream.Map).GetState();
            var loot0 = rng.Get(RngStream.Loot).GetState();
            var sel = new UtilitySelector(BrainTestKit.MakeConfig(0.15f), rng);
            var p = BrainTestKit.Percept(2f, 30f);
            for (int i = 0; i < 50; i++) sel.Select(in p, true, out _, out _, out _, out _);
            Assert.That(rng.Get(RngStream.Map).GetState(), Is.EqualTo(map0));
            Assert.That(rng.Get(RngStream.Loot).GetState(), Is.EqualTo(loot0));
        }

        [Test]
        public void ZeroRandomness_AlwaysPicksBest()
        {
            var sel = new UtilitySelector(BrainTestKit.MakeConfig(0f), new RngStreams(9UL));
            var p = BrainTestKit.Percept(2f, 30f);
            for (int i = 0; i < 500; i++)
            {
                sel.Select(in p, true, out var a, out int idx, out _, out bool r);
                Assert.That(a, Is.EqualTo(UtilityAction.Attack));
                Assert.That(idx, Is.EqualTo(0));
                Assert.That(r, Is.False);
            }
        }

        [Test]
        public void Cooldown_ExcludesAttack_FarApproaches()
        {
            var sel = new UtilitySelector(BrainTestKit.MakeConfig(0f), new RngStreams(9UL));
            var cd = new[] { 1f };
            var near = BrainTestKit.Percept(1.5f, 0f, cooldowns: cd);
            sel.Select(in near, true, out var a, out _, out _, out _);
            Assert.That(a, Is.EqualTo(UtilityAction.Reposition));

            var far = BrainTestKit.Percept(9f);
            sel.Select(in far, true, out a, out _, out _, out _);
            Assert.That(a, Is.EqualTo(UtilityAction.Approach));
        }

        [Test]
        public void LowHp_Flees_WithoutConsumingRng()
        {
            var rng = new RngStreams(3UL);
            var before = rng.Get(RngStream.Combat).GetState();
            var sel = new UtilitySelector(BrainTestKit.MakeConfig(0.15f, 0.2f), rng);
            var p = BrainTestKit.Percept(2f, 0f, hp: 0.1f);
            sel.Select(in p, true, out var a, out _, out _, out _);
            Assert.That(a, Is.EqualTo(UtilityAction.Flee));
            Assert.That(rng.Get(RngStream.Combat).GetState(), Is.EqualTo(before));
        }

        [Test]
        public void Allies_RaiseAttackScore()
        {
            var cfg = BrainTestKit.MakeConfig(0f);
            cfg.AllyAttackBonus = 0.5f;
            var sel = new UtilitySelector(cfg, new RngStreams(1UL));
            var alone = BrainTestKit.Percept(2f, 30f);
            var pack = alone; pack.AlliesNearby = 3;
            sel.Select(in alone, true, out _, out _, out float s0, out _);
            sel.Select(in pack, true, out _, out _, out float s1, out _);
            Assert.That(s1, Is.GreaterThan(s0));
        }
    }
}
