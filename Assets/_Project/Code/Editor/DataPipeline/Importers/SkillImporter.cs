using System;
using System.Collections.Generic;
using System.Globalization;
using Eclipse.Core.DataTables;
using Eclipse.Core.Tags;
using Eclipse.Data.Skills;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Parses the "key=value|key=value" params column shared by Skills.csv and Resources.csv.</summary>
    internal static class ParamColumn
    {
        public static List<SkillParam> Parse(CsvRow row, string column)
        {
            var result = new List<SkillParam>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string item in row.GetList(column))
            {
                int eq = item.IndexOf('=');
                if (eq <= 0 || eq == item.Length - 1)
                    throw new CsvFormatException("param '" + item + "' must be key=value", row.Line, column);
                string key = item.Substring(0, eq).Trim();
                string text = item.Substring(eq + 1).Trim();
                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    throw new CsvFormatException("param '" + key + "' has a non numeric value '" + text + "'", row.Line, column);
                if (!seen.Add(key))
                    throw new CsvFormatException("duplicate param '" + key + "'", row.Line, column);
                result.Add(new SkillParam(key, value));
            }
            return result;
        }
    }

    /// <summary>
    /// Imports DataSource/Skills.csv into SkillDefinition assets (08 section 4.4, 11 section 3.4.2). Only numbers and flags are applied;
    /// the Delivery/Effects/Triggers authored in the asset are preserved (11 section 3.15).
    /// </summary>
    [TableImporter("Skills", Order = 20, DependsOn = new[] { "Resources" })]
    public sealed class SkillImporter : TableImporter<SkillDefinition>
    {
        public override string CsvPath => "Skills.csv";
        public override string OutputFolder => "Assets/_Project/Content/Heroes/Kael/Skills";

        protected override void Apply(SkillDefinition definition, CsvRow row, ImportContext context)
        {
            row.GetString("name_pt");
            definition.EditorSetNameEn(row.GetString("name_en"));
            definition.EditorSetSourceDoc(row.GetString("source_doc"));

            string tagText = row.GetString("tags");
            if (!TagMask.TryParse(tagText, out TagMask tags))
                throw new CsvFormatException("invalid tags '" + tagText + "' (vocabulary 00 section 3.4)", row.Line, "tags");
            int moveDuringCast = row.GetInt("move_during_cast_pct", 0);
            if (moveDuringCast != 0 && moveDuringCast != 50 && moveDuringCast != 100)
                throw new CsvFormatException("move_during_cast_pct must be 0, 50 or 100 (06 section 3.5)", row.Line, "move_during_cast_pct");

            List<SkillParam> parameters = ParamColumn.Parse(row, "params");
            RequirePhaseFrames(parameters, row);
            // Percentages are numbers in the CSV (220 = 220%) and fractions in the asset.
            definition.EditorSetNumbers(row.GetString("hero_id"), row.GetEnum<SkillSlot>("slot"), tags,
                row.GetString("cost_resource_id", string.Empty), row.GetFloat("cost_amount", 0f), row.GetBool("cost_all_resource", false),
                row.GetFloat("cost_minimum", 0f), row.GetFloat("resource_gain", 0f), row.GetFloat("cooldown_s", 0f),
                row.GetFloat("damage_coef_pct", 0f) / 100f, row.GetFloat("coef_per_resource_pct", 0f) / 100f, moveDuringCast,
                row.GetBool("commit", false), row.GetBool("tracking", false), row.GetInt("modifier_slots"),
                parameters);
        }

        /// <summary>
        /// Every skill must carry its phase frames (06 3.5, 60 fps) as the params windup_f, active_f and recovery_f: whole numbers
        /// &gt;= 0. Values that the docs do not define are marked TUNED in the "notes" column.
        /// </summary>
        private static void RequirePhaseFrames(List<SkillParam> parameters, CsvRow row)
        {
            foreach (string key in new[] { "windup_f", "active_f", "recovery_f" })
            {
                bool found = false;
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i].Key != key) continue;
                    found = true;
                    float v = parameters[i].Value;
                    if (v < 0f || v != Math.Floor(v))
                        throw new CsvFormatException("param '" + key + "' must be a whole number of frames >= 0", row.Line, "params");
                }
                if (!found) throw new CsvFormatException("missing param '" + key + "' (phase frames)", row.Line, "params");
            }
        }
    }
}
