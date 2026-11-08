using System.Collections.Generic;
using Eclipse.Core.DataTables;
using Eclipse.Data.Heroes;
using Eclipse.Data.Skills;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports DataSource/Resources.csv into ResourceDefinition assets (08 sections 3.2 and 4.1).</summary>
    [TableImporter("Resources", Order = 10)]
    public sealed class ResourceImporter : TableImporter<ResourceDefinition>
    {
        public override string CsvPath => "Resources.csv";
        public override string OutputFolder => "Assets/_Project/Content/Heroes/Kael/Data";

        protected override void Apply(ResourceDefinition definition, CsvRow row, ImportContext context)
        {
            row.GetString("name_pt");
            definition.EditorSetNameEn(row.GetString("name_en"));
            definition.EditorSetSourceDoc(row.GetString("source_doc"));
            definition.EditorSet(row.GetEnum<ResourceKind>("kind"), row.GetFloat("base_max"), row.GetFloat("absolute_cap"),
                row.GetFloat("max_per_max_resource_pct", 0f), row.GetFloat("decay_delay_s", 0f), row.GetFloat("decay_per_s", 0f),
                row.GetFloat("start_of_room", 0f), ParamColumn.Parse(row, "params"));
        }
    }

    /// <summary>Imports DataSource/Heroes.csv into HeroDefinition assets (08 sections 3.1 and 4, 11 section 3.4.2).</summary>
    [TableImporter("Heroes", Order = 30, DependsOn = new[] { "Skills", "Resources" })]
    public sealed class HeroImporter : TableImporter<HeroDefinition>
    {
        private static readonly (SkillSlot slot, string column)[] SlotColumns =
        {
            (SkillSlot.Basic, "skill_basic"), (SkillSlot.Heavy, "skill_heavy"), (SkillSlot.Q, "skill_q"),
            (SkillSlot.E, "skill_e"), (SkillSlot.R, "skill_r"), (SkillSlot.Shift, "skill_shift"), (SkillSlot.F, "skill_f"),
        };

        public override string CsvPath => "Heroes.csv";
        public override string OutputFolder => "Assets/_Project/Content/Heroes/Kael/Data";

        protected override void Apply(HeroDefinition definition, CsvRow row, ImportContext context)
        {
            row.GetString("name_pt");
            definition.EditorSetNameEn(row.GetString("name_en"));
            definition.EditorSetSourceDoc(row.GetString("source_doc"));
            // Percent columns are numbers in the CSV (120 = 120%) and fractions in the asset.
            definition.EditorSetScalars(row.GetEnum<HeroPrimaryAttribute>("primary_attribute"),
                row.GetFloat("health_rel_pct") / 100f, row.GetFloat("armor_rel_pct") / 100f, row.GetFloat("move_speed_mps"),
                row.GetFloat("engagement_min_m"), row.GetFloat("engagement_max_m"), row.GetString("off_hand_category", string.Empty),
                row.GetList("weapon_categories"), row.GetList("passives"));
        }

        protected override bool ResolveReferences(HeroDefinition definition, CsvRow row, ImportContext context)
        {
            string owner = definition.StableIdString;
            ResourceDefinition resource = context.Require<ResourceDefinition>(row.GetString("resource_id"), row.Line, owner, "resource_id");
            var slots = new List<HeroSkillSlot>();
            foreach ((SkillSlot slot, string column) in SlotColumns)
            {
                if (row.IsEmpty(column)) continue;
                SkillDefinition skill = context.Require<SkillDefinition>(row.GetString(column), row.Line, owner, column);
                slots.Add(new HeroSkillSlot { slot = slot, skill = skill });
            }
            return definition.EditorSetReferences(resource, slots);
        }
    }
}
