using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Data.Definitions;
using Eclipse.Editor.DataPipeline;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Data.Items.Tests
{
    /// <summary>Scratch folder + temp DataSource root for importer tests (same pattern as the Affinity pilot tests).</summary>
    public abstract class ItemImportTestBase
    {
        protected const string Folder = "Assets/_Project/Content/__ItemImportTests__";
        protected string Root;

        [SetUp]
        public void BaseSetUp()
        {
            AssetDatabase.DeleteAsset(Folder);
            Root = Path.Combine(Path.GetTempPath(), "eclipse_items_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        [TearDown]
        public void BaseTearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.Refresh();
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }

        protected static string RepoRoot => Directory.GetParent(Application.dataPath).FullName;
        protected static string AbsPath(string assetPath) => Path.Combine(RepoRoot, assetPath);
        protected static string RealCsv(string file) => Path.Combine(RepoRoot, "DataSource", file);
        protected static string Sub(string name) => Folder + "/" + name;

        protected static string Csv(params string[] lines) => string.Join("\n", lines) + "\n";

        protected void Write(string file, string text) => File.WriteAllText(Path.Combine(Root, file), text);

        protected ImportContext Run(params ITableImporter[] importers)
        {
            ImportContext ctx = DataPipeline.Run(importers, Root);
            AssetDatabase.SaveAssets();
            return ctx;
        }

        protected static List<T> LoadAll<T>(string folder) where T : Definition =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(d => d.StableIdString, StringComparer.Ordinal).ToList();

        protected static string Errors(ImportContext ctx) => string.Join("\n", ctx.Messages);

        // Shared valid fixtures (one row each) used by importers that reference them.
        protected const string AffixHeader =
            "id,name_pt,name_en,abbrev,stat,value_kind,category,group,affinity,relevance_tags,t1_min,t1_max,t2_min,t2_max,t3_min,t3_max,t4_min,t4_max,t5_min,t5_max,source_doc";
        protected const string AffixEmberRow =
            "affix.core.ember_damage,Dano de Ember,Ember Damage,EMB,EmberDamage,Percent,Offensive,Affinity,Ember,,1,2,3,4,5,6,7,8,10,12.5,01 §3.9.2";
        protected const string AffixStrengthRow =
            "affix.core.strength,Força,Strength,STR,Strength,Flat,Primary,Primary,,,3,6,6,12,11,18,16,26,22,35,01 §3.9.2";
        protected const string AffixInfusionRow =
            "affix.core.infusion_ember_added,Infusão Ember,Ember Infusion,INF-EMB-ADD,,Percent,Infusion,Affinity,Ember,,1,2,3,4,5,6,7,8,10,12,01 §3.9.2";
        protected const string BaseHeader =
            "id,name_pt,name_en,slot,category,family,hero_id,tags,implicit_affix_id,implicit_factor,armor_factor,droppable,dps_factor,implicit_affinity,secondary_implicit_stat,secondary_implicit_at_100,implicit_effect_id,source_doc";
        protected const string BaseGreatswordRow =
            "base.kael.greatsword,Montante,Greatsword,MainHand,Greatsword,None,kael,Attack|Melee,affix.core.ember_damage,0.5,0,true,1.05,Ember,DodgeChance,2,,02 §3.4";
        protected const string BaseCuirassRow =
            "base.armor.cuirass,Peitoral,Cuirass,Chest,Cuirass,Plate,,,affix.core.strength,0.5,1.5,true,,,,,,02 §3.6";
    }
}
