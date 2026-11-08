using System.Collections.Generic;
using System.Reflection;
using Eclipse.Core.Damage;
using Eclipse.Core.Ids;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.EditMode.Skills.Effects
{
    /// <summary>The library wired to the real SkillController through SkillRunner: effects fire on the controller phases.</summary>
    public sealed class SkillRunnerTests
    {
        private sealed class FakeState : ISkillState
        {
            public bool IsStunned(int f) => false;
            public bool IsFrozen(int f) => false;
            public bool IsSilenced(int f) => false;
            public bool IsRooted(int f) => false;
        }

        private sealed class RawDelivery : Delivery { }

        private EffectRig _rig;
        private SkillRunner _runner;
        private SkillController _controller;

        [SetUp]
        public void SetUp()
        {
            _rig = new EffectRig();
            _rig.Resource.Value = 100f;
            _runner = new SkillRunner(_rig.Ctx);
            _controller = new SkillController(_rig.Caster.Id, new FakeState(), _rig.Resource, null, _runner, _rig.Events);
            _rig.Ctx.Controller = _controller;
        }

        private static SkillSpecBuilder Builder(string id, SkillSlot slot, string tags, SkillFrames frames, float coefficient,
            params (string key, float value)[] ps)
        {
            var b = new SkillSpecBuilder
            {
                SkillId = StableId.Parse(id), Slot = slot, Tags = Eclipse.Core.Tags.TagMask.Parse(tags), DamageCoefficient = coefficient, Frames = frames,
            };
            foreach (var p in ps) b.SetParam(p.key, p.value);
            return b;
        }

        private void Run(int from, int to)
        {
            for (int f = from; f <= to; f++)
            {
                _controller.Tick(f);
                _runner.Tick(f);
            }
        }

        private int Hits(SkillTarget t) => EffectRig.Rx(t).Hits.Count;

        [Test]
        public void Cast_DamagesOnTheFirstActiveFrameOnlyAndReportsTheSkillHit()
        {
            var enemy = _rig.AddEnemy(0f, 3f);
            SkillBuilderForCleaving(out SkillSpec spec);

            Assert.AreEqual(CastResult.Started, _controller.TryCast(spec, 100));
            Run(101, 102);
            Assert.AreEqual(0, Hits(enemy), "windup: nothing connects yet");
            Run(103, 110);

            Assert.AreEqual(1, Hits(enemy), "Active has 2 frames but the strike connects once");
            Assert.AreEqual(22f, EffectRig.Rx(enemy).Hits[0].Total, 1e-3f);
            Assert.AreEqual(0, EffectRig.Rx(enemy).Hits[0].Generation, "direct cast: Generation 0");
            Assert.AreEqual(1, _rig.Events.Count<OnSkillHit>());
            Assert.AreEqual(1, _rig.Events.Count<OnHitDealt>());
            Assert.AreEqual(80f, _rig.Resource.Value, "20 Fury spent by the controller");
            Assert.IsFalse(_controller.IsCasting);
            Assert.AreEqual(1, _rig.Events.Count<OnCastEnd>());
        }

        private void SkillBuilderForCleaving(out SkillSpec spec)
        {
            var b = Builder("skill.test.cleaving", SkillSlot.Q, "Attack|Melee|Area|Physical", new SkillFrames(3, 2, 4), 2.2f,
                ("arc_deg", 180f), ("radius_m", 3.5f));
            b.CostResource = EffectRig.Fury;
            b.CostAmount = 20f;
            spec = b.Build(null);
            _runner.Register(spec.SkillId, new SkillProgram(new MeleeArcDelivery(), new SkillEffect[] { new DamageEffect(DamageType.Physical) }));
        }

        [Test]
        public void Effects_RunAtTheirTiming()
        {
            var b = Builder("skill.test.leap", SkillSlot.Shift, "Attack|Movement|Area|Ember", new SkillFrames(3, 30, 10), 1.8f,
                ("radius_m", 3f));
            SkillSpec spec = b.Build(null);
            var program = new SkillProgram(new AreaAtSelfDelivery(), new[]
            {
                new GainResourceEffect(null, 7f).At(EffectTiming.CastStart, EffectTargeting.Caster),
                new ApplyStateEffect(SkillStateKind.Unstoppable, null, 0.5f).At(EffectTiming.ActiveStart, EffectTargeting.Caster),
                new GainResourceEffect(null, 100f).At(EffectTiming.CastEnd, EffectTargeting.Caster),
            });
            _runner.Register(spec.SkillId, program);

            _controller.TryCast(spec, 100);
            Assert.AreEqual(7f, _rig.Resource.Gained, "CastStart effect ran on the first phase");
            Run(101, 102);
            Assert.IsFalse(_rig.Caster.State.IsUnstoppable(102));
            Run(103, 103);
            Assert.IsTrue(_rig.Caster.State.IsUnstoppable(110), "ActiveStart effect ran on the Active phase");
            Assert.AreEqual(7f, _rig.Resource.Gained, "CastEnd effect has not run yet");
            Run(104, 143);
            Assert.AreEqual(107f, _rig.Resource.Gained, "CastEnd effect ran once the cast finished");
        }

        [Test]
        public void Stance_ReleasingTheButtonCountersAndEndsTheCast()
        {
            var enemy = _rig.AddEnemy(0f, 2f);
            var b = Builder("skill.test.guard", SkillSlot.R, "Defensive|Attack|Melee|Physical", new SkillFrames(3, 90, 6), 2.5f,
                ("stance_s", 1.5f), ("counter_radius_m", 3f), ("counter_arc_deg", 180f));
            SkillSpec spec = b.Build(null);
            _runner.Register(spec.SkillId, new SkillProgram(new StanceDelivery(), new SkillEffect[] { new DamageEffect(DamageType.Physical) }));

            _controller.TryCast(spec, 100);
            _rig.Ctx.ButtonHeld = true;
            Run(101, 120);
            Assert.AreEqual(0, Hits(enemy));
            Assert.IsTrue(_controller.IsCasting);

            _rig.Ctx.ButtonHeld = false;
            Run(121, 121);
            Assert.AreEqual(1, Hits(enemy));
            Assert.AreEqual(25f, EffectRig.Rx(enemy).Hits[0].Total, 1e-3f);
            Assert.IsFalse(_controller.IsCasting, "early release ends the cast");
        }

        [Test]
        public void Chains_TetherBreaksAfterTheCastEndedAndDealsBreakDamage()
        {
            var enemy = _rig.AddEnemy(0f, 5f);
            _rig.Ctx.Tethers = new TetherSet();
            var b = Builder("skill.test.chains", SkillSlot.E, "Attack|Ranged|Ember|DoT", new SkillFrames(3, 2, 4), 1.2f,
                ("range_m", 8f), ("arc_deg", 90f), ("max_targets", 5f), ("root_s", 2f), ("break_distance_m", 10f),
                ("break_damage_coef_pct", 60f));
            SkillSpec spec = b.Build(null);
            _runner.Register(spec.SkillId, new SkillProgram(new ConeMultiTargetDelivery(), new SkillEffect[]
            {
                new DamageEffect(DamageType.Ember),
                new ApplyStateEffect(SkillStateKind.Root, "root_s"),
                new TetherEffect("root_s", 0f, "break_distance_m", 0f, new DamageEffect(DamageType.Ember, "break_damage_coef_pct")),
            }));

            _controller.TryCast(spec, 100);
            Run(101, 111);
            Assert.IsFalse(_controller.IsCasting);
            Assert.AreEqual(1, Hits(enemy));
            Assert.IsTrue(enemy.State.IsRooted(111));
            Assert.IsTrue(_rig.Ctx.Tethers.IsTethered(enemy.Id));

            ((FakeEntity)enemy.Entity).Position = new Vector3(0f, 0f, 10.5f);
            Run(112, 112);

            Assert.AreEqual(2, Hits(enemy));
            Assert.AreEqual(6f, EffectRig.Rx(enemy).Hits[1].Total, 1e-3f, "60% of Weapon Power 10");
            Assert.IsFalse(_rig.Ctx.Tethers.IsTethered(enemy.Id));
        }

        [Test]
        public void Register_RejectsTheSameSkillTwice()
        {
            var spec = Builder("skill.test.dup", SkillSlot.Q, "Attack", new SkillFrames(1, 1, 1), 1f).Build(null);
            _runner.Register(spec.SkillId, new SkillProgram(null, null));
            Assert.Throws<System.InvalidOperationException>(() => _runner.Register(spec.SkillId, new SkillProgram(null, null)));
        }

        // ---- SkillProgram.From ----

        private static void SetField(SkillDefinition def, string name, object value) =>
            typeof(SkillDefinition).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(def, value);

        [Test]
        public void Program_FromDefinitionTypesDeliveryEffectsAndTriggers()
        {
            var def = ScriptableObject.CreateInstance<SkillDefinition>();
            try
            {
                SetField(def, "delivery", new MeleeArcDelivery());
                SetField(def, "effects", new List<Effect> { new DamageEffect(DamageType.Physical), new GainResourceEffect(null, 5f) });
                SetField(def, "triggers", new List<Trigger> { new SkillTrigger("trigger.test.a", TriggerEventKind.OnHitDealt, 0.5f) });

                SkillProgram p = SkillProgram.From(def);
                Assert.IsInstanceOf<MeleeArcDelivery>(p.Delivery);
                Assert.AreEqual(2, p.Effects.Length);
                Assert.AreEqual(1, p.Triggers.Length);
            }
            finally { Object.DestroyImmediate(def); }
        }

        [Test]
        public void Program_FromDefinitionWithAnUnimplementedDeliveryIsADataError()
        {
            var def = ScriptableObject.CreateInstance<SkillDefinition>();
            try
            {
                SetField(def, "delivery", new RawDelivery());
                Assert.Throws<System.InvalidOperationException>(() => SkillProgram.From(def));
            }
            finally { Object.DestroyImmediate(def); }
        }
    }
}
