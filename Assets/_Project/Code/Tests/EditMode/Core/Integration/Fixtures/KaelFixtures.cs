using Eclipse.Core.Attributes;
using Eclipse.Core.Ids;
using Eclipse.Core.Stats;

namespace Eclipse.Core.Integration.Tests
{
    /// <summary>
    /// Kael level 50 of the reference example (01 3.12). Inputs exist in the doc only as aggregates, so the
    /// equipment is a handful of <see cref="StatMod"/>s per stat. Values are written out so the tests quote the docs;
    /// in the game they come from DataSource.
    /// </summary>
    internal static class KaelFixtures
    {
        public const int Level = 50;

        // 01 3.2.3 primaries per level and 01 3.3 hero base table (Kael).
        private static readonly LevelScaling Core = new LevelScaling(20f, 3f);       // STR 167 at level 50
        private static readonly LevelScaling Secondary = new LevelScaling(15f, 2f);  // DEX 113
        private static readonly LevelScaling Tertiary = new LevelScaling(10f, 1f);   // INT 59

        public static HeroAttributeProfile Profile() => new HeroAttributeProfile(
            new StableId("hero.kael"),
            StatId.Strength, StatId.Dexterity, StatId.Intelligence, Core, Secondary, Tertiary,
            new[]
            {
                new StatScaling(StatId.MaximumHealth, 240f, 22f),   // 1318 at level 50
                new StatScaling(StatId.Armor, 100f, 30f),           // 1570 at level 50
                new StatScaling(StatId.MovementSpeed, 6.0f),
                new StatScaling(StatId.CriticalChance, 0.05f),
                new StatScaling(StatId.CriticalDamage, 0.50f),
                new StatScaling(StatId.DodgeChance, 0f),
                new StatScaling(StatId.PickupRadius, 2.0f),
            },
            new[] { new PerPointEffect(StatId.AttackSpeed, ModBucket.Flat, 0.0004f) },   // Kael DEX: +0.04% AS/point
            ResourceTranslationRule.CreatePool(ResourceKind.Fury, 100f, 100f, 150f));

        // 01 3.2.1 universal per-point effects and 3.2.2 Core Attunement coefficient (0.08%/point -> More on skills).
        public static PrimaryAttributeRules PrimaryRules() => new PrimaryAttributeRules(
            strengthEffects: new[]
            {
                new PerPointEffect(StatId.Armor, ModBucket.Flat, 1f),
                new PerPointEffect(StatId.MaximumHealth, ModBucket.Increased, 0.0005f),
            },
            dexterityEffects: new[]
            {
                new PerPointEffect(StatId.CriticalChance, ModBucket.Flat, 0.00015f),
                new PerPointEffect(StatId.DodgeChance, ModBucket.Flat, 0.0002f),
            },
            intelligenceEffects: new[]
            {
                new PerPointEffect(StatId.ElementalResistance, ModBucket.Flat, 0.0003f),
                new PerPointEffect(StatId.VoidResistance, ModBucket.Flat, 0.0003f),
                new PerPointEffect(StatId.ResourceGeneration, ModBucket.Flat, 0.0005f),
            },
            coreAttunementPerPoint: 0.0008f,
            coreAttunementStat: StatId.GeneralDamage);

        // 01 3.6 caps / diminishing returns (stats of the table that matter for the example).
        public static StatRuleTable Rules() => new StatRuleTable()
            .Set(StatId.AttackSpeed, new StatRule(0.50f, 1.00f))
            .Set(StatId.CooldownReduction, new StatRule(0.25f, 0.40f))
            .Set(StatId.DodgeChance, new StatRule(0.20f, 0.40f))
            .Set(StatId.BlockChance, new StatRule(0.30f, 0.50f))
            .Set(StatId.DamageReduction, new StatRule(0.20f, 0.40f))
            .Set(StatId.CriticalChance, new StatRule(null, 0.75f))
            .Set(StatId.ArmorPenetration, new StatRule(null, 0.60f))
            .Set(StatId.PhysicalResistance, new StatRule(null, 0.30f));

        /// <summary>
        /// Equipment of the example (iLvl ~85, +6 upgrade already included) as aggregated flat mods, plus the
        /// offensive Increased lines of the Cleaving Strike breakdown.
        /// </summary>
        public static StatMod[] Equipment() => new[]
        {
            new StatMod(StatId.Strength, ModBucket.Flat, 230f),
            new StatMod(StatId.Dexterity, ModBucket.Flat, 60f),
            new StatMod(StatId.MaximumHealth, ModBucket.Flat, 1150f),
            new StatMod(StatId.Armor, ModBucket.Flat, 1768f),             // implicit (4 x 85 x 5.2); doc omits the x1.18
            new StatMod(StatId.Armor, ModBucket.Flat, 1100f),             // rolled
            new StatMod(StatId.PhysicalResistance, ModBucket.Flat, 0.08f),
            new StatMod(StatId.ElementalResistance, ModBucket.Flat, 0.52f),
            new StatMod(StatId.VoidResistance, ModBucket.Flat, 0.30f),
            new StatMod(StatId.CriticalChance, ModBucket.Flat, 0.14f),
            new StatMod(StatId.CriticalDamage, ModBucket.Flat, 0.80f),
            new StatMod(StatId.AttackSpeed, ModBucket.Flat, 0.18f),
            new StatMod(StatId.DodgeChance, ModBucket.Flat, 0.04f),
            new StatMod(StatId.CooldownReduction, ModBucket.Flat, 0.12f),
            new StatMod(StatId.MaximumResource, ModBucket.Flat, 0.15f),
            new StatMod(StatId.ResourceGeneration, ModBucket.Flat, 0.20f),
            new StatMod(StatId.ArmorPenetration, ModBucket.Flat, 0.10f),
            // Cleaving Strike Increased lines (01 3.12): Physical 45% (Main Hand x1.5 already applied), Skill 18%, Area 22%.
            new StatMod(StatId.PhysicalDamage, ModBucket.Increased, 0.45f),
            new StatMod(StatId.SkillDamage, ModBucket.Increased, 0.18f),
            new StatMod(StatId.AreaDamage, ModBucket.Increased, 0.22f),
        };

        /// <summary>Assembled Kael: block with base, equipment and primary-attribute sources applied.</summary>
        public static StatBlock Build(out HeroAttributeProfile profile)
        {
            profile = Profile();
            var block = new StatBlock(Rules());
            profile.ApplyBase(block, Level);
            block.AddSource(block.CreateSource(), Equipment());
            var sources = new PrimarySources(block.CreateSource(), block.CreateSource(), block.CreateSource());
            PrimaryRules().Refresh(block, profile, sources);
            return block;
        }
    }
}
