using Eclipse.AI.Enemies;
using Eclipse.AI.Telegraphs;
using Eclipse.Data.Difficulties;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.EditMode.AI.Enemies
{
    /// <summary>06 section 3.10: the wind-up table is the Normal value; the 15% is only the cap for the higher difficulties.</summary>
    public class WindupReductionTests
    {
        [Test]
        public void NormalAppliesNoReductionEvenThoughTheAssetStoresTheCap()
        {
            DifficultyDefinition normal = EnemyTestKit.Normal;
            Assert.AreEqual(0.15f, normal.MaxWindupReduction, 1e-6f, "the asset keeps the cap");
            Assert.AreEqual(0f, EnemyStatsBuilder.WindupReduction(normal));
            Assert.AreEqual(0f, EnemyStatsBuilder.WindupReduction(null));
        }

        [Test]
        public void HigherTiersApplyTheCap()
        {
            var hard = ScriptableObject.CreateInstance<DifficultyDefinition>();
            hard.EditorSet(DifficultyTier.Nightmare, 4, 3, 1f, 1, 0.15f, 1.4f, 1f, 1f);
            Assert.AreEqual(0.15f, EnemyStatsBuilder.WindupReduction(hard), 1e-6f);
            Object.DestroyImmediate(hard);
        }

        [Test]
        public void MarauderOverheadKeepsItsDocumentedWindupInNormal()
        {
            var services = EnemyTestKit.Blueprint(EnemyTestKit.Marauder, 1, EnemyStatsBuilder.WindupReduction(EnemyTestKit.Normal));
            AttackPlan overhead = null;
            foreach (AttackPlan head in services.Heads)
                if (head.Definition.StableIdString == "attack.enemy.marauder.overhead") overhead = head;
            Assert.IsNotNull(overhead);
            Assert.AreEqual(0.7f, overhead.WindupSeconds, 1e-5f);
        }
    }
}
