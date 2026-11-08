using Eclipse.Core.CrowdControl;
using Eclipse.Core.Ids;
using Eclipse.Core.Stats;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Simulation;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.Skills.Effects
{
    /// <summary>ApplyState, ApplyStatus and ApplyTimedModifier with fake targets. Numbers are the 06 3.8 / R20 / 08 4.4 doc values.</summary>
    public sealed class StateStatusEffectTests
    {
        private EffectRig _rig;
        private SkillTarget _enemy;

        [SetUp]
        public void SetUp()
        {
            _rig = new EffectRig();
            _enemy = _rig.AddEnemy(0f, 5f);
        }

        // ---- ApplyState ----

        [Test]
        public void State_RootLastsTheParamSecondsOnTheTarget()
        {
            _rig.Begin(EffectRig.Spec(1.2f, ("root_s", 2f)));
            new ApplyStateEffect(SkillStateKind.Root, "root_s").Apply(_rig.Ctx, _enemy);

            Assert.IsTrue(_enemy.State.IsRooted(219));
            Assert.IsFalse(_enemy.State.IsRooted(220)); // 2 s = 120 frames from frame 100
        }

        [Test]
        public void State_RootIsRefusedByAnUnstoppableTarget()
        {
            _enemy.State.GrantUnstoppable(1000);
            _rig.Begin(EffectRig.Spec(1.2f, ("root_s", 2f)));
            new ApplyStateEffect(SkillStateKind.Root, "root_s").Apply(_rig.Ctx, _enemy);

            Assert.IsFalse(_enemy.State.IsRooted(110));
        }

        [Test]
        public void State_UnstoppableOnTheCasterBlocksStunForTheParamDuration()
        {
            _rig.Begin(EffectRig.Spec(1.8f, ("unstoppable_s", 0.5f)));
            new ApplyStateEffect(SkillStateKind.Unstoppable, "unstoppable_s").Apply(_rig.Ctx, _rig.Caster);
            var state = _rig.Caster.State;

            Assert.IsTrue(state.IsUnstoppable(129));
            Assert.IsFalse(state.IsUnstoppable(130)); // 0.5 s = 30 frames
            CcResult stun = state.ApplyCc(CcKind.Stun, 1f, new EntityId(50), 110, 0);
            Assert.AreEqual(CcOutcome.BlockedUnstoppable, stun.Outcome);
            Assert.IsFalse(state.IsStunned(111));
        }

        [Test]
        public void State_TauntMakesTheTargetTaunted()
        {
            _rig.Begin(EffectRig.Spec(0f, ("taunt_s", 2f)));
            new ApplyStateEffect(SkillStateKind.Taunt, "taunt_s").Apply(_rig.Ctx, _enemy);

            Assert.IsTrue(_enemy.State.IsTaunted(110));
            Assert.IsFalse(_enemy.State.IsTaunted(230));
        }

        [Test]
        public void State_TauntIsRefusedByBossesAndGuardians()
        {
            var boss = _rig.AddEnemy(0f, 6f);
            boss.State = new StateComponent(Eclipse.Tests.EditMode.Gameplay.States.StatesFixtures.Rules(), boss.Id, false, null, _rig.Events, guardianOrBoss: true);
            _rig.Begin(EffectRig.Spec(0f, ("taunt_s", 2f)));
            new ApplyStateEffect(SkillStateKind.Taunt, "taunt_s").Apply(_rig.Ctx, boss);

            Assert.IsFalse(boss.State.IsTaunted(110));
        }

        [Test]
        public void State_ParryWindowIsTwelveFramesFromTheCast()
        {
            _rig.Begin(EffectRig.Spec(2.5f));
            new ApplyStateEffect(SkillStateKind.ParryWindow).Apply(_rig.Ctx, _rig.Caster);
            var state = _rig.Caster.State;

            Assert.IsTrue(state.IsParryActive(100));
            Assert.IsTrue(state.IsParryActive(111));
            Assert.IsFalse(state.IsParryActive(112)); // 12 f (R20)
        }

        [Test]
        public void State_ParryWindowBonusIsCappedByTheRules()
        {
            _rig.Begin(EffectRig.Spec(2.5f, ("parry_bonus_s", 0.2f)));
            new ApplyStateEffect(SkillStateKind.ParryWindow, parryBonusParam: "parry_bonus_s").Apply(_rig.Ctx, _rig.Caster);
            var state = _rig.Caster.State;

            Assert.IsTrue(state.IsParryActive(117));
            Assert.IsFalse(state.IsParryActive(118)); // cap 0.3 s = 18 f
        }

        [Test]
        public void State_MissingStateComponentIsIgnored()
        {
            _enemy.State = null;
            _rig.Begin(EffectRig.Spec(1.2f, ("root_s", 2f)));
            Assert.DoesNotThrow(() => new ApplyStateEffect(SkillStateKind.Root, "root_s").Apply(_rig.Ctx, _enemy));
        }

        // ---- ApplyStatus ----

        [Test]
        public void Status_RequestCarriesStackDurationSourceAndGeneration()
        {
            _rig.Begin(EffectRig.Spec(1.2f, ("burning_s", 2f)));
            new ApplyStatusEffect("status.burning", 1, "burning_s").Apply(_rig.Ctx, _enemy);

            StatusRequest r = EffectRig.St(_enemy).Requests[0];
            Assert.AreEqual("status.burning", r.Status.Value);
            Assert.AreEqual(1, r.Stacks);
            Assert.AreEqual(120, r.DurationFrames);
            Assert.AreEqual(_rig.Caster.Id, r.Source);
            Assert.AreEqual(_enemy.Id, r.Target);
            Assert.AreEqual(0, r.Generation);
            Assert.AreEqual(1f, r.Potency);
            Assert.IsTrue(r.Guaranteed);
        }

        [Test]
        public void Status_GenerationTwoHalvesThePotency()
        {
            _rig.Begin(EffectRig.Spec());
            _rig.Ctx.Generation = 2;
            new ApplyStatusEffect("status.burning").Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(0.5f, EffectRig.St(_enemy).Requests[0].Potency);
            Assert.AreEqual(2, EffectRig.St(_enemy).Requests[0].Generation);
        }

        [Test]
        public void Status_WorksThroughTheRealBurningStub()
        {
            var stub = new StatusStub(_enemy.Id, new[] { StatusStubDefinition.BurningProvisional() }, _rig.Events);
            _enemy.Status = stub;
            _rig.Begin(EffectRig.Spec(1.2f, ("burning_s", 2f)));
            new ApplyStatusEffect("status.burning", 1, "burning_s").Apply(_rig.Ctx, _enemy);
            new ApplyStatusEffect("status.burning", 1, "burning_s").Apply(_rig.Ctx, _enemy);

            var burning = StableId.Parse("status.burning");
            Assert.AreEqual(2, stub.GetStacks(burning));
            Assert.AreEqual(120, stub.GetRemainingFrames(burning));
            Assert.AreEqual(2, _rig.Events.Count<OnStatusApplied>());
        }

        [Test]
        public void Status_ReceiverlessTargetIsIgnored()
        {
            _enemy.Status = null;
            _rig.Begin(EffectRig.Spec());
            Assert.DoesNotThrow(() => new ApplyStatusEffect("status.burning").Apply(_rig.Ctx, _enemy));
        }

        // ---- ApplyTimedModifier ----

        [Test]
        public void Timed_StatModLastsItsDurationAndIsRenewedNotStacked()
        {
            var fx = ApplyTimedModifierEffect.ForStat("ashen.guard", StatId.DamageReduction, ModBucket.Flat, "dr_pct", 0f, "stance_s", 0f);
            _rig.Begin(EffectRig.Spec(2.5f, ("dr_pct", 60f), ("stance_s", 1.5f)));
            var stats = _rig.CasterEntity.Stats;

            fx.Apply(_rig.Ctx, _rig.Caster);
            fx.Apply(_rig.Ctx, _rig.Caster);
            Assert.AreEqual(0.6f, stats.Evaluate(StatId.DamageReduction), 1e-4f);

            for (int f = 101; f <= 100 + 90; f++) _rig.Caster.Timed.Tick(new SimulationFrame(f));
            Assert.AreEqual(0f, stats.Evaluate(StatId.DamageReduction), 1e-4f); // 1.5 s = 90 frames
        }

        [Test]
        public void Timed_VulnerabilityAddsOneStackPerFissureUpToTheMax()
        {
            var fx = ApplyTimedModifierEffect.ForVulnerability("ruin", "ruin_vuln_pct", 0f, "ruin_s", 0f,
                "stacks_per_fissure", 0, "stacks_max", 0);
            _rig.Begin(EffectRig.Spec(6f, ("ruin_vuln_pct", 10f), ("ruin_s", 6f), ("stacks_per_fissure", 1f), ("stacks_max", 5f)));
            int key = SkillParams.KeyHash("ruin");

            _rig.Ctx.CurrentHits = 3;
            fx.Apply(_rig.Ctx, _enemy);
            Assert.AreEqual(3, _enemy.Vulnerability.GetStacks(key, 101));
            Assert.AreEqual(0.3f, _enemy.Vulnerability.Total(101), 1e-4f);

            _rig.Ctx.CurrentHits = 6;
            fx.Apply(_rig.Ctx, _enemy);
            Assert.AreEqual(5, _enemy.Vulnerability.GetStacks(key, 101)); // capped
            Assert.AreEqual(0.5f, _enemy.Vulnerability.Total(101), 1e-4f);
        }

        [Test]
        public void Timed_VulnerabilityExpiresAfterItsDuration()
        {
            var fx = ApplyTimedModifierEffect.ForVulnerability("ruin", null, 0.1f, null, 6f, null, 1, null, 5);
            _rig.Begin(EffectRig.Spec());
            fx.Apply(_rig.Ctx, _enemy);

            Assert.AreEqual(0.1f, _enemy.Vulnerability.Total(100 + 359), 1e-4f);
            Assert.AreEqual(0f, _enemy.Vulnerability.Total(100 + 360), 1e-4f); // 6 s
        }

        [Test]
        public void Timed_TargetWithoutTheStoreIsIgnored()
        {
            _enemy.Vulnerability = null;
            _enemy.Timed = null;
            _rig.Begin(EffectRig.Spec());
            Assert.DoesNotThrow(() => ApplyTimedModifierEffect.ForVulnerability("ruin", null, 0.1f, null, 6f, null, 1, null, 5).Apply(_rig.Ctx, _enemy));
            Assert.DoesNotThrow(() => ApplyTimedModifierEffect.ForStat("x", StatId.Armor, ModBucket.Flat, null, 1f, null, 1f).Apply(_rig.Ctx, _enemy));
        }
    }
}
