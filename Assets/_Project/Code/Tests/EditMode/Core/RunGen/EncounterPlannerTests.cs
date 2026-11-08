using System;
using System.Collections.Generic;
using System.Text;
using Eclipse.Core.Loot;
using Eclipse.Core.RunGen.Map;
using NUnit.Framework;

namespace Eclipse.Core.RunGen.Encounters.Tests
{
    /// <summary>
    /// Property tests of the encounter planner (M15 T4, 09 sections 5.1 and 10). The checks below are written
    /// independently of the planner's own gate: limits are literal numbers of 09 section 10.3 recomputed from the
    /// planned units, not read back from the planner counters.
    /// </summary>
    [TestFixture]
    [Category("RunGen")]
    public sealed class EncounterPlannerTests
    {
#if UNITY_5_3_OR_NEWER
        private const int SeedsPerCombo = 2;       // ~500 plans in the Unity EditMode run
        private const int EliteSeedsPerCombo = 1;
#else
        private const int SeedsPerCombo = 30;      // 13 x 4 x 7 x 30 = 10,920 plans (+ 1,820 Elite Arena)
        private const int EliteSeedsPerCombo = 5;
#endif
        private static readonly Difficulty[] Difficulties = { Difficulty.Normal, Difficulty.Veteran, Difficulty.Nightmare, Difficulty.Eclipse };
        private static readonly string[] RecipeIds = { "swarm", "phalanx", "siege", "ambush", "ritual", "brood", "mixed_patrol" };

        /// <summary>Ashen Wastes bestiary, 09 section 5.1 (same fixture as EncounterPlannerSmoke).</summary>
        private static List<EnemyCandidate> AshenWastes(bool withElite)
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

        private static EncounterRequest Req(int row, Difficulty d, string recipe, bool arena) => new EncounterRequest
        {
            Row = row, Difficulty = d, RecipeId = recipe,
            RoomType = arena ? RoomType.Elite : RoomType.Combat,
            Pool = AshenWastes(arena),
        };

        private static double Density(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Normal: return 1.0;
                case Difficulty.Veteran: return 1.1;
                case Difficulty.Nightmare: return 1.2;
                default: return 1.3; // Eclipse
            }
        }

        /// <summary>09 section 10.1: (20 + 3 x row) x density, Elite Arena x 1.2.</summary>
        private static double ExpectedBudget(int row, Difficulty d, bool arena) => (20.0 + 3.0 * row) * Density(d) * (arena ? 1.2 : 1.0);

        /// <summary>Returns null when the plan satisfies every limit, otherwise a description of the first violation.</summary>
        private static string Check(EncounterPlan p, int row, Difficulty d, string recipe, bool arena)
        {
            if (p.Error != null) return "error: " + p.Error;
            double budget = ExpectedBudget(row, d, arena);
            if (Math.Abs(p.Budget - budget) > 1e-3) return "budget " + p.Budget + " != " + budget;
            int spendable = (int)Math.Floor(budget + 1e-6);
            int expWaves = row <= 4 ? 2 : 3;
            if (p.Waves.Length > expWaves || p.Waves.Length == 0) return "waves " + p.Waves.Length + " expected <= " + expWaves;

            int total = 0, elite = 0;
            int summonerCap = recipe == "brood" ? 2 : 1; // themed recipe exception, documented in EncounterRecipe/Planner
            int tankCap = row <= 6 ? 1 : 2;
            foreach (var w in p.Waves)
            {
                int pa = 0, casters = 0, summoners = 0, tanks = 0, front = 0, spec = 0;
                foreach (var u in w.Units)
                {
                    pa += u.Threat;
                    if (u.IsElite) elite += u.Threat;
                    switch (u.Archetype)
                    {
                        case EnemyArchetype.Caster: casters++; spec += u.Threat; break;
                        case EnemyArchetype.Summoner: summoners++; spec += u.Threat; break;
                        case EnemyArchetype.Assassin:
                        case EnemyArchetype.Support: spec += u.Threat; break;
                        case EnemyArchetype.Tank: tanks++; front++; break;
                        case EnemyArchetype.Fodder:
                        case EnemyArchetype.Standard: front++; break;
                    }
                }
                total += pa;
                if (w.Units.Count > 24) return "units " + w.Units.Count;
                if (pa > 40) return "wave PA " + pa;
                if (casters > 2) return "casters " + casters;
                if (summoners > summonerCap) return "summoners " + summoners;
                if (spec > 0.40 * pa + 1e-9) return "specialist " + spec + "/" + pa;
                if (front < 1) return "no frontline";
                if (tanks > tankCap) return "tanks " + tanks + " cap " + tankCap;
            }
            if (total > spendable) return "overshoot " + total + " > " + spendable;
            if (arena && elite < 0.5 * total - 1e-9) return "elite share " + elite + "/" + total;
            return null;
        }

