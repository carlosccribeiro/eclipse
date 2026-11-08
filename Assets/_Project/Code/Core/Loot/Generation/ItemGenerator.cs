using System;
using Eclipse.Core.Items;
using Eclipse.Core.Random;

namespace Eclipse.Core.Loot.Generation
{
    /// <summary>
    /// Entry point of item generation (05 section 6.2, 12 steps; 11 section 3.12). Pure function of (context, rng stream, catalogs):
    /// the same context and stream state always yield the identical item. All numbers come from <see cref="IItemCatalog"/> / <see cref="ILootCatalog"/>.
    /// <para>NOT thread-safe: the sub-components reuse buffers across calls (no allocation in the sampling loops). Use one generator per thread.</para>
    /// Draw order: rarity, hero, kind, named/base, Primal, affixes, modifiers, instance id.
    /// </summary>
    public sealed class ItemGenerator : IItemGenerator
    {
        private readonly IItemCatalog _items;
        private readonly ILootCatalog _loot;
        private readonly ItemCountRoller _count;
        private readonly RarityRoller _rarity;
        private readonly HeroSelector _heroes;
        private readonly NamedItemSelector _named;
        private readonly LegendaryKindSelector _kinds;
        private readonly BaseSelector _bases;
        private readonly AffixRoller _affixes;
        private readonly EpicModifierRoller _epicMods;

        public ItemGenerator(IItemCatalog items, ILootCatalog loot, IModifierPoolProvider mods, GeneratorRules rules = null)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _loot = loot ?? throw new ArgumentNullException(nameof(loot));
            if (mods == null) throw new ArgumentNullException(nameof(mods));
            _count = new ItemCountRoller(loot);
            _rarity = new RarityRoller(loot);
            _heroes = new HeroSelector(items, loot);
            _named = new NamedItemSelector(items, loot);
            _kinds = new LegendaryKindSelector(loot, _named);
            _bases = new BaseSelector(items, loot);
            _affixes = new AffixRoller(items, loot, mods, rules);
            _epicMods = new EpicModifierRoller(loot, mods);
        }

        /// <summary>Number of items one occurrence of the source yields (05 sections 3.1-3.2). Draws from the same stream.</summary>
        public int RollItemCount(LootContext ctx, ref Xoshiro128StarStar rng) => _count.Roll(ctx, ref rng);

        public ItemInstance Generate(LootContext ctx, ref Xoshiro128StarStar rng)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            uint dropSeed = DeriveSeed(rng); // step 12: derived from the state before any draw
            var src = _loot.GetSource(ctx.Source);

            // 1. iLvl
            int ilvl = ILvlCalculator.Compute(_loot, ctx, src);

            // 2. rarity (+ Legendary minimum iLvl, 05 section 5.2)
            Rarity rarity = _rarity.Roll(ctx, src, ref rng);
            if (rarity == Rarity.Legendary)
            {
                int legMin = _items.GetRarity(Rarity.Legendary).MinDropILvl;
                if (ilvl < legMin)
                {
                    if (ctx.Guarantees.RaiseILvlToLegendaryMin) ilvl = legMin;
                    else rarity = Rarity.Epic;
                }
            }

            // 4. target hero (smart loot). Done before 3 because kind candidates depend on the hero.
            string hero = _heroes.Pick(ctx, rarity == Rarity.Legendary, ref rng);

            // 3 + 5. Legendary kind and named item
            NamedItemData named = null;
            if (rarity == Rarity.Legendary)
            {
                var kind = _kinds.Pick(ctx, hero, ilvl, ref rng);
                if (kind != ItemKind.Generic) named = _named.Pick(kind, ctx, hero, ilvl, ref rng);
                if (named == null) rarity = Rarity.Epic; // nothing to drop for this hero/iLvl/source
            }

            var item = new ItemInstance { ItemLevel = ilvl, Rarity = rarity, Source = ctx.Source, DropSeed = dropSeed, Kind = ItemKind.Generic };
            int tier = ItemMath.Tier(ilvl); // 6
            float rollMin = LootMath.RollFloor(_loot, ctx);

            if (named != null) BuildNamed(item, named, ctx, tier, rollMin, ref rng);
            else BuildGeneric(item, ctx, hero, tier, rollMin, ref rng);

