using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Eclipse.Core.Items;
using Eclipse.Core.Loot;
using Eclipse.Core.Loot.Generation;
using Eclipse.Core.Loot.Generation.Tests;
using Eclipse.Core.Random;
using Eclipse.Data.Definitions;
using Eclipse.Data.Items;
using Eclipse.Data.Items.Tests;
using Eclipse.Data.Loot;
using Eclipse.Editor.DataPipeline;
using NUnit.Framework;
using UnityEditor;

namespace Eclipse.Tests.EditMode.Items
{
    /// <summary>
    /// Integration: real DataSource CSVs -> scratch ScriptableObjects -> real ItemCatalog/LootCatalog -> ItemGenerator.
    /// Checks the structural invariants of the generated items (R25 structure, slot pool, tier/Named ranges, hero binding,
    /// required level, determinism) over sources x difficulties x regions with fixed seeds. Hero = Kael; modifiers come from a fake provider
    /// (the real pool arrives with M10/M13). Catalogs are plain data (no asset references), so they are built once and cached.
    /// </summary>
    public class ItemGenerationIntegrationTests : ItemImportTestBase
    {
        private const float Eps = 1e-4f;
        private const int SeedsPerCell = 30;

        private static readonly string[] Regions = { "ashen_wastes", "frozen_cathedral", "shattered_expanse", "hollow_depths", "astral_citadel" };
        private static readonly Dictionary<string, string> HeroPrefix = new Dictionary<string, string>
        {
            { "kael", "KAE-" }, { "lyra", "LYR-" }, { "nyx", "NYX-" }, { "orun", "ORU-" }
        };

        /// <summary>(difficulty, ascension level) pairs covered by the matrix.</summary>
        private static readonly (Difficulty d, int asc)[] Levels =
        {
            (Difficulty.Normal, 0), (Difficulty.Veteran, 0), (Difficulty.Nightmare, 0), (Difficulty.Eclipse, 0),
            (Difficulty.Ascension, 1), (Difficulty.Ascension, 50), (Difficulty.Ascension, 100)
        };

        private static ItemCatalog _items;
        private static LootCatalog _loot;

        private ITableImporter[] AllImporters() => new ITableImporter[]
        {
            new AffixImporter("affixes.csv", Sub("Affixes")),
            new SlotMultiplierImporter("slot_multipliers.csv", Sub("SlotMultipliers")),
            new AffixPoolImporter("affix_pools.csv", Sub("AffixPools")),
            new InfusionImporter("infusions.csv", Sub("Infusions")),
            new RarityImporter("rarities.csv", Sub("Rarities")),
            new NamedBandImporter("named_bands.csv", Sub("NamedBands")),
            new ItemBaseImporter("item_bases.csv", Sub("Bases")),
            new NamedItemImporter("items_named.csv", Sub("Named")),
            new LootSourceImporter("loot_sources.csv", Sub("LootSources")),
            new LootChanceImporter("loot_chances.csv", Sub("LootChances")),
            new IlvlRangeImporter("ilvl_ranges.csv", Sub("IlvlRanges")),
            new LootDifficultyImporter("loot_difficulty.csv", Sub("LootDifficulty")),
            new PityImporter("pity.csv", Sub("Pity")),
            new HomeRegionImporter("home_regions.csv", Sub("HomeRegions")),
            new LootGlobalImporter("loot_global.csv", Sub("LootGlobal")),
        };

        /// <summary>Imports the real CSVs once per test run (the base SetUp has already cleaned the scratch folder).</summary>
        private void EnsureCatalogs()
        {
            if (_items != null && _loot != null) return;
            ImportContext ctx = DataPipeline.Run(AllImporters(), Path.Combine(RepoRoot, "DataSource"));
            Assert.AreEqual(0, ctx.ErrorCount, Errors(ctx));
            var defs = new List<Definition>();
            foreach (string guid in AssetDatabase.FindAssets("t:Definition", new[] { Folder }))
                defs.Add(AssetDatabase.LoadAssetAtPath<Definition>(AssetDatabase.GUIDToAssetPath(guid)));
            Assert.Greater(defs.Count, 0);
            _items = new ItemCatalog(defs);
            _loot = new LootCatalog(defs);
        }

