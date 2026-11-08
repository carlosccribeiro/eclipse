using System;
using System.Linq;
using Eclipse.Core.Damage;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using Eclipse.Data.Difficulties;
using Eclipse.Data.Enemies;
using NUnit.Framework;
using UnityEditor;

namespace Eclipse.Tests.EditMode.Data.Enemies
{
    /// <summary>Checks the imported enemy, attack, difficulty and scaling assets against 09 sections 3.2, 3.3, 4 and 5.1.</summary>
    public class EnemyDataTests
    {
        private const string Root = "Assets/_Project/Content";

        private static T Load<T>(string id, string folder) where T : Definition
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
            {
                var d = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null && d.StableIdString == id) return d;
            }
            Assert.Fail("asset not found: " + id + " (run the data import)");
            return null;
        }

        private static DifficultyDefinition Difficulty(string id) => Load<DifficultyDefinition>(id, Root + "/Shared/Difficulty");
        private static EnemyDefinition Enemy(string id) => Load<EnemyDefinition>(id, Root + "/Enemies");
        private static AttackDefinition Attack(string id) => Load<AttackDefinition>(id, Root + "/Enemies");

        // 09 section 3.2 token table; 06 section 3.8.2 Stagger threshold multiplier.
        [TestCase("difficulty.normal", DifficultyTier.Normal, 3, 2, 1.2f, 1.0f)]
        [TestCase("difficulty.veteran", DifficultyTier.Veteran, 3, 3, 1.0f, 1.2f)]
        [TestCase("difficulty.nightmare", DifficultyTier.Nightmare, 4, 3, 0.9f, 1.4f)]
        [TestCase("difficulty.eclipse", DifficultyTier.Eclipse, 5, 4, 0.8f, 1.6f)]
        public void DifficultyMatchesTokenTable(string id, DifficultyTier tier, int melee, int ranged, float interval, float stagger)
        {
            DifficultyDefinition d = Difficulty(id);
            Assert.AreEqual(tier, d.Tier);
            Assert.AreEqual(melee, d.TokensMelee, "melee tokens");
            Assert.AreEqual(ranged, d.TokensRanged, "ranged tokens");
            Assert.AreEqual(interval, d.MinTokenIntervalSeconds, 1e-5f, "min interval");
            Assert.AreEqual(stagger, d.StaggerThresholdMult, 1e-5f, "stagger multiplier");
            Assert.AreEqual(0.15f, d.MaxWindupReduction, 1e-5f, "wind-up reduction cap (06 section 3.10)");
            Assert.AreEqual(1.0f, d.HpMult, 1e-5f);
            Assert.AreEqual(1.0f, d.DamageMult, 1e-5f);
        }

        [Test]
        public void MarauderIsCompleteWithItsAttacks()
        {
            EnemyDefinition e = Enemy("enemy.ashen.marauder");
            Assert.AreEqual(EnemyArchetype.Standard, e.Archetype);
            Assert.AreEqual(1.0f, e.HpMult, 1e-5f);
            Assert.AreEqual(1.0f, e.DamageMult, 1e-5f);
            Assert.AreEqual(3, e.ThreatPoints);
            Assert.AreEqual(EnemyMassClass.Medium, e.MassClass);
            Assert.AreEqual(40f, e.StaggerThreshold, 1e-5f);
            Assert.AreEqual(4.2f, e.MoveSpeed, 1e-5f);
            Assert.AreEqual(DamageType.Physical, e.BaseElement);
            Assert.AreEqual(EnemyLootClass.Standard, e.LootClass);
            Assert.IsTrue(e.Tags.Has(Tag.Melee) && e.Tags.Has(Tag.Physical));
            Assert.AreEqual(0f, e.FrontPlateReduction, 1e-5f);

            Assert.AreEqual(3, e.Attacks.Count);
            AttackDefinition combo1 = e.Attacks[0];
            AttackDefinition combo2 = e.Attacks[1];
            AttackDefinition overhead = e.Attacks[2];
            Assert.AreEqual("attack.enemy.marauder.combo1", combo1.StableIdString);
            Assert.AreEqual("attack.enemy.marauder.combo2", combo2.StableIdString);
            Assert.AreEqual("attack.enemy.marauder.overhead", overhead.StableIdString);
            Assert.AreSame(combo2, combo1.ChainNext, "combo of 2 strikes");

            Assert.AreEqual(TelegraphShape.Cone, combo1.TelegraphShape);
            Assert.AreEqual(90f, combo1.ArcDegrees, 1e-5f);
            Assert.AreEqual(2.0f, combo1.RadiusMeters, 1e-5f);
            Assert.AreEqual(0.6f, combo1.WindupSeconds, 1e-5f);

            Assert.AreEqual(TelegraphShape.Line, overhead.TelegraphShape);
            Assert.AreEqual(3.0f, overhead.LengthMeters, 1e-5f);
            Assert.AreEqual(0.7f, overhead.WindupSeconds, 1e-5f);
            Assert.AreEqual(TelegraphType.Red, overhead.TelegraphType);
            Assert.AreEqual(TokenKind.Melee, overhead.TokenKind);
            Assert.AreEqual(1, overhead.TokenCost);
        }

        [Test]
        public void AshenWastesEnemiesMatchBestiary()
        {
            EnemyDefinition husk = Enemy("enemy.ashen.cinder_husk");
            Assert.AreEqual(EnemyArchetype.Fodder, husk.Archetype);
            Assert.AreEqual(0.3f, husk.HpMult, 1e-5f);
            Assert.AreEqual(0.6f, husk.DamageMult, 1e-5f);
            Assert.AreEqual(1, husk.ThreatPoints);
            Assert.AreEqual(EnemyMassClass.Light, husk.MassClass);
            Assert.AreEqual(5.0f, husk.MoveSpeed, 1e-5f);
            AttackDefinition spark = husk.Attacks.Single(a => a.Trigger == AttackTrigger.OnDeath);
            Assert.AreEqual(0.25f, spark.TriggerChance, 1e-5f);
            Assert.AreEqual(1.5f, spark.RadiusMeters, 1e-5f);
            Assert.AreEqual(0.6f, spark.WindupSeconds, 1e-5f);

            EnemyDefinition hurler = Enemy("enemy.ashen.slag_hurler");
            Assert.AreEqual(EnemyArchetype.Ranged, hurler.Archetype);
            Assert.AreEqual(0.8f, hurler.HpMult, 1e-5f);
            Assert.AreEqual(4, hurler.ThreatPoints);
            Assert.AreEqual(RepositionBehavior.KeepDistance, hurler.Reposition);
            Assert.AreEqual(8f, hurler.PreferredRangeMin, 1e-5f);
            Assert.AreEqual(14f, hurler.PreferredRangeMax, 1e-5f);
            Assert.AreEqual(5f, hurler.RetreatBelowMeters, 1e-5f);
            AttackDefinition slag = hurler.Attacks.Single();
            Assert.AreEqual(2.5f, slag.RadiusMeters, 1e-5f);
            Assert.AreEqual(1.0f, slag.WindupSeconds, 1e-5f);
            Assert.AreEqual("Burning", slag.ZoneStatus);
            Assert.AreEqual(3f, slag.ZoneDurationSeconds, 1e-5f);
            Assert.AreEqual(TokenKind.Ranged, slag.TokenKind);

            EnemyDefinition brute = Enemy("enemy.ashen.kiln_brute");
            Assert.AreEqual(EnemyArchetype.Tank, brute.Archetype);
            Assert.AreEqual(2.5f, brute.HpMult, 1e-5f);
            Assert.AreEqual(1.4f, brute.DamageMult, 1e-5f);
            Assert.AreEqual(8, brute.ThreatPoints);
            Assert.AreEqual(EnemyMassClass.Heavy, brute.MassClass);
            Assert.AreEqual(120f, brute.StaggerThreshold, 1e-5f);
            Assert.AreEqual(0.5f, brute.FrontPlateReduction, 1e-5f);
            AttackDefinition slam = brute.Attacks.Single();
            Assert.AreEqual(4.0f, slam.RadiusMeters, 1e-5f);
            Assert.AreEqual(1.1f, slam.WindupSeconds, 1e-5f);
            Assert.AreEqual(TelegraphType.WhiteGold, slam.TelegraphType);
            Assert.IsTrue((slam.HitFlags & HitFlags.Unblockable) != 0);
            Assert.AreEqual(120, slam.RecoveryFrames, "2 s recovery at 60 Hz");
        }

        [Test]
        public void AllAttacksRespectWindupFloorAndTelegraphPalette()
        {
            string[] ids = AssetDatabase.FindAssets("t:AttackDefinition", new[] { Root + "/Enemies" });
            Assert.GreaterOrEqual(ids.Length, 7);
            foreach (string guid in ids)
            {
                var a = AssetDatabase.LoadAssetAtPath<AttackDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                // 06 section 3.10: common floor 0.45 s; the Normal wind-up must already be at or above it.
                Assert.GreaterOrEqual(a.WindupSeconds, 0.45f, a.StableIdString);
                Assert.IsTrue(Enum.IsDefined(typeof(TelegraphType), a.TelegraphType), a.StableIdString);
            }
            Assert.IsFalse(Enum.GetNames(typeof(TelegraphType)).Any(n => n.IndexOf("Orange", StringComparison.OrdinalIgnoreCase) >= 0),
                "orange is never a telegraph color (06 section 3.10)");
        }

        [Test]
        public void ScalingCurveMatchesR53()
        {
            var s = Load<EnemyScalingDefinition>("enemy.scaling.curve", Root + "/Enemies");
            Assert.AreEqual(110, s.Count);
            Assert.IsTrue(s.TryGet(1, out EnemyScalingEntry l1));
            Assert.AreEqual(DifficultyTier.Normal, l1.tier);
            Assert.AreEqual(36f, l1.stdHp, 1e-4f);
            Assert.AreEqual(20f, l1.stdDamage, 1e-4f);
            Assert.IsTrue(s.TryGet(110, out EnemyScalingEntry l110));
            Assert.AreEqual(DifficultyTier.Ascension, l110.tier);
            Assert.IsFalse(s.TryGet(111, out _));
        }
    }
}
