using Eclipse.Core.Random;
using Eclipse.Core.Stats;
using Eclipse.Core.Tags;
using NUnit.Framework;

namespace Eclipse.Core.Damage.Tests
{
    /// <summary>Offensive pipeline of 06 3.2 / 11 3.5.2 with the Cleaving Strike example of 01 3.12.</summary>
    public class OffensiveResolverTests
    {
        // 01 3.4.2: Armor Penetration hard cap 60%; 01 3.4.2: status floor on Res% = -20%. Fixture values from the docs.
        private static readonly TagMask CleavingTags = new TagMask(Tag.Attack, Tag.Melee, Tag.Area, Tag.Physical);

        private static StatBlock Attacker(float critChance, float critDamage = 1.3f, float armorPen = 0.10f)
        {
            var rules = new StatRuleTable().Set(StatId.ArmorPenetration, new StatRule(null, 0.60f));
            var sb = new StatBlock(rules);
            sb.SetBase(StatId.CriticalChance, critChance);
            sb.SetBase(StatId.CriticalDamage, critDamage);
            sb.SetBase(StatId.ArmorPenetration, armorPen);
            return sb;
        }

        // Real StatBlock carrying the 01 3.12 Cleaving Strike mods (Increased 45% Physical + 18% Skill + 22% Area; Core Attunement More).
        private static StatBlock CleavingAttacker(float critChance)
        {
            StatBlock sb = Attacker(critChance);
            SourceHandle src = sb.CreateSource();
            sb.AddMod(src, new StatMod(StatId.PhysicalDamage, ModBucket.Increased, 0.45f));
            sb.AddMod(src, new StatMod(StatId.SkillDamage, ModBucket.Increased, 0.18f));
            sb.AddMod(src, new StatMod(StatId.AreaDamage, ModBucket.Increased, 0.22f));
            sb.AddMod(src, new StatMod(StatId.GeneralDamage, ModBucket.More, 0.318f)); // Core Attunement 397 x 0.08%
            return sb;
        }

        private static OffensiveDamageResolver Resolver() => new OffensiveDamageResolver(new OffensiveRules(-0.20f));

        private static DamageInstance Hit(TagMask tags, HitKind kind = HitKind.Hit)
        {
            var h = new DamageInstance();
            h.Tags = tags;
            h.Kind = kind;
            return h;
        }

        private static OffensiveRequest Req(DamageType type, float value)
        {
            var r = new OffensiveRequest();
            r.AddBase(type, value);
            return r;
        }

        private static TargetDefense Target(float armor = 0f)
        {
            return new TargetDefense { ArmorPercent = armor };
        }

        [Test]
        public void CleavingStrike_NonCrit_Is254()
        {
            var hit = Hit(CleavingTags);
            StatBlock a = CleavingAttacker(0f);
            float total = Resolver().Resolve(Req(DamageType.Physical, 143f), hit, a, new StatBlockDamageSource(a),
                Target(0.30f), new RngStreams(1));
            Assert.That(hit.IsCrit, Is.False);
            Assert.That(total, Is.EqualTo(254.5f).Within(1f)); // 143 x 1.85 x 1.318 x 0.73
        }

        [Test]
        public void CleavingStrike_Crit_Is586()
        {
            var hit = Hit(CleavingTags);
            StatBlock a = CleavingAttacker(1f);
            float total = Resolver().Resolve(Req(DamageType.Physical, 143f), hit, a, new StatBlockDamageSource(a),
                Target(0.30f), new RngStreams(1));
            Assert.That(hit.IsCrit, Is.True);
            Assert.That(total, Is.EqualTo(585.4f).Within(1.5f)); // x 2.3
        }

        [Test]
        public void CleavingStrike_ExpectedValue_Is326()
        {
            // Analytic: NonCrit x (1 + 0.216 x 1.3).
            StatBlock a0 = CleavingAttacker(0f);
            float nonCrit = Resolver().Resolve(Req(DamageType.Physical, 143f), Hit(CleavingTags), a0,
                new StatBlockDamageSource(a0), Target(0.30f), new RngStreams(1));
            Assert.That(nonCrit * (1f + 0.216f * 1.3f), Is.EqualTo(326f).Within(1.5f));

            // Monte Carlo over the Combat stream with Crit Chance 21.6%.
            var rng = new RngStreams(20240607);
            var resolver = Resolver();
            StatBlock attacker = CleavingAttacker(0.216f);
            var mods = new StatBlockDamageSource(attacker);
            var target = Target(0.30f);
            var request = Req(DamageType.Physical, 143f);
            var hit = Hit(CleavingTags);
            const int n = 40000;
            double sum = 0;
            for (int i = 0; i < n; i++) sum += resolver.Resolve(request, hit, attacker, mods, target, rng);
            Assert.That(sum / n, Is.EqualTo(326.0).Within(326.0 * 0.03));
        }