        private static FakeModifierProvider NewMods()
        {
            var m = new FakeModifierProvider();
            m.HeroMods["kael"] = new List<ModifierCandidate> { new ModifierCandidate("KAE-CS-01", "skill.cleave", 1f), new ModifierCandidate("KAE-CS-02", "skill.guard", 1f), new ModifierCandidate("KAE-IL-10", "skill.heavy", 1f) };
            m.HeroMods["lyra"] = new List<ModifierCandidate> { new ModifierCandidate("LYR-AB-01", "skill.bolt", 1f), new ModifierCandidate("LYR-AB-02", "skill.nova", 1f) };
            m.HeroMods["nyx"] = new List<ModifierCandidate> { new ModifierCandidate("NYX-DG-01", "skill.stab", 1f), new ModifierCandidate("NYX-DG-02", "skill.shot", 1f) };
            m.HeroMods["orun"] = new List<ModifierCandidate> { new ModifierCandidate("ORU-TT-01", "skill.totem", 1f), new ModifierCandidate("ORU-TT-02", "skill.idol", 1f) };
            m.Universal.Add(new ModifierCandidate("GEN-01", null, 1f));
            m.Universal.Add(new ModifierCandidate("GEN-02", null, 1f));
            m.Universal.Add(new ModifierCandidate("GEN-03", null, 1f));
            return m;
        }

        private ItemGenerator NewGenerator() { EnsureCatalogs(); return new ItemGenerator(_items, _loot, NewMods()); }

        private static LootContext Ctx(LootSourceKind source, Difficulty d, int asc, string region, int room = 0, int? ilvl = null)
            => new LootContext
            {
                Source = source, Difficulty = d, AscensionLevel = asc, RegionId = region, HeroId = "kael",
                RoomIndex = room, TotalRooms = 6, ForcedILvl = ilvl
            };

        private static ulong SeedOf(int a, int b, int c, int d) => 0x9E3779B97F4A7C15UL ^ ((ulong)(a * 7919 + b * 104729 + c * 1299709 + d + 1));

        // ------------------------------------------------------------------ catalog content
        [Test]
        public void Catalog_has_the_35_named_items_K1_K16_and_U1_U19()
        {
            EnsureCatalogs();
            var named = _items.NamedItems;
            Assert.AreEqual(35, named.Count);
            Assert.AreEqual(35, named.Select(n => n.Id).Distinct().Count(), "ids unique");
            Assert.AreEqual(16, named.Count(n => n.HeroId == "kael"), "K1-K16 are Kael items");
            Assert.AreEqual(19, named.Count(n => n.HeroId == null), "U1-U19 are universal");
            foreach (var n in named)
            {
                Assert.IsNotNull(_items.GetBase(n.BaseId), n.Id + " base");
                Assert.That(n.Kind == ItemKind.NamedLegendary || n.Kind == ItemKind.Unique, n.Id + " kind " + n.Kind);
                Assert.AreEqual(n.Kind == ItemKind.Unique ? 5 : 4, n.FixedAffixes.Count, n.Id + " fixed affixes (R25)");
                Assert.AreEqual(n.Kind == ItemKind.Unique ? 0 : 3, n.OpenOptions.Count, n.Id + " open options (R25)");
                Assert.AreEqual(2, n.ModifierIds.Count, n.Id + " modifiers (R25)");
                Assert.AreEqual(n.Kind == ItemKind.Unique ? 35 : 10, n.MinDropILvl, n.Id + " minDropILvl");
                foreach (var f in n.FixedAffixes) Assert.IsNotNull(_items.GetAffix(f.AffixId), n.Id + " affix " + f.AffixId);
            }
            TestContext.WriteLine("Named: " + named.Count(n => n.Kind == ItemKind.NamedLegendary) + " legendary + " + named.Count(n => n.Kind == ItemKind.Unique) + " unique");
        }

        [Test]
        public void Named_items_use_only_pool_affixes_except_declared_exceptions()
        {
            EnsureCatalogs();
            var violations = new List<string>();
            foreach (var n in _items.NamedItems)
            {
                var slot = _items.GetBase(n.BaseId).Slot;
                int offPool = 0;
                foreach (var spec in n.FixedAffixes.Concat(n.OpenOptions))
                {
                    if (spec.OffPool) { offPool++; continue; }
                    if (!InPool(spec.AffixId, slot) && !IsKnownPoolException(n.Id, spec.AffixId))
                        violations.Add(n.Id + " (" + slot + "): " + spec.AffixId);
                }
                Assert.LessOrEqual(offPool, 1, n.Id + " has more than one `*` affix (02 section 3.9.3 rule 14)");
            }
            Assert.IsEmpty(violations, string.Join(Environment.NewLine, violations));
        }

