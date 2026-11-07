using Eclipse.Core.Tags;
using NUnit.Framework;

namespace Eclipse.Core.Procs.Tests
{
    [TestFixture]
    public class ProcRulesTests
    {
        // Values from 06 3.2 (R07): Echo 0.5, Clone 0.4, persistent area tick 0.25.
        private readonly ProcRules _rules = new ProcRules(new ProcRulesConfig(0.5f, 0.4f, 0.25f));
        private static readonly SourceChain Chain = SourceChain.FromSlug("test-source");

        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, false)] // 06 3.2: nothing triggers at Gen >= 2
        public void Gen2_DoesNotTrigger(int gen, bool expected)
        {
            var ctx = new ProcContext(gen, HitFlags.None, Chain, 1f);
            Assert.AreEqual(expected, _rules.CanTrigger(ctx));
        }

        [Test]
        public void NextGeneration_IsPlusOneCappedAtTwo()
        {
            Assert.AreEqual(1, _rules.NextGeneration(ProcContext.Original(Chain)));
            Assert.AreEqual(2, _rules.NextGeneration(new ProcContext(1, HitFlags.None, Chain, 1f)));
            Assert.AreEqual(2, _rules.NextGeneration(new ProcContext(2, HitFlags.None, Chain, 1f)));
        }

        [Test]
        public void Replica_OfOriginal_IsAllowed_AndIsEchoGen1()
        {
            var orig = ProcContext.Original(Chain);
            Assert.IsTrue(_rules.CanReplicate(orig));
            var echo = _rules.CreateReplica(orig, DerivedProcKind.Echo, 7UL);
            Assert.IsTrue(echo.IsEcho);
            Assert.AreEqual(1, echo.Generation);
            Assert.AreEqual(2, echo.SourceChain.Length);
        }

        [Test]
        public void ReplicaOfReplica_BlockedWithoutEchoRecursion()
        {
            var echo = _rules.CreateReplica(ProcContext.Original(Chain), DerivedProcKind.Echo, 1UL);
            Assert.IsFalse(_rules.CanReplicate(echo)); // 06 3.2: replicas never spawn replicas
        }

        [Test]
        public void ReplicaOfReplica_OnlyOnce_WithEchoRecursion1x_AndStillGenBelow2()
        {
            var orig = ProcContext.Original(Chain, 1f, HitFlags.None, allowEchoRecursionOnce: true);
            var echo1 = _rules.CreateReplica(orig, DerivedProcKind.Echo, 1UL);   // Gen 1
            Assert.IsTrue(_rules.CanReplicate(echo1));                          // the single recursion
            var echo2 = _rules.CreateReplica(echo1, DerivedProcKind.Echo, 2UL);  // Gen 2
            Assert.IsTrue(echo2.EchoRecursionUsed);
            Assert.IsFalse(_rules.CanReplicate(echo2));                         // never recursive on itself
            // Even with the recursion unspent, Gen >= 2 blocks.
            var late = new ProcContext(2, HitFlags.IsEcho, Chain, 1f, true, false);
            Assert.IsFalse(_rules.CanReplicate(late));
        }

        [Test]
        public void DerivedCoefficients_MultiplySourceCoefficient()
        {
            Assert.AreEqual(0.5f, _rules.DerivedProcCoefficient(DerivedProcKind.Echo, 1f), 1e-6f);
            Assert.AreEqual(0.4f, _rules.DerivedProcCoefficient(DerivedProcKind.Clone, 1f), 1e-6f);
            Assert.AreEqual(0.25f, _rules.DerivedProcCoefficient(DerivedProcKind.AreaTick, 1f), 1e-6f);
            Assert.AreEqual(0.25f, _rules.DerivedProcCoefficient(DerivedProcKind.Echo, 0.5f), 1e-6f);
            Assert.AreEqual(0.2f, _rules.DerivedProcCoefficient(DerivedProcKind.Clone, 0.5f), 1e-6f);
            Assert.AreEqual(0.125f, _rules.DerivedProcCoefficient(DerivedProcKind.AreaTick, 0.5f), 1e-6f);
        }

        [Test]
        public void CreateProc_IncrementsGenerationAndExtendsChain()
        {
            var p = _rules.CreateProc(ProcContext.Original(Chain), 99UL);
            Assert.AreEqual(1, p.Generation);
            Assert.IsTrue(p.Flags.Has(HitFlags.IsTriggered));
            Assert.IsTrue(p.SourceChain.Contains(99UL));
            Assert.AreEqual(2, p.SourceChain.Length);
        }

        [Test]
        public void SourceChain_CapsAtThree()
        {
            var c = SourceChain.Empty.Append(1).Append(2).Append(3).Append(4);
            Assert.AreEqual(3, c.Length);
            Assert.IsFalse(c.Contains(4));
        }
    }
}
