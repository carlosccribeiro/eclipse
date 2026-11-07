using Eclipse.Core.Ids;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;

namespace Eclipse.Core.Attributes.Tests
{
    /// <summary>
    /// Test fixtures with the values of 01 3.2 (primaries), 3.3 (hero bases) and 3.8 (resource translation).
    /// In the game these come from DataSource; here they are written out so the tests quote the docs.
    /// </summary>
    internal static class AttributeFixtures
    {
        // 01 3.2.1 universal per-point effects, as fractions.
        public static PrimaryAttributeRules Rules() => new PrimaryAttributeRules(
            strengthEffects: new[]
            {
                new PerPointEffect(StatId.Armor, ModBucket.Flat, 1f),                      // +1 Armor
                new PerPointEffect(StatId.MaximumHealth, ModBucket.Increased, 0.0005f),    // +0.05% Max Health
            },
            dexterityEffects: new[]
            {
                new PerPointEffect(StatId.CriticalChance, ModBucket.Flat, 0.00015f),       // +0.015 pp
                new PerPointEffect(StatId.DodgeChance, ModBucket.Flat, 0.0002f),           // +0.02 pp
            },
            intelligenceEffects: new[]
            {
                new PerPointEffect(StatId.ElementalResistance, ModBucket.Flat, 0.0003f),   // +0.03 pp
                new PerPointEffect(StatId.VoidResistance, ModBucket.Flat, 0.0003f),
                new PerPointEffect(StatId.ResourceGeneration, ModBucket.Flat, 0.0005f),    // +0.05%
            },
            coreAttunementPerPoint: 0.0008f,                                               // 01 3.2.2
            coreAttunementStat: StatId.GeneralDamage);

        // 01 3.2.3 primaries per level: Core 20 +3, Secondary 15 +2, Tertiary 10 +1.
        private static readonly LevelScaling Core = new LevelScaling(20f, 3f);
        private static readonly LevelScaling Secondary = new LevelScaling(15f, 2f);
        private static readonly LevelScaling Tertiary = new LevelScaling(10f, 1f);

        public static HeroAttributeProfile Kael() => new HeroAttributeProfile(
            new StableId("hero.kael"),
            StatId.Strength, StatId.Dexterity, StatId.Intelligence, Core, Secondary, Tertiary,
            CommonStats(240f, 22f, 100f, 30f, 6.0f, 0.05f, 0.50f, 0f),
            new[] { new PerPointEffect(StatId.AttackSpeed, ModBucket.Flat, 0.0004f) },       // 01 3.2.2
            ResourceTranslationRule.CreatePool(ResourceKind.Fury, 100f, 100f, 150f));

        public static HeroAttributeProfile Lyra() => new HeroAttributeProfile(
            new StableId("hero.lyra"),
            StatId.Intelligence, StatId.Dexterity, StatId.Strength, Core, Secondary, Tertiary,
            CommonStats(180f, 16f, 30f, 12f, 6.0f, 0.05f, 0.50f, 0f),
            new[] { new PerPointEffect(StatId.CastSpeed, ModBucket.Flat, 0.0004f) },
            ResourceTranslationRule.CreateDiscrete(ResourceKind.ArcaneCharges, 3, 0.25f, 6, 8,
                RestBonusKind.ChargeSkillDamage, 0.004f));

        public static HeroAttributeProfile Nyx() => new HeroAttributeProfile(
            new StableId("hero.nyx"),
            StatId.Dexterity, StatId.Intelligence, StatId.Strength, Core, Secondary, Tertiary,
            CommonStats(190f, 17f, 40f, 16f, 6.5f, 0.07f, 0.60f, 0.03f),
            new[]
            {
                // "Clone" or "Trap": two mods, since RequiredTags is all-of.
                new PerPointEffect(StatId.SkillDamage, ModBucket.Increased, 0.0005f, TagMask.Of(Tag.Clone)),
                new PerPointEffect(StatId.SkillDamage, ModBucket.Increased, 0.0005f, TagMask.Of(Tag.Trap)),
            },
            ResourceTranslationRule.CreatePool(ResourceKind.EchoEnergy, 100f, 100f, 150f));

        public static HeroAttributeProfile Orun() => new HeroAttributeProfile(
            new StableId("hero.orun"),
            StatId.Intelligence, StatId.Strength, StatId.Dexterity, Core, Secondary, Tertiary,
            CommonStats(230f, 20f, 80f, 24f, 5.8f, 0.05f, 0.50f, 0f),
            new[]
            {
                new PerPointEffect(StatId.MaximumHealth, ModBucket.Increased, 0.0015f, TagMask.Of(Tag.Summon)),
                new PerPointEffect(StatId.MaximumHealth, ModBucket.Increased, 0.0015f, TagMask.Of(Tag.Construct)),
                new PerPointEffect(StatId.Armor, ModBucket.Flat, 0.5f, TagMask.Of(Tag.Summon)),
                new PerPointEffect(StatId.Armor, ModBucket.Flat, 0.5f, TagMask.Of(Tag.Construct)),
            },
            ResourceTranslationRule.CreateDiscrete(ResourceKind.VoidConstructs, 3, 0.30f, 6, 8,
                RestBonusKind.ConstructLifeAndDamage, 0.005f));

        // 01 3.3 table (health, armor, movement, crit, crit damage, dodge).
        private static StatScaling[] CommonStats(float hp, float hpPer, float armor, float armorPer,
            float move, float crit, float critDamage, float dodge) => new[]
        {
            new StatScaling(StatId.MaximumHealth, hp, hpPer),
            new StatScaling(StatId.Armor, armor, armorPer),
            new StatScaling(StatId.MovementSpeed, move),
            new StatScaling(StatId.CriticalChance, crit),
            new StatScaling(StatId.CriticalDamage, critDamage),
            new StatScaling(StatId.DodgeChance, dodge),
            new StatScaling(StatId.PickupRadius, 2.0f),
        };
    }
}