        [Test]
        public void Plans_respect_every_limit_and_never_overshoot_the_budget()
        {
            var planner = new EncounterPlanner();
            int plans = 0;
            var failures = new List<string>();
            double usedSum = 0, spendSum = 0;
            int under = 0, ok90 = 0, nonArena = 0;

            for (int arenaPass = 0; arenaPass < 2; arenaPass++)
            {
                bool arena = arenaPass == 1;
                int seeds = arena ? EliteSeedsPerCombo : SeedsPerCombo;
                for (int row = 1; row <= 13; row++)
                    foreach (var d in Difficulties)
                        foreach (var recipe in RecipeIds)
                            for (ulong seed = 1; seed <= (ulong)seeds; seed++)
                            {
                                var p = planner.Plan(Req(row, d, recipe, arena), seed);
                                plans++;
                                string err = Check(p, row, d, recipe, arena);
                                if (err != null && failures.Count < 20)
                                    failures.Add("row=" + row + " diff=" + d + " recipe=" + recipe + " arena=" + arena + " seed=" + seed + " -> " + err);
                                else if (err != null) failures.Add(null);
                                if (arena) continue;
                                nonArena++;
                                int spendable = (int)Math.Floor(ExpectedBudget(row, d, false) + 1e-6);
                                usedSum += p.TotalThreat; spendSum += spendable;
                                if (p.UnderBudget) under++;
                                if (p.TotalThreat >= 0.9 * spendable) ok90++;
                            }
            }

#if !UNITY_5_3_OR_NEWER
            Assert.GreaterOrEqual(plans, 10000);
#endif
            TestContext.Out.WriteLine("plans=" + plans + " budgetUse(mean)=" + (usedSum / spendSum).ToString("P2") +
                " >=90%=" + ((double)ok90 / nonArena).ToString("P2") + " UnderBudget=" + ((double)under / nonArena).ToString("P2"));

            if (failures.Count > 0)
            {
                var sb = new StringBuilder();
                int shown = 0, count = 0;
                foreach (var f in failures) { count++; if (f != null && shown++ < 10) sb.AppendLine(f); }
                Assert.Fail(count + " violations in " + plans + " plans. First:\n" + sb);
            }
        }

        /// <summary>
        /// Interpretation of 09 section 10.1 budget use: the planner flags UnderBudget whenever any PA is left,
        /// which is stricter than the criterion. So the criterion is measured as: among non-arena plans, at least
        /// 95% spend >= 90% of the spendable budget (floor of the exact budget).
        /// </summary>
        [Test]
        public void At_least_95_percent_of_plans_use_90_percent_of_the_budget()
        {
            var planner = new EncounterPlanner();
            int n = 0, ok = 0, under = 0;
            double used = 0, spend = 0, worst = 1;
            string worstCase = "";
            for (int row = 1; row <= 13; row++)
                foreach (var d in Difficulties)
                    foreach (var recipe in RecipeIds)
                        for (ulong seed = 1; seed <= (ulong)SeedsPerCombo; seed++)
                        {
                            var p = planner.Plan(Req(row, d, recipe, false), seed);
                            int spendable = (int)Math.Floor(ExpectedBudget(row, d, false) + 1e-6);
                            double ratio = (double)p.TotalThreat / spendable;
                            n++; used += p.TotalThreat; spend += spendable;
                            if (ratio >= 0.9) ok++;
                            if (p.UnderBudget) under++;
                            if (ratio < worst) { worst = ratio; worstCase = "row=" + row + " diff=" + d + " recipe=" + recipe + " seed=" + seed; }
                        }
            double frac = (double)ok / n;
            TestContext.Out.WriteLine("n=" + n + " mean use=" + (used / spend).ToString("P2") + " >=90% in " + frac.ToString("P2") +
                " UnderBudget=" + ((double)under / n).ToString("P2") + " worst=" + worst.ToString("P1") + " (" + worstCase + ")");
            Assert.GreaterOrEqual(frac, 0.95, "share of plans using >=90% of budget; worst " + worst.ToString("P1") + " at " + worstCase);
        }

