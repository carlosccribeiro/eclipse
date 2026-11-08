#if UNITY_EDITOR
using System.Collections;
using Eclipse.Core.CrowdControl;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Skills
{
    /// <summary>M08 T8, Burning Chains (08 4.4): up to 5 targets in an 8 m cone, 120%, Root 2 s, tether breaking beyond 10 m for 60%.</summary>
    public class BurningChainsTests : KaelPlayTestBase
    {
        private static Vector3[] Layout()
        {
            var o = new Vector3[9];
            for (int i = 0; i < 6; i++) o[i] = new Vector3(0f, 0f, 2f + i);  // 2..7 m on the axis (six candidates)
            o[6] = new Vector3(4f, 0f, 4f);                                 // 45 degrees off the axis: outside the 60 degree cone
            o[7] = new Vector3(0f, 0f, 8.5f);                               // beyond the 8 m range
            o[8] = new Vector3(0f, 0f, -3f);                                // behind
            return o;
        }

        [UnityTest]
        public IEnumerator Hits_at_most_five_targets_in_the_cone_and_roots_each_for_2_seconds()
        {
            yield return Boot(Layout());
            Run.SetFury(100f);

            int f0 = Run.PressAndCast(InputActionId.Skill2);
            Assert.GreaterOrEqual(f0, 0, "cast started: " + Run.Kael.Skills.LastResult);
            Assert.AreEqual(85f, Run.Fury, 0.01f, "15 Fury cost");
            Run.FinishCast();

            int hitTargets = 0;
            for (int i = 0; i < 6; i++) hitTargets += Run.CountHitsOn(Run.Dummies[i]);
            Assert.AreEqual(5, hitTargets, "maximum of 5 targets");
            for (int i = 0; i < 5; i++) Assert.AreEqual(1, Run.CountHitsOn(Run.Dummies[i]), "target at " + (2 + i) + " m");
            Assert.AreEqual(0, Run.CountHitsOn(Run.Dummies[5]), "the sixth (farthest) candidate is cut");
            Assert.AreEqual(0, Run.CountHitsOn(Run.Dummies[6]), "outside the cone");
            Assert.AreEqual(0, Run.CountHitsOn(Run.Dummies[7]), "beyond 8 m");
            Assert.AreEqual(0, Run.CountHitsOn(Run.Dummies[8]), "behind");

            var first = Run.HitsOn(Run.Dummies[0])[0];
            KaelRun.AssertDamage(Run.Expected(1.2f, first.Frame), first.Value, "120% of Weapon Power");

            int roots = 0;
            foreach (OnCCApplied cc in Run.Ccs)
            {
                if (cc.Kind != CcKind.Root) continue;
                roots++;
                Assert.AreEqual(120, cc.DurationFrames, "Root 2 s = 120 frames");
            }
            Assert.AreEqual(5, roots, "one Root per chained target");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Breaking_the_tether_beyond_10_m_deals_60_percent()
        {
            yield return Boot(new Vector3(0f, 0f, 5f));
            CombatActor dummy = Run.Dummies[0];
            Run.SetFury(100f);

            Assert.GreaterOrEqual(Run.PressAndCast(InputActionId.Skill2), 0);
            Assert.IsTrue(Run.TickUntil(() => Run.CountHitsOn(dummy) == 1, 120), "initial hit");
            Run.Tick(5);
            Assert.AreEqual(1, Run.CountHitsOn(dummy), "no break while inside 10 m");

            dummy.transform.position = KaelRun.Origin + new Vector3(0f, 0f, 10.5f);
            Assert.IsTrue(Run.TickUntil(() => Run.CountHitsOn(dummy) == 2, 10), "break hit");

            var hits = Run.HitsOn(dummy);
            KaelRun.AssertDamage(Run.Expected(0.6f, hits[1].Frame), hits[1].Value, "break damage: 60% of Weapon Power");
            Run.Tick(30);
            Assert.AreEqual(2, Run.CountHitsOn(dummy), "the tether broke only once");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
