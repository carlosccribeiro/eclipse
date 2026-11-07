using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Items.Tests;
using Eclipse.Core.Random;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Loot.Generation.Tests
{
    /// <summary>
    /// Small catalogs for the generator tests. Test numbers (not game data): they mirror the doc tables only where a test asserts them.
    /// Slot order: MainHand, OffHand, Head, Chest, Hands, Legs, Feet, Ring, Amulet, Relic.
    /// </summary>
    public sealed class GenFixture
    {
        public readonly FakeItemCatalog Items = new FakeItemCatalog();
        public readonly FakeLootCatalog Loot = new FakeLootCatalog();
        public readonly FakeModifierProvider Mods = new FakeModifierProvider();

        public static GenFixture Create()
        {
            var f = new GenFixture();
            f.BuildItems();
            f.BuildLoot();
            f.BuildMods();
            return f;
        }

        public ItemGenerator NewGenerator() => new ItemGenerator(Items, Loot, Mods);

        public static Xoshiro128StarStar Rng(ulong seed) => Xoshiro128StarStar.FromSeed(seed);

        public static LootContext Ctx(LootSourceKind source = LootSourceKind.Trash, Difficulty diff = Difficulty.Normal,
            string region = "ashen_wastes", string hero = "kael", int? ilvl = null, int ascension = 0)
            => new LootContext
            {
                Source = source, Difficulty = diff, RegionId = region, HeroId = hero, ForcedILvl = ilvl,
                AscensionLevel = ascension, TotalRooms = 6
            };

        // ------------------------------------------------------------------ items
        private void Aff(string id, AffixGroup group, AffixCategory cat, float[] slots, string restriction = null,
            TagMask? affinity = null, TagMask? relevance = null)
        {
            var a = new AffixData
            {
                Id = id, Group = group, Category = cat, ValueKind = AffixValueKind.Percent,
                Affinity = affinity ?? TagMask.None, RelevanceTags = relevance ?? TagMask.None
            };
            for (int t = 1; t <= 5; t++) a.Tiers[t - 1] = new TierRange(0.1f * t, 0.1f * t + 0.05f);
            Items.AffixList.Add(a);
            Items.PoolList.Add(new AffixPoolEntry { AffixId = id, SlotWeights = slots, Restriction = restriction });
        }

        private void BuildItems()
        {
            var N = AffixGroup.None;
            //                                      MH  OH  Hd  Ch  Hn  Lg  Ft  Rg  Am  Rl
            float[] prim = new float[] { 10, 0, 10, 10, 10, 10, 10, 0, 20, 0 };
            Aff("str", AffixGroup.Primary, AffixCategory.Primary, (float[])prim.Clone());
            Aff("dex", AffixGroup.Primary, AffixCategory.Primary, (float[])prim.Clone());
            Aff("int", AffixGroup.Primary, AffixCategory.Primary, (float[])prim.Clone());
            foreach (var (id, tag) in new[] { ("ember", Tag.Ember), ("frost", Tag.Frost), ("storm", Tag.Storm) })
                Aff(id, AffixGroup.Affinity, AffixCategory.Offensive, new float[] { 20, 10, 0, 0, 10, 0, 0, 10, 10, 20 },
                    "relic_family_affinity", new TagMask(tag));
            Aff("inf_ember", AffixGroup.Affinity, AffixCategory.Infusion, new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 10, 20 },
                "relic_family_affinity", new TagMask(Tag.Ember));
            Aff("pickup", AffixGroup.Comfort, AffixCategory.Utility, new float[] { 0, 0, 0, 0, 0, 0, 10, 10, 0, 0 });
            Aff("xp", AffixGroup.Comfort, AffixCategory.Utility, new float[] { 0, 0, 10, 0, 0, 0, 0, 10, 0, 0 });
            Aff("gold", AffixGroup.Comfort, AffixCategory.Utility, new float[] { 0, 0, 0, 0, 0, 0, 10, 10, 0, 0 });
            foreach (var id in new[] { "phys_res", "elem_res", "void_res" })
                Aff(id, AffixGroup.SingleResistance, AffixCategory.Defensive, new float[] { 0, 0, 10, 20, 0, 10, 10, 10, 20, 0 });
            Aff("armor", N, AffixCategory.Defensive, new float[] { 0, 0, 10, 20, 10, 10, 10, 0, 0, 0 });
            Aff("hp", N, AffixCategory.Flat, new float[] { 0, 0, 20, 20, 10, 20, 10, 10, 10, 0 });
            Aff("crit_chance", N, AffixCategory.Offensive, new float[] { 20, 10, 0, 0, 20, 0, 0, 20, 0, 0 }, null, null, new TagMask(Tag.Attack));
            Aff("atk_speed", N, AffixCategory.Offensive, new float[] { 20, 10, 0, 0, 20, 0, 0, 10, 0, 0 }, "weapon_tag:Attack");
            Aff("cast_speed", N, AffixCategory.Offensive, new float[] { 20, 10, 0, 0, 20, 0, 0, 10, 0, 0 }, "weapon_tag:Spell");
            Aff("summon_dmg", N, AffixCategory.Offensive, new float[] { 10, 10, 0, 0, 0, 0, 0, 10, 10, 20 },
                "hero:orun_except_ring_relic", null, new TagMask(Tag.Summon));
            Aff("dur", N, AffixCategory.Utility, new float[] { 10, 10, 0, 0, 0, 10, 0, 0, 10, 20 });
            Aff("srs", N, AffixCategory.Utility, new float[] { 0, 0, 20, 10, 0, 0, 10, 0, 0, 10 });
            Aff("move_speed", N, AffixCategory.Utility, new float[] { 0, 0, 0, 0, 0, 0, 20, 0, 0, 0 });
            Aff("block", N, AffixCategory.Defensive, new float[] { 0, 20, 0, 10, 0, 0, 0, 0, 0, 0 }, "base:warbrand");

            Items.RarityMap[Rarity.Common] = new RarityData { Rarity = Rarity.Common, RandomAffixCount = 2, ModifierCount = 0, MinDropILvl = 1 };
            Items.RarityMap[Rarity.Rare] = new RarityData { Rarity = Rarity.Rare, RandomAffixCount = 3, ModifierCount = 0, MinDropILvl = 1 };
            Items.RarityMap[Rarity.Epic] = new RarityData { Rarity = Rarity.Epic, RandomAffixCount = 4, ModifierCount = 1, MinDropILvl = 1 };
            Items.RarityMap[Rarity.Legendary] = new RarityData { Rarity = Rarity.Legendary, RandomAffixCount = 0, ModifierCount = 2, MinDropILvl = 10 };
            Items.BandMap[NamedBand.Envelope] = new NamedBandRange(0.70f, 1.15f);
            Items.BandMap[NamedBand.Default] = new NamedBandRange(0.80f, 1.00f);
            Items.BandMap[NamedBand.High] = new NamedBandRange(0.95f, 1.15f);
            Items.BandMap[NamedBand.Low] = new NamedBandRange(0.70f, 0.80f);

            Base("kael_sword", EquipSlot.MainHand, "kael", new TagMask(Tag.Attack, Tag.Melee), "crit_chance");
            Base("lyra_staff", EquipSlot.MainHand, "lyra", new TagMask(Tag.Spell));
            Base("nyx_dagger", EquipSlot.MainHand, "nyx", new TagMask(Tag.Attack));
            Base("orun_scepter", EquipSlot.MainHand, "orun", new TagMask(Tag.Spell));
            Base("kael_warbrand", EquipSlot.OffHand, "kael", TagMask.None, null, "Warbrand");
            Base("lyra_grimoire", EquipSlot.OffHand, "lyra", TagMask.None, null, "Grimoire");
            Base("nyx_quiver", EquipSlot.OffHand, "nyx", TagMask.None, null, "Quiver");
            Base("orun_idol", EquipSlot.OffHand, "orun", TagMask.None, null, "Idol");
            Base("helm", EquipSlot.Head, null, TagMask.None);
            Base("cuirass", EquipSlot.Chest, null, TagMask.None);
            Base("gauntlets", EquipSlot.Hands, null, TagMask.None);
            Base("greaves", EquipSlot.Legs, null, TagMask.None);
            Base("boots", EquipSlot.Feet, null, TagMask.None);
            Base("band", EquipSlot.Ring, null, TagMask.None);
            Base("pendant", EquipSlot.Amulet, null, TagMask.None);
            Base("relic_cinder", EquipSlot.Relic, null, new TagMask(Tag.Ember));
            Base("keystone", EquipSlot.Relic, null, TagMask.None).Droppable = false;

            // Legendaries (kael x2, universal x1, lyra x1)
            Named("item.ashfang", ItemKind.NamedLegendary, "kael_sword", "kael", Tag.Ember, 10);
            Named("item.frostbite", ItemKind.NamedLegendary, "kael_sword", "kael", Tag.Frost, 10);
            Named("item.band_of_all", ItemKind.NamedLegendary, "band", null, null, 10);
            Named("item.lyra_orb", ItemKind.NamedLegendary, "lyra_staff", "lyra", null, 10);
            // Set pieces
            Named("piece.ashen.helm", ItemKind.SetPiece, "helm", "kael", null, 10).SetId = "ashen";
            Named("piece.ashen.chest", ItemKind.SetPiece, "cuirass", "kael", null, 10).SetId = "ashen";
            Named("piece.eternal.helm", ItemKind.SetPiece, "helm", "kael", null, 10).SetId = "eternal";
            Named("piece.frozen.helm", ItemKind.SetPiece, "helm", "lyra", null, 10).SetId = "frozen";
            // Uniques (universal)
            var u1 = Named("item.u_region_a", ItemKind.Unique, "band", null, null, 35);
            u1.UniqueSource = UniqueSourceKind.Region; u1.UniqueSourceRef = "ashen_wastes";
            var u2 = Named("item.u_region_b", ItemKind.Unique, "pendant", null, null, 35);
            u2.UniqueSource = UniqueSourceKind.Region; u2.UniqueSourceRef = "ashen_wastes";
            var u3 = Named("item.u_boss", ItemKind.Unique, "pendant", null, null, 35);
            u3.UniqueSource = UniqueSourceKind.Boss; u3.UniqueSourceRef = "boss.colossus";
            var u4 = Named("item.u_challenge", ItemKind.Unique, "band", null, null, 35);
            u4.UniqueSource = UniqueSourceKind.Challenge; u4.UniqueSourceRef = "challenge";
        }

        private ItemBaseData Base(string id, EquipSlot slot, string hero, TagMask tags, string implicitAffix = null, string category = null)
        {
            var b = new ItemBaseData
            {
                Id = id, Slot = slot, HeroId = hero, Tags = tags, ImplicitAffixId = implicitAffix, ImplicitFactor = 0.5f,
                Category = category ?? id
            };
            Items.BaseList.Add(b);
            return b;
        }

        private NamedItemData Named(string id, ItemKind kind, string baseId, string hero, Tag? affinity, int minILvl)
        {
            var n = new NamedItemData
            {
                Id = id, Kind = kind, BaseId = baseId, HeroId = hero, EffectId = "effect." + id, MinDropILvl = minILvl,
                Affinity = affinity.HasValue ? new TagMask(affinity.Value) : TagMask.None
            };
            n.FixedAffixes.Add(new NamedAffixSpec("ember", NamedBand.High, false));
            n.FixedAffixes.Add(new NamedAffixSpec("crit_chance", NamedBand.Default, false));
            n.FixedAffixes.Add(new NamedAffixSpec("hp", NamedBand.Low, false));
            n.FixedAffixes.Add(new NamedAffixSpec("armor", NamedBand.Default, true));
            if (kind == ItemKind.Unique) n.FixedAffixes.Add(new NamedAffixSpec("str", NamedBand.High, false));
            else
            {
                n.OpenOptions.Add(new NamedAffixSpec("str", NamedBand.High, false)); // spec band is ignored: Default is used
                n.OpenOptions.Add(new NamedAffixSpec("hp", NamedBand.Default, false));
                n.OpenOptions.Add(new NamedAffixSpec("block", NamedBand.Default, false));
            }
            n.ModifierIds.Add("KAE-CS-01");
            n.ModifierIds.Add(kind == ItemKind.Unique ? "SIG-K16" : "GEN-27");
            Items.NamedList.Add(n);
            return n;
        }

        // ------------------------------------------------------------------ loot
        private static float[] Pct(params float[] p)
        {
            var r = new float[p.Length];
            for (int i = 0; i < p.Length; i++) r[i] = p[i] < 0f ? -1f : p[i] / 100f;
            return r;
        }

        private void Source(LootSourceKind k, float drop, int guar, float extra, int extraAll, int chooseOf, int epicPlus,
            Rarity floor, bool qty, int bonus, bool top, bool hard, float[] l, float[] e, float[] r = null)
        {
            Loot.SourceMap[k] = new LootSourceData
            {
                Source = k, DropChance = drop, GuaranteedItems = guar, ExtraItemChance = extra, ExtraItemsIfAllChallenges = extraAll,
                ChooseOf = chooseOf, GuaranteedEpicPlus = epicPlus, RarityFloor = floor, AffectedByQuantity = qty, ILvlBonus = bonus,
                ILvlFromRangeTop = top, ConsumesHardPity = hard
            };
            Loot.ChanceMap[k] = new LootChanceData { Source = k, Legendary = Pct(l), Epic = Pct(e), Rare = Pct(r ?? new float[] { -1, -1, -1, -1, -1, -1 }) };
        }

        private void BuildLoot()
        {
            var C = Rarity.Common; var R = Rarity.Rare; var E = Rarity.Epic;
            Source(LootSourceKind.Trash, 0.03f, 0, 0, 0, 0, 0, C, true, 0, false, false,
                new float[] { 0.1f, 0.4f, 0.9f, 1.8f, 2.5f, 4 }, new float[] { 2, 6, 12, 20, 25, 32 }, new float[] { 25, 32, 38, 42, 42, 40 });
            Source(LootSourceKind.Elite, 1f, 1, 0.3f, 0, 0, 0, R, true, 1, false, true,
                new float[] { 0.5f, 1.5f, 3, 6, 8, 12 }, new float[] { 8, 15, 22, 30, 34, 40 });
            Source(LootSourceKind.Guardian, 1f, 2, 0, 0, 0, 1, R, false, 2, false, false,
                new float[] { 2, 4, 7, 11, 14, 18 }, new float[] { 12, 20, 28, 36, 40, 45 });
            Source(LootSourceKind.ArenaReward, 1f, 1, 0, 0, 0, 0, R, false, 1, false, false,
                new float[] { 0.5f, 1.5f, 3, 6, 8, 12 }, new float[] { 8, 15, 22, 30, 34, 40 });
            Source(LootSourceKind.TreasureDelivered, 1f, 2, 0, 0, 0, 0, R, false, 1, false, true,
                new float[] { 1, 3, 5, 9, 12, 16 }, new float[] { 12, 20, 28, 36, 40, 45 });
            Source(LootSourceKind.Reliquary, 1f, 0, 0, 0, 3, 0, E, false, 1, false, true,
                new float[] { 2, 5, 8, 12, 15, 20 }, new float[] { -1, -1, -1, -1, -1, -1 });
            Source(LootSourceKind.Challenge, 1f, 2, 0, 0, 0, 0, E, false, 2, false, true,
                new float[] { 5, 10, 15, 22, 26, 32 }, new float[] { -1, -1, -1, -1, -1, -1 });
            Source(LootSourceKind.Event, 1f, 1, 0, 0, 0, 0, R, false, 1, false, true,
                new float[] { 1, 3, 5, 9, 12, 16 }, new float[] { 10, 18, 25, 33, 37, 42 });
            Source(LootSourceKind.Boss, 1f, 4, 0, 0, 0, 1, R, true, 2, true, true,
                new float[] { 4, 8, 13, 20, 24, 30 }, new float[] { 15, 22, 30, 38, 42, 48 });
            Source(LootSourceKind.RiftCache, 1f, 2, 0, 1, 0, 0, R, false, 2, true, true,
                new float[] { 2, 5, 9, 14, 17, 22 }, new float[] { 20, 28, 35, 42, 45, 50 });

            Range(Difficulty.Normal, "ashen_wastes", 1, 6); Range(Difficulty.Normal, "frozen_cathedral", 6, 10);
            Range(Difficulty.Veteran, "ashen_wastes", 25, 30); Range(Difficulty.Veteran, "shattered_expanse", 35, 40);
            Range(Difficulty.Nightmare, "ashen_wastes", 50, 54);
            Range(Difficulty.Eclipse, null, 75, 84);

            Diff(Difficulty.Normal, 0, 0, 0, 0, 1, 0, 0);
            Diff(Difficulty.Veteran, 0.1f, 0, 0.4f, 0.15f, 0.45f, 0, 0);
            Diff(Difficulty.Nightmare, 0.2f, 0, 0.4f, 0.15f, 0.45f, 0.10f, 0);
            Diff(Difficulty.Eclipse, 0.3f, 0, 0.4f, 0.15f, 0.45f, 0.20f, 0);
            Diff(Difficulty.Ascension, 0.3f, 0.004f, 0.4f, 0.18f, 0.42f, 0.25f, 0.004f);
            var asc = Loot.DifficultyMap[Difficulty.Ascension];
            asc.AscensionBaseILvl = 85; asc.AscensionMaxILvl = 100; asc.AscensionILvlCapLevel = 90;
            asc.AscensionPrimalMinLevel = 50; asc.PrimalBaseChance = 0.02f; asc.PrimalChancePerLevel = 0.0016f; asc.PrimalRollMin = 0.85f;

            Pity(Difficulty.Normal, 80, 160, 0); Pity(Difficulty.Veteran, 50, 100, 4); Pity(Difficulty.Nightmare, 30, 60, 4);
            Pity(Difficulty.Eclipse, 15, 35, 4); Pity(Difficulty.Ascension, 12, 30, 4);

            Loot.HomeList.Add(new HomeRegionData { SetId = "ashen", HeroId = "kael", RegionId = "ashen_wastes", HomeWeight = 4, HomeBossWeight = 6, AttunementMultiplier = 3 });
            Loot.HomeList.Add(new HomeRegionData { SetId = "eternal", HeroId = "kael", RegionId = "frozen_cathedral", HomeWeight = 4, HomeBossWeight = 6, AttunementMultiplier = 3 });
            Loot.HomeList.Add(new HomeRegionData { SetId = "frozen", HeroId = "lyra", RegionId = "frozen_cathedral", HomeWeight = 4, HomeBossWeight = 6, AttunementMultiplier = 3 });
            Loot.HomeList.Add(new HomeRegionData { SetId = "storm", HeroId = "nyx", RegionId = "shattered_expanse", HomeWeight = 4, HomeBossWeight = 6, AttunementMultiplier = 3 });
            Loot.HomeList.Add(new HomeRegionData { SetId = "abyssal", HeroId = "orun", RegionId = "shattered_expanse", HomeWeight = 4, HomeBossWeight = 6, AttunementMultiplier = 3 });
            Loot.RegionList.Add(new RegionAffinityData { RegionId = "ashen_wastes", DominantAffinities = new TagMask(Tag.Ember), AffinityWeight = 2 });
            Loot.RegionList.Add(new RegionAffinityData { RegionId = "frozen_cathedral", DominantAffinities = new TagMask(Tag.Frost), AffinityWeight = 2 });
            Loot.RegionList.Add(new RegionAffinityData { RegionId = "astral_citadel", DominantAffinities = TagMask.None, AffinityWeight = 2, UniqueBonus = 0.03f });

            Loot.SlotWeights = new SlotWeightData { Weights = new float[] { 10, 7, 8, 8, 8, 8, 8, 9, 6, 7 } };
            Loot.SmartWeights = new SmartLootWeights
            {
                IrrelevantAffix = 0.25f, PrimaryCore = 2f, PrimarySecondary = 1f, PrimaryTertiary = 0.5f,
                EpicHeroModifierShare = 0.75f, EquippedSkillMultiplier = 3f
            };
        }

        private void Range(Difficulty d, string region, int min, int max)
            => Loot.RangeList.Add(new IlvlRangeData { Difficulty = d, RegionId = region, Min = min, Max = max });

        private void Diff(Difficulty d, float qty, float qtyAsc, float set, float uni, float std, float floor, float floorAsc)
        {
            if (d == Difficulty.Normal) { set = 0; uni = 0; std = 1; }
            Loot.DifficultyMap[d] = new DifficultyLootData
            {
                Difficulty = d, QuantityBonus = qty, QuantityBonusPerAscension = qtyAsc,
                SetPieceShare = d == Difficulty.Normal ? 0 : set, UniqueShare = uni, StandardShare = std,
                RollFloor = floor, RollFloorPerAscension = floorAsc
            };
        }

        private void Pity(Difficulty d, int soft, int hard, int setAfter)
            => Loot.PityMap[d] = new PityConfigData
            {
                Difficulty = d, SoftStart = soft, Hard = hard, SoftStep = 0.05f, LegendaryCapMultiplier = 2f,
                SetPityAfter = setAfter, DuplicateUnownedWeight = 3
            };

        // ------------------------------------------------------------------ modifiers
        private void BuildMods()
        {
            Mods.HeroMods["kael"] = new List<ModifierCandidate>
            {
                new ModifierCandidate("KAE-CS-01", "skill.cleave", 1f),
                new ModifierCandidate("KAE-GR-02", "skill.guard", 1f)
            };
            Mods.HeroMods["lyra"] = new List<ModifierCandidate> { new ModifierCandidate("LYR-AB-01", "skill.bolt", 1f) };
            Mods.HeroMods["nyx"] = new List<ModifierCandidate> { new ModifierCandidate("NYX-DG-01", "skill.stab", 1f) };
            Mods.HeroMods["orun"] = new List<ModifierCandidate> { new ModifierCandidate("ORU-TT-01", "skill.totem", 1f) };
            Mods.Universal.Add(new ModifierCandidate("GEN-01", null, 1f));
            Mods.Universal.Add(new ModifierCandidate("GEN-02", null, 1f));
        }
    }
}