        [Test]
        public void Rarity_structure_matches_R25()
        {
            EnsureCatalogs();
            Assert.AreEqual(2, _items.GetRarity(Rarity.Common).RandomAffixCount);
            Assert.AreEqual(3, _items.GetRarity(Rarity.Rare).RandomAffixCount);
            Assert.AreEqual(4, _items.GetRarity(Rarity.Epic).RandomAffixCount);
            Assert.AreEqual(1, _items.GetRarity(Rarity.Epic).ModifierCount);
            Assert.AreEqual(0, _items.GetRarity(Rarity.Common).ModifierCount);
            Assert.AreEqual(0, _items.GetRarity(Rarity.Rare).ModifierCount);
            Assert.AreEqual(10, _items.GetRarity(Rarity.Legendary).MinDropILvl);
        }

        [Test]
        public void Required_level_is_clamp_ceil_half_ilvl_1_50()
        {
            EnsureCatalogs();
            for (int ilvl = 1; ilvl <= 100; ilvl++)
            {
                int expected = Math.Max(1, Math.Min(50, (int)Math.Ceiling(ilvl * 0.5)));
                Assert.AreEqual(expected, ItemMath.RequiredLevel(ilvl, _loot.RequiredLevelFactor, _loot.RequiredLevelMax), "iLvl " + ilvl);
            }
            Assert.AreEqual(12, ItemMath.RequiredLevel(24, _loot.RequiredLevelFactor, _loot.RequiredLevelMax));
            Assert.AreEqual(42, ItemMath.RequiredLevel(84, _loot.RequiredLevelFactor, _loot.RequiredLevelMax));
            Assert.AreEqual(50, ItemMath.RequiredLevel(100, _loot.RequiredLevelFactor, _loot.RequiredLevelMax));
        }

        // ------------------------------------------------------------------ matrix
        [Test]
        public void Natural_drops_hold_every_invariant_across_sources_difficulties_regions()
        {
            var g = NewGenerator();
            int total = 0, epics = 0, bound = 0, legendaries = 0, uniques = 0;
            int ci = 0;
            foreach (LootSourceKind source in Enum.GetValues(typeof(LootSourceKind)))
            foreach (var (d, asc) in Levels)
            foreach (string region in Regions)
            {
                ci++;
                for (int s = 0; s < SeedsPerCell; s++)
                {
                    var ctx = Ctx(source, d, asc, region, s % 6);
                    var rng = Xoshiro128StarStar.FromSeed(SeedOf((int)source, (int)d * 1000 + asc, ci, s));
                    var it = g.Generate(ctx, ref rng);
                    CheckItem(it, ctx);
                    total++;
                    if (it.Rarity == Rarity.Epic) epics++;
                    if (it.Rarity == Rarity.Legendary) legendaries++;
                    if (it.Kind == ItemKind.Unique) uniques++;
                    if (it.BoundHeroId != null) bound++;
                }
            }
            TestContext.WriteLine($"natural matrix: items={total} epics={epics} legendaries={legendaries} uniques={uniques} heroBound={bound}");
            Assert.Greater(epics, 100);
            Assert.Greater(legendaries, 50);
        }

        [Test]
        public void Forced_legendaries_hold_every_invariant_and_cover_named_items()
        {
            var g = NewGenerator();
            var seen = new HashSet<string>();
            int ci = 0;
            foreach (LootSourceKind source in new[] { LootSourceKind.Boss, LootSourceKind.Challenge, LootSourceKind.RiftCache, LootSourceKind.Reliquary })
            foreach (var (d, asc) in Levels)
            foreach (string region in Regions)
            {
                ci++;
                for (int s = 0; s < 60; s++)
                {
                    var ctx = Ctx(source, d, asc, region, 0, 10 + (s * 7) % 91);
                    ctx.Guarantees.ForceLegendary = true;
                    var rng = Xoshiro128StarStar.FromSeed(SeedOf((int)source + 50, (int)d * 1000 + asc, ci, s));
                    var it = g.Generate(ctx, ref rng);
                    Assert.AreEqual(Rarity.Legendary, it.Rarity);
                    CheckItem(it, ctx);
                    seen.Add(it.DefinitionId);
                    if (d == Difficulty.Normal) Assert.AreEqual(ItemKind.NamedLegendary, it.Kind, "Normal has no Uniques/sets");
                }
            }
            TestContext.WriteLine("named items reached by forced Legendary drops: " + seen.Count + " of " + _items.NamedItems.Count);
            Assert.Greater(seen.Count, 10);
            foreach (string id in seen) Assert.IsNotNull(_items.GetNamedItem(id), id);
        }

