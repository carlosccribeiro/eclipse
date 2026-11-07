using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using NUnit.Framework;

namespace Eclipse.Core.Attributes.Tests
{
    public class HeroAttributeTests
    {
        private const float Eps = 0.0005f; // 0.05 pp on fractions

        // 01 3.3: Value(lv) = Base + (lv - 1) * PerLevel.
        [TestCase(1, 240f)]
        [TestCase(2, 262f)]
        [TestCase(50, 1318f)]
        public void LevelScaling_KaelHealth(int level, float expected) =>
            Assert.AreEqual(expected, HeroLevelScaling.Value(240f, 22f, level), 0.001f);

        [Test]
        public void LevelScaling_LevelBelowOneCountsAsOne() =>
            Assert.AreEqual(240f, HeroLevelScaling.Value(240f, 22f, 0), 0.001f);

        // 01 3.2.3: Core 20/+3 -> 167, Secondary 15/+2 -> 113, Tertiary 10/+1 -> 59 at level 50.
        [Test]
        public void PrimaryPointsAtLevel50()
        {
            var kael = AttributeFixtures.Kael();
            Assert.AreEqual(167f, kael.BasePrimaryPoints(StatId.Strength, 50), 0.001f);
            Assert.AreEqual(113f, kael.BasePrimaryPoints(StatId.Dexterity, 50), 0.001f);
            Assert.AreEqual(59f, kael.BasePrimaryPoints(StatId.Intelligence, 50), 0.001f);
            Assert.AreEqual(20f, kael.BasePrimaryPoints(StatId.Strength, 1), 0.001f);
        }

        // 01 3.3: nv 50 health/armor of the four heroes.
        [TestCase("kael", 1318f, 1570f)]
        [TestCase("lyra", 964f, 618f)]
        [TestCase("nyx", 1023f, 824f)]
        [TestCase("orun", 1210f, 1256f)]
        public void BaseStatsAtLevel50(string hero, float health, float armor)
        {
            HeroAttributeProfile p = hero == "kael" ? AttributeFixtures.Kael()
                : hero == "lyra" ? AttributeFixtures.Lyra()
                : hero == "nyx" ? AttributeFixtures.Nyx() : AttributeFixtures.Orun();
            Assert.AreEqual(health, p.BaseStatValue(StatId.MaximumHealth, 50), 0.001f);
            Assert.AreEqual(armor, p.BaseStatValue(StatId.Armor, 50), 0.001f);
        }

        [Test]
        public void Profile_RejectsRepeatedPrimary()
        {
            Assert.Throws<System.ArgumentException>(() => new HeroAttributeProfile(
                new Eclipse.Core.Ids.StableId("hero.bad"),
                StatId.Strength, StatId.Strength, StatId.Intelligence,
                new LevelScaling(1, 1), new LevelScaling(1, 1), new LevelScaling(1, 1),
                null, null, AttributeFixtures.Kael().Resource));
        }

        // 01 3.12: Kael lv 50 with equipment STR +230, DEX +60 -> STR 397, DEX 173, INT 59.
        private static StatBlock BuildKaelSheet(out HeroAttributeProfile kael)
        {
            kael = AttributeFixtures.Kael();
            var block = new StatBlock(new StatRuleTable());
            kael.ApplyBase(block, 50);
            block.SetBase(StatId.GeneralDamage, 1f); // neutral Base so a More mod is observable

            SourceHandle equip = block.CreateSource();
            block.AddSource(equip, new[]
            {
                new StatMod(StatId.Strength, ModBucket.Flat, 230f),
                new StatMod(StatId.Dexterity, ModBucket.Flat, 60f),
                new StatMod(StatId.MaximumHealth, ModBucket.Flat, 1150f),
                new StatMod(StatId.Armor, ModBucket.Flat, 1768f + 1100f), // implicit + rolled
                new StatMod(StatId.CriticalChance, ModBucket.Flat, 0.14f),
                new StatMod(StatId.AttackSpeed, ModBucket.Flat, 0.18f),
                new StatMod(StatId.DodgeChance, ModBucket.Flat, 0.04f),
                new StatMod(StatId.ElementalResistance, ModBucket.Flat, 0.52f - 0.25f), // equip - Eclipse penalty
                new StatMod(StatId.VoidResistance, ModBucket.Flat, 0.30f - 0.25f),
                new StatMod(StatId.ResourceGeneration, ModBucket.Flat, 0.20f),
            });

            AttributeFixtures.Rules().Refresh(block, kael, new PrimarySources(
                block.CreateSource(), block.CreateSource(), block.CreateSource()));
            return block;
        }

        [Test]
        public void KaelExample_Primaries()
        {
            var b = BuildKaelSheet(out _);
            Assert.AreEqual(397f, b.Evaluate(StatId.Strength), 0.001f);
            Assert.AreEqual(173f, b.Evaluate(StatId.Dexterity), 0.001f);
            Assert.AreEqual(59f, b.Evaluate(StatId.Intelligence), 0.001f);
        }

