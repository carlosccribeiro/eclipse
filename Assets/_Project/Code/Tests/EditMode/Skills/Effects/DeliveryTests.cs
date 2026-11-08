using Eclipse.Core.Damage;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.EditMode.Skills.Effects
{
    /// <summary>
    /// The seven Deliveries against fake targets (real shape math, 1 cm target radius). Shapes are the 08 4.4 values passed as skill params;
    /// every Delivery is paired with a Damage effect so a "hit" is a recorded 220% strike.
    /// </summary>
    public sealed class DeliveryTests
    {
        private EffectRig _rig;
        private static readonly SkillEffect[] Strike = { new DamageEffect(DamageType.Physical) };

        [SetUp]
        public void SetUp()
        {
            _rig = new EffectRig();
        }

        private void Cast(SkillSpec spec, SkillEffect[] effects = null, int frame = 100) => _rig.Begin(spec, frame, effects ?? Strike);

        private static int HitsOf(SkillTarget t) => EffectRig.Rx(t).Hits.Count;

        // ---- MeleeArc ----

        [Test]
        public void MeleeArc_HitsInsideTheArcAndRadiusOnly()
        {
            var front = _rig.AddEnemy(0f, 3.4f);
            var tooFar = _rig.AddEnemy(0f, 3.6f);
            var behind = _rig.AddEnemy(0f, -1f);
            Cast(EffectRig.Spec(2.2f, ("arc_deg", 180f), ("radius_m", 3.5f)));

            new MeleeArcDelivery().OnPhaseEnter(_rig.Ctx, SkillPhase.Active);

            Assert.AreEqual(1, HitsOf(front));
            Assert.AreEqual(22f, EffectRig.Rx(front).Hits[0].Total, 1e-3f);
            Assert.AreEqual(0, HitsOf(tooFar), "3.6 m is outside 3.5 m");
            Assert.AreEqual(0, HitsOf(behind));
            AreaSpec area = _rig.Query.Specs[0];
            Assert.AreEqual(3.5f, area.Shape.Radius);
            Assert.AreEqual(180f, area.Shape.ArcDegrees);
            Assert.AreEqual(0, area.Generation);
        }

        [Test]
        public void MeleeArc_DoesNothingOutsideItsPhase()
        {
            var front = _rig.AddEnemy(0f, 2f);
            Cast(EffectRig.Spec(2.2f, ("arc_deg", 180f), ("radius_m", 3.5f)));
            var d = new MeleeArcDelivery();

            d.OnPhaseEnter(_rig.Ctx, SkillPhase.Windup);
            d.OnPhaseEnter(_rig.Ctx, SkillPhase.Recovery);
            Assert.AreEqual(0, HitsOf(front));

            d.WithImpactPhase(SkillPhase.Recovery).OnPhaseEnter(_rig.Ctx, SkillPhase.Recovery);
            Assert.AreEqual(1, HitsOf(front));
        }

        [Test]
        public void MeleeArc_RespectsTheCasterFacing()
        {
            var behind = _rig.AddEnemy(0f, -2f);
            _rig.Motion.Forward = Vector3.back;
            Cast(EffectRig.Spec(2.2f, ("arc_deg", 180f), ("radius_m", 3.5f)));
            new MeleeArcDelivery().OnPhaseEnter(_rig.Ctx, SkillPhase.Active);
            Assert.AreEqual(1, HitsOf(behind));
        }

        // ---- AreaAtSelf / AreaAtPoint ----

        [Test]
        public void AreaAtSelf_HitsAllAroundWithinTheRadius()
        {
            var a = _rig.AddEnemy(0f, 2.9f);
            var b = _rig.AddEnemy(0f, -2.9f);
            var c = _rig.AddEnemy(3.1f, 0f);
            Cast(EffectRig.Spec(1.8f, ("radius_m", 3f)));
            new AreaAtSelfDelivery().OnPhaseEnter(_rig.Ctx, SkillPhase.Active);

            Assert.AreEqual(1, HitsOf(a));
            Assert.AreEqual(1, HitsOf(b));
            Assert.AreEqual(0, HitsOf(c));
        }

        [Test]
        public void AreaAtPoint_IsCentredOnTheAimClampedToRange()
        {
            var nearTarget = _rig.AddEnemy(0f, 9f);
            var casterSide = _rig.AddEnemy(0f, 3f);
            _rig.Motion.AimPoint = new Vector3(0f, 0f, 20f);
            Cast(EffectRig.Spec(1.5f, ("radius_m", 3f), ("range_m", 8f)));
            new AreaAtPointDelivery("radius_m", "range_m").OnPhaseEnter(_rig.Ctx, SkillPhase.Active);

            Assert.AreEqual(1, HitsOf(nearTarget), "aim at 20 m is pulled back to 8 m");
            Assert.AreEqual(0, HitsOf(casterSide));
            Assert.AreEqual(8f, _rig.Ctx.ImpactPoint.z, 1e-3f);
        }

        [Test]
        public void AreaAtPoint_WithoutRangeUsesTheAimPoint()
        {
            var far = _rig.AddEnemy(0f, 19f);
            _rig.Motion.AimPoint = new Vector3(0f, 0f, 20f);
            Cast(EffectRig.Spec(1.5f, ("radius_m", 3f)));
            new AreaAtPointDelivery("radius_m").OnPhaseEnter(_rig.Ctx, SkillPhase.Active);
            Assert.AreEqual(1, HitsOf(far));
        }

        // ---- ConeMultiTarget ----

        [Test]
        public void ConeMultiTarget_ReachesAtMostNTheClosestFirst()
        {
            var all = new SkillTarget[7];
            for (int i = 6; i >= 0; i--) all[i] = _rig.AddEnemy(0f, 1f + i); // registered far to near
            var outside = _rig.AddEnemy(0f, 8.5f);
            Cast(EffectRig.Spec(1.2f, ("range_m", 8f), ("arc_deg", 90f), ("max_targets", 5f)));
            new ConeMultiTargetDelivery().OnPhaseEnter(_rig.Ctx, SkillPhase.Active);

            for (int i = 0; i < 5; i++) Assert.AreEqual(1, HitsOf(all[i]), "target " + i);
            Assert.AreEqual(0, HitsOf(all[5]));
            Assert.AreEqual(0, HitsOf(all[6]));
            Assert.AreEqual(0, HitsOf(outside));
            Assert.AreEqual(5, _rig.Ctx.TargetsHit);
        }

        [Test]
        public void ConeMultiTarget_FewerTargetsThanTheCapHitsThemAll()
        {
            var a = _rig.AddEnemy(0f, 2f);
            var b = _rig.AddEnemy(1f, 3f);
            Cast(EffectRig.Spec(1.2f, ("range_m", 8f), ("arc_deg", 90f), ("max_targets", 5f)));
            new ConeMultiTargetDelivery().OnPhaseEnter(_rig.Ctx, SkillPhase.Active);
            Assert.AreEqual(1, HitsOf(a));
            Assert.AreEqual(1, HitsOf(b));
        }

        // ---- Leap ----

        private SkillSpec LeapSpec() =>
            EffectRig.Spec("skill.test.leap", "Attack|Movement|Area|Ember", 1.8f, new SkillFrames(3, 30, 10), ("range_m", 8f), ("radius_m", 3f));

        [Test]
        public void Leap_FollowsTheCurvesAndLandsAtTheClampedPoint()
        {
            var leap = new LeapDelivery("range_m", "radius_m", heightMeters: 2f);
            _rig.Motion.AimPoint = new Vector3(0f, 0f, 20f);
            Cast(LeapSpec());

            _rig.Ctx.Frame = 103;
            leap.OnPhaseEnter(_rig.Ctx, SkillPhase.Active);
            leap.Tick(_rig.Ctx);
            Assert.AreEqual(Vector3.zero, _rig.Motion.Positions[0]);

            _rig.Ctx.Frame = 118; // halfway through 30 frames
            leap.Tick(_rig.Ctx);
            Vector3 mid = _rig.Motion.Positions[_rig.Motion.Positions.Count - 1];
            Assert.AreEqual(4f, mid.z, 1e-3f);
            Assert.Greater(mid.y, 1f, "arc peaks mid-air");

            _rig.Ctx.Frame = 133;
            leap.OnPhaseEnter(_rig.Ctx, SkillPhase.Recovery);
            Vector3 end = _rig.Motion.Positions[_rig.Motion.Positions.Count - 1];
            Assert.AreEqual(8f, end.z, 1e-3f);
            Assert.AreEqual(0f, end.y, 1e-3f);
            Assert.IsFalse(_rig.Ctx.InFlight);
        }

        [Test]
        public void Leap_ShortAimLandsWhereAimed()
        {
            var leap = new LeapDelivery("range_m", "radius_m");
            _rig.Motion.AimPoint = new Vector3(0f, 0f, 5f);
            Cast(LeapSpec());
            _rig.Ctx.Frame = 103;
            leap.OnPhaseEnter(_rig.Ctx, SkillPhase.Active);
            Assert.AreEqual(5f, _rig.Ctx.PathEnd.z, 1e-3f);
        }

        [Test]
        public void Leap_ImpactHitsAroundTheLandingNotTheTakeOff()
        {
            var atLanding = _rig.AddEnemy(0f, 9f);
            var atTakeOff = _rig.AddEnemy(0f, 2f);
            var leap = new LeapDelivery("range_m", "radius_m");
            _rig.Motion.AimPoint = new Vector3(0f, 0f, 20f);
            Cast(LeapSpec());
            _rig.Ctx.Frame = 103;
            leap.OnPhaseEnter(_rig.Ctx, SkillPhase.Active);
            Assert.AreEqual(0, HitsOf(atLanding), "nothing connects while airborne");
            _rig.Ctx.Frame = 133;
            leap.OnPhaseEnter(_rig.Ctx, SkillPhase.Recovery);

            Assert.AreEqual(1, HitsOf(atLanding));
            Assert.AreEqual(0, HitsOf(atTakeOff));
            Assert.AreEqual(3f, _rig.Query.Specs[0].Shape.Radius);
        }

        [Test]
        public void Leap_CancelledInTheAirStopsMoving()
        {
            var leap = new LeapDelivery("range_m", "radius_m");
            _rig.Motion.AimPoint = new Vector3(0f, 0f, 20f);
            Cast(LeapSpec());
            _rig.Ctx.Frame = 103;
            leap.OnPhaseEnter(_rig.Ctx, SkillPhase.Active);
            leap.OnCastEnd(_rig.Ctx, true);
            int count = _rig.Motion.Positions.Count;
            _rig.Ctx.Frame = 110;
            leap.Tick(_rig.Ctx);
            Assert.AreEqual(count, _rig.Motion.Positions.Count);
        }

        [Test]
        public void LeapTrajectory_UsesTheCurveForHorizontalProgress()
        {
            var ease = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0.25f), new Keyframe(1f, 1f));
            Vector3 p = LeapTrajectory.Evaluate(Vector3.zero, new Vector3(0f, 0f, 8f), 0.5f, ease, null, 0f);
            Assert.AreEqual(2f, p.z, 1e-3f);
        }

        // ---- Stance ----

        private SkillSpec StanceSpec() =>
            EffectRig.Spec("skill.test.guard", "Defensive|Attack|Melee|Physical", 2.5f, new SkillFrames(3, 90, 6),
                ("stance_s", 1.5f), ("counter_radius_m", 3f), ("counter_arc_deg", 180f));

        [Test]
        public void Stance_EndsWithTheCounterWhenItsDurationElapses()
        {
            var front = _rig.AddEnemy(0f, 2f);
            var stance = new StanceDelivery();
            Cast(StanceSpec());
            _rig.Ctx.Frame = 103;
            stance.OnPhaseEnter(_rig.Ctx, SkillPhase.Active);
            Assert.IsTrue(_rig.Ctx.StanceActive);

            _rig.Ctx.Frame = 192; // 89 frames in
            stance.Tick(_rig.Ctx);
            Assert.AreEqual(0, HitsOf(front));

            _rig.Ctx.Frame = 193; // 1.5 s = 90 frames
            stance.Tick(_rig.Ctx);
            Assert.AreEqual(1, HitsOf(front));
            Assert.IsFalse(_rig.Ctx.StanceActive);

            _rig.Ctx.Frame = 194;
            stance.Tick(_rig.Ctx);
            Assert.AreEqual(1, HitsOf(front), "the counter fires once");
        }

        [Test]
        public void Stance_ReleasingTheButtonEndsItEarlyWithTheCounter()
        {
            var front = _rig.AddEnemy(0f, 2f);
            var stance = new StanceDelivery();
            Cast(StanceSpec());
            _rig.Ctx.Frame = 103;
            stance.OnPhaseEnter(_rig.Ctx, SkillPhase.Active);

            _rig.Ctx.Frame = 120;
            _rig.Ctx.ButtonHeld = true;
            stance.Tick(_rig.Ctx);
            Assert.AreEqual(0, HitsOf(front));

            _rig.Ctx.ButtonHeld = false;
            _rig.Ctx.Frame = 121;
            stance.Tick(_rig.Ctx);
            Assert.AreEqual(1, HitsOf(front));
            Assert.IsFalse(_rig.Ctx.StanceActive);
        }

        [Test]
        public void Stance_CancelledByADodgeDropsTheCounter()
        {
            var front = _rig.AddEnemy(0f, 2f);
            var stance = new StanceDelivery();
            Cast(StanceSpec());
            _rig.Ctx.Frame = 103;
            stance.OnPhaseEnter(_rig.Ctx, SkillPhase.Active);
            stance.OnCastEnd(_rig.Ctx, true);
            _rig.Ctx.Frame = 300;
            stance.Tick(_rig.Ctx);
            Assert.AreEqual(0, HitsOf(front));
        }

        // ---- RadialLines ----

        private SkillSpec FissureSpec() =>
            EffectRig.Spec("skill.test.world", "Attack|Area|Ultimate|Physical|Ruin", 6f, new SkillFrames(10, 5, 20),
                ("fissure_count", 6f), ("radius_m", 7f), ("fissure_width_m", 1f), ("ruin_vuln_pct", 10f), ("ruin_s", 6f),
                ("stacks_per_fissure", 1f), ("stacks_max", 5f));

        private SkillEffect[] FissureEffects() => new SkillEffect[]
        {
            new DamageEffect(DamageType.Physical),
            ApplyTimedModifierEffect.ForVulnerability("ruin", "ruin_vuln_pct", 0f, "ruin_s", 0f, "stacks_per_fissure", 0, "stacks_max", 0),
        };

        [Test]
        public void RadialLines_HitsTargetsOnTheLinesOncePerTarget()
        {
            var onLine = _rig.AddEnemy(0f, 5f);       // line 0
            var opposite = _rig.AddEnemy(0f, -5f);    // line 3 (180 degrees)
            var between = _rig.AddEnemy(2.5f, 4.33f); // 30 degrees: between two lines
            var beyond = _rig.AddEnemy(0f, 7.5f);     // past the 7 m length
            Cast(FissureSpec(), FissureEffects());
            new RadialLinesDelivery().OnPhaseEnter(_rig.Ctx, SkillPhase.Active);

            Assert.AreEqual(1, HitsOf(onLine));
            Assert.AreEqual(6f * EffectRig.WeaponPower, EffectRig.Rx(onLine).Hits[0].Total, 1e-3f);
            Assert.AreEqual(1, HitsOf(opposite));
            Assert.AreEqual(0, HitsOf(between));
            Assert.AreEqual(0, HitsOf(beyond));
        }

        [Test]
        public void RadialLines_StacksFollowTheNumberOfFissuresThatReachedTheTarget()
        {
            var onLine = _rig.AddEnemy(0f, 5f);
            var center = _rig.AddEnemy(0f, 0f);
            Cast(FissureSpec(), FissureEffects());
            new RadialLinesDelivery().OnPhaseEnter(_rig.Ctx, SkillPhase.Active);

            int key = SkillParams.KeyHash("ruin");
            Assert.AreEqual(1, onLine.Vulnerability.GetStacks(key, 101));
            Assert.AreEqual(1, HitsOf(center), "one hit even though 6 fissures cross the centre");
            Assert.AreEqual(5, center.Vulnerability.GetStacks(key, 101), "6 fissures capped at the 5-stack provisional maximum");
        }

        [Test]
        public void RadialLines_BuildsOneBoxPerFissureEvenlySpread()
        {
            Cast(FissureSpec(), FissureEffects());
            new RadialLinesDelivery().OnPhaseEnter(_rig.Ctx, SkillPhase.Active);

            Assert.AreEqual(6, _rig.Query.Specs.Count);
            Assert.AreEqual(HitboxKind.Box, _rig.Query.Specs[0].Shape.Kind);
            Assert.AreEqual(7f, _rig.Query.Specs[0].Shape.Length);
            Assert.AreEqual(-1f, _rig.Query.Specs[3].Forward.z, 1e-4f, "fourth line points backwards");
        }
    }
}
