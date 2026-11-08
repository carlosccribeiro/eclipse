#if UNITY_EDITOR
using Eclipse.Core.Damage;
using EntityId = Eclipse.Gameplay.Combat.EntityId;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.PlayMode.Combat.Damage
{
    /// <summary>06 3.3 / 3.8.3 / 3.14: i-frames, Parry and the grace period on a player-team entity (1000 HP, no mitigation).</summary>
    public class IncomingDamageTests
    {
        private CombatEnv _env;

        [SetUp]
        public void SetUp() => _env = new CombatEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();

        private CombatEntity Player() => _env.MakeEntity(Team.Player, 1000f);

        private static DamageOutcome Hit(CombatEntity e, int frame, float dmg, HitFlags flags = HitFlags.None, HitKind kind = HitKind.Hit)
        {
            e.CurrentFrame = frame;
            return e.ReceiveHit(CombatEnv.Hit(dmg, flags, kind), Vector3.back);
        }

        [Test]
        public void Dodge_iframes_negate_normal_and_unavoidable_hits_but_not_annihilation()
        {
            var normal = Player();
            normal.Invulnerability.AddWindow(100, 110);
            Assert.AreNotEqual(HitResult.Hit, Hit(normal, 105, 10f).Result, "normal hit");
            Assert.AreEqual(1000f, normal.Health.Current, 0.001f);

            var unavoidable = Player();
            unavoidable.Invulnerability.AddWindow(100, 110);
            Assert.AreNotEqual(HitResult.Hit, Hit(unavoidable, 105, 10f, HitFlags.Unavoidable).Result, "Unavoidable is negated by i-frames");
            Assert.AreEqual(1000f, unavoidable.Health.Current, 0.001f);

            var annihilation = Player();
            annihilation.Invulnerability.AddWindow(100, 110);
            Assert.AreEqual(HitResult.Hit, Hit(annihilation, 105, 10f, HitFlags.Annihilation).Result, "Annihilation ignores i-frames");
            Assert.AreEqual(990f, annihilation.Health.Current, 0.01f);

            var after = Player();
            after.Invulnerability.AddWindow(100, 110);
            Assert.AreEqual(HitResult.Hit, Hit(after, 111, 10f).Result, "window over");
        }

        [Test]
        public void Parry_negates_a_common_hit_but_not_unblockable_or_annihilation()
        {
            const int open = 50;
            CombatEntity Parrying(out int last)
            {
                var e = Player();
                var states = new StateComponent(_env.Rules, e.Id, true, null, _env.Events);
                last = states.OpenParry(open);
                e.ParryActiveUntilFrame = states.ParryEndFrame;
                return e;
            }

            var a = Parrying(out int end);
            Assert.AreEqual(open + _env.Rules.V.ParryWindowFrames - 1, end, "window length comes from the data");
            Assert.AreEqual(HitResult.Parried, Hit(a, open + 1, 10f).Result, "common hit parried");
            Assert.AreEqual(1000f, a.Health.Current, 0.001f);

            var b = Parrying(out _);
            Assert.AreEqual(HitResult.Hit, Hit(b, open + 1, 10f, HitFlags.Unblockable).Result, "Unblockable passes");

            var c = Parrying(out _);
            Assert.AreEqual(HitResult.Hit, Hit(c, open + 1, 10f, HitFlags.Annihilation).Result, "Annihilation passes");

            var d = Parrying(out int last);
            Assert.AreEqual(HitResult.Hit, Hit(d, last + 1, 10f).Result, "after the window");
        }

        [Test]
        public void StateComponent_TryParry_follows_the_same_exclusions()
        {
            var e = Player();
            var states = new StateComponent(_env.Rules, e.Id, true, null, _env.Events);
            states.OpenParry(10);
            Assert.IsTrue(states.TryParry(HitFlags.None, 10, out float stagger));
            Assert.AreEqual(_env.Rules.V.ParryAttackerStagger, stagger);
            Assert.IsFalse(states.TryParry(HitFlags.Unblockable, 10, out _));
            Assert.IsFalse(states.TryParry(HitFlags.Annihilation, 10, out _));
        }

        [Test]
        public void Grace_period_after_a_hit_lasts_0_25_s_and_0_8_s_after_a_heavy_hit()
        {
            Assert.AreEqual(0.25f, _env.Config.graceSeconds, 1e-4f);
            Assert.AreEqual(0.8f, _env.Config.heavyGraceSeconds, 1e-4f);

            var light = Player();
            Assert.AreEqual(HitResult.Hit, Hit(light, 100, 10f).Result);
            Assert.AreNotEqual(HitResult.Hit, Hit(light, 100 + 14, 10f).Result, "frame 14 of 15: still in grace");
            Assert.AreEqual(HitResult.Hit, Hit(light, 100 + 15, 10f).Result, "0.25 s = 15 frames");

            var heavy = Player();
            Assert.AreEqual(HitResult.Hit, Hit(heavy, 100, 300f).Result, "30% of max health is a heavy hit (>= 20%)");
            Assert.AreNotEqual(HitResult.Hit, Hit(heavy, 100 + 47, 10f).Result, "frame 47 of 48: still in grace");
            Assert.AreEqual(HitResult.Hit, Hit(heavy, 100 + 48, 10f).Result, "0.8 s = 48 frames");
        }

        [Test]
        public void Annihilation_bypasses_grace_and_ticks_do_not_start_it()
        {
            var e = Player();
            Assert.AreEqual(HitResult.Hit, Hit(e, 100, 10f).Result);
            Assert.AreEqual(HitResult.Hit, Hit(e, 101, 10f, HitFlags.Annihilation).Result, "assumption logged in M05: Annihilation bypasses grace");

            var t = Player();
            Assert.AreEqual(HitResult.Hit, Hit(t, 100, 5f, HitFlags.None, HitKind.Tick).Result);
            Assert.AreEqual(HitResult.Hit, Hit(t, 101, 10f).Result, "a Tick does not open grace");
        }
    }
}
#endif
