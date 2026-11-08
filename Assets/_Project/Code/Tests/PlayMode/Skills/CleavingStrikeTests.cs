#if UNITY_EDITOR
using System.Collections;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Skills
{
    /// <summary>M08 T8, Cleaving Strike (08 4.4): 220% of Weapon Power, 180 degree arc, 3.5 m, 20 Fury, no cooldown; Greatsword area kit bonus.</summary>
    public class CleavingStrikeTests : KaelPlayTestBase
    {
        private const int LongswordIndex = 3;  // no Cleaving kit bonus: nominal 3.5 m radius
        private const int GreatswordIndex = 0; // +20% skill area (08 4.6)

        [UnityTest]
        public IEnumerator Deals_220_percent_in_a_180_degree_arc_of_3_5_m()
        {
            yield return Boot(new Vector3(0f, 0f, 3.4f), new Vector3(0f, 0f, 3.6f), new Vector3(0f, 0f, -1f));
            CombatActor inside = Run.Dummies[0], tooFar = Run.Dummies[1], behind = Run.Dummies[2];
            Assert.IsTrue(Run.Kael.Equip(LongswordIndex, force: true));
            Run.Tick(2);
            Run.SetFury(50f);

            int castFrame = Run.PressAndCast(InputActionId.Skill1);
            Assert.GreaterOrEqual(castFrame, 0, "cast started: " + Run.Kael.Skills.LastResult);
            Assert.AreEqual(30f, Run.Fury, 0.01f, "20 Fury spent");
            Run.FinishCast();

            var hits = Run.HitsOn(inside);
            Assert.AreEqual(1, hits.Count, "target at 3.4 m is hit exactly once");
            KaelRun.AssertDamage(Run.Expected(2.2f, hits[0].Frame), hits[0].Value, "220% of Weapon Power");
            Assert.AreEqual(0, Run.CountHitsOn(tooFar), "3.6 m is outside the 3.5 m radius");
            Assert.AreEqual(0, Run.CountHitsOn(behind), "behind the caster is outside the 180 degree arc");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Can_be_spammed_while_there_is_Fury_and_stops_without_it()
        {
            yield return Boot(new Vector3(0f, 0f, 2f));
            CombatActor dummy = Run.Dummies[0];
            Run.Kael.Equip(LongswordIndex, force: true);
            Run.Tick(2);
            Run.SetFury(40f);

            for (int i = 0; i < 2; i++)
            {
                Assert.GreaterOrEqual(Run.PressAndCast(InputActionId.Skill1), 0, "cast " + i + ": " + Run.Kael.Skills.LastResult);
                Run.FinishCast();
            }
            Assert.AreEqual(0f, Run.Fury, 0.01f, "40 Fury = two casts");
            Assert.AreEqual(2, Run.CountHitsOn(dummy), "one hit per cast");
            Assert.AreEqual(0, Run.Kael.Skills.Controller.Cooldowns.RemainingFrames(SkillSlot.Q, Run.Frame), "Cleaving Strike has no cooldown");

            Assert.AreEqual(-1, Run.PressAndCast(InputActionId.Skill1), "third cast refused");
            Assert.AreEqual(CastResult.InsufficientResource, Run.Kael.Skills.LastResult);
            Assert.AreEqual(2, Run.CountHitsOn(dummy));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Greatsword_kit_bonus_scales_the_radius_by_sqrt_1_2()
        {
            // Inside sqrt(1.2) x 3.5 = 3.834 m but outside 3.5 m; the second dummy is beyond both.
            yield return Boot(new Vector3(0f, 0f, 3.7f), new Vector3(0.4f, 0f, 3.9f));
            CombatActor near = Run.Dummies[0], far = Run.Dummies[1];
            Run.SetFury(100f);

            Run.Kael.Equip(LongswordIndex, force: true);
            Run.Tick(2);
            Assert.AreEqual(3.5f, Run.Kael.Skills.Specs.Get(SkillSlot.Q).GetParam("radius_m"), 1e-4f, "Longsword: nominal radius");
            Assert.GreaterOrEqual(Run.PressAndCast(InputActionId.Skill1), 0);
            Run.FinishCast();
            Assert.AreEqual(0, Run.CountHitsOn(near), "3.7 m is out of the nominal 3.5 m");

            Run.Kael.Equip(GreatswordIndex, force: true);
            Run.Tick(2);
            float scaled = Run.Kael.Skills.Specs.Get(SkillSlot.Q).GetParam("radius_m");
            Assert.AreEqual(3.5f * Mathf.Sqrt(1.2f), scaled, 1e-3f, "radius x sqrt(1 + 20%) (06 3.2)");
            Assert.GreaterOrEqual(Run.PressAndCast(InputActionId.Skill1), 0);
            Run.FinishCast();
            Assert.AreEqual(1, Run.CountHitsOn(near), "3.7 m is inside the Greatsword radius");
            Assert.AreEqual(0, Run.CountHitsOn(far), "3.92 m is beyond it");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
