#if UNITY_EDITOR
using System.Collections;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Skills
{
    /// <summary>M08 T8, Worldbreaker (08 4.4): needs 50 Fury, consumes all, 600% + 6% per Fury, Ruin Vulnerability per fissure, 6 f global hit-stop.</summary>
    public class WorldbreakerTests : KaelPlayTestBase
    {
        [UnityTest]
        public IEnumerator Is_refused_below_50_Fury_and_allowed_at_50()
        {
            yield return Boot();
            Run.SetFury(49f);
            Assert.AreEqual(-1, Run.PressAndCast(InputActionId.Ultimate), "49 Fury: refused");
            Assert.AreEqual(CastResult.InsufficientResource, Run.Kael.Skills.LastResult);
            Assert.AreEqual(49f, Run.Fury, 0.01f, "nothing spent on a refused cast");
            Assert.AreEqual(0, Run.Casts.Count);

            Run.SetFury(50f);
            Assert.GreaterOrEqual(Run.PressAndCast(InputActionId.Ultimate), 0, "50 Fury: allowed (" + Run.Kael.Skills.LastResult + ")");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Deals_600_percent_plus_6_percent_per_Fury_stacks_Ruin_and_stops_the_world()
        {
            // 2 cm from the centre: within the start of all six fissures (a fissure box starts at the caster). 3 m ahead: on the forward fissure only. 7.5 m: beyond the 7 m radius.
            yield return Boot(new Vector3(0.02f, 0f, 0f), new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, 7.5f));
            CombatActor centre = Run.Dummies[0], onLine = Run.Dummies[1], outside = Run.Dummies[2];
            Run.SetFury(80f);

            int f0 = Run.PressAndCast(InputActionId.Ultimate);
            Assert.GreaterOrEqual(f0, 0, "cast started: " + Run.Kael.Skills.LastResult);
            float spent = Run.Kael.Skills.Controller.ResourceSpent;
            Assert.AreEqual(80f, spent, 0.5f, "all Fury is spent");
            Assert.AreEqual(0f, Run.Fury, 0.01f, "pool emptied");
            Assert.IsTrue(Run.Player.States.IsUnstoppable(f0 + 70), "Unstoppable through the cast");

            Run.FinishCast();

            float coefficient = 6f + 0.06f * spent;
            var onLineHits = Run.HitsOn(onLine);
            Assert.AreEqual(1, onLineHits.Count);
            KaelRun.AssertDamage(Run.Expected(coefficient, onLineHits[0].Frame), onLineHits[0].Value, "600% + 6% x Fury");
            var centreHits = Run.HitsOn(centre);
            Assert.AreEqual(1, centreHits.Count, "one hit per target even when six fissures cross it");
            Assert.AreEqual(0, Run.CountHitsOn(outside), "beyond the radius");

            Assert.IsTrue(Run.Kael.Skills.Directory.TryGet(onLine.Entity.Id, out SkillTarget a));
            Assert.IsTrue(Run.Kael.Skills.Directory.TryGet(centre.Entity.Id, out SkillTarget b));
            int hitFrame = onLineHits[0].Frame;
            Assert.AreEqual(0.04f, a.Vulnerability.Total(hitFrame + 1), 1e-5f, "Ruin Vulnerability: 1 stack of 4%");
            Assert.AreEqual(0.20f, b.Vulnerability.Total(hitFrame + 1), 1e-5f, "six fissures capped at 5 stacks = 20%");
            Assert.AreEqual(0f, a.Vulnerability.Total(hitFrame + 6 * 60 + 2), 1e-5f, "lasts 6 s");

            Assert.AreEqual(1, Run.HitStops.Count, "one global hit-stop request");
            Assert.AreEqual(6, Run.HitStops[0].Frames, "6 frames");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
