using Eclipse.Core.Attributes;
using Eclipse.Core.Damage;
using Eclipse.Core.Random;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using NUnit.Framework;

namespace Eclipse.Core.Integration.Tests
{
    /// <summary>
    /// Reproduces the numeric example of 01 3.12 (Kael level 50 vs eLvl 85 on Eclipse) through the Core APIs only.
    /// Tolerances: +-0.5 pp on percentages, +-1% on absolutes (M03 criteria 6 and 7).
    /// </summary>
    public class KaelReferenceExampleTests
    {
        private const float Pp = 0.005f; // 0.5 percentage points

        private StatBlock _kael;
        private HeroAttributeProfile _profile;
        private IncomingDefense _defense;
        private MitigationRules _mit;

        [SetUp]
        public void SetUp()
        {
            _kael = KaelFixtures.Build(out _profile);
            _defense = EclipseTargetFixtures.DefenseOf(_kael);
            _mit = EclipseTargetFixtures.Mitigation();
        }

        private static NUnit.Framework.Constraints.Constraint Within1Percent(float expected) =>
            Is.EqualTo(expected).Within(expected * 0.01f);

        [Test]
        public void Primaries_AreStr397_Dex173_Int59()
        {
            Assert.That(_kael.Evaluate(StatId.Strength), Is.EqualTo(397f));
            Assert.That(_kael.Evaluate(StatId.Dexterity), Is.EqualTo(173f));
            Assert.That(_kael.Evaluate(StatId.Intelligence), Is.EqualTo(59f));
        }

        [Test]
        public void MaximumHealth_Is2958()
        {
            Assert.That(_kael.Evaluate(StatId.MaximumHealth), Within1Percent(2958f));
        }

        [Test]
        public void Armor_Is4835()
        {
            Assert.That(_kael.Evaluate(StatId.Armor), Within1Percent(4835f));
        }

        [Test]
        public void ArmorMitigation_Is50Point4Percent()
        {
            float armorMit = MitigationMath.ArmorMitigation(_defense.Armor, EclipseTargetFixtures.MonsterLevel, _mit);
            Assert.That(armorMit, Is.EqualTo(0.504f).Within(Pp));
        }

        [Test]
        public void PhysicalMitigation_Is54Point4Percent()
        {
            float mit = MitigationMath.Mitigation(DamageType.Physical, _defense, EclipseTargetFixtures.MonsterLevel,
                EclipseTargetFixtures.EclipseResistancePenalty, _mit);
            Assert.That(_defense.PhysicalResistance, Is.EqualTo(0.08f).Within(Pp));
            Assert.That(mit, Is.EqualTo(0.544f).Within(Pp));
        }

        [Test]
        public void ElementalResistance_Is28Point8Percent_AfterEclipsePenalty()
        {
            float res = MitigationMath.Mitigation(DamageType.Ember, _defense, EclipseTargetFixtures.MonsterLevel,
                EclipseTargetFixtures.EclipseResistancePenalty, _mit);
            Assert.That(res, Is.EqualTo(0.288f).Within(Pp));
        }

        [Test]
        public void VoidResistance_Is6Point8Percent_AfterEclipsePenalty()
        {
            float res = MitigationMath.Mitigation(DamageType.Void, _defense, EclipseTargetFixtures.MonsterLevel,
                EclipseTargetFixtures.EclipseResistancePenalty, _mit);
            Assert.That(res, Is.EqualTo(0.068f).Within(Pp));
        }

        [Test]
        public void OffensiveAndUtilityStats_MatchTheSheet()
        {
            Assert.That(_kael.Evaluate(StatId.CriticalChance), Is.EqualTo(0.216f).Within(Pp));
            Assert.That(_kael.Evaluate(StatId.CriticalDamage), Is.EqualTo(1.30f).Within(Pp)); // x2.3 total
            Assert.That(1f + _kael.Evaluate(StatId.CriticalDamage), Is.EqualTo(2.3f).Within(0.005f));
            Assert.That(_kael.Evaluate(StatId.AttackSpeed), Is.EqualTo(0.249f).Within(Pp));
            Assert.That(_kael.Evaluate(StatId.DodgeChance), Is.EqualTo(0.075f).Within(Pp));
            Assert.That(_kael.Evaluate(StatId.CooldownReduction), Is.EqualTo(0.12f).Within(Pp));
            Assert.That(_kael.Evaluate(StatId.ResourceGeneration), Is.EqualTo(0.23f).Within(Pp));
        }

