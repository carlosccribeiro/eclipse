using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.PlayMode
{
    public class PlayModeSmokeTests
    {
        [Test]
        public void FixedTimestepIs60Hz() => Assert.AreEqual(1f / 60f, Time.fixedDeltaTime, 1e-6f);
    }
}
