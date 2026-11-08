using System.Collections.Generic;
using Eclipse.Core.Loot;
using Eclipse.Core.RunGen.Map;
using NUnit.Framework;

namespace Eclipse.Core.RunGen.Encounters.Tests
{
    [TestFixture]
    [Category("RunGen")]
    public sealed class EncounterPlannerSmoke
    {
        /// <summary>Ashen Wastes bestiary, 09 section 5.1: PA 1, 3, 4, 5, 8, 6, 5, 5 (+ one test-only Elite).</summary>
        private static List<EnemyCandidate> AshenWastes(bool withElite = false)
        {
            var l = new List<EnemyCandidate>
            {
                new EnemyCandidate("enemy.cinder_husk", EnemyArchetype.Fodder, 1),
                new EnemyCandidate("enemy.ashborn_marauder", EnemyArchetype.Standard, 3),
                new EnemyCandidate("enemy.slag_hurler", EnemyArchetype.Ranged, 4),
                new EnemyCandidate("enemy.choir_pyrecaller", EnemyArchetype.Caster, 5),
                new EnemyCandidate("enemy.kiln_brute", EnemyArchetype.Tank, 8),
                new EnemyCandidate("enemy.ember_matron", EnemyArchetype.Summoner, 6),
                new EnemyCandidate("enemy.cinder_stalker", EnemyArchetype.Assassin, 5),
                new EnemyCandidate("enemy.choir_cantor", EnemyArchetype.Support, 5),
            };
            if (withElite)
            {
                l.Add(new EnemyCandidate("enemy.elite_marauder", EnemyArchetype.Standard, 9, 1f, true));
                l.Add(new EnemyCandidate("enemy.elite_brute", EnemyArchetype.Tank, 14, 1f, true));
            }
            return l;
        }

        private static EncounterRequest Req(int row, Difficulty d, string recipe = null, RoomType type = RoomType.Combat, int asc = 0) =>
            new EncounterRequest { Row = row, Difficulty = d, RecipeId = recipe, RoomType = type, AscensionLevel = asc, Pool = AshenWastes(type == RoomType.Elite) };

        private static void AssertNoViolations(EncounterPlan p, EncounterConfig cfg, string recipeId, string ctx)
        {
            var lim = cfg.Limits;
            Assert.LessOrEqual(p.TotalThreat, p.SpendableBudget, ctx + " overshoot");
            int casterCap = lim.MaxCastersPerWave, summonerCap = recipeId == "brood" ? 2 : lim.MaxSummonersPerWave;
            foreach (var w in p.Waves)
            {
                Assert.LessOrEqual(w.Units.Count, lim.MaxLiveUnits, ctx + " units");
                Assert.LessOrEqual(w.Threat, lim.MaxLivePa, ctx + " live PA");
                Assert.LessOrEqual(w.Casters, casterCap, ctx + " casters");
                Assert.LessOrEqual(w.Summoners, summonerCap, ctx + " summoners");
                Assert.LessOrEqual(w.Tanks, lim.MaxTanks(p.Row), ctx + " tanks");
                Assert.GreaterOrEqual(w.Frontline, 1, ctx + " frontline");
                Assert.LessOrEqual(w.SpecialistThreat, lim.MaxSpecialistShare * w.Threat + 1e-6, ctx + " specialist share");
            }
        }

        [Test]
        public void Budget_matches_the_examples_of_09_section_10_1()
        {
            var cfg = EncounterConfig.Default();
            Assert.AreEqual(23.0, ThreatBudget.Compute(cfg, 1, Difficulty.Normal, 0, 1f, false), 1e-6);
            Assert.AreEqual(72.8, ThreatBudget.Compute(cfg, 12, Difficulty.Eclipse, 0, 1f, false), 1e-4);
            Assert.AreEqual(1.8f, ThreatBudget.DensityMult(cfg, Difficulty.Ascension, 100), 1e-6);
            Assert.AreEqual(1.4f, ThreatBudget.DensityMult(cfg, Difficulty.Ascension, 20), 1e-6);
            Assert.AreEqual(23 * 1.2, ThreatBudget.Compute(cfg, 1, Difficulty.Normal, 0, 1f, true), 1e-4);
        }

        [Test]
        public void Row1_normal_spends_23_PA_in_two_waves()
        {
            var planner = new EncounterPlanner();
            var p = planner.Plan(Req(1, Difficulty.Normal, "mixed_patrol"), 7UL);
            Assert.AreEqual(23, p.SpendableBudget);
            Assert.AreEqual(23, p.TotalThreat);
            Assert.IsFalse(p.UnderBudget);
            Assert.AreEqual(2, p.Waves.Length);
            AssertNoViolations(p, EncounterConfig.Default(), "mixed_patrol", "row1");
        }

