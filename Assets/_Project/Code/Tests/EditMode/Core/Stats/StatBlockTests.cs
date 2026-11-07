using Eclipse.Core.Tags;
using NUnit.Framework;

namespace Eclipse.Core.Stats.Tests
{
    [TestFixture]
    public class StatBlockTests
    {
        private sealed class Conds : IConditionEvaluator
        {
            public bool Met;
            public bool IsMet(ConditionId condition) => Met;
        }

        private StatBlock _block;

        [SetUp]
        public void SetUp() { _block = new StatBlock(Rules01.Create()); }

        [Test]
        public void Formula_BaseFlatIncreasedMore()
        {
            _block.SetBase(StatId.Armor, 100f);
            var s = _block.CreateSource();
            _block.AddSource(s, new[]
            {
                new StatMod(StatId.Armor, ModBucket.Flat, 50f),
                new StatMod(StatId.Armor, ModBucket.Increased, 0.2f),
                new StatMod(StatId.Armor, ModBucket.Increased, 0.3f),
                new StatMod(StatId.Armor, ModBucket.More, 0.1f),
                new StatMod(StatId.Armor, ModBucket.More, 0.1f),
            });
            // (100 + 50) * (1 + 0.5) * 1.1 * 1.1
            Assert.AreEqual(150f * 1.5f * 1.21f, _block.Evaluate(StatId.Armor), 1e-3f);
        }

        [Test]
        public void AttackSpeed_SoftAndHardCap()
        {
            var a = _block.CreateSource();
            _block.AddMod(a, new StatMod(StatId.AttackSpeed, ModBucket.Flat, 0.80f));
            Assert.AreEqual(0.65f, _block.Evaluate(StatId.AttackSpeed), 1e-5f);
            var b = _block.CreateSource();
            _block.AddMod(b, new StatMod(StatId.AttackSpeed, ModBucket.Flat, 1.20f));
            Assert.AreEqual(1.00f, _block.Evaluate(StatId.AttackSpeed), 1e-5f); // raw 2.0
        }

        [Test]
        public void CdrAndDodge_ThroughBlock()
        {
            _block.AddMod(_block.CreateSource(), new StatMod(StatId.CooldownReduction, ModBucket.Flat, 0.60f));
            _block.AddMod(_block.CreateSource(), new StatMod(StatId.DodgeChance, ModBucket.Flat, 0.30f));
            Assert.AreEqual(0.40f, _block.Evaluate(StatId.CooldownReduction), 1e-5f); // 0.25 + 0.35*0.5 = 0.425 -> cap
            Assert.AreEqual(0.25f, _block.Evaluate(StatId.DodgeChance), 1e-5f);
        }

        [Test]
        public void RemoveSource_RemovesOnlyThatSource()
        {
            var a = _block.CreateSource();
            var b = _block.CreateSource();
            _block.AddMod(a, new StatMod(StatId.MaximumHealth, ModBucket.Flat, 100f));
            _block.AddMod(b, new StatMod(StatId.MaximumHealth, ModBucket.Flat, 40f));
            Assert.AreEqual(140f, _block.Evaluate(StatId.MaximumHealth));
            Assert.IsTrue(_block.RemoveSource(a));
            Assert.AreEqual(40f, _block.Evaluate(StatId.MaximumHealth));
            Assert.IsFalse(_block.RemoveSource(a));
        }

        [Test]
        public void Dirty_PerStat()
        {
            _block.AddMod(_block.CreateSource(), new StatMod(StatId.Armor, ModBucket.Flat, 1f));
            _block.Evaluate(StatId.Armor);
            _block.Evaluate(StatId.MaximumHealth);
            Assert.IsFalse(_block.IsDirty(StatId.Armor));
            Assert.IsFalse(_block.IsDirty(StatId.MaximumHealth));
            _block.AddMod(_block.CreateSource(), new StatMod(StatId.Armor, ModBucket.Flat, 1f));
            Assert.IsTrue(_block.IsDirty(StatId.Armor));
            Assert.IsFalse(_block.IsDirty(StatId.MaximumHealth));
        }

