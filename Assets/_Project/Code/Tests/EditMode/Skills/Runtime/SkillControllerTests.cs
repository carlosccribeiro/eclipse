using System.Collections.Generic;
using Eclipse.Core.Damage;
using Eclipse.Core.Ids;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.EditMode.Skills.Runtime
{
    /// <summary>Execution cycle of 11 3.6.3 with fakes for state, resource, cancel table and Delivery. Fixture numbers are the 08 4.4 doc values.</summary>
    public sealed class SkillControllerTests
    {
        private static readonly EntityId Owner = new EntityId(1);
        private static readonly StableId Fury = StableId.Parse("resource.fury");

        private sealed class FakeState : ISkillState
        {
            public bool Stun, Freeze, Silence, Root;
            public bool IsStunned(int f) => Stun;
            public bool IsFrozen(int f) => Freeze;
            public bool IsSilenced(int f) => Silence;
            public bool IsRooted(int f) => Root;
        }

        private sealed class FakeResource : ISkillResource
        {
            public float Value;
            public float Gained;
            public float Current => Value;
            public bool PublishesSpentEvent => false;
            public bool TrySpend(float a) { if (a > Value) return false; Value -= a; return true; }
            public float SpendAll() { float v = Value; Value = 0; return v; }
            public void Gain(float a) { Gained += a; Value += a; }
        }

        private sealed class FakeAttacks : IAttackCancelHost
        {
            public bool Allow = true;
            public int Cancels;
            public ActionKind LastAsked;
            public bool CanCancelInto(ActionKind to) { LastAsked = to; return Allow; }
            public void CancelForAction() => Cancels++;
        }

        private sealed class FakeRunner : ISkillDeliveryRunner
        {
            public readonly List<string> Log = new List<string>();
            public void OnPhaseEnter(in SkillCastInfo cast, SkillPhase phase, int frame) => Log.Add(phase + "@" + frame);
            public void OnCastEnd(in SkillCastInfo cast, bool cancelled, int frame) => Log.Add((cancelled ? "cancel@" : "end@") + frame);
        }

        private sealed class Op : ISkillSpecModifier
        {
            private readonly System.Action<SkillSpecBuilder> _apply;
            public Op(SkillBuildPhase phase, SkillModifierChannel ch, string key, System.Action<SkillSpecBuilder> apply)
            { Phase = phase; Channel = ch; OrderKey = key; _apply = apply; }
            public SkillBuildPhase Phase { get; }
            public SkillModifierChannel Channel { get; }
            public string OrderKey { get; }
            public void Apply(SkillSpecBuilder b) => _apply(b);
        }

        private FakeState _state;
        private FakeResource _res;
        private FakeAttacks _attacks;
        private FakeRunner _runner;
        private CombatEventStream _events;
        private SkillController _c;

        [SetUp]
        public void SetUp()
        {
            _state = new FakeState();
            _res = new FakeResource { Value = 100f };
            _attacks = new FakeAttacks();
            _runner = new FakeRunner();
            _events = new CombatEventStream();
            _c = new SkillController(Owner, _state, _res, _attacks, _runner, _events);
        }

        private static SkillSpecBuilder Builder(string id, SkillSlot slot, string tags, float cost = 0f, float cd = 0f,
            int w = 3, int a = 2, int r = 4, bool commit = false)
        {
            return new SkillSpecBuilder
            {
                SkillId = StableId.Parse(id),
                Slot = slot,
                Tags = TagMask.Parse(tags),
                CostResource = cost > 0f ? Fury : default,
                CostAmount = cost,
                CooldownSeconds = cd,
                DamageCoefficient = 2.2f,
                Commit = commit,
                Frames = new SkillFrames(w, a, r),
            };
        }

        private static SkillSpec Cleaving() => Builder("skill.test.cleaving", SkillSlot.Q, "Attack|Melee|Area|Physical", cost: 20f).Build(null);
        private static SkillSpec Leap() => Builder("skill.test.leap", SkillSlot.Shift, "Attack|Movement|Area|Ember", cd: 9f).Build(null);
        private static SkillSpec Ultimate(bool commit = false) =>
            Builder("skill.test.world", SkillSlot.F, "Attack|Area|Ultimate|Physical", commit: commit).Build(null);

        [Test]
        public void InsufficientResource_BlocksCastAndSpendsNothing()
        {
            _res.Value = 19f;
            Assert.AreEqual(CastResult.InsufficientResource, _c.TryCast(Cleaving(), 10));
            Assert.AreEqual(19f, _res.Value);
            Assert.IsFalse(_c.IsCasting);
            Assert.AreEqual(0, _events.Count<OnSkillCast>());
        }

        [Test]
        public void Cast_SpendsCostPublishesEventsAndStartsCooldown()
        {
            var spec = Builder("skill.test.chains", SkillSlot.E, "Attack|Ranged", cost: 15f, cd: 12f).Build(null);
            Assert.AreEqual(CastResult.Started, _c.TryCast(spec, 10));
            Assert.AreEqual(85f, _res.Value, 1e-4f);
            Assert.AreEqual(1, _events.Count<OnSkillCast>());
            Assert.AreEqual(1, _events.Count<OnResourceSpent>());
            Assert.AreEqual(15f, _events.Read<OnResourceSpent>()[0].Value, 1e-4f);
            Assert.AreEqual(720, _c.Cooldowns.RemainingFrames(SkillSlot.E, 10));
        }

        [Test]
        public void CostAllResource_RequiresMinimumAndSpendsEverything()
        {
            var b = Builder("skill.test.world", SkillSlot.F, "Attack|Ultimate", cost: 0f);
            b.CostResource = Fury; b.CostAllResource = true; b.CostMinimum = 50f; b.CoefficientPerResource = 0.06f; b.DamageCoefficient = 6f;
            SkillSpec spec = b.Build(null);

            _res.Value = 49f;
            Assert.AreEqual(CastResult.InsufficientResource, _c.TryCast(spec, 1));
            _res.Value = 100f;
            Assert.AreEqual(CastResult.Started, _c.TryCast(spec, 2));
            Assert.AreEqual(0f, _res.Value);
            Assert.AreEqual(100f, _c.ResourceSpent, 1e-4f);
            Assert.AreEqual(6f + 0.06f * 100f, _c.CurrentDamageCoefficient, 1e-4f);
        }

        [Test]
        public void Cooldown_BlocksUntilReady()
        {
            SkillSpec leap = Leap();
            Assert.AreEqual(CastResult.Started, _c.TryCast(leap, 0));
            for (int f = 1; f <= 10; f++) _c.Tick(f); // cast ends (9 frames)
            Assert.IsFalse(_c.IsCasting);
            Assert.AreEqual(CastResult.OnCooldown, _c.TryCast(leap, 539));
            Assert.AreEqual(CastResult.Started, _c.TryCast(leap, 540));
        }

        [Test]
        public void Cdr_ReducesCooldown_UltimateGetsHalf_DodgeUnaffected()
        {
            var rules = new StatRuleTable().Set(StatId.CooldownReduction, new StatRule(0.25f, 0.40f));
            var stats = new StatBlock(rules);
            stats.AddMod(stats.CreateSource(), new StatMod(StatId.CooldownReduction, ModBucket.Flat, 0.12f));

            SkillSpec leap = Builder("skill.test.leap", SkillSlot.Shift, "Attack|Movement", cd: 9f).Build(stats);
            SkillSpec ult = Builder("skill.test.world", SkillSlot.F, "Attack|Ultimate", cd: 90f).Build(stats);

            Assert.AreEqual(476, leap.CooldownFrames); // 9 s x (1 - 0.12) = 7.92 s -> 475.2 -> 476
            Assert.AreEqual(5076, ult.CooldownFrames); // 90 s x (1 - 0.06) = 84.6 s
            Assert.AreEqual(90f, SkillBalance.DodgeRechargeSeconds(90f, 0.12f)); // dodge recharge ignores CDR
        }

        [Test]
        public void CdrIsCapped_ByStatRules()
        {
            var stats = new StatBlock(new StatRuleTable().Set(StatId.CooldownReduction, new StatRule(0.25f, 0.40f)));
            stats.AddMod(stats.CreateSource(), new StatMod(StatId.CooldownReduction, ModBucket.Flat, 2f));
            SkillSpec s = Builder("skill.test.leap", SkillSlot.Shift, "Attack|Movement", cd: 10f).Build(stats);
            Assert.AreEqual(360, s.CooldownFrames); // hard cap 40% -> 6 s
        }

        [Test]
        public void StunFreezeSilence_BlockEverySkill()
        {
            _state.Stun = true;
            Assert.AreEqual(CastResult.Stunned, _c.TryCast(Cleaving(), 1));
            _state.Stun = false; _state.Freeze = true;
            Assert.AreEqual(CastResult.Frozen, _c.TryCast(Cleaving(), 1));
            Assert.AreEqual(CastResult.Frozen, _c.TryCast(Leap(), 1));
            _state.Freeze = false; _state.Silence = true;
            Assert.AreEqual(CastResult.Silenced, _c.TryCast(Cleaving(), 1));
            Assert.AreEqual(CastResult.Silenced, _c.TryCast(Leap(), 1));
            Assert.AreEqual(100f, _res.Value);
        }

        [Test]
        public void Root_BlocksOnlyMovementSkills()
        {
            _state.Root = true;
            Assert.AreEqual(CastResult.Rooted, _c.TryCast(Leap(), 1));
            Assert.AreEqual(CastResult.Started, _c.TryCast(Cleaving(), 2));
        }

        [Test]
        public void Phases_FollowFramesAndAnnounceEachEntry()
        {
            Assert.AreEqual(CastResult.Started, _c.TryCast(Cleaving(), 10)); // windup 3, active 2, recovery 4
            var seen = new List<string> { _c.Phase + ":" + _c.PhaseFrame };
            for (int f = 11; f <= 18; f++)
            {
                _c.Tick(f);
                seen.Add(_c.Phase + ":" + _c.PhaseFrame);
            }
            CollectionAssert.AreEqual(new[]
            {
                "Windup:1", "Windup:2", "Windup:3", "Active:1", "Active:2", "Recovery:1", "Recovery:2", "Recovery:3", "Recovery:4",
            }, seen);
            _c.Tick(19);
            Assert.IsFalse(_c.IsCasting);
            CollectionAssert.AreEqual(new[] { "Windup@10", "Active@13", "Recovery@15", "end@19" }, _runner.Log);
            Assert.AreEqual(1, _events.Count<OnCastEnd>());
        }

        [Test]
        public void ZeroWindup_EntersActiveInsideTryCast()
        {
            SkillSpec s = Builder("skill.test.zero", SkillSlot.R, "Attack", w: 0, a: 2, r: 1).Build(null);
            _c.TryCast(s, 5);
            Assert.AreEqual(SkillPhase.Active, _c.Phase);
            CollectionAssert.AreEqual(new[] { "Active@5" }, _runner.Log);
        }

        [Test]
        public void LateTick_DoesNotSkipPhaseAnnouncements()
        {
            _c.TryCast(Cleaving(), 0);
            _c.Tick(50);
            CollectionAssert.AreEqual(new[] { "Windup@0", "Active@50", "Recovery@50", "end@50" }, _runner.Log);
        }

        [Test]
        public void BasicToSkill_AsksCancelTableAndCancelsAttack()
        {
            _attacks.Allow = false;
            Assert.AreEqual(CastResult.Busy, _c.TryCast(Cleaving(), 1));
            Assert.AreEqual(ActionKind.Skill, _attacks.LastAsked);
            Assert.AreEqual(0, _attacks.Cancels);
            Assert.AreEqual(100f, _res.Value);

            _attacks.Allow = true;
            Assert.AreEqual(CastResult.Started, _c.TryCast(Cleaving(), 2));
            Assert.AreEqual(1, _attacks.Cancels);
        }

        [Test]
        public void Ultimate_AsksTheTableAsUltimate()
        {
            _c.TryCast(Ultimate(), 1);
            Assert.AreEqual(ActionKind.Ultimate, _attacks.LastAsked);
        }

        [Test]
        public void SkillCannotInterruptAnotherSkill()
        {
            _c.TryCast(Cleaving(), 1);
            Assert.AreEqual(CastResult.Busy, _c.TryCast(Leap(), 3));
        }

        [Test]
        public void SkillToDodge_OnlyInRecovery()
        {
            _c.TryCast(Cleaving(), 0);
            Assert.IsFalse(_c.CanCancelIntoDodge()); // windup
            _c.Tick(3); // active
            Assert.IsFalse(_c.CanCancelIntoDodge());
            _c.Tick(5); // recovery frame 1
            Assert.IsTrue(_c.CanCancelIntoDodge());
            Assert.IsTrue(_c.CancelIntoDodge(5));
            Assert.IsFalse(_c.IsCasting);
            Assert.IsTrue(_runner.Log.Contains("cancel@5"));
            Assert.AreEqual(1, _events.Count<OnCastEnd>());
        }

        [Test]
        public void SkillToDodge_BlockedByCommit()
        {
            SkillSpec s = Builder("skill.test.commit", SkillSlot.R, "Attack", commit: true).Build(null);
            _c.TryCast(s, 0);
            _c.Tick(6);
            Assert.AreEqual(SkillPhase.Recovery, _c.Phase);
            Assert.IsFalse(_c.CanCancelIntoDodge());
            Assert.IsFalse(_c.CancelIntoDodge(6));
            Assert.IsTrue(_c.IsCasting);
        }

        [Test]
        public void Ultimate_IsNeverCancelable()
        {
            _c.TryCast(Ultimate(commit: false), 0);
            _c.Tick(6);
            Assert.AreEqual(SkillPhase.Recovery, _c.Phase);
            Assert.IsFalse(_c.CanCancelIntoDodge());
        }

        [Test]
        public void ReportHit_PublishesOncePerTargetPerUse()
        {
            _c.TryCast(Cleaving(), 0);
            Assert.IsTrue(_c.ReportHit(new EntityId(7), 123f, 3));
            Assert.IsFalse(_c.ReportHit(new EntityId(7), 50f, 4));
            Assert.IsTrue(_c.ReportHit(new EntityId(8), 10f, 4));
            Assert.AreEqual(2, _events.Count<OnSkillHit>());
        }

        [Test]
        public void PrepareDamage_UsesGenerationZeroAndSkillTags()
        {
            SkillSpec s = Cleaving();
            _c.TryCast(s, 0);
            var d = new DamageInstance { Generation = 2 };
            _c.PrepareDamage(d);
            Assert.AreEqual(0, d.Generation);
            Assert.AreEqual(s.Tags, d.Tags);
            Assert.AreEqual(Owner.Value, d.SourceId);
        }

        [Test]
        public void ProcessInput_MapsSlotsAndRequeuesBusyPresses()
        {
            var buffer = new InputBuffer();
            var cache = new Dictionary<SkillSlot, SkillSpec>
            {
                { SkillSlot.Q, Cleaving() }, { SkillSlot.Shift, Leap() },
            };
            var source = new DictSource(cache);

            buffer.Push(InputActionId.Skill1, 10);
            Assert.IsTrue(_c.ProcessInput(buffer, source, 10, out CastResult r));
            Assert.AreEqual(CastResult.Started, r);
            Assert.AreEqual(Fury, Fury);
            Assert.AreEqual(80f, _res.Value, 1e-4f);

            // Shift pressed while casting: Busy, stays buffered until it expires (9 frames).
            buffer.Push(InputActionId.Skill4, 12);
            Assert.IsTrue(_c.ProcessInput(buffer, source, 12, out r));
            Assert.AreEqual(CastResult.Busy, r);
            Assert.IsTrue(buffer.Contains(InputActionId.Skill4, 15));
            Assert.IsFalse(buffer.Contains(InputActionId.Skill4, 21));

            // Slot without a skill: consumed, NoSkill.
            buffer.Push(InputActionId.Skill2, 30);
            _c.Interrupt(30);
            Assert.IsTrue(_c.ProcessInput(buffer, source, 30, out r));
            Assert.AreEqual(CastResult.NoSkill, r);
        }

        [Test]
        public void SlotBinding_MapsQERShiftF()
        {
            Assert.IsTrue(SkillSlotBinding.TryGetSlot(InputActionId.Skill1, out var q) && q == SkillSlot.Q);
            Assert.IsTrue(SkillSlotBinding.TryGetSlot(InputActionId.Skill2, out var e) && e == SkillSlot.E);
            Assert.IsTrue(SkillSlotBinding.TryGetSlot(InputActionId.Skill3, out var rr) && rr == SkillSlot.R);
            Assert.IsTrue(SkillSlotBinding.TryGetSlot(InputActionId.Skill4, out var sh) && sh == SkillSlot.Shift);
            Assert.IsTrue(SkillSlotBinding.TryGetSlot(InputActionId.Ultimate, out var f) && f == SkillSlot.F);
            Assert.IsFalse(SkillSlotBinding.TryGetSlot(InputActionId.Basic, out _));
            Assert.IsFalse(SkillSlotBinding.TryGetInput(SkillSlot.Heavy, out _));
        }

        [Test]
        public void ResourceGain_AppliesOnCast()
        {
            var b = Builder("skill.test.leap", SkillSlot.Shift, "Attack|Movement");
            b.ResourceGain = 15f;
            _c.TryCast(b.Build(null), 0);
            Assert.AreEqual(15f, _res.Gained, 1e-4f);
        }

        [Test]
        public void Builder_AppliesModifiersByPhaseThenChannelThenKey_AndIsRepeatable()
        {
            var order = new List<string>();
            SkillSpecBuilder b = Builder("skill.test.mod", SkillSlot.Q, "Attack");
            b.AddModifier(new Op(SkillBuildPhase.Resource, SkillModifierChannel.Item, "a", x => { order.Add("6"); x.CostAmount += 1; }));
            b.AddModifier(new Op(SkillBuildPhase.Parameters, SkillModifierChannel.Run, "a", x => order.Add("3run")));
            b.AddModifier(new Op(SkillBuildPhase.Parameters, SkillModifierChannel.Item, "b", x => order.Add("3itemB")));
            b.AddModifier(new Op(SkillBuildPhase.Parameters, SkillModifierChannel.Item, "a", x => order.Add("3itemA")));
            b.AddModifier(new Op(SkillBuildPhase.Conversion, SkillModifierChannel.Set, "z", x => order.Add("1")));

            SkillSpec s1 = b.Build(null);
            SkillSpec s2 = b.Build(null);
            CollectionAssert.AreEqual(new[] { "1", "3itemA", "3itemB", "3run", "6" }, order.GetRange(0, 5));
            Assert.AreEqual(s1.CostAmount, s2.CostAmount); // base builder untouched: Build is repeatable
        }

        private sealed class DictSource : ISkillSpecSource
        {
            private readonly Dictionary<SkillSlot, SkillSpec> _map;
            public DictSource(Dictionary<SkillSlot, SkillSpec> map) { _map = map; }
            public SkillSpec Get(SkillSlot slot) => _map.TryGetValue(slot, out var s) ? s : null;
        }
    }
}
