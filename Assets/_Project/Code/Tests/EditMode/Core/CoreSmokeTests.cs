using Eclipse.Core;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.Core
{
    public class CoreSmokeTests
    {
        [Test]
        public void CoreAssemblyLoads() => Assert.AreEqual("0.1.0", CoreInfo.Version);
    }
}