        [Test]
        public void Row12_eclipse_spends_the_72_PA_that_fit_in_three_waves()
        {
            var planner = new EncounterPlanner();
            var p = planner.Plan(Req(12, Difficulty.Eclipse, "mixed_patrol"), 11UL);
            Assert.AreEqual(72, p.SpendableBudget);
            Assert.AreEqual(72, p.TotalThreat);
            Assert.AreEqual(3, p.Waves.Length);
            AssertNoViolations(p, EncounterConfig.Default(), "mixed_patrol", "row12");
        }

        [Test]
        public void No_limit_violations_for_all_rows_difficulties_recipes()
        {
            var cfg = EncounterConfig.Default();
            var planner = new EncounterPlanner(cfg);
            var difficulties = new[] { Difficulty.Normal, Difficulty.Veteran, Difficulty.Nightmare, Difficulty.Eclipse, Difficulty.Ascension };
            foreach (var recipe in cfg.Recipes)
                for (int row = 1; row <= 13; row++)
                    foreach (var d in difficulties)
                        for (ulong seed = 1; seed <= 8; seed++)
                        {
                            var p = planner.Plan(Req(row, d, recipe.Id, RoomType.Combat, 100), seed);
                            AssertNoViolations(p, cfg, recipe.Id, recipe.Id + " row" + row + " " + d + " seed" + seed);
                        }
        }

        [Test]
        public void Elite_arena_respects_limits_and_elite_share()
        {
            var cfg = EncounterConfig.Default();
            var planner = new EncounterPlanner(cfg);
            for (int row = 1; row <= 12; row++)
                for (ulong seed = 1; seed <= 5; seed++)
                {
                    var p = planner.Plan(Req(row, Difficulty.Nightmare, "mixed_patrol", RoomType.Elite), seed);
                    AssertNoViolations(p, cfg, "mixed_patrol", "elite row" + row + " seed" + seed);
                    Assert.IsTrue(p.EliteShareMet, "elite share row" + row + " seed" + seed);
                    Assert.GreaterOrEqual(p.EliteThreat * 2, p.TotalThreat);
                }
        }

        [Test]
        public void Plan_is_deterministic_and_seed_sensitive()
        {
            var planner = new EncounterPlanner();
            string Sig(EncounterPlan p)
            {
                var s = p.RecipeId + ":";
                foreach (var w in p.Waves) { foreach (var u in w.Units) s += u.EnemyId + ","; s += "|"; }
                return s;
            }
            var a = planner.Plan(Req(9, Difficulty.Veteran), 42UL);
            var b = planner.Plan(Req(9, Difficulty.Veteran), 42UL);
            Assert.AreEqual(Sig(a), Sig(b));
            bool differs = false;
            for (ulong s = 43; s < 53 && !differs; s++) differs = Sig(planner.Plan(Req(9, Difficulty.Veteran), s)) != Sig(a);
            Assert.IsTrue(differs);
        }

        [Test]
        public void Pool_without_frontline_returns_error_not_exception()
        {
            var req = Req(3, Difficulty.Normal, "mixed_patrol");
            req.Pool = new List<EnemyCandidate> { new EnemyCandidate("enemy.c", EnemyArchetype.Caster, 5) };
            var p = new EncounterPlanner().Plan(req, 1UL);
            Assert.IsNotNull(p.Error);
            Assert.AreEqual(0, p.Waves.Length);
            Assert.IsTrue(p.UnderBudget);
        }

        [Test]
        public void Runtime_rules_follow_09_section_10_2()
        {
            Assert.IsTrue(EncounterRuntimeRules.ShouldSpawnNextWave(2f, 10f, 3f));    // 20% < 30%
            Assert.IsFalse(EncounterRuntimeRules.ShouldSpawnNextWave(3f, 10f, 3f));   // exactly 30% is not below
            Assert.IsFalse(EncounterRuntimeRules.ShouldSpawnNextWave(9f, 10f, 24.9f));
            Assert.IsTrue(EncounterRuntimeRules.ShouldSpawnNextWave(9f, 10f, 25f));
            Assert.IsTrue(EncounterRuntimeRules.IsValidSpawnDistance(8f));
            Assert.IsFalse(EncounterRuntimeRules.IsValidSpawnDistance(7.99f));
            Assert.AreEqual(0.8f, EncounterRuntimeRules.PortalSeconds(), 1e-6);
        }
    }
}
