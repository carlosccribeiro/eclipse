using Eclipse.AI.Director;
using Eclipse.Data.Difficulties;
using NUnit.Framework;
using UnityEditor;

namespace Eclipse.Tests.EditMode.AI.Tokens
{
    public class AttackTokenDirectorTests
    {
        private static TokenPolicy Policy(int melee = 3, int ranged = 2, int ambush = 1, float interval = 1.2f)
        {
            return new TokenPolicy { MeleeTokens = melee, RangedTokens = ranged, AmbushTokens = ambush,
                MinIntervalSeconds = interval, Orbit = TokenOrbitConfig.Default };
        }

        private static int Attackers(AttackTokenDirector d, int count, bool ranged)
        {
            int n = 0;
            for (int i = 0; i < count; i++) if (d.TryAcquire(i, ranged)) n++;
            return n;
        }

        [Test]
        public void TenMeleeInNormal_NeverMoreThanThreeAttack()
        {
            var d = new AttackTokenDirector(Policy());
            for (int i = 0; i < 10; i++) d.Register(i, TokenClass.Normal);
            Assert.AreEqual(3, Attackers(d, 10, false));
            Assert.AreEqual(3, d.ActiveMelee);
        }

        [Test]
        public void RangedPoolIsSeparate()
        {
            var d = new AttackTokenDirector(Policy());
            for (int i = 0; i < 10; i++) d.Register(i, TokenClass.Normal);
            Assert.AreEqual(3, Attackers(d, 5, false));
            Assert.IsTrue(d.TryAcquire(5, true));
            Assert.IsTrue(d.TryAcquire(6, true));
            Assert.IsFalse(d.TryAcquire(7, true));
        }

        [Test]
        public void ReleaseFreesToken_ThenIntervalApplies()
        {
            var d = new AttackTokenDirector(Policy());
            for (int i = 0; i < 4; i++) d.Register(i, TokenClass.Normal);
            Attackers(d, 3, false);
            Assert.IsFalse(d.TryAcquire(3, false));
            d.Release(0);
            Assert.IsTrue(d.TryAcquire(3, false));
            d.Release(3);
            d.Now = 1.0;
            Assert.IsFalse(d.TryAcquire(3, false), "interval 1.2s not elapsed");
            d.Now = 1.3;
            Assert.IsTrue(d.TryAcquire(3, false));
            Assert.IsTrue(d.TryAcquire(3, false), "already holding stays true");
            Assert.AreEqual(3, d.ActiveMelee);
        }

        [Test]
        public void Elite_AttacksWithoutToken_ButObeysInterval()
        {
            var d = new AttackTokenDirector(Policy());
            for (int i = 0; i < 3; i++) { d.Register(i, TokenClass.Normal); d.TryAcquire(i, false); }
            d.Register(10, TokenClass.Elite);
            d.Register(11, TokenClass.Guardian);
            Assert.IsTrue(d.TryAcquire(10, false));
            Assert.IsTrue(d.TryAcquire(11, false));
            Assert.AreEqual(3, d.ActiveMelee);
            d.Release(10);
            d.Now = 0.5;
            Assert.IsFalse(d.TryAcquire(10, false));
            d.Now = 1.3;
            Assert.IsTrue(d.TryAcquire(10, false));
        }

        [Test]
        public void Boss_IgnoresTokensAndInterval()
        {
            var d = new AttackTokenDirector(Policy());
            d.Register(1, TokenClass.Boss);
            Assert.IsTrue(d.TryAcquire(1, false));
            d.Release(1);
            Assert.IsTrue(d.TryAcquire(1, false));
        }

        [Test]
        public void Assassin_UsesAmbushPool()
        {
            var d = new AttackTokenDirector(Policy(ambush: 2));
            for (int i = 0; i < 3; i++) d.Register(i, TokenClass.Assassin);
            d.Register(9, TokenClass.Normal);
            Assert.IsTrue(d.TryAcquire(0, false));
            Assert.IsTrue(d.TryAcquire(1, false));
            Assert.IsFalse(d.TryAcquire(2, false));
            Assert.AreEqual(2, d.ActiveAmbush);
            Assert.AreEqual(0, d.ActiveMelee);
            Assert.IsTrue(d.TryAcquire(9, false));
        }

        [Test]
        public void Taunted_IgnoresTokens()
        {
            var d = new AttackTokenDirector(Policy(melee: 1));
            d.Register(0, TokenClass.Normal); d.Register(1, TokenClass.Normal);
            Assert.IsTrue(d.TryAcquire(0, false));
            Assert.IsFalse(d.TryAcquire(1, false));
            d.SetTaunted(1, true);
            Assert.IsTrue(d.TryAcquire(1, false));
            Assert.AreEqual(1, d.ActiveMelee);
        }

        [Test]
        public void UnknownOwner_DoesNotBreak()
        {
            var d = new AttackTokenDirector(Policy());
            Assert.DoesNotThrow(() => d.Release(999));
            Assert.DoesNotThrow(() => d.Unregister(999));
            Assert.DoesNotThrow(() => d.SetTaunted(999, true));
            Assert.IsFalse(d.TryAcquire(999, false));
        }

        [Test]
        public void Unregister_ReturnsToken()
        {
            var d = new AttackTokenDirector(Policy(melee: 1));
            d.Register(0, TokenClass.Normal); d.Register(1, TokenClass.Normal);
            d.TryAcquire(0, false);
            d.Unregister(0);
            Assert.IsTrue(d.TryAcquire(1, false));
        }

        [Test]
        public void Register_FailsWhenFull()
        {
            var d = new AttackTokenDirector(Policy(), 2);
            Assert.IsTrue(d.Register(0, TokenClass.Normal));
            Assert.IsTrue(d.Register(1, TokenClass.Normal));
            Assert.IsFalse(d.Register(2, TokenClass.Normal));
        }

        [Test]
        public void OrbitBand_Is4To7()
        {
            var o = Policy().Orbit;
            Assert.AreEqual(4f, o.MinMeters);
            Assert.AreEqual(7f, o.MaxMeters);
        }

        [TestCase("normal", 3, 2, 1.2f, 1)]
        [TestCase("veteran", 3, 3, 1.0f, 1)]
        [TestCase("nightmare", 4, 3, 0.9f, 2)]
        [TestCase("eclipse", 5, 4, 0.8f, 2)]
        public void FromDifficultyAsset_MatchesDoc(string id, int melee, int ranged, float interval, int ambush)
        {
            var asset = AssetDatabase.LoadAssetAtPath<DifficultyDefinition>(
                "Assets/_Project/Content/Shared/Difficulty/" + id + ".asset");
            Assert.IsNotNull(asset);
            TokenPolicy p = TokenPolicy.From(asset);
            Assert.AreEqual(melee, p.MeleeTokens);
            Assert.AreEqual(ranged, p.RangedTokens);
            Assert.AreEqual(interval, p.MinIntervalSeconds, 1e-4f);
            Assert.AreEqual(ambush, p.AmbushTokens);
        }
    }
}
