#if UNITY_EDITOR
using System.Collections;
using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Input;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Skills
{
    /// <summary>M08 T8: the first basic strike of each of the five weapons run through the real controller (06 3.6.2 frame data at Attack Speed 1.0).</summary>
    public class KaelWeaponFrameDataTests : KaelPlayTestBase
    {
        // weapon index (KaelHeroRig order), startup, active, recovery
        [UnityTest]
        public IEnumerator First_basic_strike_of_each_weapon_matches_the_frame_data()
        {
            yield return Boot();
            var expected = new (string name, int startup, int active, int recovery)[]
            {
                ("greatsword", 20, 6, 40), ("dual_axes", 10, 3, 24), ("warhammer", 28, 8, 50), ("longsword", 14, 4, 32), ("heavy_blade", 22, 7, 46),
            };
            Assert.AreEqual(expected.Length, Run.Kael.WeaponCount);

            for (int w = 0; w < expected.Length; w++)
            {
                Assert.IsTrue(Run.Kael.Equip(w, force: true), "equip " + expected[w].name);
                Run.Tick(3);
                Assert.IsFalse(Run.Attack.IsAttacking);

                var counts = new int[4];
                bool started = false;
                Run.Press(InputActionId.Basic);
                for (int i = 0; i < 300; i++)
                {
                    Run.Tick();
                    if (i == 0) Run.Release(InputActionId.Basic);
                    AttackPhase p = Run.Attack.Phase;
                    if (p == AttackPhase.None) { if (started) break; continue; }
                    started = true;
                    counts[(int)p]++;
                }

                string label = expected[w].name;
                Assert.IsTrue(started, label + " swing started");
                Assert.AreEqual(expected[w].startup, counts[(int)AttackPhase.Startup], label + " Startup");
                Assert.AreEqual(expected[w].active, counts[(int)AttackPhase.Active], label + " Active");
                Assert.AreEqual(expected[w].recovery, counts[(int)AttackPhase.Recovery], label + " Recovery");

                // The data on the asset agrees with what the controller ran.
                AttackStep step = Run.Kael.CurrentWeapon.BasicChain[0].Step;
                Assert.AreEqual((step.StartupFrames, step.ActiveFrames, step.RecoveryFrames),
                    (counts[(int)AttackPhase.Startup], counts[(int)AttackPhase.Active], counts[(int)AttackPhase.Recovery]), label + " asset");
                Assert.AreEqual("weapon.kael." + expected[w].name, Run.Kael.CurrentWeapon.StableIdString);
                Run.Tick(60);
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
