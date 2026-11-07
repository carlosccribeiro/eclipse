#if UNITY_EDITOR
using System.Collections.Generic;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Events;
using Eclipse.Gameplay.Physics;
using Eclipse.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.PlayMode.Player.Dodge
{
    public class DodgeTests
    {
        private PlayerTestRig _rig;

        [SetUp] public void SetUp() => _rig = new PlayerTestRig(true);
        [TearDown] public void TearDown() => _rig.Dispose();

        [Test]
        public void Distance_is_4_5_meters_after_18_ticks()
        {
            _rig.PressDodge();
            _rig.Scheduler.StepManual(18);
            Assert.AreEqual(4.5f, _rig.Motor.SimPosition.z, 0.05f);
        }

        [Test]
        public void Invulnerability_is_exactly_dodge_frames_2_to_13()
        {
            _rig.PressDodge();
            var flags = new List<bool>();
            for (int i = 0; i < 20; i++)
            {
                _rig.Scheduler.StepManual(1);
                flags.Add(_rig.Dodge.Invulnerability.IsInvulnerable(_rig.Scheduler.CurrentFrame));
            }
            for (int dodgeFrame = 1; dodgeFrame <= 20; dodgeFrame++)
                Assert.AreEqual(dodgeFrame >= 2 && dodgeFrame <= 13, flags[dodgeFrame - 1], "dodge frame " + dodgeFrame);
        }

        [Test]
        public void Cancel_allowed_only_from_frame_15()
        {
            _rig.PressDodge();
            for (int f = 1; f <= 18; f++)
            {
                _rig.Scheduler.StepManual(1);
                Assert.AreEqual(f, _rig.Dodge.ActiveFrame);
                Assert.AreEqual(f >= 15, _rig.Dodge.CanCancel, "dodge frame " + f);
            }
            _rig.Scheduler.StepManual(1);
            Assert.IsFalse(_rig.Dodge.IsDodging);
            Assert.IsTrue(_rig.Dodge.CanCancel);
        }

        [Test]
        public void Two_charges_third_refused_and_sequential_recharge_90_ticks()
        {
            Assert.AreEqual(2, _rig.Dodge.Charges);
            var started = new List<int>();
            _rig.Dodge.DodgeBus.Subscribe((in OnDodge e) => started.Add(e.Frame));

            _rig.PressDodge();
            _rig.Scheduler.StepManual(19); // dodge 1 runs 18 ticks, ends on tick 19
            Assert.AreEqual(1, _rig.Dodge.Charges);
            _rig.PressDodge();
            _rig.Scheduler.StepManual(19);
            Assert.AreEqual(0, _rig.Dodge.Charges);
            Assert.AreEqual(2, started.Count);

            _rig.PressDodge(); // third: no charge
            _rig.Scheduler.StepManual(1);
            Assert.AreEqual(2, started.Count, "third dodge must be refused");
            Assert.IsFalse(_rig.Dodge.IsDodging);

            var chargeGainFrames = new List<int>();
            int prev = _rig.Dodge.Charges;
            for (int i = 0; i < 260 && chargeGainFrames.Count < 2; i++)
            {
                _rig.Scheduler.StepManual(1);
                if (_rig.Dodge.Charges > prev) chargeGainFrames.Add(_rig.Scheduler.CurrentFrame);
                prev = _rig.Dodge.Charges;
            }
            Assert.AreEqual(2, chargeGainFrames.Count);
            Assert.AreEqual(90, chargeGainFrames[1] - chargeGainFrames[0], "sequential recharge: 1.5 s per charge");
        }

        [Test]
        public void First_charge_returns_90_ticks_after_spend()
        {
            _rig.PressDodge();
            _rig.Scheduler.StepManual(1);
            int spendFrame = _rig.Scheduler.CurrentFrame;
            int gained = -1;
            for (int i = 0; i < 200 && gained < 0; i++)
            {
                _rig.Scheduler.StepManual(1);
                if (_rig.Dodge.Charges == 2) gained = _rig.Scheduler.CurrentFrame;
            }
            Assert.That(gained - spendFrame, Is.InRange(89, 91));
        }

        [Test]
        public void Movement_speed_does_not_change_dodge()
        {
            _rig.Motor.SpeedMultiplier = 1.5f;
            _rig.PressDodge();
            _rig.Scheduler.StepManual(18);
            Assert.AreEqual(4.5f, _rig.Motor.SimPosition.z, 0.05f);
        }

        [Test]
        public void Stops_at_heavy_body()
        {
            _rig.Box(new Vector3(0, 1, 2f), new Vector3(1, 2, 1), PhysicsLayers.Character, MassClass.Heavy); // near face z = 1.5
            _rig.PressDodge();
            _rig.Scheduler.StepManual(18);
            Assert.Less(_rig.Motor.SimPosition.z, 1.5f - 0.4f + 0.01f);
            Assert.Greater(_rig.Motor.SimPosition.z, 0.8f);
        }

        [Test]
        public void Passes_through_medium_body()
        {
            var box = _rig.Box(new Vector3(0, 1, 2f), new Vector3(1, 2, 1), PhysicsLayers.Character, MassClass.Medium);
            _rig.PressDodge();
            _rig.Scheduler.StepManual(18);
            Assert.AreEqual(4.5f, _rig.Motor.SimPosition.z, 0.05f);
            _rig.Scheduler.StepManual(1);
            Assert.IsTrue(box.GetComponent<Collider>().enabled, "collider restored after dodge");
        }

        [Test]
        public void Perfect_dodge_classified_by_impact_frame_and_events_emitted()
        {
            var dodges = new List<OnDodge>();
            var perfects = new List<OnPerfectDodge>();
            _rig.Dodge.DodgeBus.Subscribe((in OnDodge e) => dodges.Add(e));
            _rig.Dodge.PerfectDodgeBus.Subscribe((in OnPerfectDodge e) => perfects.Add(e));

            _rig.PressDodge();
            _rig.Scheduler.StepManual(1);
            Assert.AreEqual(1, dodges.Count);
            int s = dodges[0].Frame; // global frame of dodge frame 1
            Assert.AreEqual(s, _rig.Scheduler.CurrentFrame);

            var inv = _rig.Dodge.Invulnerability;
            Assert.AreEqual(HitResult.NotNegated, inv.NotifyIncomingHit(s));       // dodge frame 1
            Assert.AreEqual(HitResult.PerfectDodge, inv.NotifyIncomingHit(s + 1)); // first i-frame
            Assert.AreEqual(HitResult.PerfectDodge, inv.NotifyIncomingHit(s + 6)); // 6th i-frame
            Assert.AreEqual(HitResult.Negated, inv.NotifyIncomingHit(s + 7));
            Assert.AreEqual(HitResult.Negated, inv.NotifyIncomingHit(s + 12));     // dodge frame 13
            Assert.AreEqual(HitResult.NotNegated, inv.NotifyIncomingHit(s + 13));  // dodge frame 14

            Assert.AreEqual(0, perfects.Count, "NotifyIncomingHit alone publishes nothing");
            Assert.AreEqual(HitResult.PerfectDodge, _rig.Dodge.ReportIncomingHit(s + 3));
            Assert.AreEqual(1, perfects.Count);
            Assert.AreEqual(s + 3, perfects[0].ImpactFrame);
            Assert.AreEqual(HitResult.Negated, _rig.Dodge.ReportIncomingHit(s + 9));
            Assert.AreEqual(1, perfects.Count);
        }
    }
}
#endif
