using NUnit.Framework;

namespace Eclipse.Core.Random.Tests
{
    public class SplitMix64Tests
    {
        [Test]
        public void Known_vector_seed_1234567()
        {
            ulong s = 1234567UL;
            Assert.AreEqual(6457827717110365317UL, SplitMix64.Next(ref s));
            Assert.AreEqual(3203168211198807973UL, SplitMix64.Next(ref s));
            Assert.AreEqual(9817491932198370423UL, SplitMix64.Next(ref s));
            Assert.AreEqual(4593380528125082431UL, SplitMix64.Next(ref s));
            Assert.AreEqual(16408922859458223821UL, SplitMix64.Next(ref s));
        }
    }
}
