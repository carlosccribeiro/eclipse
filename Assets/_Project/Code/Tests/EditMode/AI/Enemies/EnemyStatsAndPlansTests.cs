using System;
using Eclipse.AI.Enemies;
using Eclipse.AI.Telegraphs;
using Eclipse.Core.Tags;
using Eclipse.Data.Difficulties;
using Eclipse.Data.Enemies;
using Eclipse.Gameplay.Entities;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.EditMode.AI.Enemies
{
    /// <summary>Stats from data with the R53 curve (06 section 3.13) and the runtime plans derived from the attack data.</summary>
    public class EnemyStatsAndPlansTests
    {
        // R53 (06 section 3.13): HP_Std = 36 x 1.048^(eLvl - 1); damage base 20 x 1.048^(eLvl - 1) up to level 24, Normal.
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(24)]
        public void StandardStatsFollowTheR53Curve(int level)
        {
            EnemyStats s = EnemyTestKit.StatsOf(EnemyTestKit.Marauder, level);
            double growth = Math.Pow(1.048, level - 1);
            Assert.AreEqual(36.0 * growth, s.MaxHealth, 0.01, "HP");
            Assert.AreEqual(20.0 * growth, s.Damage, 0.01, "damage");
        }

        [Test]
        public void StatsMultiplyTheArchetypeFactors()
        {
            // 09 section 4: Fodder 0.3/0.6, Ranged 0.8/1.0, Tank 2.5/1.4 on top of the Standard row (36 / 20 at level 1).
            Assert.AreEqual(36f * 0.3f, EnemyTestKit.StatsOf(EnemyTestKit.Husk).MaxHealth, 1e-3f);
            Assert.AreEqual(20f * 0.6f, EnemyTestKit.StatsOf(EnemyTestKit.Husk).Damage, 1e-3f);
            Assert.AreEqual(36f * 0.8f, EnemyTestKit.StatsOf(EnemyTestKit.Hurler).MaxHealth, 1e-3f);
            Assert.AreEqual(36f * 2.5f, EnemyTestKit.StatsOf(EnemyTestKit.Brute).MaxHealth, 1e-3f);
            Assert.AreEqual(20f * 1.4f, EnemyTestKit.StatsOf(EnemyTestKit.Brute).Damage, 1e-3f);
        }

        [Test]
        public void DifficultyMultipliersApply()
        {
            var hard = ScriptableObject.CreateInstance<DifficultyDefinition>();
            hard.EditorSet(DifficultyTier.Eclipse, 5, 4, 0.8f, 2, 0.15f, 1.6f, 2f, 1.5f);
            Assert.IsTrue(EnemyStatsBuilder.TryBuild(EnemyTestKit.Marauder, EnemyTestKit.Scaling, 1, hard, out EnemyStats s));
            Assert.AreEqual(72f, s.MaxHealth, 1e-3f);
            Assert.AreEqual(30f, s.Damage, 1e-3f);
        }

        [Test]
        public void UnknownLevelFails()
        {
            Assert.IsFalse(EnemyStatsBuilder.TryBuild(EnemyTestKit.Marauder, EnemyTestKit.Scaling, 9999, EnemyTestKit.Normal, out _));
        }

        [Test]
        public void AttackDamageIsRelativeToTheEnemyHit()
        {
            EnemyStats s = EnemyTestKit.StatsOf(EnemyTestKit.Marauder);
            AttackDefinition overhead = EnemyTestKit.Marauder.Attacks[2];
            Assert.AreEqual(s.Damage * 1.5f, EnemyStatsBuilder.AttackDamage(in s, overhead), 1e-3f); // damage_rel 1.5
        }

        [Test]
        public void MassMapsToTheStaggerTierAndBodyClass()
        {
            Assert.AreEqual(Eclipse.Gameplay.Combat.StaggerTier.None, EnemyStatsBuilder.TierFor(EnemyMassClass.Light));
            Assert.AreEqual(Eclipse.Gameplay.Combat.StaggerTier.Medium, EnemyStatsBuilder.TierFor(EnemyMassClass.Medium));
            Assert.AreEqual(Eclipse.Gameplay.Combat.StaggerTier.Heavy, EnemyStatsBuilder.TierFor(EnemyMassClass.Heavy));
            Assert.AreEqual(Eclipse.Gameplay.Combat.StaggerTier.Guardian, EnemyStatsBuilder.TierFor(EnemyMassClass.Colossal));
            Assert.AreEqual(MassClass.Heavy, EnemyStatsBuilder.ToGameplay(EnemyTestKit.Brute.MassClass));
            Assert.AreEqual(MassClass.Light, EnemyStatsBuilder.ToGameplay(EnemyTestKit.Husk.MassClass));
        }

        [TestCase("target_dist<=2.0", 9f, 0f, 2f)]
        [TestCase("target_dist>=5.0", 14f, 5f, 14f)]
        [TestCase("target_dist>=5.0 & target_dist<=12", 14f, 5f, 12f)]
        [TestCase("chain_only", 3f, 0f, 3f)]
        [TestCase("on_death", 3f, 0f, 3f)]
        [TestCase("", 3f, 0f, 3f)]
        public void ConditionDistanceBounds(string condition, float defaultMax, float expectedMin, float expectedMax)
        {
            float min = 0f, max = defaultMax;
            AttackCondition.Parse(condition, ref min, ref max);
            Assert.AreEqual(expectedMin, min, 1e-5f);
            Assert.AreEqual(expectedMax, max, 1e-5f);
        }

        [Test]
        public void HuskLungeIsAFodderStrikeWithTheReachCovered()
        {
            EnemyBlueprint bp = EnemyTestKit.Blueprint(EnemyTestKit.Husk);
            Assert.AreEqual(1, bp.Heads.Length, "lunge");
            Assert.AreEqual(1, bp.DeathPlans.Length, "spark");
            AttackPlan lunge = bp.Heads[0];
            Assert.AreEqual(0.6f, lunge.WindupSeconds, 1e-5f);
            Assert.AreEqual(6f / 60f, lunge.ActiveSeconds, 1e-5f);
            Assert.AreEqual(30f / 60f, lunge.RecoverSeconds, 1e-5f);
            Assert.AreEqual(2f, lunge.MaxRange, 1e-5f);
            Assert.AreEqual(1f, lunge.ForwardOffset, 1e-5f, "1 m circle centered 1 m ahead covers the 2 m reach");
            Assert.AreEqual(TelegraphVisual.Red, lunge.Visual);

            AttackPlan spark = bp.DeathPlans[0];
            Assert.IsTrue(spark.OnDeath);
            Assert.AreEqual(0.25f, spark.TriggerChance, 1e-5f);
            Assert.AreEqual(1.5f, spark.Radius, 1e-5f);
            Assert.AreEqual(0f, spark.ForwardOffset, 1e-5f);
        }

        [Test]
        public void WindupAppliesTheDifficultyReductionAndTheFloor()
        {
            AttackPlan normal = EnemyTestKit.Blueprint(EnemyTestKit.Husk, 1, 0f).Heads[0];
            AttackPlan hard = EnemyTestKit.Blueprint(EnemyTestKit.Husk, 1, 0.15f).Heads[0];
            AttackPlan capped = EnemyTestKit.Blueprint(EnemyTestKit.Husk, 1, 0.9f).Heads[0];
            Assert.AreEqual(0.6f, normal.WindupSeconds, 1e-5f);
            Assert.AreEqual(0.51f, hard.WindupSeconds, 1e-5f);
            Assert.AreEqual(0.51f, capped.WindupSeconds, 1e-5f, "reduction is capped at 15%");
            Assert.GreaterOrEqual(capped.WindupSeconds, WindupMath.FloorCommon);
        }

        [Test]
        public void MarauderComboIsOneSelectableAttackWithTwoStrikes()
        {
            EnemyBlueprint bp = EnemyTestKit.Blueprint(EnemyTestKit.Marauder);
            Assert.AreEqual(2, bp.Heads.Length, "combo head and overhead; the second strike is reached through the chain");
            AttackPlan combo = bp.Heads[0];
            Assert.IsNotNull(combo.Next, "combo2 follows combo1");
            Assert.IsNull(combo.Next.Next);
            // 0.6 s wind-up + 6 f active + 18 f recovery between strikes + 0.6 s + 6 f; last recovery 36 f.
            Assert.AreEqual(0.6f + 0.1f + 0.3f + 0.6f + 0.1f, combo.ChainAttackSeconds, 1e-4f);
            Assert.AreEqual(0.6f, combo.ChainRecoverSeconds, 1e-5f);
            Assert.AreEqual(combo.ChainAttackSeconds, bp.Utility.Attacks[0].DurationSeconds, 1e-5f);
            Assert.AreEqual(combo.ChainRecoverSeconds, bp.Utility.Attacks[0].RecoverSeconds, 1e-5f);
            Assert.AreEqual(1.0f, bp.Utility.Attacks[0].Weight, 1e-5f);
            Assert.AreEqual(0.6f, bp.Utility.Attacks[1].Weight, 1e-5f, "overhead weight from data");
            Assert.AreEqual(2f, bp.Utility.DesiredRange, 1e-5f, "preferred max range");
        }

        [Test]
        public void HurlerLobIsRangedWithAMinimumRangeAndABurningPool()
        {
            EnemyBlueprint bp = EnemyTestKit.Blueprint(EnemyTestKit.Hurler);
            AttackPlan slag = bp.Heads[0];
            Assert.IsTrue(slag.IsLobbed);
            Assert.IsTrue(slag.Ranged);
            Assert.IsTrue(slag.IsProjectileLike);
            Assert.AreEqual(5f, slag.MinRange, 1e-5f);
            Assert.AreEqual(14f, slag.MaxRange, 1e-5f);
            Assert.AreEqual(14f, slag.ProjectileSpeed, 1e-5f, "06 section 3.9, Normal");
            Assert.AreEqual("Burning", slag.ZoneStatus);
            Assert.AreEqual(3f, slag.ZoneDurationSeconds, 1e-5f);
            Assert.AreEqual(2.5f, slag.Radius, 1e-5f);
            Assert.AreEqual(5f, bp.Utility.Attacks[0].MinRange, 1e-5f);
            Assert.IsTrue(bp.Utility.Attacks[0].Ranged);
        }

        [Test]
        public void BruteSlamIsUnblockableWithTwoSecondsOfRecovery()
        {
            AttackPlan slam = EnemyTestKit.Blueprint(EnemyTestKit.Brute).Heads[0];
            Assert.IsTrue(slam.Flags.Has(HitFlags.Unblockable));
            Assert.AreEqual(TelegraphVisual.WhiteGold, slam.Visual);
            Assert.AreEqual(1.1f, slam.WindupSeconds, 1e-5f);
            Assert.AreEqual(2.0f, slam.ChainRecoverSeconds, 1e-5f);
            Assert.AreEqual(4f, slam.Radius, 1e-5f);
            Assert.AreEqual(0f, slam.ForwardOffset, 1e-5f, "slam radius equals its reach: centered on the Brute");
        }

        [Test]
        public void HitGroupIsStableAndUniquePerAttack()
        {
            AttackDefinition a = EnemyTestKit.Marauder.Attacks[0];
            AttackDefinition b = EnemyTestKit.Marauder.Attacks[2];
            Assert.AreEqual(AttackPlan.HitGroupOf(a), AttackPlan.HitGroupOf(a));
            Assert.AreNotEqual(AttackPlan.HitGroupOf(a), AttackPlan.HitGroupOf(b));
            Assert.Greater(AttackPlan.HitGroupOf(a), 0);
        }

        [Test]
        public void TemplatesFollowTheHfsmIdThenTheArchetype()
        {
            Assert.AreEqual(SteeringStyle.Swarm, ArchetypeTemplates.For(EnemyTestKit.Husk).Style);
            Assert.AreEqual(SteeringStyle.Flank, ArchetypeTemplates.For(EnemyTestKit.Marauder).Style);
            Assert.AreEqual(SteeringStyle.KeepDistance, ArchetypeTemplates.For(EnemyTestKit.Hurler).Style);
            Assert.IsTrue(ArchetypeTemplates.For(EnemyTestKit.Hurler).UsesRetreat);
            Assert.AreEqual(SteeringStyle.Escort, ArchetypeTemplates.For(EnemyTestKit.Brute).Style);
            // Unknown id falls back to the archetype; archetypes outside M07 use the melee template.
            Assert.AreEqual(SteeringStyle.Flank, ArchetypeTemplates.For(Eclipse.Core.RunGen.Encounters.EnemyArchetype.Caster).Style);
            Assert.AreEqual(SteeringStyle.Swarm, ArchetypeTemplates.For(Eclipse.Core.RunGen.Encounters.EnemyArchetype.Fodder).Style);
        }
    }
}