        [Test]
        public void Same_seed_and_context_give_identical_items()
        {
            var g1 = NewGenerator();
            var g2 = NewGenerator();
            int n = 0;
            foreach (var (d, asc) in Levels)
            foreach (LootSourceKind source in Enum.GetValues(typeof(LootSourceKind)))
            {
                var rngA = Xoshiro128StarStar.FromSeed(SeedOf((int)source, asc, (int)d, 99));
                var rngB = Xoshiro128StarStar.FromSeed(SeedOf((int)source, asc, (int)d, 99));
                for (int i = 0; i < 100; i++)
                {
                    var ctxA = Ctx(source, d, asc, Regions[i % 5], i % 6);
                    var ctxB = Ctx(source, d, asc, Regions[i % 5], i % 6);
                    string a = Fingerprint(g1.Generate(ctxA, ref rngA));
                    string b = Fingerprint(g2.Generate(ctxB, ref rngB));
                    Assert.AreEqual(a, b);
                    n++;
                }
                Assert.AreEqual(rngA.GetState(), rngB.GetState(), "stream consumption identical");
            }
            TestContext.WriteLine("determinism compared " + n + " item pairs");
        }

        [Test]
        public void Different_seeds_produce_different_items()
        {
            var g = NewGenerator();
            var set = new HashSet<string>();
            for (int s = 1; s <= 200; s++)
            {
                var rng = Xoshiro128StarStar.FromSeed((ulong)s);
                set.Add(Fingerprint(g.Generate(Ctx(LootSourceKind.Boss, Difficulty.Eclipse, 0, "ashen_wastes"), ref rng)));
            }
            Assert.Greater(set.Count, 150);
        }

