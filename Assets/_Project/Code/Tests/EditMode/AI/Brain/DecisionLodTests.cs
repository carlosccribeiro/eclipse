using Eclipse.AI.Lod;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.AI.Brain
{
    public class DecisionLodTests
    {
        private static int Count(DecisionLod lod, int ticks, float dist, bool onScreen)
        {
            int c = 0;
            for (int i = 0; i < ticks; i++) if (lod.ShouldDecide(BrainTestKit.Dt, dist, onScreen)) c++;
            return c;
        }

        [Test]
        public void Near_DecidesAt10Hz_Far_At4Hz()
        {
            Assert.That(Count(new DecisionLod(BrainTestKit.MakeLod()), 600, 5f, false), Is.EqualTo(100));
            Assert.That(Count(new DecisionLod(BrainTestKit.MakeLod()), 600, 50f, false), Is.EqualTo(40));
        }

        [Test]
        public void OnScreen_CountsAsNear_EvenBeyondRadius()
        {
            Assert.That(Count(new DecisionLod(BrainTestKit.MakeLod()), 600, 50f, true), Is.EqualTo(100));
        }

        [Test]
        public void LeavingRadius_SwitchesFrom10To4Hz()
        {
            var lod = new DecisionLod(BrainTestKit.MakeLod());
            Assert.That(Count(lod, 120, 5f, false), Is.EqualTo(20));
            Assert.That(lod.CurrentHz, Is.EqualTo(10f));
            Assert.That(Count(lod, 120, 50f, false), Is.EqualTo(8));
            Assert.That(lod.CurrentHz, Is.EqualTo(4f));
            Assert.That(lod.IsNear, Is.False);
        }
    }
}