            item.InstanceId = NextGuid(ref rng);
            ctx.Pity?.OnItemGenerated(item.Rarity, item.Kind, src.ConsumesHardPity);
            return item;
        }

        // ---------- Common / Rare / Epic ----------
        private void BuildGeneric(ItemInstance item, LootContext ctx, string hero, int tier, float rollMin, ref Xoshiro128StarStar rng)
        {
            var b = _bases.Pick(hero, ref rng);
            if (b == null) throw new InvalidOperationException("No droppable base for hero '" + hero + "'");
            item.DefinitionId = b.Id;
            item.BaseId = b.Id;
            // Armor implicit is derived from iLvl and not rolled; any other implicit always rolls (02 section 3.9.3 rule 4).
            item.ImplicitRoll01 = string.IsNullOrEmpty(b.ImplicitAffixId) ? 0f : Sampling.RollQuality(ref rng, rollMin);

            var r = _items.GetRarity(item.Rarity);
            _affixes.RollRandom(item, b, b.Slot, hero, r.RandomAffixCount, tier, rollMin, ref rng);
            if (item.Rarity == Rarity.Epic && r.ModifierCount > 0)
                _epicMods.Roll(item, hero, b.Slot, ctx, r.ModifierCount, rollMin, ref rng);
        }

        // ---------- Legendary / Set / Unique ----------
        private void BuildNamed(ItemInstance item, NamedItemData n, LootContext ctx, int tier, float rollMin, ref Xoshiro128StarStar rng)
        {
            item.Kind = n.Kind;
            item.DefinitionId = n.Id;
            item.BaseId = n.BaseId;
            item.BoundHeroId = n.HeroId; // R27: hero-specific named item

            var d = _loot.GetDifficulty(ctx.Difficulty);
            if (ctx.Difficulty == Difficulty.Ascension && ctx.AscensionLevel >= d.AscensionPrimalMinLevel && d.AscensionPrimalMinLevel > 0)
            {
                float chance = d.PrimalBaseChance + d.PrimalChancePerLevel * (ctx.AscensionLevel - d.AscensionPrimalMinLevel);
                if (rng.Chance(chance))
                {
                    item.Flags |= ItemFlags.Primal;
                    rollMin = Math.Max(rollMin, d.PrimalRollMin);
                }
            }

            var b = _items.GetBase(n.BaseId);
            item.ImplicitRoll01 = b != null && !string.IsNullOrEmpty(b.ImplicitAffixId) ? Sampling.RollQuality(ref rng, rollMin) : 0f;

            for (int i = 0; i < n.FixedAffixes.Count; i++)
            {
                var spec = n.FixedAffixes[i];
                item.Affixes.Add(new RolledAffix
                {
                    AffixId = spec.AffixId, Tier = (byte)tier,
                    Roll01 = Sampling.RollQuality(ref rng, rollMin),
                    Named = spec.Band != NamedBand.None, Band = spec.Band
                });
            }
            if (n.OpenOptions.Count > 0)
            {
                int choice = rng.Range(0, n.OpenOptions.Count);
                item.OpenAffixChoice = (byte)choice;
                // Decision of the orchestrator: the chosen Open Affix always uses the Default band.
                item.Affixes.Add(new RolledAffix
                {
                    AffixId = n.OpenOptions[choice].AffixId, Tier = (byte)tier,
                    Roll01 = Sampling.RollQuality(ref rng, rollMin),
                    Named = true, Band = NamedBand.Default
                });
            }
            for (int i = 0; i < n.ModifierIds.Count; i++)
                item.Modifiers.Add(new RolledModifier { ModifierId = n.ModifierIds[i], Roll01 = Sampling.RollQuality(ref rng, rollMin) });
        }

        // ---------- helpers ----------
        /// <summary>FNV-style mix of the 128-bit state; does not consume the stream.</summary>
        private static uint DeriveSeed(Xoshiro128StarStar rng)
        {
            var (s0, s1, s2, s3) = rng.GetState();
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ s0) * 16777619u;
                h = (h ^ s1) * 16777619u;
                h = (h ^ s2) * 16777619u;
                h = (h ^ s3) * 16777619u;
                return h;
            }
        }

        private static Guid NextGuid(ref Xoshiro128StarStar rng)
        {
            uint a = rng.NextUInt(), b = rng.NextUInt(), c = rng.NextUInt(), d = rng.NextUInt();
            unchecked
            {
                return new Guid((int)a, (short)(b >> 16), (short)b,
                    (byte)(c >> 24), (byte)(c >> 16), (byte)(c >> 8), (byte)c,
                    (byte)(d >> 24), (byte)(d >> 16), (byte)(d >> 8), (byte)d);
            }
        }
    }
}
