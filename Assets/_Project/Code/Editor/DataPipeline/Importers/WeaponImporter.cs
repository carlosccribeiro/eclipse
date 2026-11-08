using System.Collections.Generic;
using Eclipse.Core.DataTables;
using Eclipse.Data.Combat;
using Eclipse.Data.Items;
using UnityEngine;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/HitFeedback.csv into HitFeedbackProfile assets (06 section 3.11).</summary>
    [TableImporter("HitFeedback", Order = 10)]
    public sealed class HitFeedbackImporter : TableImporter<HitFeedbackProfile>
    {
        public override string CsvPath => "HitFeedback.csv";
        public override string OutputFolder => "Assets/_Project/Content/Heroes/Kael/Weapons/Feedback";

        protected override void Apply(HitFeedbackProfile definition, CsvRow row, ImportContext context)
        {
            row.GetString("name_pt");
            definition.EditorSetNameEn(row.GetString("name_en"));
            definition.EditorSetSourceDoc(row.GetString("source_doc"));
            definition.EditorSet(row.GetEnum<WeaponWeightClass>("weight_class"), row.GetInt("crit_hitstop_bonus_f"),
                row.GetInt("min_hitstop_spacing_ms"), row.GetFloat("shake_trauma"), row.GetFloat("shake_trauma_crit"),
                row.GetFloat("trauma_decay_per_s"), row.GetInt("flash_f"), row.GetFloat("squash_s"));
        }
    }

    /// <summary>Imports DataSource/AttackSteps.csv into AttackChainDefinition assets (06 sections 3.5-3.6, 08 section 4.3).</summary>
    [TableImporter("AttackSteps", Order = 20)]
    public sealed class AttackStepImporter : TableImporter<AttackChainDefinition>
    {
        public override string CsvPath => "AttackSteps.csv";
        public override string OutputFolder => "Assets/_Project/Content/Heroes/Kael/Weapons";

        protected override void Apply(AttackChainDefinition definition, CsvRow row, ImportContext context)
        {
            row.GetString("name_pt");
            definition.EditorSetNameEn(row.GetString("name_en"));
            definition.EditorSetSourceDoc(row.GetString("source_doc"));

            // Percentages are numbers in the CSV (110 = 110%) and fractions in the asset.
            var step = new AttackStep(
                row.GetInt("startup_f"), row.GetInt("active_f"), row.GetInt("recovery_f"),
                row.GetEnum<AttackHitShape>("shape"), row.GetFloat("radius_m", 0f), row.GetFloat("arc_deg", 0f),
                row.GetFloat("length_m", 0f), row.GetFloat("width_m", 0f),
                new Vector3(row.GetFloat("offset_x", 0f), row.GetFloat("offset_y", 0f), row.GetFloat("offset_z", 0f)),
                row.GetFloat("stagger"), row.GetFloat("stagger_max"), row.GetInt("hitstop_f"),
                row.GetFloat("proc_coef"), row.GetFloat("damage_pct") / 100f, row.GetFloat("damage_pct_max") / 100f,
                row.GetFloat("lunge_min_m", 0f), row.GetFloat("lunge_max_m", 0f), row.GetFloat("lunge_cone_deg", 0f),
                row.GetInt("hit_group", 0), row.GetInt("rehit_f", 0), row.GetBool("tracking", false), row.GetBool("commit", false),
                row.GetInt("charge_min_f", 0), row.GetInt("charge_max_f", 0));
            definition.EditorSet(row.GetString("weapon_id"), row.GetEnum<AttackKind>("kind"), row.GetInt("combo_index", 0), step);
        }
    }

    /// <summary>Imports DataSource/Weapons.csv into WeaponDefinition assets (02 section 3.4, 06 section 3.6, 08 section 4.3).</summary>
    [TableImporter("Weapons", Order = 50, DependsOn = new[] { "ItemBases", "AttackSteps", "HitFeedback" })]
    public sealed class WeaponImporter : TableImporter<WeaponDefinition>
    {
        public override string CsvPath => "Weapons.csv";
        public override string OutputFolder => "Assets/_Project/Content/Heroes/Kael/Weapons";

        protected override void Apply(WeaponDefinition definition, CsvRow row, ImportContext context)
        {
            row.GetString("name_pt");
            definition.EditorSetNameEn(row.GetString("name_en"));
            definition.EditorSetSourceDoc(row.GetString("source_doc"));
            float aps = row.GetFloat("aps");
            if (aps <= 0f) throw new CsvFormatException("aps must be greater than 0", row.Line, "aps");
            definition.EditorSetScalars(row.GetString("hero_id"), row.GetString("base_id"),
                row.GetEnum<WeaponWeightClass>("weight_class"), aps, row.GetFloat("weapon_power_ilvl1"),
                row.GetFloat("combo_reset_s"));
        }

        protected override bool ResolveReferences(WeaponDefinition definition, CsvRow row, ImportContext context)
        {
            string owner = definition.StableIdString;
            context.Require<ItemBaseDefinition>(definition.BaseId, row.Line, owner, "base_id");
            var basic = new List<AttackChainDefinition>();
            foreach (string id in row.GetList("basic_chain"))
                basic.Add(context.Require<AttackChainDefinition>(id, row.Line, owner, "basic_chain"));
            AttackChainDefinition heavy = context.Require<AttackChainDefinition>(row.GetString("heavy_id"), row.Line, owner, "heavy_id");
            AttackChainDefinition dash = context.Require<AttackChainDefinition>(row.GetString("dash_id"), row.Line, owner, "dash_id");
            HitFeedbackProfile feedback = context.Require<HitFeedbackProfile>(row.GetString("feedback_id"), row.Line, owner, "feedback_id");
            return definition.EditorSetReferences(basic, heavy, dash, feedback);
        }
    }
}
