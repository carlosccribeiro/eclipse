using Eclipse.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.EditMode.Gameplay.Player
{
    public class DodgeTests
    {
        // Field defaults of DodgeConfig mirror 06 3.7.
        private static DodgeConfig Cfg() => ScriptableObject.CreateInstance<DodgeConfig>();

        private static InvulnerabilityState DodgeAt(DodgeConfig c, int start)
        {
            var inv = new InvulnerabilityState();
            inv.AddWindow(DodgeState.InvulnerableStart(c, start), DodgeState.InvulnerableEnd(c, start), c.perfectDodgeWindowFrames, 1);
            return inv;
        }

        [Test]
        public void IFrames_AreExactlyDodgeFrames2To13()
        {
            var c = Cfg();
            var inv = DodgeAt(c, 100); // dodge frame 1 = global 100
            for (int f = 95; f <= 120; f++)
            {
                int dodgeFrame = f - 100 + 1;
                Assert.AreEqual(dodgeFrame >= 2 && dodgeFrame <= 13, inv.IsInvulnerable(f), "global " + f);
            }
        }

        [Test]
        public void NotifyIncomingHit_ClassifiesNegatedPerfectAndNot()
        {
            var c = Cfg();
            var inv = DodgeAt(c, 100); // i-frames 101..112, perfect 101..106
            Assert.AreEqual(HitResult.NotNegated, inv.NotifyIncomingHit(100));
            Assert.AreEqual(HitResult.PerfectDodge, inv.NotifyIncomingHit(101));
            Assert.AreEqual(HitResult.PerfectDodge, inv.NotifyIncomingHit(106));
            Assert.AreEqual(HitResult.Negated, inv.NotifyIncomingHit(107));
            Assert.AreEqual(HitResult.Negated, inv.NotifyIncomingHit(112));
            Assert.AreEqual(HitResult.NotNegated, inv.NotifyIncomingHit(113));
        }

        [Test]
        public void MultipleSources_AreIndependent()
        {
            var inv = new InvulnerabilityState();
            inv.AddWindow(10, 20, 0, 1);
            inv.AddWindow(15, 60, 0, 2);
            Assert.IsTrue(inv.IsInvulnerable(11));
            Assert.IsTrue(inv.IsInvulnerable(55));
            Assert.IsFalse(inv.IsInvulnerable(70));
            inv.RemoveSource(1);
            Assert.IsFalse(inv.IsInvulnerable(11));
            Assert.IsTrue(inv.IsInvulnerable(55));
        }

        [Test]
        public void ThirdDodgeWithoutCharge_IsRefused()
        {
            var s = new DodgeState(Cfg());
            Assert.IsTrue(s.TryStart(1));
            for (int i = 0; i < 18; i++) s.Advance();
            Assert.IsTrue(s.TryStart(20));
            for (int i = 0; i < 18; i++) s.Advance();
            Assert.AreEqual(0, s.Charges);
            Assert.IsFalse(s.TryStart(40));
        }

        [Test]
        public void Recharge_IsSequential_90TicksPerCharge()
        {
            var c = Cfg();
            Assert.AreEqual(90, c.RechargeTicks);
            var s = new DodgeState(c);
            s.TryStart(1);
            s.TryStart(1); // refused while dodging
            for (int i = 0; i < 18; i++) s.Advance();
            Assert.IsTrue(s.TryStart(19));
            int baseline = s.RechargeProgressTicks;
            int ticks = 0;
            while (s.Charges < 1) { s.Advance(); ticks++; }
            Assert.AreEqual(c.RechargeTicks - baseline, ticks);
            int second = 0;
            while (s.Charges < 2) { s.Advance(); second++; }
            Assert.AreEqual(90, second, "second charge needs a full 90 ticks after the first");
        }

        [Test]
        public void Cancel_OnlyFromFrame15()
        {
            var s = new DodgeState(Cfg());
            Assert.IsTrue(s.CanCancel);
            s.TryStart(1);
            for (int f = 1; f <= 18; f++)
            {
                Assert.AreEqual(f >= 15, s.CanCancel, "frame " + f);
                s.Advance();
            }
            Assert.IsFalse(s.IsDodging);
            Assert.IsTrue(s.CanCancel);
        }

        [Test]
        public void ExtraCharges_CappedAtThree()
        {
            var s = new DodgeState(Cfg());
            s.SetExtraCharges(5);
            Assert.AreEqual(3, s.MaxCharges);
        }

        [Test]
        public void StepDistances_SumToConfiguredDistance_AndDecrease()
        {
            var c = Cfg();
            float sum = 0f, prev = float.MaxValue;
            for (int f = 1; f <= c.totalFrames; f++)
            {
                float d = c.StepDistance(f);
                Assert.LessOrEqual(d, prev + 1e-6f);
                prev = d;
                sum += d;
            }
            Assert.AreEqual(4.5f, sum, 1e-4f);
        }
    }
}
