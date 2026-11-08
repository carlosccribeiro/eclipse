#if UNITY_EDITOR
using System.Collections;
using Eclipse.Core.Damage;
using Eclipse.Core.Tags;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Input;
using Eclipse.Tests.PlayMode.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HitResult = Eclipse.Gameplay.Combat.HitResult;

namespace Eclipse.Tests.PlayMode.Skills
{
    /// <summary>
    /// M08 T8, Ashen Guard (08 4.4, 00 R20): -60% damage in the stance, Parry only in the first 12 frames, +50% counter after a Parry,
    /// Unblockable goes through the frontal block. Incoming hits are delivered to the receiver bound to Kael's hurtbox (the guard
    /// wrapper over the combat receiver), at an exact simulation frame relative to the start of the stance.
    /// </summary>
    public class AshenGuardTests : KaelPlayTestBase
    {
        private const float Incoming = 100f;
        private static readonly Vector3 FromFront = Vector3.back;   // travelling toward -Z: the attacker stands in front of Kael (facing +Z)
        private static readonly Vector3 FromBehind = Vector3.forward;

        private struct StanceResult
        {
            public int Start;
            public bool Delivered;
            public DamageOutcome Outcome;
            public int HitFrame;
            public float MultiplierAfterHit;
            public float CounterValue;
            public int CounterFrame;
            public float FuryAfterCounter;
            public int ParryEvents, BlockEvents;
        }

        private CombatActor _target;

        private IEnumerator BootStance()
        {
            yield return Boot(new Vector3(0f, 0f, 2f)); // inside the counter: 3 m radius, 120 degrees
            _target = Run.Dummies[0];
            Run.Kael.Equip(0, force: true);
            Run.Tick(2);
        }

        /// <summary>Raw damage a rear hit does to Kael outside the stance (the receiver forwards untouched).</summary>
        private float Baseline()
        {
            DamageOutcome o = Run.Kael.Guard.ReceiveHit(CombatEnv.Hit(Incoming), FromBehind);
            Assert.AreEqual(HitResult.Hit, o.Result, "baseline hit lands");
            Run.Tick(420); // grace period of the baseline hit is over
            Run.Player.Entity.Health.Restore(1f);
            return o.FinalDamage;
        }

        /// <summary>One whole stance: starts it, optionally delivers a hit at <paramref name="hitOffset"/> frames after its start, releases R and waits for the counter.</summary>
        private StanceResult Stance(int hitOffset, Vector3 direction, HitFlags flags = HitFlags.None)
        {
            Run.Kael.Skills.Controller.Cooldowns.Reset(SkillSlot.R);
            Run.ClearEvents();
            Run.SetFury(0f);
            var r = new StanceResult();
            var ctx = Run.Ctx;

            Run.Press(InputActionId.Skill3);
            Assert.IsTrue(Run.TickUntil(() => ctx.StanceActive, 40), "stance started: " + Run.Kael.Skills.LastResult);
            r.Start = ctx.StanceStartFrame;

            if (hitOffset >= 0)
            {
                Assert.IsTrue(Run.TickUntil(() => ctx.Frame >= r.Start + hitOffset, 100));
                Assert.AreEqual(r.Start + hitOffset, ctx.Frame, "delivered on the exact frame");
                r.HitFrame = ctx.Frame;
                r.Outcome = Run.Kael.Guard.ReceiveHit(CombatEnv.Hit(Incoming, flags), direction);
                r.Delivered = true;
                r.MultiplierAfterHit = ctx.DamageMultiplier;
            }

            Run.Release(InputActionId.Skill3);
            Assert.IsTrue(Run.TickUntil(() => Run.CountHitsOn(_target) > 0, 200), "counter-attack landed");
            var counter = Run.HitsOn(_target)[0];
            r.CounterValue = counter.Value;
            r.CounterFrame = counter.Frame;
            Run.Tick(1);
            r.FuryAfterCounter = Run.Fury;
            Run.FinishCast();
            r.ParryEvents = Run.Parries.Count;
            r.BlockEvents = Run.Blocks.Count;
            Run.Tick(420); // grace period over, Fury decay irrelevant
            Run.Player.Entity.Health.Restore(1f);
            Run.ClearEvents();
            return r;
        }

        [UnityTest]
        public IEnumerator Reduces_non_blocked_hits_by_60_percent_and_counters_250_percent_plus_30_percent_absorbed()
        {
            yield return BootStance();
            float baseline = Baseline();

            StanceResult s = Stance(30, FromBehind);
            Assert.IsTrue(s.Delivered);
            Assert.AreEqual(HitResult.Hit, s.Outcome.Result, "a hit from behind is not blocked");
            Assert.AreEqual(baseline * 0.4f, s.Outcome.FinalDamage, baseline * 0.01f, "-60% on the stance");
            Assert.AreEqual(60f, Run.Kael.Guard.AbsorbedThisStance, 1e-3f, "60% of the 100 incoming was absorbed");
            KaelRun.AssertDamage(Run.Expected(2.5f, s.CounterFrame, 1f, 0.3f * 60f), s.CounterValue, "counter: 250% + 30% of what was absorbed");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Parry_exists_only_in_the_first_12_frames_and_boosts_the_counter_by_50_percent()
        {
            yield return BootStance();

            StanceResult plain = Stance(-1, FromFront);
            KaelRun.AssertDamage(Run.Expected(2.5f, plain.CounterFrame), plain.CounterValue, "counter without a Parry: 250%");

            // Frame 11 after the start is the last one of the 12 frame window (frames 0..11).
            StanceResult parried = Stance(11, FromFront);
            Assert.AreEqual(HitResult.Parried, parried.Outcome.Result, "frame 11: Parry");
            Assert.AreEqual(1.5f, parried.MultiplierAfterHit, 1e-5f, "+50% to the counter-attack");
            Assert.AreEqual(1, Run.Kael.Guard.ParriedHits);
            KaelRun.AssertDamage(Run.Expected(2.5f, parried.CounterFrame, 1.5f), parried.CounterValue, "counter after a Parry: 250% x 1.5");
            Assert.AreEqual(1, parried.ParryEvents, "OnParry published once");
            Assert.AreEqual(20f, parried.FuryAfterCounter, 0.5f, "+20 Fury on the counter-attack");

            // Frame 12 is already a plain block.
            StanceResult late = Stance(12, FromFront);
            Assert.AreEqual(HitResult.Blocked, late.Outcome.Result, "frame 12: no longer a Parry");
            Assert.AreEqual(1f, late.MultiplierAfterHit, 1e-5f, "no Parry bonus");
            KaelRun.AssertDamage(Run.Expected(2.5f, late.CounterFrame, 1f, 0.3f * Incoming), late.CounterValue, "blocked damage feeds the counter");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Unblockable_goes_through_the_frontal_block_but_not_through_the_reduction()
        {
            yield return BootStance();
            float baseline = Baseline();

            StanceResult blocked = Stance(30, FromFront);
            Assert.AreEqual(HitResult.Blocked, blocked.Outcome.Result, "a frontal hit is blocked");
            Assert.AreEqual(0f, blocked.Outcome.FinalDamage, "nothing reaches the pipeline");
            Assert.AreEqual(1, blocked.BlockEvents, "OnBlock published once");

            StanceResult through = Stance(30, FromFront, HitFlags.Unblockable);
            Assert.AreEqual(HitResult.Hit, through.Outcome.Result, "Unblockable is not blocked");
            Assert.AreEqual(baseline * 0.4f, through.Outcome.FinalDamage, baseline * 0.01f, "but it still takes the -60%");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
