using NUnit.Framework;

namespace Eclipse.Core.RunGen.Map.Tests
{
    [TestFixture]
    [Category("RunGen")]
    public class MapGeneratorSmoke
    {
        [Test]
        public void Seed_1_generates_a_valid_map()
        {
            RunMap map = MapGenerator.Generate(1UL, new MapGenContext());
            Assert.AreEqual(14, map.Rows.Length);
            Assert.AreEqual(RoomType.Boss, map.Boss.Type);
            Assert.IsEmpty(MapConstraints.Validate(map));
        }

        [Test]
        public void Same_seed_gives_same_hash()
        {
            Assert.AreEqual(MapGenerator.Generate(7UL).Hash(), MapGenerator.Generate(7UL).Hash());
        }
    }
}