        [Test]
        public void Conversion_RuinComponentGetsRuinAreaSkill_NotPhysical()
        {
            // 06 3.2 step 3 example: converted Ruin sums Ruin + Skill + Area, never Physical Damage.
            StatBlock a = Attacker(0f, armorPen: 0f);
            SourceHandle src = a.CreateSource();
            a.AddMod(src, new StatMod(StatId.PhysicalDamage, ModBucket.Increased, 1.0f));
            a.AddMod(src, new StatMod(StatId.RuinDamage, ModBucket.Increased, 0.5f));
            a.AddMod(src, new StatMod(StatId.SkillDamage, ModBucket.Increased, 0.2f));
            a.AddMod(src, new StatMod(StatId.AreaDamage, ModBucket.Increased, 0.1f));
            // Needs the Physical tag: the source's Physical tag must not leak into the Ruin component.
            a.AddMod(src, new StatMod(StatId.RuinDamage, ModBucket.Increased, 9f, requiredTags: new TagMask(Tag.Physical)));

            var req = Req(DamageType.Physical, 100f);
            req.AddConversion(DamageType.Physical, DamageType.Ruin, 0.5f);
            var hit = Hit(CleavingTags);
            Resolver().Resolve(req, hit, a, new StatBlockDamageSource(a), Target(), new RngStreams(1));

            Assert.That(hit.GetValue(DamageType.Physical), Is.EqualTo(50f * (1f + 1.0f + 0.2f + 0.1f)).Within(0.01f));
            Assert.That(hit.GetValue(DamageType.Ruin), Is.EqualTo(50f * (1f + 0.5f + 0.2f + 0.1f)).Within(0.01f));
        }

