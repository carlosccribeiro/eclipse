using System;
using System.Linq;
using Eclipse.Gameplay.Combat;
using NUnit.Framework;

namespace Eclipse.Tests.EditMode.Gameplay.Contracts
{
    public class CombatContractsTests
    {
        // Canonical hook list of 06 3.11 (R47).
        private static readonly string[] Hooks =
        {
            "OnHitDealt", "OnCritDealt", "OnTickDealt", "OnKill", "OnOverkill", "OnExecute", "OnStaggerApplied",
            "OnStaggered", "OnStaggerBreak", "OnCCApplied", "OnInterrupt", "OnDodge", "OnPerfectDodge", "OnParry",
            "OnBlock", "OnEvade", "OnHitTaken", "OnShieldBroken", "OnLowHealth", "OnFlaskUsed", "OnDeath", "OnRevive",
            "OnSkillCast", "OnSkillHit", "OnResourceSpent", "OnResourceGained", "OnWallImpact", "OnStealthEnter",
            "OnStealthBreak",
        };

        private static Type[] EventTypes(string name) =>
            typeof(CombatEventStream).Assembly.GetTypes()
                .Where(t => t.Name == name && t.IsValueType && t.Namespace != null && t.Namespace.StartsWith("Eclipse.Gameplay"))
                .ToArray();

        [Test]
        public void EveryHookOf06Has_ExactlyOneEventStruct()
        {
            foreach (string hook in Hooks)
                Assert.AreEqual(1, EventTypes(hook).Length, "Event struct for hook " + hook);
        }

        [Test]
        public void EveryCombatEvent_CarriesGeneration_ExceptM04Dodges()
        {
            foreach (string hook in Hooks.Where(h => h != "OnDodge" && h != "OnPerfectDodge"))
            {
                var t = EventTypes(hook).Single();
                Assert.IsNotNull(t.GetField("Generation"), hook + " must carry Generation");
                Assert.IsNotNull(t.GetField("Frame"), hook + " must carry Frame");
            }
        }

        [Test]
        public void Stream_PublishRead_EndFrameClears()
        {
            var s = new CombatEventStream(4);
            s.Publish(new OnKill(1, 0, new EntityId(1), new EntityId(2), 10f, false, default));
            Assert.AreEqual(1, s.Count<OnKill>());
            Assert.AreEqual(2, s.Read<OnKill>()[0].Target.Value);
            s.EndFrame();
            Assert.AreEqual(0, s.Count<OnKill>());
        }

        [Test]
        public void Stream_OverflowKeepsNewest()
        {
            var s = new CombatEventStream(2);
            for (int i = 1; i <= 3; i++) s.Publish(new OnEvade(i, 0, default, default));
            Assert.AreEqual(2, s.Count<OnEvade>());
            Assert.AreEqual(2, s.Read<OnEvade>()[0].Frame);
            Assert.AreEqual(1, s.Read<OnEvade>().DroppedCount);
        }
    }
}
