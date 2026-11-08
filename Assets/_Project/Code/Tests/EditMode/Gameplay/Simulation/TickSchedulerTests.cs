using System;
using System.Collections.Generic;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.Gameplay.Simulation
{
    public class TickSchedulerTests
    {
        private sealed class Counter : IFixedTickable
        {
            public readonly List<int> Frames = new List<int>();
            public string Name;
            public List<string> Log;
            public Action OnTick;

            public void Tick(in SimulationFrame f)
            {
                Frames.Add(f.Index);
                if (Log != null) Log.Add(Name);
                if (OnTick != null) OnTick();
            }
        }

        private sealed class Gate : IFixedTickable
        {
            private readonly EntityClock _clock;
            private readonly Counter _inner;
            public Gate(EntityClock c, Counter inner) { _clock = c; _inner = inner; }
            public void Tick(in SimulationFrame f) { if (_clock.Advance()) _inner.Tick(in f); }
        }

        [Test]
        public void StepManual60_CallsEachTickable60TimesWithFrames1To60()
        {
            var s = new TickScheduler();
            var a = new Counter();
            var b = new Counter();
            s.Register(a);
            s.Register(b);
            s.StepManual(60);
            Assert.AreEqual(60, a.Frames.Count);
            Assert.AreEqual(60, b.Frames.Count);
            Assert.AreEqual(1, a.Frames[0]);
            Assert.AreEqual(60, a.Frames[59]);
            Assert.AreEqual(60, s.CurrentFrame);
        }

        [Test]
        public void Order_IsPriorityThenRegistration()
        {
            var s = new TickScheduler();
            var log = new List<string>();
            s.Register(new Counter { Name = "p0a", Log = log }, 0);
            s.Register(new Counter { Name = "m1", Log = log }, -1);
            s.Register(new Counter { Name = "p0b", Log = log }, 0);
            s.StepManual(1);
            CollectionAssert.AreEqual(new[] { "m1", "p0a", "p0b" }, log);
        }

        [Test]
        public void RegisteredDuringTick_EntersNextTick()
        {
            var s = new TickScheduler();
            var late = new Counter();
            var reg = new Counter();
            reg.OnTick = () => { if (reg.Frames.Count == 1) s.Register(late); };
            s.Register(reg);
            s.StepManual(3);
            CollectionAssert.AreEqual(new[] { 2, 3 }, late.Frames);
        }

        [Test]
        public void UnregisterDuringTick_StopsAfterCurrentTick()
        {
            var s = new TickScheduler();
            var a = new Counter();
            a.OnTick = () => s.Unregister(a);
            s.Register(a);
            s.StepManual(3);
            Assert.AreEqual(1, a.Frames.Count);
        }

        [Test]
        public void Advance_RunsFixedTicksFromRealTime()
        {
            var s = new TickScheduler();
            Assert.AreEqual(3, s.Advance(3.5f / 60f));
        }

        [Test]
        public void EntityClock_Freeze5_SkipsExactly5Ticks()
        {
            var s = new TickScheduler();
            var clock = new EntityClock();
            var c = new Counter();
            var other = new Counter();
            s.Register(new Gate(clock, c));
            s.Register(other);
            clock.Freeze(5);
            s.StepManual(10);
            CollectionAssert.AreEqual(new[] { 6, 7, 8, 9, 10 }, c.Frames);
            Assert.AreEqual(10, other.Frames.Count);
            Assert.AreEqual(5, clock.LocalFrame);
        }

        [Test]
        public void EntityClock_OverlappingFreeze_KeepsLongest()
        {
            var c = new EntityClock();
            c.Freeze(5);
            c.Freeze(3);
            Assert.AreEqual(5, c.FrozenFramesRemaining);
        }
    }
}
