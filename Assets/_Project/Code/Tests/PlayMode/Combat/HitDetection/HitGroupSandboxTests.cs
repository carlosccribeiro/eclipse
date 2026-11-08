#if UNITY_EDITOR
using System.Collections;
using Eclipse.Gameplay.Input;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Combat.HitDetection
{
    /// <summary>The real first swing (6 Active frames, one Hit Group) applies exactly once to the dummy.</summary>
    public class HitGroupSandboxTests
    {
        private SandboxCombatRun _run;

        [TearDown]
        public void TearDown() => _run?.Restore();

        [UnityTest]
        public IEnumerator First_swing_applies_once_to_the_dummy()
        {
            yield return SandboxKit.Load();
            _run = new SandboxCombatRun();
            _run.Setup("TrainingDummy_Heavy", noCrit: true);

            _run.Press(InputActionId.Basic);
            _run.Release(InputActionId.Basic);
            for (int i = 0; i < 70; i++) _run.Tick(); // 20 + 6 + 40 = one full swing and no follow-up input

            Assert.AreEqual(1, _run.PlayerHits.Count, "one application per Hit Group per swing");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