        [Test]
        public void BasicAndHeavy_DoNotReceiveSkillDamage()
        {
            var mods = new StatModDamageSource();
            mods.Add(new StatMod(StatId.SkillDamage, ModBucket.Increased, 0.5f));
            var hit = Hit(new TagMask(Tag.Attack, Tag.Basic, Tag.Physical));
            Resolver().Resolve(Req(DamageType.Physical, 100f), hit, Attacker(0f), mods, Target(), new RngStreams(1));
            Assert.That(hit.TotalDamage, Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void FlatAdded_JoinsBase_IsConvertedAndFeedsGainAsExtra()
        {
            // 06 3.2 step 1: flat is inside Base. Base 50 + flat 50 = 100 Physical; 50% -> Ruin moves half of the flat too.
            var req = Req(DamageType.Physical, 50f);
            req.AddFlat(DamageType.Physical, 50f);
            req.AddGainAsExtra(DamageType.Physical, DamageType.Ember, 0.20f);
            req.AddConversion(DamageType.Physical, DamageType.Ruin, 0.50f);
            var hit = Hit(TagMask.None);
            Resolver().Resolve(req, hit, Attacker(0f), new StatModDamageSource(), Target(), new RngStreams(1));

            Assert.That(hit.GetValue(DamageType.Ember), Is.EqualTo(20f).Within(0.001f)); // 20% of (50 + 50)
            Assert.That(hit.GetValue(DamageType.Ruin), Is.EqualTo(50f).Within(0.001f));
            Assert.That(hit.GetValue(DamageType.Physical), Is.EqualTo(50f).Within(0.001f));
        }

        [Test]
        public void GeneralDamageMore_AppliesToBasicAndToEveryComponent()
        {
            StatBlock a = Attacker(0f);
            a.AddMod(a.CreateSource(), new StatMod(StatId.GeneralDamage, ModBucket.More, 0.5f));
            var req = Req(DamageType.Physical, 100f);
            req.AddFlat(DamageType.Ember, 100f);
            var hit = Hit(new TagMask(Tag.Attack, Tag.Basic));
            Resolver().Resolve(req, hit, a, new StatBlockDamageSource(a), Target(), new RngStreams(1));
            Assert.That(hit.GetValue(DamageType.Physical), Is.EqualTo(150f).Within(0.01f));
            Assert.That(hit.GetValue(DamageType.Ember), Is.EqualTo(150f).Within(0.01f));
        }

        [Test]
        public void CritStats_AreReadWithTheSourceTagContext()
        {
            StatBlock a = Attacker(0f, critDamage: 1.0f);
            SourceHandle src = a.CreateSource();
            // Only hits tagged Spell get +100% crit chance and +50% crit damage.
            a.AddMod(src, new StatMod(StatId.CriticalChance, ModBucket.Flat, 1f, requiredTags: new TagMask(Tag.Spell)));
            a.AddMod(src, new StatMod(StatId.CriticalDamage, ModBucket.Flat, 0.5f, requiredTags: new TagMask(Tag.Spell)));
            var resolver = Resolver();

            var melee = Hit(new TagMask(Tag.Attack));
            resolver.Resolve(Req(DamageType.Physical, 100f), melee, a, new StatModDamageSource(), Target(), new RngStreams(1));
            Assert.That(melee.IsCrit, Is.False);

            var spell = Hit(new TagMask(Tag.Spell));
            float total = resolver.Resolve(Req(DamageType.Physical, 100f), spell, a, new StatModDamageSource(), Target(), new RngStreams(1));
            Assert.That(spell.IsCrit, Is.True);
            Assert.That(total, Is.EqualTo(250f).Within(0.01f)); // 1 + (1.0 + 0.5)
        }

        [Test]
        public void Conversion_OverOneHundredPercent_IsNormalized()
        {
            var req = Req(DamageType.Physical, 100f);
            req.AddConversion(DamageType.Physical, DamageType.Ruin, 0.8f);
            req.AddConversion(DamageType.Physical, DamageType.Frost, 0.8f);
            var hit = Hit(TagMask.None);
            Resolver().Resolve(req, hit, Attacker(0f), new StatModDamageSource(), Target(), new RngStreams(1));
            Assert.That(hit.GetValue(DamageType.Ruin), Is.EqualTo(50f).Within(0.001f));
            Assert.That(hit.GetValue(DamageType.Frost), Is.EqualTo(50f).Within(0.001f));
            Assert.That(hit.GetValue(DamageType.Physical), Is.EqualTo(0f));
        }

        [Test]
        public void Conversion_HasNoChain()
        {
            var req = Req(DamageType.Physical, 100f);
            req.AddConversion(DamageType.Physical, DamageType.Ruin, 1f);
            req.AddConversion(DamageType.Ruin, DamageType.Ember, 1f);
            var hit = Hit(TagMask.None);
            Resolver().Resolve(req, hit, Attacker(0f), new StatModDamageSource(), Target(), new RngStreams(1));
            Assert.That(hit.GetValue(DamageType.Ruin), Is.EqualTo(100f).Within(0.001f));
            Assert.That(hit.GetValue(DamageType.Ember), Is.EqualTo(0f));
        }

        [Test]
        public void Vulnerability_IsCappedAtSixtyPercent()
        {
            var t = Target();
            t.Vulnerability = 0.90f; // R12 cap 0.60
            var hit = Hit(TagMask.None);
            Resolver().Resolve(Req(DamageType.Physical, 100f), hit, Attacker(0f), new StatModDamageSource(), t, new RngStreams(1));
            Assert.That(hit.TotalDamage, Is.EqualTo(160f).Within(0.01f));

            t.Vulnerability = 0.30f;
            Resolver().Resolve(Req(DamageType.Physical, 100f), hit, Attacker(0f), new StatModDamageSource(), t, new RngStreams(1));
            Assert.That(hit.TotalDamage, Is.EqualTo(130f).Within(0.01f));
        }

        [Test]
        public void Tick_NeverCrits_AndDoesNotConsumeRng()
        {
            var rng = new RngStreams(7);
            var reference = new RngStreams(7);
            var hit = Hit(new TagMask(Tag.DoT), HitKind.Tick);
            Resolver().Resolve(Req(DamageType.Ember, 100f), hit, Attacker(1f), new StatModDamageSource(), Target(), rng);
            Assert.That(hit.IsCrit, Is.False);
            Assert.That(hit.TotalDamage, Is.EqualTo(100f).Within(0.001f));
            Assert.That(rng.Get(RngStream.Combat).NextUInt(), Is.EqualTo(reference.Get(RngStream.Combat).NextUInt()));
        }

        [Test]
        public void Hit_RollsCritOncePerHit_ForAllComponents()
        {
            var req = Req(DamageType.Physical, 100f);
            req.AddFlat(DamageType.Ember, 100f);
            var hit = Hit(TagMask.None);
            var rng = new RngStreams(7);
            var reference = new RngStreams(7);
            Resolver().Resolve(req, hit, Attacker(1f, 1.0f), new StatModDamageSource(), Target(), rng);
            reference.Get(RngStream.Combat).NextUInt(); // exactly one draw
            Assert.That(rng.Get(RngStream.Combat).NextUInt(), Is.EqualTo(reference.Get(RngStream.Combat).NextUInt()));
            Assert.That(hit.GetValue(DamageType.Physical), Is.EqualTo(200f).Within(0.01f));
            Assert.That(hit.GetValue(DamageType.Ember), Is.EqualTo(200f).Within(0.01f));
        }

        [Test]
        public void MinimumOnePerHit_AlsoForTicks()
        {
            var hit = Hit(TagMask.None);
            float total = Resolver().Resolve(Req(DamageType.Physical, 0.2f), hit, Attacker(0f), new StatModDamageSource(),
                Target(0.5f), new RngStreams(1));
            Assert.That(total, Is.EqualTo(1f).Within(0.0001f));

            var tick = Hit(TagMask.None, HitKind.Tick);
            total = Resolver().Resolve(Req(DamageType.Ember, 0.01f), tick, Attacker(0f), new StatModDamageSource(), Target(),
                new RngStreams(1));
            Assert.That(total, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ResistanceFloor_UnderStatus()
        {
            var t = Target();
            t.SetFamilyResistances(-0.10f, 0.40f);
            t.SetStatusResistanceReduction(DamageType.Ember, 0.30f); // -0.10 - 0.30 -> floored at -0.20
            var hit = Hit(TagMask.None);
            Resolver().Resolve(Req(DamageType.Ember, 100f), hit, Attacker(0f), new StatModDamageSource(), t, new RngStreams(1));
            Assert.That(hit.TotalDamage, Is.EqualTo(120f).Within(0.01f));

            // Base already below the floor (declared weakness): statuses do not lower it further.
            t.SetResistance(DamageType.Frost, -0.30f);
            t.SetStatusResistanceReduction(DamageType.Frost, 0.20f);
            Resolver().Resolve(Req(DamageType.Frost, 100f), hit, Attacker(0f), new StatModDamageSource(), t, new RngStreams(1));
            Assert.That(hit.TotalDamage, Is.EqualTo(130f).Within(0.01f));

            // Normal reduction above the floor.
            t.SetResistance(DamageType.Storm, 0.30f);
            t.SetStatusResistanceReduction(DamageType.Storm, 0.10f);
            Resolver().Resolve(Req(DamageType.Storm, 100f), hit, Attacker(0f), new StatModDamageSource(), t, new RngStreams(1));
            Assert.That(hit.TotalDamage, Is.EqualTo(80f).Within(0.01f));
        }

        [Test]
        public void EchoAndRuin_AreMitigatedByVoidResistance()
        {
            var t = Target();
            t.SetFamilyResistances(0.75f, 0.40f);
            var hit = Hit(TagMask.None);
            Resolver().Resolve(Req(DamageType.Echo, 100f), hit, Attacker(0f), new StatModDamageSource(), t, new RngStreams(1));
            Assert.That(hit.TotalDamage, Is.EqualTo(60f).Within(0.01f));
            Resolver().Resolve(Req(DamageType.Ruin, 100f), hit, Attacker(0f), new StatModDamageSource(), t, new RngStreams(1));
            Assert.That(hit.TotalDamage, Is.EqualTo(60f).Within(0.01f));
        }

        [Test]
        public void ArmorPenetration_UsesHardCapFromRules()
        {
            var hit = Hit(TagMask.None);
            Resolver().Resolve(Req(DamageType.Physical, 100f), hit, Attacker(0f, armorPen: 0.90f), new StatModDamageSource(),
                Target(0.50f), new RngStreams(1));
            // Penetration capped at 0.60 by the rule table: 0.50 x 0.40 = 0.20 mitigation.
            Assert.That(hit.TotalDamage, Is.EqualTo(80f).Within(0.01f));
        }

        [Test]
        public void CritRoll_IsDeterministicForTheSameSeed()
        {
            var a = new RngStreams(99);
            var b = new RngStreams(99);
            var c = new RngStreams(100);
            var resolver = Resolver();
            var attacker = Attacker(0.5f);
            var req = Req(DamageType.Physical, 100f);
            var ha = Hit(TagMask.None); var hb = Hit(TagMask.None); var hc = Hit(TagMask.None);
            bool anyDiff = false;
            for (int i = 0; i < 64; i++)
            {
                resolver.Resolve(req, ha, attacker, new StatModDamageSource(), Target(), a);
                resolver.Resolve(req, hb, attacker, new StatModDamageSource(), Target(), b);
                resolver.Resolve(req, hc, attacker, new StatModDamageSource(), Target(), c);
                Assert.That(hb.IsCrit, Is.EqualTo(ha.IsCrit));
                if (hc.IsCrit != ha.IsCrit) anyDiff = true;
            }
            Assert.That(anyDiff, Is.True);
        }
    }
}
