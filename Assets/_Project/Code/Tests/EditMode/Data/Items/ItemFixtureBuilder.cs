using System;
using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using Eclipse.Data.Loot;
using Eclipse.Data.Validation.Tests;
using UnityEngine;

namespace Eclipse.Data.Items.Tests
{
    /// <summary>Builds in-memory definitions (no assets) for rule and catalog tests; call <see cref="Dispose"/> in TearDown.</summary>
    public sealed class ItemFixtureBuilder : IDisposable
    {
        public readonly List<Definition> Defs = new List<Definition>();

        public T Add<T>(string id) where T : Definition
        {
            T d = FakeValidationDef.Make<T>(id);
            Defs.Add(d);
            return d;
        }

        public void Dispose()
        {
            foreach (Definition d in Defs) UnityEngine.Object.DestroyImmediate(d);
            Defs.Clear();
        }

        public AffixDefinition Affix(string slug, string abbrev, AffixCategory category = AffixCategory.Offensive,
            AffixGroup group = AffixGroup.None, bool inPool = true, StatId stat = StatId.Strength,
            AffixValueKind kind = AffixValueKind.Percent)
        {
            AffixDefinition a = Add<AffixDefinition>("affix.core." + slug);
            a.EditorSet(abbrev, true, stat, kind, category, group, TagMask.None, TagMask.None,
                new float[] { 0.01f, 0.02f, 0.02f, 0.03f, 0.03f, 0.04f, 0.04f, 0.05f, 0.05f, 0.06f });
            AffixPoolDefinition p = Add<AffixPoolDefinition>("pool.core." + slug);
            var weights = new float[10];
            if (inPool) for (int i = 0; i < weights.Length; i++) weights[i] = 10f;
            p.EditorSet(a.StableIdString, weights, string.Empty);
            return a;
        }

        public ItemBaseDefinition Base(string slug, EquipSlot slot)
        {
            ItemBaseDefinition b = Add<ItemBaseDefinition>("base.test." + slug);
            b.EditorSet(slot, slug, ArmorFamily.None, string.Empty, TagMask.None, string.Empty, 0.5f, 0f, true,
                0f, TagMask.None, string.Empty, 0f, string.Empty);
            return b;
        }

        public NamedBandDefinition Band(NamedBand band, float lo, float hi)
        {
            NamedBandDefinition d = Add<NamedBandDefinition>("band.core." + band.ToString().ToLowerInvariant());
            d.EditorSet(band, lo, hi);
            return d;
        }

        public LootGlobalDefinition Global(string key, float value)
        {
            LootGlobalDefinition d = Add<LootGlobalDefinition>("lootglobal.core." + key.Replace('.', '_'));
            d.EditorSet(key, value);
            return d;
        }

        /// <summary>Entry text: <c>slug:Band</c> with optional trailing <c>*</c>; slug expands to affix.core.slug.</summary>
        public static NamedAffixEntry Entry(string text)
        {
            bool off = text.EndsWith("*", StringComparison.Ordinal);
            if (off) text = text.Substring(0, text.Length - 1);
            string[] parts = text.Split(':');
            var band = (NamedBand)Enum.Parse(typeof(NamedBand), parts.Length > 1 ? parts[1] : "Default");
            return new NamedAffixEntry("affix.core." + parts[0], band, off);
        }

        public NamedItemDefinition Item(string slug, string catalogNo, ItemKind kind, string baseSlug, string[] fixedAffixes,
            string[] openOptions, string[] modifiers, int minILvl)
        {
            NamedItemDefinition item = Add<NamedItemDefinition>("item.test." + slug);
            var fixedList = new List<NamedAffixEntry>();
            foreach (string t in fixedAffixes) fixedList.Add(Entry(t));
            var openList = new List<NamedAffixEntry>();
            foreach (string t in openOptions) openList.Add(Entry(t));
            item.EditorSet(catalogNo, kind, "base.test." + baseSlug, string.Empty, string.Empty, fixedList, openList,
                new List<string>(modifiers), TagMask.None, kind == ItemKind.SetPiece ? "set_a" : string.Empty,
                UniqueSourceKind.None, string.Empty, minILvl);
            return item;
        }

        public static readonly string[] TwoModifiers = { "KAE-CS-01", "GEN-27" };
    }
}