        // ------------------------------------------------------------------ invariants
        private void CheckItem(ItemInstance it, LootContext ctx)
        {
            string what = $"[{ctx.Source}/{ctx.Difficulty}{ctx.AscensionLevel}/{ctx.RegionId}/seed {it.DropSeed}] {it.DefinitionId} {it.Rarity} ilvl {it.ItemLevel}";
            Assert.IsNotNull(it.DefinitionId, what);
            Assert.IsNotNull(it.InstanceId, what);
            Assert.AreNotEqual(Guid.Empty, it.InstanceId, what);
            Assert.AreEqual(ctx.Source, it.Source, what);
            Assert.That(it.ItemLevel, Is.InRange(1, 100), what);
            CheckILvl(it, ctx, what);

            // Required level (05 section 4.3).
            int req = ItemMath.RequiredLevel(it.ItemLevel, _loot.RequiredLevelFactor, _loot.RequiredLevelMax);
            Assert.AreEqual(Math.Max(1, Math.Min(50, (int)Math.Ceiling(it.ItemLevel * 0.5))), req, what);

            int tier = ItemMath.Tier(it.ItemLevel);
            var baseData = _items.GetBase(it.BaseId);
            Assert.IsNotNull(baseData, what + " base " + it.BaseId);
            float rollFloor = LootMath.RollFloor(_loot, ctx);
            var seenAffix = new HashSet<string>();

            if (it.Kind == ItemKind.Generic)
            {
                Assert.AreNotEqual(Rarity.Legendary, it.Rarity, what);
                Assert.IsTrue(baseData.Droppable, what + " droppable base");
                Assert.AreEqual(it.DefinitionId, it.BaseId, what);
                var r = _items.GetRarity(it.Rarity);
                Assert.AreEqual(r.RandomAffixCount, it.Affixes.Count, what + " random affix count");
                Assert.AreEqual(r.ModifierCount, it.Modifiers.Count, what + " modifier count (fake provider never runs dry)");
                int affinity = 0, comfort = 0;
                foreach (var a in it.Affixes)
                {
                    var data = _items.GetAffix(a.AffixId);
                    Assert.IsNotNull(data, what + " affix " + a.AffixId);
                    Assert.IsTrue(seenAffix.Add(a.AffixId), what + " repeated affix " + a.AffixId);
                    Assert.IsFalse(a.Named, what);
                    Assert.AreEqual(NamedBand.None, a.Band, what);
                    Assert.IsTrue(InPool(a.AffixId, baseData.Slot), what + " affix " + a.AffixId + " not in pool of " + baseData.Slot);
                    if (data.Group == AffixGroup.Affinity || data.Category == AffixCategory.Infusion) affinity++;
                    if (data.Group == AffixGroup.Comfort) comfort++;
                    CheckAffixValue(it, a, baseData, tier, rollFloor, what);
                }
                Assert.LessOrEqual(affinity, 2, what + " affinity affixes (02 3.9.3)");
                Assert.LessOrEqual(comfort, 1, what + " comfort affixes");
            }
            else
            {
                var def = _items.GetNamedItem(it.DefinitionId);
                Assert.IsNotNull(def, what);
                Assert.AreEqual(it.Kind, def.Kind, what);
                Assert.AreEqual(Rarity.Legendary, it.Rarity, what);
                Assert.AreEqual(def.BaseId, it.BaseId, what);
                Assert.GreaterOrEqual(it.ItemLevel, def.MinDropILvl, what + " minDropILvl");
                Assert.AreEqual(5, it.Affixes.Count, what + " 4+1 / 5 affixes (R25)");
                Assert.AreEqual(2, it.Modifiers.Count, what);
                for (int i = 0; i < def.FixedAffixes.Count; i++)
                {
                    var spec = def.FixedAffixes[i];
                    var a = it.Affixes[i];
                    Assert.AreEqual(spec.AffixId, a.AffixId, what);
                    Assert.IsTrue(a.Named, what);
                    Assert.AreEqual(spec.Band, a.Band, what + " band of " + spec.AffixId);
                    if (!spec.OffPool) Assert.IsTrue(InPool(spec.AffixId, baseData.Slot), what + " fixed affix " + spec.AffixId + " not in pool of " + baseData.Slot);
                    CheckAffixValue(it, a, baseData, tier, rollFloor, what);
                }
                if (def.Kind == ItemKind.Unique)
                {
                    Assert.AreEqual(0, def.OpenOptions.Count, what);
                    Assert.GreaterOrEqual(it.ItemLevel, 35, what + " Unique iLvl");
                }
                else
                {
                    Assert.AreEqual(5, it.Affixes.Count, what);
                    var spec = def.OpenOptions[it.OpenAffixChoice];
                    var open = it.Affixes[4];
                    Assert.AreEqual(spec.AffixId, open.AffixId, what + " open affix");
                    Assert.IsTrue(open.Named, what);
                    Assert.AreEqual(NamedBand.Default, open.Band, what);
                    if (!spec.OffPool && !IsKnownPoolException(def.Id, spec.AffixId))
                        Assert.IsTrue(InPool(spec.AffixId, baseData.Slot), what + " open affix " + spec.AffixId + " not in pool of " + baseData.Slot);
                    CheckAffixValue(it, open, baseData, tier, rollFloor, what);
                }
                CollectionAssert.AreEqual(def.ModifierIds, it.Modifiers.Select(m => m.ModifierId).ToList(), what);
                Assert.AreEqual(def.HeroId, it.BoundHeroId, what + " named items bind to their hero (R27)");
            }

            foreach (var m in it.Modifiers) Assert.That(m.Roll01, Is.InRange(0f, 1f), what);
            Assert.That(it.ImplicitRoll01, Is.InRange(0f, 1f), what);
            CheckHeroBinding(it, what);
        }

        private void CheckILvl(ItemInstance it, LootContext ctx, string what)
        {
            if (ctx.ForcedILvl.HasValue) return;
            if (ctx.Difficulty == Difficulty.Ascension)
            {
                Assert.That(it.ItemLevel, Is.InRange(85, 100), what);
                return;
            }
            var range = _loot.IlvlRanges.First(r => r.Difficulty == ctx.Difficulty
                && (string.IsNullOrEmpty(r.RegionId) || r.RegionId == ctx.RegionId));
            Assert.That(it.ItemLevel, Is.InRange(range.Min, Math.Min(100, range.Max + 2)), what + " range " + range.Min + "-" + range.Max);
        }