        // 01 3.12 table: Maximum Health 2958 = (1318 + 1150) * (1 + 397 * 0.05%); Armor 4835.
        [Test]
        public void KaelExample_HealthAndArmor()
        {
            var b = BuildKaelSheet(out _);
            Assert.AreEqual(2958f, b.Evaluate(StatId.MaximumHealth), 2958f * 0.01f);
            Assert.AreEqual(4835f, b.Evaluate(StatId.Armor), 4835f * 0.01f);
        }

        // 01 3.12 table: Crit 21.6%, Attack Speed +24.9%, Dodge 7.5%, Elemental 28.8%, Void 6.8%, Res Gen +23%.
        [Test]
        public void KaelExample_PerPointEffects()
        {
            var b = BuildKaelSheet(out _);
            Assert.AreEqual(0.216f, b.Evaluate(StatId.CriticalChance), Eps);
            Assert.AreEqual(0.249f, b.Evaluate(StatId.AttackSpeed), Eps);   // Kael Secondary DEX: +0.04%/pt
            Assert.AreEqual(0.075f, b.Evaluate(StatId.DodgeChance), Eps);
            Assert.AreEqual(0.288f, b.Evaluate(StatId.ElementalResistance), Eps);
            Assert.AreEqual(0.068f, b.Evaluate(StatId.VoidResistance), Eps);
            Assert.AreEqual(0.23f, b.Evaluate(StatId.ResourceGeneration), Eps);
        }

        // 01 3.2.2 / 3.12: Core Attunement = 397 * 0.08% = +31.8% More.
        [Test]
        public void KaelExample_CoreAttunement()
        {
            var b = BuildKaelSheet(out _);
            StatMod m = AttributeFixtures.Rules().BuildCoreAttunement(397f);
            Assert.AreEqual(ModBucket.More, m.Bucket);
            Assert.AreEqual(0.318f, m.Value, Eps);
            Assert.AreEqual(1.318f, b.Evaluate(StatId.GeneralDamage), 0.001f);
        }

        // 01 3.3: Kael has Fury 100; 01 3.12: Fury max 115 with +15% Maximum Resource.
        [Test]
        public void KaelExample_FuryMaximum()
        {
            var kael = AttributeFixtures.Kael();
            Assert.AreEqual(115f, ResourceTranslation.Translate(kael.Resource, 0.15f).Maximum, 0.001f);
        }

        // 01 3.2.2: Nyx Secondary (INT) gives +0.05% Increased only for Clone and Trap.
        [Test]
        public void NyxSecondary_AppliesOnlyToCloneAndTrap()
        {
            var nyx = AttributeFixtures.Nyx();
            var block = new StatBlock(new StatRuleTable());
            block.SetBase(StatId.SkillDamage, 1f);
            block.AddSource(block.CreateSource(), AttributeFixtures.Rules().BuildSecondaryMods(nyx, 100f));
            Assert.AreEqual(1.05f, block.Evaluate(StatId.SkillDamage, TagMask.Of(Tag.Clone), null), 0.0001f);
            Assert.AreEqual(1.05f, block.Evaluate(StatId.SkillDamage, TagMask.Of(Tag.Trap), null), 0.0001f);
            Assert.AreEqual(1.00f, block.Evaluate(StatId.SkillDamage, TagMask.Of(Tag.Melee), null), 0.0001f);
        }

        // 01 3.2.2: Orun Secondary (STR) +0.15% life and +0.5 Armor for Summon/Construct.
        [Test]
        public void OrunSecondary_AppliesToConstructs()
        {
            var orun = AttributeFixtures.Orun();
            var block = new StatBlock(new StatRuleTable());
            block.SetBase(StatId.MaximumHealth, 100f);
            block.AddSource(block.CreateSource(), AttributeFixtures.Rules().BuildSecondaryMods(orun, 100f));
            Assert.AreEqual(115f, block.Evaluate(StatId.MaximumHealth, TagMask.Of(Tag.Construct), null), 0.001f);
            Assert.AreEqual(50f, block.Evaluate(StatId.Armor, TagMask.Of(Tag.Summon), null), 0.001f);
            Assert.AreEqual(100f, block.Evaluate(StatId.MaximumHealth), 0.001f);
        }

        // Refresh replaces (does not stack) the attribute sources.
        [Test]
        public void Refresh_ReplacesPreviousMods()
        {
            var kael = AttributeFixtures.Kael();
            var b = new StatBlock(new StatRuleTable());
            kael.ApplyBase(b, 50);
            var rules = AttributeFixtures.Rules();
            var s = new PrimarySources(b.CreateSource(), b.CreateSource(), b.CreateSource());
            rules.Refresh(b, kael, s);
            float once = b.Evaluate(StatId.Armor);
            rules.Refresh(b, kael, s);
            Assert.AreEqual(1570f + 167f, once, 0.001f); // base Armor + STR 167
            Assert.AreEqual(once, b.Evaluate(StatId.Armor), 0.001f);
        }
    }
}