        [Test]
        public void StatsChanged_MaskAndOneEventPerBatch()
        {
            int calls = 0;
            ulong bits = 0;
            _block.Changed.Subscribe((in StatsChanged e) => { calls++; bits = e.Mask.Bits; });
            var s = _block.CreateSource();
            _block.AddSource(s, new[]
            {
                new StatMod(StatId.Armor, ModBucket.Flat, 1f),
                new StatMod(StatId.Dexterity, ModBucket.Flat, 1f),
            });
            Assert.AreEqual(1, calls);
            Assert.AreEqual(StatMask.Of(StatId.Armor).Bits | StatMask.Of(StatId.Dexterity).Bits, bits);
            _block.RemoveSource(s);
            Assert.AreEqual(2, calls);
            _block.SetBase(StatId.Strength, 5f);
            Assert.AreEqual(3, calls);
            Assert.AreEqual(StatMask.Of(StatId.Strength).Bits, bits);
            _block.RemoveSource(s); // nothing owned -> no event
            Assert.AreEqual(3, calls);
        }

        [Test]
        public void TagFilter_RequiredAndExcluded()
        {
            var ember = new TagMask(Tag.Ember);
            var s = _block.CreateSource();
            _block.AddSource(s, new[]
            {
                new StatMod(StatId.EmberDamage, ModBucket.Increased, 0.5f, requiredTags: ember),
                new StatMod(StatId.EmberDamage, ModBucket.Increased, 0.25f, excludedTags: new TagMask(Tag.DoT)),
            });
            Assert.AreEqual(0f, _block.Evaluate(StatId.EmberDamage, TagMask.None, null), 1e-6f); // base 0 -> 0
            _block.SetBase(StatId.EmberDamage, 1f);
            Assert.AreEqual(1.25f, _block.Evaluate(StatId.EmberDamage), 1e-5f);           // only the unfiltered-excluded one
            Assert.AreEqual(1.75f, _block.Evaluate(StatId.EmberDamage, ember, null), 1e-5f);
            Assert.AreEqual(1.75f, _block.Evaluate(StatId.EmberDamage, ember | new TagMask(Tag.Spell), null), 1e-5f);
            Assert.AreEqual(1.5f, _block.Evaluate(StatId.EmberDamage, ember | new TagMask(Tag.DoT), null), 1e-5f);
        }

        [Test]
        public void Condition_RequiresEvaluatorAndMet()
        {
            _block.SetBase(StatId.CriticalDamage, 1f);
            var cond = new ConditionId(7);
            _block.AddMod(_block.CreateSource(), new StatMod(StatId.CriticalDamage, ModBucket.Increased, 0.5f, condition: cond));
            var ev = new Conds { Met = false };
            Assert.AreEqual(1f, _block.Evaluate(StatId.CriticalDamage, TagMask.None, ev), 1e-6f);
            Assert.AreEqual(1f, _block.Evaluate(StatId.CriticalDamage));
            ev.Met = true;
            Assert.AreEqual(1.5f, _block.Evaluate(StatId.CriticalDamage, TagMask.None, ev), 1e-6f);
            Assert.AreEqual(1f, _block.Evaluate(StatId.CriticalDamage)); // cached context-free value ignores conditions
        }

        [Test]
        public void ExternalCap_RawThenApply()
        {
            // Elemental resistance: cap applies after a caller-side penalty, so the block has no rule for it.
            _block.AddMod(_block.CreateSource(), new StatMod(StatId.ElementalResistance, ModBucket.Flat, 0.90f));
            float raw = _block.Evaluate(StatId.ElementalResistance);
            Assert.AreEqual(0.90f, raw, 1e-6f);
            Assert.AreEqual(0.75f, DiminishingReturns.Apply(raw - 0.05f, new StatRule(null, 0.75f)), 1e-6f);
        }

#if !UNITY_5_3_OR_NEWER
        [Test]
        public void Evaluate_Cached_DoesNotAllocate()
        {
            _block.AddMod(_block.CreateSource(), new StatMod(StatId.AttackSpeed, ModBucket.Flat, 0.8f));
            _block.Evaluate(StatId.AttackSpeed);
            var tags = new TagMask(Tag.Attack);
            for (int i = 0; i < 50; i++) { _block.Evaluate(StatId.AttackSpeed); _block.Evaluate(StatId.AttackSpeed, tags, null); }
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                _block.Evaluate(StatId.AttackSpeed);
                _block.Evaluate(StatId.AttackSpeed, tags, null);
            }
            long delta = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0, delta);
        }
#endif
    }
}
