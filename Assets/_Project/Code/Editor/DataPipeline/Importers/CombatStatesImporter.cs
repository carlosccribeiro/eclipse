using Eclipse.Core.DataTables;
using Eclipse.Data.Combat;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/CombatStates.csv into CombatStatesDefinition (06 section 3.8, R09, R11).</summary>
    [TableImporter("CombatStates", Order = 30)]
    public sealed class CombatStatesImporter : TableImporter<CombatStatesDefinition>
    {
        public override string CsvPath => "CombatStates.csv";
        public override string OutputFolder => "Assets/_Project/Content/Shared/CombatStates";

        protected override void Apply(CombatStatesDefinition definition, CsvRow row, ImportContext context)
        {
            row.GetString("name_pt");
            definition.EditorSetNameEn(row.GetString("name_en"));
            definition.EditorSetSourceDoc(row.GetString("source_doc"));

            // *_pct columns are numbers in the CSV (15 = 15%) and fractions in the asset.
            float Pct(string col) => row.GetFloat(col) / 100f;
            var v = new CombatStatesValues
            {
                FlinchMinStaggerLight = row.GetFloat("flinch_min_light"),
                FlinchMinStaggerMedium = row.GetFloat("flinch_min_medium"),
                EliteFlinchMult = row.GetFloat("elite_flinch_mult"),
                KnockbackLight = Pct("knockback_light_pct"),
                KnockbackMedium = Pct("knockback_medium_pct"),
                KnockbackHeavy = Pct("knockback_heavy_pct"),
                KnockbackColossal = Pct("knockback_colossal_pct"),
                EliteKnockbackMult = row.GetFloat("elite_knockback_mult"),
                EliteCcDurationMult = row.GetFloat("elite_cc_duration_mult"),
                StaggerThresholdMedium = row.GetFloat("stagger_medium"),
                StaggerThresholdHeavy = row.GetFloat("stagger_heavy"),
                StaggerThresholdEliteMult = row.GetFloat("stagger_elite_mult"),
                StaggerThresholdGuardian = row.GetFloat("stagger_guardian"),
                StaggerThresholdBoss = row.GetFloat("stagger_boss"),
                DifficultyNormal = row.GetFloat("diff_normal"),
                DifficultyVeteran = row.GetFloat("diff_veteran"),
                DifficultyNightmare = row.GetFloat("diff_nightmare"),
                DifficultyEclipse = row.GetFloat("diff_eclipse"),
                DifficultyAscensionPerLevel = Pct("diff_ascension_per_level_pct"),
                StaggerDecayDelaySeconds = row.GetFloat("decay_delay_s"),
                StaggerDecayFractionPerSecond = Pct("decay_pct_per_s"),
                StaggeredSeconds = row.GetFloat("staggered_s"),
                StaggeredEliteSeconds = row.GetFloat("staggered_elite_s"),
                StaggeredGuardianSeconds = row.GetFloat("staggered_guardian_s"),
                StaggeredVulnerability = Pct("staggered_vuln_pct"),
                BrokenSeconds = row.GetFloat("broken_s"),
                BrokenVulnerability = Pct("broken_vuln_pct"),
                BrokenHitStopFrames = row.GetInt("broken_hitstop_f"),
                BreakThresholdGrowth = Pct("break_growth_pct"),
                BreakThresholdMaxGrowths = row.GetInt("break_max_growths"),
                CcToStaggerPerSecond = row.GetFloat("cc_to_stagger_per_s"),
                CcDrWindowSeconds = row.GetFloat("dr_window_s"),
                CcDrFirst = Pct("dr_1_pct"),
                CcDrSecond = Pct("dr_2_pct"),
                CcDrThird = Pct("dr_3_pct"),
                PlayerHardMaxSeconds = row.GetFloat("player_hard_max_s"),
                PlayerFreezeMaxSeconds = row.GetFloat("player_freeze_max_s"),
                PlayerRootMaxSeconds = row.GetFloat("player_root_max_s"),
                PlayerHardImmunitySeconds = row.GetFloat("player_hard_immunity_s"),
                TauntMaxSeconds = row.GetFloat("taunt_max_s"),
                TauntEliteMult = row.GetFloat("taunt_elite_mult"),
                WallImpactDamageFraction = Pct("wall_impact_damage_pct"),
                WallImpactStunSeconds = row.GetFloat("wall_impact_stun_s"),
                SlowEnemyCap = Pct("slow_enemy_cap_pct"),
                SlowPlayerMinSpeed = Pct("slow_player_min_speed_pct"),
                StealthMaxSeconds = row.GetFloat("stealth_max_s"),
                StealthReenterIcdSeconds = row.GetFloat("stealth_icd_s"),
                StealthRevealMeters = row.GetFloat("stealth_reveal_m"),
                ParryWindowFrames = row.GetInt("parry_window_f"),
                ParryCapSeconds = row.GetFloat("parry_cap_s"),
                ParryAttackerStagger = row.GetFloat("parry_attacker_stagger"),
                PlayerFlinchFrames = row.GetInt("player_flinch_f"),
                PlayerFlinchCancelFrame = row.GetInt("player_flinch_cancel_f"),
                KnockdownFrames = row.GetInt("knockdown_f"),
                KnockdownInvulnerableFrames = row.GetInt("knockdown_iframes_f"),
            };
            definition.EditorSet(v);
        }
    }
}