        private void CheckHeroBinding(ItemInstance it, string what)
        {
            if (it.Kind != ItemKind.Generic) return; // named: checked against the definition
            bool hasHeroModifier = it.Modifiers.Any(m => !m.ModifierId.StartsWith("GEN-", StringComparison.Ordinal));
            if (!hasHeroModifier)
            {
                Assert.IsNull(it.BoundHeroId, what + " bound without a hero modifier");
                return;
            }
            Assert.IsNotNull(it.BoundHeroId, what + " hero modifier but not bound (R27)");
            foreach (var m in it.Modifiers)
                if (!m.ModifierId.StartsWith("GEN-", StringComparison.Ordinal))
                    StringAssert.StartsWith(HeroPrefix[it.BoundHeroId], m.ModifierId, what);
            Assert.AreEqual(Rarity.Epic, it.Rarity, what + " only Epic carries a random modifier");
        }

        /// <summary>
        /// Data divergence found by T9, NOT silenced silently: U3 Seal of the Remnants lists Healing Effectiveness as an Open Affix option
        /// (02 section 3.12 table, U3 row) but the pool table of 02 section 3.9.3 forbids HEF on Amulet (affix_pools.csv w_amulet = 0) and the
        /// item carries no `*` mark. Same for U1 Aetherbound Loop: Open option DUR (Duration of Effects) on a Ring, which the pool forbids.
        /// Both need a doc decision (mark them `*` or change the pool/option); reported in the T9 hand-off.
        /// </summary>
        private static bool IsKnownPoolException(string itemId, string affixId)
            => (itemId == "item.universal.seal_of_the_remnants" && affixId == "affix.core.healing_effectiveness")
            || (itemId == "item.universal.aetherbound_loop" && affixId == "affix.core.duration_of_effects");

        private bool InPool(string affixId, EquipSlot slot)
        {
            foreach (var e in _items.Pool)
                if (e.AffixId == affixId && e.SlotWeights[(int)slot] > 0f) return true;
            return false;
        }

        private void CheckAffixValue(ItemInstance it, RolledAffix a, ItemBaseData b, int tier, float rollFloor, string what)
        {
            Assert.AreEqual(tier, a.Tier, what + " tier of " + a.AffixId);
            Assert.That(a.Roll01, Is.InRange(0f, 1f), what);
            Assert.GreaterOrEqual(a.Roll01, rollFloor - Eps, what + " roll floor of the difficulty");
            var data = _items.GetAffix(a.AffixId);
            var range = data.GetTier(tier);
            float slotMult = ItemMath.SlotMultiplier(_items, b.Slot, a.AffixId, b.Id);
            float lo, hi;
            if (a.Named)
            {
                var band = _items.GetBandRange(a.Band);
                var envelope = _items.GetBandRange(NamedBand.Envelope);
                Assert.GreaterOrEqual(band.Lo, envelope.Lo - Eps, what + " band inside the Named Range envelope");
                Assert.LessOrEqual(band.Hi, envelope.Hi + Eps, what + " band inside the Named Range envelope");
                lo = band.Lo * range.Max * slotMult; hi = band.Hi * range.Max * slotMult;
            }
            else { lo = range.Min * slotMult; hi = range.Max * slotMult; }
            float v = ItemMath.AffixValue(_items, it, a, b.Slot, b.Id);
            float tol = Eps * Math.Max(1f, Math.Abs(hi));
            Assert.That(v, Is.InRange(lo - tol, hi + tol), what + " value of " + a.AffixId + " (" + a.Band + ") outside [" + lo + "," + hi + "]");
        }

        private static string Fingerprint(ItemInstance it)
        {
            var sb = new StringBuilder();
            sb.Append(it.InstanceId).Append('|').Append(it.DefinitionId).Append('|').Append(it.BaseId).Append('|').Append(it.Rarity).Append('|')
              .Append(it.Kind).Append('|').Append(it.ItemLevel).Append('|').Append(it.BoundHeroId).Append('|').Append(it.DropSeed).Append('|')
              .Append(it.ImplicitRoll01.ToString("R")).Append('|').Append(it.OpenAffixChoice).Append('|').Append(it.Flags);
            foreach (var a in it.Affixes) sb.Append("|A:").Append(a.AffixId).Append(a.Tier).Append(a.Band).Append(a.Roll01.ToString("R"));
            foreach (var m in it.Modifiers) sb.Append("|M:").Append(m.ModifierId).Append(m.Roll01.ToString("R"));
            return sb.ToString();
        }
    }
}
