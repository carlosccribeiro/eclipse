#if UNITY_EDITOR
using System.Collections;
using Eclipse.Core.CrowdControl;
using Eclipse.Core.Ids;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EntityId = Eclipse.Gameplay.Combat.EntityId;

namespace Eclipse.Tests.PlayMode.Skills
{
    /// <summary>M08 T8, Infernal Leap (08 4.4): +15 Fury, Unstoppable for the 0.5 s of air, 8 m range, 180% on landing, Burning 2 s (provisional until M11).</summary>
    public class InfernalLeapTests : KaelPlayTestBase
    {
        private const int AirFrames = 30; // unstoppable_s 0.5 s at 60 Hz
        private const int WindupFrames = 8;

        [UnityTest]
        public IEnumerator Gives_15_Fury_is_unstoppable_in_the_air_and_lands_8_m_away_burning_the_target()
        {
            yield return Boot(new Vector3(0f, 0f, 8.5f));
            CombatActor dummy = Run.Dummies[0];
            Run.SetFury(0f);
            Run.AimAt(KaelRun.Origin + new Vector3(0f, 0f, 20f)); // beyond range_m = 8: the landing is capped

            int f0 = Run.PressAndCast(InputActionId.Skill4);
            Assert.GreaterOrEqual(f0, 0, "cast started: " + Run.Kael.Skills.LastResult);
            Assert.AreEqual(15f, Run.Fury, 0.01f, "+15 Fury on cast (no cost)");

            var state = Run.Player.States;
            Assert.IsTrue(Run.TickUntil(() => Run.Frame >= f0 + WindupFrames, 50)); // Unstoppable is granted when the Active phase starts
            Assert.IsTrue(state.IsUnstoppable(f0 + WindupFrames), "unstoppable when the leap leaves the ground");
            Assert.IsTrue(state.IsUnstoppable(f0 + WindupFrames + AirFrames - 1), "unstoppable on the last airborne frame");

            // Stun while in the air is refused (a real request through the state component).
            Assert.IsTrue(Run.TickUntil(() => Run.Frame >= f0 + WindupFrames + AirFrames / 2, 100));
            CcResult airborne = state.ApplyCc(CcKind.Stun, 1f, new EntityId(900), Run.Frame, 0);
            Assert.AreEqual(CcOutcome.BlockedUnstoppable, airborne.Outcome, "Stun refused during the leap");

            // Burning appears when the leap lands.
            StatusStub status = null;
            StableId burning = StableId.Parse("status.burning");
            int seenFrame = -1, seenRemaining = -1;
            for (int i = 0; i < 200 && seenFrame < 0; i++)
            {
                Run.Tick();
                if (status == null && Run.Kael.Skills.Directory.TryGet(dummy.Entity.Id, out SkillTarget t)) status = t.Status as StatusStub;
                if (status != null && status.GetStacks(burning) > 0)
                {
                    seenFrame = Run.Frame;
                    seenRemaining = status.GetRemainingFrames(burning);
                }
            }
            Assert.GreaterOrEqual(seenFrame, 0, "Burning was applied");
            Assert.AreEqual(f0 + WindupFrames + AirFrames, seenFrame, 1, "applied at the landing");
            Assert.That(seenRemaining, Is.InRange(118, 120), "Burning lasts 2 s = 120 frames");

            Assert.IsFalse(state.IsUnstoppable(f0 + WindupFrames + AirFrames + 1), "unstoppable ends with the air time");
            Run.FinishCast();
            Assert.AreEqual(8f, Run.Player.Entity.Position.z - KaelRun.Origin.z, 0.05f, "range capped at 8 m");

            var hits = Run.HitsOn(dummy);
            Assert.AreEqual(1, hits.Count, "one landing hit");
            KaelRun.AssertDamage(Run.Expected(1.8f, hits[0].Frame), hits[0].Value, "180% of Weapon Power");

            // With the air time over, a Stun applies again.
            CcResult grounded = state.ApplyCc(CcKind.Stun, 1f, new EntityId(900), Run.Frame, 0);
            Assert.AreEqual(CcOutcome.Applied, grounded.Outcome, "Stun lands once the leap is over");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
