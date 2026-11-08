using Eclipse.Core.Damage;
using Eclipse.Core.Procs;
using Eclipse.Core.Tags;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.Skills.Effects
{
    /// <summary>Triggers of 11 3.6.1/3.6.5: (event, condition, effects, ICD), only for Generation below 2, never from their own hit.</summary>
    public sealed class TriggerTests
    {
        private EffectRig _rig;
        private SkillTarget _enemy;
        private SkillTriggerSystem _system;
        private SkillSpec _spec;

        [SetUp]
        public void SetUp()
        {
            _rig = new EffectRig();
            _enemy = _rig.AddEnemy(0f, 3f);
            _system = new SkillTriggerSystem();
            _spec = EffectRig.Spec(2.2f);
            _rig.Begin(_spec, 50);
        }

        private SkillTrigger OnHit(string id = "trigger.test.burst", float icd = 0.5f, TriggerConditionKind cond = TriggerConditionKind.None,
            TriggerEventKind kind = TriggerEventKind.OnHitDealt) =>
            new SkillTrigger(id, kind, icd, cond, 0f, 0UL, new DamageEffect(DamageType.Physical));

        private TriggerEventData Event(int frame, int generation = 0, SourceChain chain = default, bool crit = false,
            TriggerEventKind kind = TriggerEventKind.OnHitDealt) =>
            new TriggerEventData(kind, frame, generation, HitFlags.None, chain, _rig.Caster.Id, _enemy.Id, 20f, crit);

        private int Hits => EffectRig.Rx(_enemy).Hits.Count;

        [Test]
        public void Trigger_FiresOnItsEventAndRunsItsEffectsOneGenerationDeeper()
        {
            _system.Add(_spec, OnHit());
            var e = Event(100);
            int fired = _system.Feed(_rig.Ctx, in e);

            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, Hits);
            HitSnapshot s = EffectRig.Rx(_enemy).Hits[0];
            Assert.AreEqual(1, s.Generation);
            Assert.IsTrue(s.Flags.Has(HitFlags.IsTriggered));
            Assert.AreEqual(22f, s.Total, 1e-3f);
        }

        [Test]
        public void Trigger_NeverEvaluatedAtGenerationTwo()
        {
            _system.Add(_spec, OnHit());
            var gen2 = Event(100, generation: 2);
            Assert.AreEqual(0, _system.Feed(_rig.Ctx, in gen2));
            Assert.AreEqual(0, Hits);

            var gen1 = Event(100, generation: 1);
            Assert.AreEqual(1, _system.Feed(_rig.Ctx, in gen1));
            Assert.AreEqual(2, EffectRig.Rx(_enemy).Hits[0].Generation, "a Generation 1 event may still fire; its result is Generation 2");
        }

        [Test]
        public void Trigger_IcdBlocksRepeatsUntilItElapses()
        {
            _system.Add(_spec, OnHit(icd: 0.5f));
            var first = Event(100);
            var early = Event(129);
            var later = Event(130);

            Assert.AreEqual(1, _system.Feed(_rig.Ctx, in first));
            Assert.AreEqual(0, _system.Feed(_rig.Ctx, in early));
            Assert.AreEqual(1, _system.Feed(_rig.Ctx, in later)); // 0.5 s = 30 frames
            Assert.AreEqual(2, Hits);
        }

        [Test]
        public void Trigger_IcdHasAFloorOfTenthOfASecond()
        {
            Assert.AreEqual(6, OnHit(icd: 0f).IcdFrames);
            Assert.AreEqual(6, OnHit(icd: 0.01f).IcdFrames);
            Assert.AreEqual(30, OnHit(icd: SkillTrigger.DefaultIcdSeconds).IcdFrames);

            _system.Add(_spec, OnHit(icd: 0f));
            var a = Event(100);
            var b = Event(105);
            var c = Event(106);
            Assert.AreEqual(1, _system.Feed(_rig.Ctx, in a));
            Assert.AreEqual(0, _system.Feed(_rig.Ctx, in b));
            Assert.AreEqual(1, _system.Feed(_rig.Ctx, in c));
        }

        [Test]
        public void Trigger_DoesNotFireFromAHitItOriginated()
        {
            var trigger = OnHit("trigger.test.loop");
            _system.Add(_spec, trigger);
            SourceChain own = SourceChain.Empty.Append(trigger.SourceHash);
            var e = Event(100, 1, own);

            Assert.AreEqual(0, _system.Feed(_rig.Ctx, in e));
            Assert.AreEqual(0, Hits);
        }

        [Test]
        public void Trigger_ConditionCritOnlyIgnoresPlainHits()
        {
            _system.Add(_spec, OnHit(cond: TriggerConditionKind.CritOnly));
            var plain = Event(100);
            var crit = Event(100, crit: true);

            Assert.AreEqual(0, _system.Feed(_rig.Ctx, in plain));
            Assert.AreEqual(1, _system.Feed(_rig.Ctx, in crit));
        }

        [Test]
        public void Trigger_OtherEventKindsAreIgnored()
        {
            _system.Add(_spec, OnHit(kind: TriggerEventKind.OnKill));
            var hit = Event(100);
            var kill = Event(100, kind: TriggerEventKind.OnKill);

            Assert.AreEqual(0, _system.Feed(_rig.Ctx, in hit));
            Assert.AreEqual(1, _system.Feed(_rig.Ctx, in kill));
        }

        [Test]
        public void Trigger_ConditionMinValueAndTags()
        {
            var big = new SkillTrigger("trigger.test.big", TriggerEventKind.OnHitDealt, 0.5f, TriggerConditionKind.MinValue, 50f, 0UL,
                new DamageEffect(DamageType.Physical));
            var tagged = new SkillTrigger("trigger.test.tag", TriggerEventKind.OnHitDealt, 0.5f, TriggerConditionKind.RequiresTags, 0f,
                TagMask.Parse("Area|Physical").Bits, new DamageEffect(DamageType.Physical));
            _system.Add(_spec, big);
            _system.Add(_spec, tagged);

            var small = new TriggerEventData(TriggerEventKind.OnHitDealt, 100, 0, HitFlags.None, default, _rig.Caster.Id, _enemy.Id, 20f, false,
                TagMask.Parse("Area|Physical|Melee"));
            Assert.AreEqual(1, _system.Feed(_rig.Ctx, in small), "value 20 < 50 but the tags match");
            var large = new TriggerEventData(TriggerEventKind.OnHitDealt, 100, 0, HitFlags.None, default, _rig.Caster.Id, _enemy.Id, 80f, false,
                TagMask.Parse("Ember"));
            Assert.AreEqual(1, _system.Feed(_rig.Ctx, in large), "value 80 passes, tags miss");
        }

        [Test]
        public void Trigger_RestoresTheRunningCastContext()
        {
            _rig.Ctx.Generation = 0;
            _rig.Ctx.DamageMultiplier = 1.5f;
            _system.Add(_spec, OnHit());
            var e = Event(100);
            _system.Feed(_rig.Ctx, in e);

            Assert.AreEqual(0, _rig.Ctx.Generation);
            Assert.AreEqual(HitFlags.None, _rig.Ctx.ExtraFlags);
            Assert.AreEqual(1.5f, _rig.Ctx.DamageMultiplier);
            Assert.AreEqual(50, _rig.Ctx.Frame);
            Assert.AreSame(_spec, _rig.Ctx.Cast.Spec);
        }
    }
}
