using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Eclipse.Core.Events.Tests
{
    public struct TestEvent
    {
        public int A;
        public float B;
        public long C;
    }

    public class EventBusTests
    {
        private readonly List<string> _log = new List<string>();
        private EventBus<TestEvent> _bus;

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _bus = new EventBus<TestEvent>();
        }

        private InEventHandler<TestEvent> Logger(string name) => (in TestEvent e) => _log.Add(name);

        private static TestEvent Ev(int a = 0) => new TestEvent { A = a };

        [Test]
        public void HigherPriorityRunsFirst()
        {
            _bus.Subscribe(Logger("low"), -5);
            _bus.Subscribe(Logger("high"), 10);
            _bus.Subscribe(Logger("mid"), 0);
            _bus.Publish(Ev());
            Assert.AreEqual(new[] { "high", "mid", "low" }, _log.ToArray());
        }

        [Test]
        public void EqualPriorityKeepsSubscriptionOrder()
        {
            _bus.Subscribe(Logger("a"), 1);
            _bus.Subscribe(Logger("b"), 0);
            _bus.Subscribe(Logger("c"), 1);
            _bus.Subscribe(Logger("d"), 0);
            _bus.Publish(Ev());
            Assert.AreEqual(new[] { "a", "c", "b", "d" }, _log.ToArray());
        }

        [Test]
        public void NullHandlerThrows()
        {
            Assert.Throws<ArgumentNullException>(() => _bus.Subscribe(null));
        }

        [Test]
        public void SameHandlerTwiceIsTwoSubscriptions()
        {
            var h = Logger("x");
            var t1 = _bus.Subscribe(h);
            var t2 = _bus.Subscribe(h);
            Assert.AreNotEqual(t1, t2);
            Assert.AreEqual(2, _bus.Count);
            _bus.Publish(Ev());
            Assert.AreEqual(2, _log.Count);
            Assert.IsTrue(_bus.Unsubscribe(t1));
            _bus.Publish(Ev());
            Assert.AreEqual(3, _log.Count);
        }

        [Test]
        public void UnsubscribeStopsDeliveryAndInvalidTokensReturnFalse()
        {
            var t = _bus.Subscribe(Logger("a"));
            Assert.IsTrue(t.IsValid);
            Assert.IsTrue(_bus.Unsubscribe(t));
            Assert.IsFalse(_bus.Unsubscribe(t));
            Assert.IsFalse(_bus.Unsubscribe(default));
            Assert.AreEqual(0, _bus.Count);
            _bus.Publish(Ev());
            Assert.AreEqual(0, _log.Count);
            Assert.IsFalse(default(SubscriptionToken).IsValid);
        }

        [Test]
        public void ClearRemovesEverything()
        {
            _bus.Subscribe(Logger("a"));
            _bus.Subscribe(Logger("b"));
            _bus.Clear();
            Assert.AreEqual(0, _bus.Count);
            _bus.Publish(Ev());
            Assert.AreEqual(0, _log.Count);
        }

        [Test]
        public void InPassesCorrectValue()
        {
            TestEvent got = default;
            _bus.Subscribe((in TestEvent e) => got = e);
            var ev = new TestEvent { A = 7, B = 2.5f, C = 1L << 40 };
            _bus.Publish(in ev);
            Assert.AreEqual(7, got.A);
            Assert.AreEqual(2.5f, got.B);
            Assert.AreEqual(1L << 40, got.C);
        }

        [Test]
        public void HandlerCanUnsubscribeItself()
        {
            SubscriptionToken self = default;
            self = _bus.Subscribe((in TestEvent e) => { _log.Add("self"); _bus.Unsubscribe(self); });
            _bus.Subscribe(Logger("other"));
            _bus.Publish(Ev());
            _bus.Publish(Ev());
            Assert.AreEqual(new[] { "self", "other", "other" }, _log.ToArray());
            Assert.AreEqual(1, _bus.Count);
        }

        [Test]
        public void RemovedLaterHandlerDoesNotRun()
        {
            SubscriptionToken later = default;
            _bus.Subscribe((in TestEvent e) => { _log.Add("first"); _bus.Unsubscribe(later); }, 10);
            later = _bus.Subscribe(Logger("later"), 0);
            _bus.Subscribe(Logger("last"), -10);
            _bus.Publish(Ev());
            Assert.AreEqual(new[] { "first", "last" }, _log.ToArray());
        }

        [Test]
        public void RemovedEarlierHandlerSkipsNoneAndRepeatsNone()
        {
            SubscriptionToken first = default;
            first = _bus.Subscribe(Logger("first"), 10);
            _bus.Subscribe((in TestEvent e) => { _log.Add("second"); _bus.Unsubscribe(first); }, 5);
            _bus.Subscribe(Logger("third"), 0);
            _bus.Subscribe(Logger("fourth"), -5);
            _bus.Publish(Ev());
            Assert.AreEqual(new[] { "first", "second", "third", "fourth" }, _log.ToArray());
            _log.Clear();
            _bus.Publish(Ev());
            Assert.AreEqual(new[] { "second", "third", "fourth" }, _log.ToArray());
        }

        [Test]
        public void SubscribeDuringDispatchRunsOnlyOnNextPublish()
        {
            bool added = false;
            _bus.Subscribe((in TestEvent e) =>
            {
                _log.Add("a");
                if (!added) { added = true; _bus.Subscribe(Logger("new"), 100); }
            });
            _bus.Subscribe(Logger("b"));
            _bus.Publish(Ev());
            Assert.AreEqual(new[] { "a", "b" }, _log.ToArray());
            Assert.AreEqual(3, _bus.Count);
            _log.Clear();
            _bus.Publish(Ev());
            Assert.AreEqual(new[] { "new", "a", "b" }, _log.ToArray());
        }

        [Test]
        public void SubscribeThenUnsubscribeDuringDispatch()
        {
            _bus.Subscribe((in TestEvent e) =>
            {
                var t = _bus.Subscribe(Logger("temp"));
                Assert.IsTrue(_bus.Unsubscribe(t));
            });
            _bus.Publish(Ev());
            _bus.Publish(Ev());
            Assert.AreEqual(0, _log.Count);
            Assert.AreEqual(1, _bus.Count);
        }

        [Test]
        public void ReentrantPublishWorks()
        {
            int depth = 0;
            _bus.Subscribe((in TestEvent e) =>
            {
                _log.Add("a" + e.A);
                if (depth++ == 0) _bus.Publish(Ev(2));
            }, 1);
            _bus.Subscribe((in TestEvent e) => _log.Add("b" + e.A), 0);
            _bus.Publish(Ev(1));
            Assert.AreEqual(new[] { "a1", "a2", "b2", "b1" }, _log.ToArray());
        }

        [Test]
        public void UnsubscribeInsideNestedPublishIsCompactedAfterOuter()
        {
            SubscriptionToken b = default;
            _bus.Subscribe((in TestEvent e) =>
            {
                if (e.A == 1) { _bus.Publish(Ev(2)); _log.Add("a-after"); }
                else _bus.Unsubscribe(b);
            }, 1);
            b = _bus.Subscribe(Logger("b"), 0);
            _bus.Subscribe(Logger("c"), -1);
            _bus.Publish(Ev(1));
            Assert.AreEqual(new[] { "c", "a-after", "c" }, _log.ToArray());
            Assert.AreEqual(2, _bus.Count);
        }

        [Test]
        public void HandlerExceptionPropagatesAndBusStaysUsable()
        {
            bool throwNow = true;
            _bus.Subscribe((in TestEvent e) => { if (throwNow) throw new InvalidOperationException("boom"); }, 10);
            _bus.Subscribe(Logger("b"));
            Assert.Throws<InvalidOperationException>(() => _bus.Publish(Ev()));
            Assert.AreEqual(0, _log.Count);
            // dispatch state recovered: subscribe is immediate again and normal publish works
            throwNow = false;
            _bus.Subscribe(Logger("c"), -1);
            _bus.Publish(Ev());
            Assert.AreEqual(new[] { "b", "c" }, _log.ToArray());
        }

        [Test]
        public void ManySubscriptionsGrowArray()
        {
            int sum = 0;
            for (int i = 0; i < 100; i++)
            {
                int p = i % 5;
                _bus.Subscribe((in TestEvent e) => sum += p);
            }
            _bus.Publish(Ev());
            Assert.AreEqual(100, _bus.Count);
            Assert.AreEqual(20 * (0 + 1 + 2 + 3 + 4), sum);
        }

#if !UNITY_5_3_OR_NEWER
        private int _hits;
        private void H1(in TestEvent e) { _hits += e.A; }
        private void H2(in TestEvent e) { _hits += 2; }
        private static void H3(in TestEvent e) { }

        [Test]
        public void PublishAllocatesNothing()
        {
            var bus = new EventBus<TestEvent>();
            bus.Subscribe(H1, 5);
            bus.Subscribe(H2, 0);
            bus.Subscribe(H3, -5);
            bus.Subscribe(H1, -10);
            var ev = new TestEvent { A = 1 };
            for (int i = 0; i < 10; i++) bus.Publish(in ev);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) bus.Publish(in ev);
            long delta = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0, delta);
            Assert.Greater(_hits, 0);
        }
#endif
    }
}
