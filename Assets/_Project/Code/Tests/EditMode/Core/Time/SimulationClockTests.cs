using System;
using NUnit.Framework;

namespace Eclipse.Core.Time.Tests
{
    public class SimulationClockTests
    {
        [Test]
        public void FramesToSeconds_Values()
        {
            Assert.AreEqual(0.2f, SimulationClock.FramesToSeconds(12));
            Assert.AreEqual(1f, SimulationClock.FramesToSeconds(60));
            Assert.AreEqual(0f, SimulationClock.FramesToSeconds(0));
        }

        [TestCase(0.2f, 12)]
        [TestCase(0.1f, 6)]
        [TestCase(1f / 60f, 1)]
        [TestCase(0.0083f, 0)]
        [TestCase(0.0084f, 1)]
        [TestCase(0f, 0)]
        [TestCase(1f, 60)]
        public void SecondsToFrames_Table(float seconds, int expected)
        {
            Assert.AreEqual(expected, SimulationClock.SecondsToFrames(seconds));
        }

        [Test]
        public void SecondsToFrames_HalfRoundsAwayFromZero()
        {
            Assert.AreEqual(1, SimulationClock.SecondsToFrames(0.5f / 60f));
            Assert.AreEqual(3, SimulationClock.SecondsToFrames(2.5f / 60f));
        }

        [Test]
        public void SecondsToFrames_InvalidThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SimulationClock.SecondsToFrames(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => SimulationClock.SecondsToFrames(float.NaN));
        }

        [Test]
        public void Advance_OneSecond_Is60Ticks()
        {
            var c = new SimulationClock(120);
            Assert.AreEqual(60, c.Advance(1f));
            Assert.AreEqual(60, c.Tick);
            Assert.AreEqual(1.0, c.ElapsedSeconds, 1e-12);
        }

        [Test]
        public void Advance_KeepsRemainder()
        {
            var c = new SimulationClock();
            int total = c.Advance(0.01f) + c.Advance(0.01f) + c.Advance(0.01f);
            Assert.AreEqual(1, total);
            Assert.AreEqual(0.8f, c.Alpha, 0.01f); // 0.0133 / 0.016667
        }

        [Test]
        public void Advance_60SmallSteps_Is60Ticks()
        {
            var c = new SimulationClock();
            int total = 0;
            for (int i = 0; i < 60; i++) total += c.Advance(1f / 60f);
            Assert.AreEqual(60, total);
            Assert.AreEqual(60, c.Tick);
        }

        [Test]
        public void Advance_CapsAndCountsDropped()
        {
            var c = new SimulationClock(8);
            Assert.AreEqual(8, c.Advance(1f));
            Assert.AreEqual(8, c.Tick);
            Assert.AreEqual(52, c.DroppedTicks);
            Assert.AreEqual(0, c.Advance(0f));
        }

        [Test]
        public void Alpha_StaysInRange()
        {
            var c = new SimulationClock();
            Assert.AreEqual(0f, c.Alpha);
            for (int i = 0; i < 500; i++)
            {
                c.Advance(0.0071f);
                Assert.GreaterOrEqual(c.Alpha, 0f);
                Assert.Less(c.Alpha, 1f);
            }
        }

        [Test]
        public void Advance_InvalidThrows()
        {
            var c = new SimulationClock();
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Advance(-0.01f));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Advance(float.NaN));
        }

        [Test]
        public void Ctor_InvalidMaxThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationClock(0));
        }

        [Test]
        public void Reset_ClearsState()
        {
            var c = new SimulationClock(8);
            c.Advance(1f);
            c.Advance(0.01f);
            c.Reset();
            Assert.AreEqual(0, c.Tick);
            Assert.AreEqual(0, c.DroppedTicks);
            Assert.AreEqual(0f, c.Alpha);
            Assert.AreEqual(0.0, c.ElapsedSeconds);
        }

        [Test]
        public void Advance_RejectsInfinity()
        {
            var c = new SimulationClock();
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Advance(float.PositiveInfinity));
        }

        [Test]
        public void Advance_HugeDelta_SaturatesWithoutCorruptingState()
        {
            var c = new SimulationClock(8);
            Assert.AreEqual(8, c.Advance(float.MaxValue));
            Assert.Greater(c.DroppedTicks, 0L);
            Assert.That(c.Alpha, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            Assert.AreEqual(1, c.Advance(1f / 60f));
            Assert.AreEqual(9L, c.Tick);
        }
    }
}
