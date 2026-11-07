using System;
using NUnit.Framework;

namespace Eclipse.Core.Events.Tests
{
    public class FrameEventBufferTests
    {
        private static TestEvent Ev(int a) => new TestEvent { A = a };

        [Test]
        public void InvalidCapacityThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FrameEventBuffer<TestEvent>(0));
        }

        [Test]
        public void FifoOrder()
        {
            var b = new FrameEventBuffer<TestEvent>(4);
            b.Push(Ev(1)); b.Push(Ev(2)); b.Push(Ev(3));
            Assert.AreEqual(3, b.Count);
            Assert.AreEqual(4, b.Capacity);
            Assert.IsTrue(b.TryPop(out var e)); Assert.AreEqual(1, e.A);
            Assert.IsTrue(b.TryPop(out e)); Assert.AreEqual(2, e.A);
            Assert.IsTrue(b.TryPop(out e)); Assert.AreEqual(3, e.A);
            Assert.IsFalse(b.TryPop(out e));
            Assert.AreEqual(0, b.Count);
        }

        [Test]
        public void WrapAroundKeepsOrder()
        {
            var b = new FrameEventBuffer<TestEvent>(3);
            b.Push(Ev(1)); b.Push(Ev(2)); b.Push(Ev(3));
            b.TryPop(out _); b.TryPop(out _);
            b.Push(Ev(4)); b.Push(Ev(5));
            Assert.AreEqual(3, b.Count);
            Assert.AreEqual(3, b[0].A);
            Assert.AreEqual(4, b[1].A);
            Assert.AreEqual(5, b[2].A);
            Assert.AreEqual(0, b.DroppedCount);
        }

        [Test]
        public void OverflowOverwritesOldestAndCountsDropped()
        {
            var b = new FrameEventBuffer<TestEvent>(3);
            for (int i = 1; i <= 5; i++) b.Push(Ev(i));
            Assert.AreEqual(3, b.Count);
            Assert.AreEqual(2, b.DroppedCount);
            Assert.AreEqual(3, b[0].A);
            Assert.AreEqual(4, b[1].A);
            Assert.AreEqual(5, b[2].A);
            b.TryPop(out var e);
            Assert.AreEqual(3, e.A);
        }

        [Test]
        public void IndexerOutOfRangeThrows()
        {
            var b = new FrameEventBuffer<TestEvent>(2);
            b.Push(Ev(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = b[1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = b[-1]; });
        }

        [Test]
        public void ClearEmptiesAndResetsDropped()
        {
            var b = new FrameEventBuffer<TestEvent>(2);
            b.Push(Ev(1)); b.Push(Ev(2)); b.Push(Ev(3));
            b.Clear();
            Assert.AreEqual(0, b.Count);
            Assert.AreEqual(0, b.DroppedCount);
            Assert.IsFalse(b.TryPop(out _));
            b.Push(Ev(9));
            Assert.AreEqual(9, b[0].A);
        }

#if !UNITY_5_3_OR_NEWER
        [Test]
        public void PushPopClearAllocateNothing()
        {
            var b = new FrameEventBuffer<TestEvent>(8);
            var ev = new TestEvent { A = 1 };
            for (int i = 0; i < 10; i++) { b.Push(in ev); b.TryPop(out _); }

            long before = GC.GetAllocatedBytesForCurrentThread();
            TestEvent e = default;
            bool ok = true;
            for (int i = 0; i < 1000; i++)
            {
                b.Push(in ev);
                ok &= b.TryPop(out e);
                if (i % 100 == 0) b.Clear();
            }
            long delta = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0, delta);
            Assert.IsTrue(ok);
            Assert.AreEqual(1, e.A);
        }
#endif
    }
}
