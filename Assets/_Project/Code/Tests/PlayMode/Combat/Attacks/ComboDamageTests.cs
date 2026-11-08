#if UNITY_EDITOR
using System.Collections;
using Eclipse.Gameplay.Input;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Combat.Attacks
{
    /// <summary>Sandbox integration: the real controller, evaluator and damage pipeline against an unmitigated Medium dummy.</summary>
    public class ComboDamageTests
    {
        private SandboxCombatRun _run;

        [TearDown]
        public void TearDown() => _run?.Restore();

        [UnityTest]
        public IEnumerator Third_hit_deals_160_percent_of_the_damage_per_hit()
        {
            yield return SandboxKit.Load();
            _run = new SandboxCombatRun();
            _run.Setup("TrainingDummy_Medium", noCrit: true);
            var weapon = CombatEnv.LoadWeapon();
            float perHit = weapon.DamagePerHitILvl1;

            _run.Press(InputActionId.Basic);
            for (int i = 0; i < 260; i++) _run.Tick();
            _run.Release(InputActionId.Basic);

            Assert.GreaterOrEqual(_run.PlayerHits.Count, 3, "3-hit combo");
            Assert.AreEqual(1.1f, _run.PlayerHits[0].Value / perHit, 1e-3f, "hit 1 = 110%");
            Assert.AreEqual(1.1f, _run.PlayerHits[1].Value / perHit, 1e-3f, "hit 2 = 110%");
            Assert.AreEqual(1.6f, _run.PlayerHits[2].Value / perHit, 1e-3f, "hit 3 = 160%");
            Assert.IsFalse(_run.PlayerHits[2].IsCrit);
            Assert.IsTrue(_run.SawAttackerFrozen && _run.SawTargetFrozen, "hit-stop on attacker and target in the real flow");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