        [Test]
        public void MaximumFury_Is115()
        {
            ResourceMaximum fury = ResourceTranslation.Translate(_profile.Resource, _kael.Evaluate(StatId.MaximumResource));
            Assert.That(fury.Kind, Is.EqualTo(ResourceKind.Fury));
            Assert.That(fury.Maximum, Is.EqualTo(115f).Within(0.01f));
        }

        [Test]
        public void CoreAttunement_Is31Point8PercentMore()
        {
            float more = _kael.ProductMore(StatId.GeneralDamage, TagMask.None, null) - 1f;
            Assert.That(more, Is.EqualTo(0.318f).Within(Pp));
        }

        [Test]
        public void PhysicalEhp_Is6487()
        {
            float ehp = MitigationMath.EffectiveHealthPhysical(_kael.Evaluate(StatId.MaximumHealth), _defense.Armor,
                EclipseTargetFixtures.MonsterLevel, _defense.PhysicalResistance, _mit);
            Assert.That(ehp, Within1Percent(6487f));
        }

        // ---- Cleaving Strike (criterion 7) ----

        private static readonly TagMask CleavingTags = new TagMask(Tag.Attack, Tag.Melee, Tag.Area, Tag.Physical);

        private static OffensiveRequest CleavingRequest()
        {
            const float weaponPower = 65f;  // Greatsword iLvl 85 (02 3.4)
            const float coefficient = 2.20f; // 220% of Weapon Power (08)
            var request = new OffensiveRequest();
            request.AddBase(DamageType.Physical, weaponPower * coefficient);
            return request;
        }

        /// <summary>Resolves hits with Kael's real Crit Chance until both a non-crit and a crit sample exist.</summary>
        private void SampleHits(out float nonCrit, out float crit)
        {
            var resolver = new OffensiveDamageResolver(EclipseTargetFixtures.Offensive());
            var rng = new RngStreams(1234);
            var mods = new StatBlockDamageSource(_kael);
            var target = EclipseTargetFixtures.ArmoredTarget();
            var request = CleavingRequest();
            var hit = new DamageInstance { Tags = CleavingTags };
            nonCrit = crit = -1f;
            for (int i = 0; i < 200 && (nonCrit < 0f || crit < 0f); i++)
            {
                float total = resolver.Resolve(request, hit, _kael, mods, target, rng);
                if (hit.IsCrit) crit = total; else nonCrit = total;
            }
        }

        [Test]
        public void CleavingStrike_NonCrit_Is255()
        {
            SampleHits(out float nonCrit, out _);
            Assert.That(nonCrit, Is.GreaterThan(0f));
            Assert.That(nonCrit, Is.EqualTo(254.5f).Within(254.5f * 0.01f));
        }

        [Test]
        public void CleavingStrike_Crit_Is586()
        {
            SampleHits(out _, out float crit);
            Assert.That(crit, Is.GreaterThan(0f));
            Assert.That(crit, Is.EqualTo(585.5f).Within(585.5f * 0.01f));
        }

        [Test]
        public void CleavingStrike_ExpectedValue_Is326()
        {
            var resolver = new OffensiveDamageResolver(EclipseTargetFixtures.Offensive());
            var rng = new RngStreams(20240607);
            var mods = new StatBlockDamageSource(_kael);
            var target = EclipseTargetFixtures.ArmoredTarget();
            var request = CleavingRequest();
            var hit = new DamageInstance { Tags = CleavingTags };
            const int n = 40000;
            double sum = 0;
            for (int i = 0; i < n; i++) sum += resolver.Resolve(request, hit, _kael, mods, target, rng);
            Assert.That(sum / n, Is.EqualTo(326.0).Within(326.0 * 0.03));
        }
    }
}