        [Test]
        public void Elite_arena_budget_is_1_2x_and_elite_share_is_at_least_half()
        {
            var planner = new EncounterPlanner();
            for (int row = 1; row <= 13; row++)
                foreach (var d in Difficulties)
                {
                    var plain = planner.Plan(Req(row, d, "mixed_patrol", false), 1UL);
                    var arena = planner.Plan(Req(row, d, "mixed_patrol", true), 1UL);
                    Assert.AreEqual(plain.Budget * 1.2, arena.Budget, 1e-3, "row " + row + " " + d);
                    Assert.IsNull(arena.Error);
                    Assert.GreaterOrEqual(arena.EliteThreat * 2, arena.TotalThreat, "elite share row " + row + " " + d);
                }
        }

        [Test]
        public void Wave_count_is_two_through_row_4_and_three_from_row_5()
        {
            var planner = new EncounterPlanner();
            for (int row = 1; row <= 13; row++)
                for (ulong seed = 1; seed <= 10; seed++)
                {
                    var p = planner.Plan(Req(row, Difficulty.Normal, "mixed_patrol", false), seed);
                    Assert.AreEqual(row <= 4 ? 2 : 3, p.Waves.Length, "row " + row + " seed " + seed);
                }
        }

        [Test]
        public void Budget_examples_of_09_section_10_1()
        {
            var cfg = EncounterConfig.Default();
            Assert.AreEqual(23.0, ThreatBudget.Compute(cfg, 1, Difficulty.Normal, 0, 1f, false), 1e-6);
            Assert.AreEqual(72.8, ThreatBudget.Compute(cfg, 12, Difficulty.Eclipse, 0, 1f, false), 1e-4);
        }

        [Test]
        public void Runtime_rules_next_wave_spawn_distance_and_portal()
        {
            // Next wave: alive PA below 30% of the wave PA, or 25 s elapsed.
            Assert.IsTrue(EncounterRuntimeRules.ShouldSpawnNextWave(2.9f, 10f, 0f));
            Assert.IsFalse(EncounterRuntimeRules.ShouldSpawnNextWave(3f, 10f, 24.99f));
            Assert.IsFalse(EncounterRuntimeRules.ShouldSpawnNextWave(10f, 10f, 0f));
            Assert.IsTrue(EncounterRuntimeRules.ShouldSpawnNextWave(10f, 10f, 25f));
            Assert.IsTrue(EncounterRuntimeRules.ShouldSpawnNextWave(0f, 10f, 0f));
            // Spawn at least 8 m from the player.
            Assert.IsTrue(EncounterRuntimeRules.IsValidSpawnDistance(8f));
            Assert.IsTrue(EncounterRuntimeRules.IsValidSpawnDistance(20f));
            Assert.IsFalse(EncounterRuntimeRules.IsValidSpawnDistance(7.9f));
            Assert.IsFalse(EncounterRuntimeRules.IsValidSpawnDistance(0f));
            // Portal 0.8 s.
            Assert.AreEqual(0.8f, EncounterRuntimeRules.PortalSeconds(), 1e-6);
        }

        [Test]
        public void Same_seed_gives_the_same_plan()
        {
            var planner = new EncounterPlanner();
            string Sig(EncounterPlan p)
            {
                var sb = new StringBuilder(p.RecipeId).Append(':').Append(p.TotalThreat).Append(':');
                foreach (var w in p.Waves) { foreach (var u in w.Units) sb.Append(u.EnemyId).Append(','); sb.Append('|'); }
                return sb.ToString();
            }
            for (int row = 1; row <= 13; row++)
                foreach (var d in Difficulties)
                    for (ulong seed = 1; seed <= 5; seed++)
                    {
                        bool arena = seed % 2 == 0;
                        // Recipe left null: the recipe draw must be deterministic too.
                        var a = planner.Plan(Req(row, d, null, arena), seed);
                        var b = new EncounterPlanner().Plan(Req(row, d, null, arena), seed);
                        Assert.AreEqual(Sig(a), Sig(b), "row " + row + " " + d + " seed " + seed);
                    }
        }
    }
}
