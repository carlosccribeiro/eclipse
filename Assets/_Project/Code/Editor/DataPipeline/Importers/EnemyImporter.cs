using System;
using System.Collections.Generic;
using System.Text;
using Eclipse.Core.Damage;
using Eclipse.Core.DataTables;
using Eclipse.Core.RunGen.Encounters;
using Eclipse.Core.Tags;
using Eclipse.Data.Difficulties;
using Eclipse.Data.Enemies;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>
    /// Imports DataSource/EnemyAttacks.csv into AttackDefinition assets (09 sections 3.1, 3.3, 5.1; 06 section 3.10).
    /// Percent columns (<c>*_pct</c>) are numbers in the CSV and fractions in the asset. Rows whose values the docs do
    /// not define carry <c>provisional</c> in the notes column.
    /// </summary>
    [TableImporter("EnemyAttacks", Order = 10)]
    public sealed class EnemyAttackImporter : TableImporter<AttackDefinition>
    {
        public override string CsvPath => "EnemyAttacks.csv";
        public override string OutputFolder => "Assets/_Project/Content/Enemies/AshenWastes/Attacks";

        protected override void Apply(AttackDefinition d, CsvRow row, ImportContext context)
        {
            row.GetString("name_pt");
            d.EditorSetNameEn(row.GetString("name_en"));
            d.EditorSetSourceDoc(row.GetString("source_doc"));

            float windup = row.GetFloat("windup_s");
            if (windup <= 0f) throw new CsvFormatException("windup_s must be greater than 0", row.Line, "windup_s");
            float chance = row.GetFloat("trigger_chance_pct", 100f) / 100f;
            if (chance <= 0f || chance > 1f) throw new CsvFormatException("trigger_chance_pct must be in (0, 100]", row.Line, "trigger_chance_pct");

            HitFlags flags = HitFlags.None;
            foreach (string item in row.GetList("hit_flags"))
            {
                if (!Enum.TryParse(item, false, out HitFlags f) || f == HitFlags.None || !Enum.IsDefined(typeof(HitFlags), f))
                    throw new CsvFormatException("unknown HitFlags '" + item + "'", row.Line, "hit_flags");
                flags |= f;
            }

            d.EditorSetScalars(
                row.GetEnum<AttackTrigger>("trigger"), row.GetEnum<AttackDelivery>("delivery"),
                row.GetFloat("cooldown_s"), row.GetFloat("range_m"), row.GetFloat("weight"), row.GetString("condition", string.Empty),
                row.GetEnum<TelegraphType>("telegraph_type"), row.GetEnum<TelegraphShape>("telegraph_shape"),
                row.GetFloat("radius_m", 0f), row.GetFloat("arc_deg", 0f), row.GetFloat("length_m", 0f),
                row.GetFloat("width_m", 0f), row.GetFloat("inner_radius_m", 0f),
                windup, row.GetInt("active_f"), row.GetInt("recovery_f"), row.GetFloat("damage_rel"), row.GetFloat("stagger"),
                flags, row.GetInt("token_cost"), row.GetEnum<TokenKind>("token_kind"), row.GetBool("interruptible"),
                chance, row.GetFloat("projectile_speed_mps", 0f), row.GetString("zone_status", string.Empty),
                row.GetFloat("zone_duration_s", 0f));
        }

        protected override bool ResolveReferences(AttackDefinition d, CsvRow row, ImportContext context)
        {
            string next = row.GetString("chain_next", string.Empty);
            AttackDefinition target = next.Length == 0
                ? null
                : context.Require<AttackDefinition>(next, row.Line, d.StableIdString, "chain_next");
            return d.EditorSetChainNext(target);
        }
    }

    /// <summary>Imports DataSource/Enemies.csv into EnemyDefinition assets (11 section 3.4.2, 09 sections 3.1, 4, 5.1).</summary>
    [TableImporter("Enemies", Order = 20, DependsOn = new[] { "EnemyAttacks" })]
    public sealed class EnemyImporter : TableImporter<EnemyDefinition>
    {
        public override string CsvPath => "Enemies.csv";
        public override string OutputFolder => "Assets/_Project/Content/Enemies/AshenWastes/Enemies";

        protected override void Apply(EnemyDefinition d, CsvRow row, ImportContext context)
        {
            row.GetString("name_pt");
            d.EditorSetNameEn(row.GetString("name_en"));
            d.EditorSetSourceDoc(row.GetString("source_doc"));

            float hp = row.GetFloat("hp_mult");
            float damage = row.GetFloat("damage_mult");
            if (hp <= 0f) throw new CsvFormatException("hp_mult must be greater than 0", row.Line, "hp_mult");
            if (damage <= 0f) throw new CsvFormatException("damage_mult must be greater than 0", row.Line, "damage_mult");
            float speed = row.GetFloat("move_speed_mps");
            if (speed <= 0f) throw new CsvFormatException("move_speed_mps must be greater than 0", row.Line, "move_speed_mps");
            float rangeMin = row.GetFloat("preferred_range_min_m");
            float rangeMax = row.GetFloat("preferred_range_max_m");
            if (rangeMin < 0f || rangeMax < rangeMin)
                throw new CsvFormatException("preferred range must satisfy 0 <= min <= max", row.Line, "preferred_range_max_m");

            var resist = new float[Enum.GetValues(typeof(DamageType)).Length];
            foreach (DamageType t in Enum.GetValues(typeof(DamageType)))
                resist[(int)t] = row.GetFloat("res_" + t.ToString().ToLowerInvariant() + "_pct", 0f) / 100f;

            ulong bits = 0UL;
            foreach (string item in row.GetList("tags"))
            {
                if (!Enum.TryParse(item, false, out Tag tag) || !Enum.IsDefined(typeof(Tag), tag))
                    throw new CsvFormatException("unknown Tag '" + item + "' (vocabulary: 00 section 3.4)", row.Line, "tags");
                bits |= 1UL << (int)tag;
            }

            DamageType element = row.GetEnum<DamageType>("base_element");
            d.EditorSetScalars(row.GetEnum<EnemyArchetype>("archetype"), row.GetString("faction"), hp, damage,
                row.GetInt("pa"), row.GetEnum<EnemyMassClass>("mass_class"), row.GetFloat("stagger_threshold"), speed,
                element, row.GetEnum<DamageType>("secondary_element", element), resist,
                row.GetFloat("status_res_pct", 0f) / 100f, row.GetEnum<EnemyLootClass>("loot_class"), bits,
                rangeMin, rangeMax, row.GetEnum<RepositionBehavior>("reposition"),
                row.GetFloat("retreat_below_m", 0f), row.GetFloat("retreat_cooldown_s", 0f),
                row.GetFloat("front_plate_reduction_pct", 0f) / 100f, row.GetFloat("front_plate_arc_deg", 0f),
                row.GetString("hfsm_template"));
        }

        protected override bool ResolveReferences(EnemyDefinition d, CsvRow row, ImportContext context)
        {
            var list = new List<AttackDefinition>();
            foreach (string id in row.GetList("attacks"))
            {
                AttackDefinition a = context.Require<AttackDefinition>(id, row.Line, d.StableIdString, "attacks");
                if (a != null) list.Add(a);
            }
            return d.EditorSetAttacks(list);
        }
    }

    /// <summary>
    /// Imports DataSource/EnemyScaling.csv (<c>level,tier,std_hp,std_damage,source_doc</c>, no id column) into the
    /// single EnemyScalingDefinition asset <c>enemy.scaling.curve</c>. Idempotent: the asset is touched only when the
    /// content hash of the table changes.
    /// </summary>
    [TableImporter("EnemyScaling", Order = 10)]
    public sealed class EnemyScalingImporter : ITableImporter
    {
        public const string AssetId = "enemy.scaling.curve";

        public string TableName => "EnemyScaling";
        public string CsvPath => "EnemyScaling.csv";
        public string OutputFolder => "Assets/_Project/Content/Enemies/Scaling";

        public void Import(CsvTable table, ImportContext context)
        {
            const string name = "EnemyScaling";
            var entries = new List<EnemyScalingEntry>();
            var levels = new HashSet<int>();
            var hash = new StringBuilder();
            foreach (CsvRow row in table.Rows)
            {
                try
                {
                    var e = new EnemyScalingEntry
                    {
                        level = row.GetInt("level"),
                        tier = row.GetEnum<DifficultyTier>("tier"),
                        stdHp = row.GetFloat("std_hp"),
                        stdDamage = row.GetFloat("std_damage"),
                    };
                    row.GetString("source_doc");
                    if (e.level < 1 || e.stdHp <= 0f || e.stdDamage <= 0f)
                        throw new CsvFormatException("level must be >= 1 and std_hp/std_damage > 0", row.Line, "level");
                    if (!levels.Add(e.level))
                        throw new CsvFormatException("duplicate level " + e.level, row.Line, "level");
                    entries.Add(e);
                    hash.Append(TableImporter<EnemyScalingDefinition>.ComputeHash(table, row, string.Empty));
                }
                catch (CsvFormatException ex) { context.Error(name, ex.Line, AssetId, ex.Message); }
                catch (Exception ex) { context.Error(name, row.Line, AssetId, ex.GetType().Name + ": " + ex.Message); }
            }
            if (entries.Count == 0)
            {
                context.Error(name, 0, AssetId, "no valid rows");
                return;
            }
            entries.Sort((a, b) => a.level.CompareTo(b.level));
            string digest = Eclipse.Core.Random.RngStreams.Fnv1a64(hash.ToString()).ToString("x16");

            TableImporter<EnemyScalingDefinition>.EnsureFolder(OutputFolder);
            EnemyScalingDefinition def = null;
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyScalingDefinition", new[] { OutputFolder }))
            {
                var found = AssetDatabase.LoadAssetAtPath<EnemyScalingDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (found != null && found.StableIdString == AssetId) { def = found; break; }
            }

            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EnemyScalingDefinition>();
                def.EditorSetStableId(AssetId);
                def.EditorSetNameEn("Enemy Scaling");
                def.EditorSetSourceDoc("06 3.13 R53");
                def.EditorSetEntries(entries.ToArray());
                def.ImportedHash = digest;
                AssetDatabase.CreateAsset(def, OutputFolder + "/EnemyScaling.asset");
                context.Created++;
            }
            else if (def.ImportedHash != digest)
            {
                def.EditorSetEntries(entries.ToArray());
                def.ImportedHash = digest;
                EditorUtility.SetDirty(def);
                context.Updated++;
            }
            else
            {
                context.Unchanged++;
            }
            // The CSV has no name columns (values are owned by T0), so the display name comes from a synthetic row.
            CsvTable names = CsvTable.Parse("id,name_pt,name_en\n" + AssetId + ",Curva de Escala de Inimigos,Enemy Scaling\n");
            foreach (CsvRow nameRow in names.Rows)
            {
                if (LocalizationWriter.ApplyRow(def, AssetId, names, nameRow)) EditorUtility.SetDirty(def);
            }
            LocalizationWriter.Flush();
            context.Register(def);
        }
    }
}
