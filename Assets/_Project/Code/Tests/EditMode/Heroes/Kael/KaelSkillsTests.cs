using Eclipse.Core.Damage;
using Eclipse.Core.Tags;
using Eclipse.Data.Heroes;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Heroes.Kael;
using Eclipse.Gameplay.Skills;
using Eclipse.Tests.EditMode.Skills.Effects;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Tests.EditMode.Heroes.Kael
{
    /// <summary>
    /// Kael's five skills (08 4.4) as authored in the assets (structure) and run through the real controller/runner with fake
    /// targets. Fixture: Weapon Power 10, no mitigation (EffectRig). Run the data import and Eclipse.Editor.Setup.KaelSkillsCreator.Run first.
    /// </summary>
    public sealed class KaelSkillsTests
    {
        private const string Folder = "Assets/_Project/Content/Heroes/Kael";

        private sealed class NoCc : ISkillState
        {
            public bool IsStunned(int f) => false;
            public bool IsFrozen(int f) => false;
            public bool IsSilenced(int f) => false;
            public bool IsRooted(int f) => false;
        }

        private EffectRig _rig;
        private SkillRunner _runner;
        private SkillController _controller;

        private static SkillDefinition Def(string name)
        {
            var d = AssetDatabase.LoadAssetAtPath<SkillDefinition>(Folder + "/Skills/" + name + ".asset");
            Assert.IsNotNull(d, "skill asset " + name + " (run the data import)");
            return d;
        }

        [SetUp]
        public void SetUp()
        {
            _rig = new EffectRig();
            _rig.Resource.Value = 100f;
            _runner = new SkillRunner(_rig.Ctx);
            _controller = new SkillController(_rig.Caster.Id, new NoCc(), _rig.Resource, null, _runner, _rig.Events);
            _rig.Ctx.Controller = _controller;
            _rig.Ctx.Tethers = new TetherSet();
            _rig.Ctx.WeaponPower = EffectRig.WeaponPower;
        }

        private SkillSpec Load(string name)
        {
            SkillDefinition d = Def(name);
            _runner.Register(d);
            return KaelSkillFrames.Builder(d).Build(null);
        }

        private void Run(int from, int to)
        {
            for (int f = from; f <= to; f++)
            {
                _controller.Tick(f);
                _runner.Tick(f);
            }
        }

        private static float Total(SkillTarget t, int i = 0) => EffectRig.Rx(t).Hits[i].Total;
        private static int Hits(SkillTarget t) => EffectRig.Rx(t).Hits.Count;

        // ---------------------------------------------------------------- structure of the assets

        [Test]
        public void Assets_HaveTheAuthoredStructure()
        {
            AssertShape("cleaving_strike", typeof(MeleeArcDelivery), typeof(DamageEffect));
            AssertShape("infernal_leap", typeof(LeapDelivery), typeof(ApplyStateEffect), typeof(DamageEffect), typeof(ApplyStatusEffect));
            AssertShape("burning_chains", typeof(ConeMultiTargetDelivery), typeof(DamageEffect), typeof(ApplyStatusEffect),
                typeof(ApplyStateEffect), typeof(TetherEffect));
            AssertShape("ashen_guard", typeof(StanceDelivery), typeof(ApplyStateEffect), typeof(DamageEffect), typeof(GainResourceEffect));
            AssertShape("worldbreaker", typeof(RadialLinesDelivery), typeof(ApplyStateEffect), typeof(DamageEffect),
                typeof(ApplyTimedModifierEffect), typeof(GlobalHitStopEffect));
        }

        private static void AssertShape(string name, System.Type delivery, params System.Type[] effects)
        {
            SkillDefinition d = Def(name);
            Assert.IsInstanceOf(delivery, d.Delivery, name + " delivery");
            Assert.AreEqual(effects.Length, d.Effects.Count, name + " effect count");
            for (int i = 0; i < effects.Length; i++) Assert.IsInstanceOf(effects[i], d.Effects[i], name + " effect #" + i);
            Assert.DoesNotThrow(() => SkillProgram.From(d), name + " is a runnable program");
        }

        [TestCase("cleaving_strike", 14, 6, 26)]
        [TestCase("infernal_leap", 8, 30, 20)]
        [TestCase("burning_chains", 12, 4, 22)]
        [TestCase("ashen_guard", 6, 90, 18)]
        [TestCase("worldbreaker", 30, 6, 40)]
        public void PhaseFrames_AreTheTunedValuesOfTheCsv(string name, int windup, int active, int recovery)
        {
            SkillFrames f = KaelSkillFrames.From(Def(name));
            Assert.AreEqual((windup, active, recovery), (f.Windup, f.Active, f.Recovery));
        }

        [Test]
        public void TunedParams_ArePresent()
        {
            Assert.AreEqual(60f, Def("burning_chains").GetParam("arc_deg"));
            Assert.AreEqual(120f, Def("ashen_guard").GetParam("counter_arc_deg"));
            Assert.AreEqual(1.5f, Def("worldbreaker").GetParam("fissure_width_m"));
            Assert.AreEqual(4f, Def("worldbreaker").GetParam("ruin_vulnerability_pct"), "07 4.6: Ruin Vulnerability 4% per stack");
        }

        [Test]
        public void HeroAsset_BindsTheFiveSlotsPassivesWeaponsAndMovement()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(Folder + "/Data/kael.asset");
            Assert.IsNotNull(hero);
            Assert.AreEqual("skill.kael.cleaving_strike", hero.GetSkill(SkillSlot.Q).StableIdString);
            Assert.AreEqual("skill.kael.burning_chains", hero.GetSkill(SkillSlot.E).StableIdString);
            Assert.AreEqual("skill.kael.ashen_guard", hero.GetSkill(SkillSlot.R).StableIdString);
            Assert.AreEqual("skill.kael.infernal_leap", hero.GetSkill(SkillSlot.Shift).StableIdString);
            Assert.AreEqual("skill.kael.worldbreaker", hero.GetSkill(SkillSlot.F).StableIdString);
            Assert.AreEqual(2, hero.PassiveIds.Count);
            Assert.AreEqual(5, hero.WeaponCategories.Count);
            Assert.IsNotNull(hero.Resource);
            Assert.IsNotNull(hero.MovementProfile, "M04 Kael MovementProfile assigned");
        }

        // ---------------------------------------------------------------- Cleaving Strike

        [Test]
        public void CleavingStrike_Deals220PercentInA180DegreeArcOf3_5m()
        {
            SkillSpec spec = Load("cleaving_strike");
            var inside = _rig.AddEnemy(0f, 3.4f);
            var tooFar = _rig.AddEnemy(0f, 3.6f);
            var behind = _rig.AddEnemy(0f, -1f);

            Assert.AreEqual(CastResult.Started, _controller.TryCast(spec, 100));
            Run(101, 150);

            Assert.AreEqual(1, Hits(inside));
            Assert.AreEqual(22f, Total(inside), 1e-3f, "220% of Weapon Power 10");
            Assert.AreEqual(0, Hits(tooFar), "3.6 m is outside the 3.5 m radius");
            Assert.AreEqual(0, Hits(behind), "behind is outside the 180 degree arc");
            Assert.AreEqual(80f, _rig.Resource.Value, "20 Fury");
        }

        [Test]
        public void CleavingStrike_ChainsWhileThereIsFuryAndHasNoCooldown()
        {
            SkillSpec spec = Load("cleaving_strike");
            var enemy = _rig.AddEnemy(0f, 2f);
            _rig.Resource.Value = 40f;
            int frame = 100;
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(CastResult.Started, _controller.TryCast(spec, frame), "cast " + i);
                Run(frame + 1, frame + 60);
                frame += 61;
            }
            Assert.AreEqual(CastResult.InsufficientResource, _controller.TryCast(spec, frame), "out of Fury");
            Assert.AreEqual(2, Hits(enemy));
        }

        // ---------------------------------------------------------------- Infernal Leap

        [Test]
        public void InfernalLeap_GainsFury_IsUnstoppableInTheAir_AndBurnsTheLanding()
        {
            SkillSpec spec = Load("infernal_leap");
            _rig.Resource.Value = 0f;
            _rig.Motion.AimPoint = new Vector3(0f, 0f, 20f);
            var enemy = _rig.AddEnemy(0f, 8.5f);

            Assert.AreEqual(CastResult.Started, _controller.TryCast(spec, 100));
            Assert.AreEqual(15f, _rig.Resource.Gained, "+15 Fury");
            Run(101, 108);
            Assert.IsTrue(_rig.Caster.State.IsUnstoppable(120), "Unstoppable while airborne");
            Run(109, 140);

            Assert.IsTrue(_rig.Caster.State.IsUnstoppable(137), "0.5 s = 30 frames of air");
            Assert.IsFalse(_rig.Caster.State.IsUnstoppable(138));
            Assert.AreEqual(8f, _rig.CasterEntity.Position.z, 1e-3f, "range capped at 8 m");
            Assert.AreEqual(18f, Total(enemy), 1e-3f, "180% of Weapon Power");
            Assert.AreEqual(1, EffectRig.St(enemy).Requests.Count, "Burning applied at the landing");
            Assert.AreEqual(120, EffectRig.St(enemy).Requests[0].DurationFrames, "2 s");
        }

        // ---------------------------------------------------------------- Burning Chains

        [Test]
        public void BurningChains_HitsAtMostFiveInTheConeAndRootsForTwoSeconds()
        {
            SkillSpec spec = Load("burning_chains");
            var near = new SkillTarget[6];
            for (int i = 0; i < 6; i++) near[i] = _rig.AddEnemy(0f, 2f + i); // 2..7 m, all on the axis
            var outsideCone = _rig.AddEnemy(4f, 4f);                             // 45 degrees off axis, cone is 60 degrees
            var beyond = _rig.AddEnemy(0f, 8.5f);

            Assert.AreEqual(CastResult.Started, _controller.TryCast(spec, 100));
            Run(101, 150);

            int hit = 0;
            foreach (SkillTarget t in near) hit += Hits(t);
            Assert.AreEqual(5, hit, "max 5 targets");
            Assert.AreEqual(0, Hits(near[5]), "the farthest of the six is cut");
            Assert.AreEqual(0, Hits(outsideCone));
            Assert.AreEqual(0, Hits(beyond));
            Assert.AreEqual(12f, Total(near[0]), 1e-3f, "120% of Weapon Power");
            Assert.AreEqual(1, EffectRig.St(near[0]).Requests.Count, "Burning applied");
            Assert.IsTrue(near[0].State.IsRooted(140), "Root 2 s");
            Assert.IsTrue(_rig.Ctx.Tethers.IsTethered(near[0].Id));
            Assert.AreEqual(85f, _rig.Resource.Value, "15 Fury cost");
        }

        [Test]
        public void BurningChains_BreakingBeyond10mDealsSixtyPercent()
        {
            SkillSpec spec = Load("burning_chains");
            var enemy = _rig.AddEnemy(0f, 5f);
            _controller.TryCast(spec, 100);
            Run(101, 140);
            Assert.AreEqual(1, Hits(enemy));

            ((FakeEntity)enemy.Entity).Position = new Vector3(0f, 0f, 10.5f);
            Run(141, 141);

            Assert.AreEqual(2, Hits(enemy));
            Assert.AreEqual(6f, Total(enemy, 1), 1e-3f, "60% of Weapon Power");
            Assert.IsFalse(_rig.Ctx.Tethers.IsTethered(enemy.Id));
        }

        [Test]
        public void BurningChains_BreakingAtTheEndOfTheRootDealsSixtyPercent()
        {
            SkillSpec spec = Load("burning_chains");
            var enemy = _rig.AddEnemy(0f, 5f);
            _controller.TryCast(spec, 100);
            Run(101, 260);
            Assert.AreEqual(2, Hits(enemy));
            Assert.AreEqual(6f, Total(enemy, 1), 1e-3f);
        }

        // ---------------------------------------------------------------- Ashen Guard

        private AshenGuardReceiver StartStance(out FakeReceiver inner)
        {
            SkillSpec spec = Load("ashen_guard");
            _rig.Motion.Forward = Vector3.forward;
            inner = new FakeReceiver(_rig.Caster.Id);
            _rig.Caster.Receiver = inner;
            Assert.AreEqual(CastResult.Started, _controller.TryCast(spec, 100));
            _rig.Ctx.ButtonHeld = true;
            Run(101, 108);
            Assert.IsTrue(_rig.Ctx.StanceActive);
            return new AshenGuardReceiver(inner, _rig.Ctx);
        }

        private static DamageInstance Incoming(float physical, HitFlags flags = HitFlags.None)
        {
            var hit = new DamageInstance { SourceId = 99, Flags = flags };
            hit.AddComponent(DamageType.Physical, physical);
            return hit;
        }

        [Test]
        public void AshenGuard_ReducesRearHitsBy60Percent()
        {
            AshenGuardReceiver guard = StartStance(out FakeReceiver inner);
            _rig.Ctx.Frame = 130; // past the 12 f Parry window
            guard.ReceiveHit(Incoming(100f), Vector3.forward); // attacker behind: direction attacker->Kael is +Z

            Assert.AreEqual(1, inner.Hits.Count);
            Assert.AreEqual(40f, inner.Hits[0].Total, 1e-3f, "-60%");
            Assert.AreEqual(60f, guard.AbsorbedThisStance, 1e-3f);
        }

        [Test]
        public void AshenGuard_BlocksFrontalHitsEntirelyExceptUnblockable()
        {
            AshenGuardReceiver guard = StartStance(out FakeReceiver inner);
            _rig.Ctx.Frame = 130;

            DamageOutcome blocked = guard.ReceiveHit(Incoming(100f), Vector3.back); // attacker in front
            Assert.AreEqual(HitResult.Blocked, blocked.Result);
            Assert.AreEqual(0, inner.Hits.Count, "nothing reaches the pipeline");
            Assert.AreEqual(1, _rig.Events.Count<OnBlock>());

            guard.ReceiveHit(Incoming(100f, HitFlags.Unblockable), Vector3.back);
            Assert.AreEqual(1, inner.Hits.Count, "Unblockable goes through");
            Assert.AreEqual(40f, inner.Hits[0].Total, 1e-3f, "but still at -60%");
        }

        [Test]
        public void AshenGuard_ParryWindowIs12FramesAndBoostsTheCounterBy50Percent()
        {
            var enemy = _rig.AddEnemy(0f, 2f);
            AshenGuardReceiver guard = StartStance(out FakeReceiver inner);
            int start = _rig.Ctx.StanceStartFrame;
            Assert.AreEqual(12, _rig.Caster.State.ParryEndFrame - start + 1, "Parry of 12 frames (R20)");

            inner.Result = HitResult.Parried; // the combat entity negates it inside the window
            _rig.Ctx.Frame = start + 11;
            Assert.AreEqual(HitResult.Parried, guard.ReceiveHit(Incoming(100f), Vector3.back).Result);
            Assert.AreEqual(1, guard.ParriedHits);
            Assert.AreEqual(1.5f, _rig.Ctx.DamageMultiplier, 1e-5f);

            _rig.Ctx.ButtonHeld = false;
            Run(109, 110);
            Assert.AreEqual(1, Hits(enemy));
            Assert.AreEqual(37.5f, Total(enemy), 1e-3f, "250% x 1.5 of Weapon Power");
            Assert.AreEqual(20f, _rig.Resource.Gained, "+20 Fury on the counter");
        }

        [Test]
        public void AshenGuard_NoParryAfterTheWindow_AndCounterAdds30PercentOfAbsorbed()
        {
            var enemy = _rig.AddEnemy(0f, 2f);
            AshenGuardReceiver guard = StartStance(out FakeReceiver inner);
            int start = _rig.Ctx.StanceStartFrame;

            _rig.Ctx.Frame = start + 12; // window closed
            Assert.AreEqual(HitResult.Blocked, guard.ReceiveHit(Incoming(100f), Vector3.back).Result);
            Assert.AreEqual(1f, _rig.Ctx.DamageMultiplier, 1e-5f, "no Parry bonus");

            _rig.Ctx.ButtonHeld = false;
            Run(109, 110);
            Assert.AreEqual(25f + 30f, Total(enemy), 1e-3f, "250% + 30% of the 100 absorbed");
        }

        [Test]
        public void AshenGuard_CounterUsesTheTunedArcOf120DegreesAndRadius3()
        {
            var front = _rig.AddEnemy(0f, 2.9f);
            var side = _rig.AddEnemy(2.5f, 0.1f);   // ~87 degrees off axis: outside a 120 degree arc
            var beyond = _rig.AddEnemy(0f, 3.1f);
            AshenGuardReceiver guard = StartStance(out _);
            _rig.Ctx.ButtonHeld = false;
            Run(109, 110);
            Assert.AreEqual(1, Hits(front));
            Assert.AreEqual(0, Hits(side));
            Assert.AreEqual(0, Hits(beyond));
            Assert.IsNotNull(guard);
        }

        [Test]
        public void AshenGuard_OutsideTheStanceTheReceiverForwardsUntouched()
        {
            var inner = new FakeReceiver(_rig.Caster.Id);
            var guard = new AshenGuardReceiver(inner, _rig.Ctx);
            guard.ReceiveHit(Incoming(100f), Vector3.back);
            Assert.AreEqual(100f, inner.Hits[0].Total, 1e-3f);
        }

        // ---------------------------------------------------------------- Worldbreaker

        [Test]
        public void Worldbreaker_IsRefusedBelow50Fury()
        {
            SkillSpec spec = Load("worldbreaker");
            _rig.Resource.Value = 49f;
            Assert.AreEqual(CastResult.InsufficientResource, _controller.TryCast(spec, 100));
        }

        [Test]
        public void Worldbreaker_Deals600PlusSixPerFury_StacksRuinPerFissure_AndStopsTheWorld()
        {
            SkillSpec spec = Load("worldbreaker");
            _rig.Resource.Value = 80f;
            var onLine = _rig.AddEnemy(0f, 3f);      // on one fissure
            var centre = _rig.AddEnemy(0f, 0f);      // at the origin: crossed by all six
            var outside = _rig.AddEnemy(0f, 7.5f);

            Assert.AreEqual(CastResult.Started, _controller.TryCast(spec, 100));
            Assert.AreEqual(0f, _rig.Resource.Value, "all Fury consumed");
            Run(101, 190);

            float expected = EffectRig.WeaponPower * (6f + 0.06f * 80f);
            Assert.AreEqual(expected, Total(onLine), 1e-2f, "600% + 6% x 80 Fury");
            Assert.AreEqual(1, Hits(centre), "one hit per target even when six fissures cross it");
            Assert.AreEqual(0, Hits(outside), "beyond the 7 m radius");
            Assert.AreEqual(0.04f, onLine.Vulnerability.Total(140), 1e-5f, "1 stack of 4%");
            Assert.AreEqual(0.20f, centre.Vulnerability.Total(140), 1e-5f, "six fissures, capped at 5 stacks = 20%");
            Assert.AreEqual(0f, onLine.Vulnerability.Total(140 + 6 * 60 + 1), 1e-5f, "6 s duration");
            Assert.IsTrue(_rig.Caster.State.IsUnstoppable(100 + 70), "Unstoppable through the cast");

            Assert.AreEqual(1, _rig.Events.Count<OnGlobalHitStop>());
            Assert.AreEqual(6, _rig.Events.Read<OnGlobalHitStop>()[0].Frames, "global hit-stop of 6 frames");
            Assert.AreEqual(0.5f, _rig.Events.Read<OnSkillShake>()[0].Amount, 1e-5f);
        }
    }
}
