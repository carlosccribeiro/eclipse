using Eclipse.Core.Random;
using Eclipse.Core.Tags;
using NUnit.Framework;

namespace Eclipse.Core.Damage.Tests
{
    /// <summary>Incoming pipeline of 06 3.3 with the mitigation math of 01 3.4.1 and the example of 01 3.12.</summary>
    public class IncomingResolverTests
    {
        // Fixture caps from the docs: Armor 75%, PhysRes 30%, Res 75% / floor -50% (01 3.4.1), Dodge 40% (01 3.6),
        // Block 50%, Damage Reduction 40% (01 3.4.1).
        private static readonly MitigationRules Rules = new MitigationRules(0.75f, 0.30f, 0.75f, -0.50f, 0.40f, 0.50f, 0.40f);

        private static IncomingDamageResolver NewResolver() => new IncomingDamageResolver(Rules);
        private static RngStreams Rng() => new RngStreams(12345UL);

        private static DamageInstance Hit(float physical, HitFlags flags = HitFlags.None, HitKind kind = HitKind.Hit)
        {
            var h = new DamageInstance { Flags = flags, Kind = kind };
            h.AddComponent(DamageType.Physical, physical);
            return h;
        }

        // A defense with no mitigation, so only the stage under test changes the number. Chances of 1 make rolls certain.
        private static IncomingDefense Plain() => new IncomingDefense { BlockAmount = 0.40f };

        private static IncomingContext Ctx(bool telegraphed = false, bool arc = true, float penalty = 0f, float eLvl = 1f) =>
            new IncomingContext(eLvl, penalty, telegraphed, arc);

        // ---- 01 3.4.1 Armor table ----

        [TestCase(500f, 1f, 0.476f)]
        [TestCase(2500f, 40f, 0.500f)]
        [TestCase(5000f, 85f, 0.513f)]
        [TestCase(9000f, 85f, 0.655f)]
        [TestCase(2500f, 1f, 0.75f)]
        [TestCase(500f, 40f, 0.167f)]
        [TestCase(500f, 85f, 0.095f)]
        [TestCase(9000f, 40f, 0.75f)]
        public void ArmorMitigation_MatchesDocTable(float armor, float eLvl, float expected)
        {
            Assert.That(MitigationMath.ArmorMitigation(armor, eLvl, Rules), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void ArmorConstant_IsFiftyPerLevelPlusFiveHundred()
        {
            Assert.That(MitigationMath.ArmorConstant(85f), Is.EqualTo(4750f));
        }

        [Test]
        public void PhysicalMitigation_IsTwoMultiplicativeLayers_AndPhysResCapsAt30()
        {
            // 01 3.12: ArmorMit 50.4% and PhysRes 8% -> 54.4%.
            Assert.That(MitigationMath.PhysicalMitigation(4835f, 85f, 0.08f, Rules), Is.EqualTo(0.544f).Within(0.001f));
            // PhysRes 50% raw clamps to 30%.
            float armorMit = MitigationMath.ArmorMitigation(4835f, 85f, Rules);
            Assert.That(MitigationMath.PhysicalMitigation(4835f, 85f, 0.50f, Rules),
                Is.EqualTo(1f - (1f - armorMit) * 0.70f).Within(1e-5f));
        }

        // ---- Resistances ----

        [Test]
        public void Resistance_AppliesDifficultyPenalty_01_3_12()
        {
            Assert.That(MitigationMath.EffectiveResistance(0.538f, 0.25f, Rules), Is.EqualTo(0.288f).Within(1e-4f)); // Elemental
            Assert.That(MitigationMath.EffectiveResistance(0.318f, 0.25f, Rules), Is.EqualTo(0.068f).Within(1e-4f)); // Void
        }

        [Test]
        public void Resistance_CapIsAppliedAfterPenalty()
        {
            // 90% raw - 25pp = 65% (< cap) - not 75% - 25pp = 50%.
            Assert.That(MitigationMath.EffectiveResistance(0.90f, 0.25f, Rules), Is.EqualTo(0.65f).Within(1e-5f));
            Assert.That(MitigationMath.EffectiveResistance(1.20f, 0.25f, Rules), Is.EqualTo(0.75f).Within(1e-5f));
        }

        [Test]
        public void Resistance_FloorsAtMinus50()
        {
            Assert.That(MitigationMath.EffectiveResistance(0.10f, 0.25f, Rules), Is.EqualTo(-0.15f).Within(1e-5f));
            Assert.That(MitigationMath.EffectiveResistance(-0.40f, 0.25f, Rules), Is.EqualTo(-0.50f).Within(1e-5f));
        }

        [Test]
        public void NegativeResistance_IncreasesDamageTaken()
        {
            var h = new DamageInstance();
            h.AddComponent(DamageType.Ember, 100f);
            IncomingDefense d = Plain();
            d.DodgeChance = 0f;
            d.ElementalResistance = 0.10f;
            IncomingResult r = NewResolver().Resolve(h, d, Ctx(telegraphed: true, penalty: 0.25f), Rng());
            Assert.That(r.AfterMitigation, Is.EqualTo(115f).Within(1e-3f));
            Assert.That(r.HealthDamage, Is.EqualTo(115f).Within(1e-3f));
        }

        [Test]
        public void ComponentsUseTheirOwnFamily()
        {
            var h = new DamageInstance();
            h.AddComponent(DamageType.Physical, 100f);
            h.AddComponent(DamageType.Frost, 100f);
            h.AddComponent(DamageType.Ruin, 100f);
            IncomingDefense d = Plain();
            d.Armor = 550f; // eLvl 1: K = 550 -> 50%
            d.ElementalResistance = 0.30f;
            d.VoidResistance = 0.10f;
            IncomingResult r = NewResolver().Resolve(h, d, Ctx(telegraphed: true), Rng());
            Assert.That(r.AfterMitigation, Is.EqualTo(50f + 70f + 90f).Within(1e-3f));
        }

        // ---- EHP 01 3.12 ----

        [Test]
        public void EffectiveHealthPhysical_ReproducesDocExample()
        {
            // Maximum Health 2958, Armor 4835, PhysRes 8%, eLvl 85 -> ~6487 (doc rounds MitPhysical to 54.4%).
            float ehp = MitigationMath.EffectiveHealthPhysical(2958f, 4835f, 85f, 0.08f, Rules);
            Assert.That(ehp, Is.EqualTo(6487f).Within(5f));
            // Elemental ~4155 and void ~3174 with raw resistances before the Eclipse penalty (52+1.77 and 30+1.8).
            Assert.That(MitigationMath.EffectiveHealthResistance(2958f, 0.5377f, 0.25f, Rules), Is.EqualTo(4155f).Within(8f));
            Assert.That(MitigationMath.EffectiveHealthResistance(2958f, 0.318f, 0.25f, Rules), Is.EqualTo(3174f).Within(8f));
        }

        // ---- Order: Block -> Damage Reduction -> Shield ----

        [Test]
        public void Order_MitigationThenBlockThenDamageReduction_01_3_4_1()
        {
            IncomingDefense d = Plain();
            d.Armor = 550f;            // 50% at eLvl 1
            d.BlockChance = 1f;        // certain block, -40%
            d.DamageReduction = 0.20f;
            IncomingResult r = NewResolver().Resolve(Hit(100f), d, Ctx(telegraphed: true), Rng());
            Assert.That(r.Blocked, Is.True);
            Assert.That(r.AfterMitigation, Is.EqualTo(50f).Within(1e-4f));
            Assert.That(r.AfterBlock, Is.EqualTo(30f).Within(1e-4f));
            Assert.That(r.AfterDamageReduction, Is.EqualTo(24f).Within(1e-4f));
            Assert.That(r.HealthDamage, Is.EqualTo(24f).Within(1e-4f));
        }

        [Test]
        public void DamageReduction_AndBlockChance_AreCapped()
        {
            IncomingDefense d = Plain();
            d.DamageReduction = 0.90f; // clamps to 40%
            IncomingResult r = NewResolver().Resolve(Hit(100f), d, Ctx(telegraphed: true), Rng());
            Assert.That(r.AfterDamageReduction, Is.EqualTo(60f).Within(1e-4f));

            // Block cap 50%: over many rolls with raw 100% the rate stays near 50%.
            d = Plain();
            d.BlockChance = 1f;
            var rng = Rng();
            var resolver = NewResolver();
            int blocked = 0;
            const int n = 4000;
            for (int i = 0; i < n; i++)
                if (resolver.Resolve(Hit(10f), d, Ctx(telegraphed: true), rng).Blocked) blocked++;
            Assert.That(blocked / (float)n, Is.EqualTo(0.5f).Within(0.05f));
        }

        [Test]
        public void Shield_AbsorbsBeforeHealth()
        {
            IncomingDefense d = Plain();
            d.ShieldPoints = 30f;
            IncomingResult r = NewResolver().Resolve(Hit(100f), d, Ctx(telegraphed: true), Rng());
            Assert.That(r.ShieldAbsorbed, Is.EqualTo(30f));
            Assert.That(r.HealthDamage, Is.EqualTo(70f));

            d.ShieldPoints = 500f;
            r = NewResolver().Resolve(Hit(100f), d, Ctx(telegraphed: true), Rng());
            Assert.That(r.ShieldAbsorbed, Is.EqualTo(100f));
            Assert.That(r.HealthDamage, Is.EqualTo(0f));
        }

        // ---- HitFlags x defenses matrix (06 3.3) ----

        private const HitFlags Normal = HitFlags.None;

        // Expected negation reason when the player has the given defenses active (each always succeeds when allowed).
        // Normal hit (non-telegraphed): i-frames, Parry, Dodge all negate.
        [TestCase(Normal, true, false, false, NegationReason.IFrames)]
        [TestCase(Normal, false, true, false, NegationReason.Parry)]
        [TestCase(Normal, false, false, true, NegationReason.Dodge)]
        // Unavoidable: i-frames and Parry still work, Dodge does not.
        [TestCase(HitFlags.Unavoidable, true, false, false, NegationReason.IFrames)]
        [TestCase(HitFlags.Unavoidable, false, true, false, NegationReason.Parry)]
        [TestCase(HitFlags.Unavoidable, false, false, true, NegationReason.None)]
        // Unblockable: i-frames work; Parry does not; Dodge still works (not Unavoidable).
        [TestCase(HitFlags.Unblockable, true, false, false, NegationReason.IFrames)]
        [TestCase(HitFlags.Unblockable, false, true, false, NegationReason.None)]
        [TestCase(HitFlags.Unblockable, false, false, true, NegationReason.Dodge)]
        // Annihilation ignores everything.
        [TestCase(HitFlags.Annihilation, true, false, false, NegationReason.None)]
        [TestCase(HitFlags.Annihilation, false, true, false, NegationReason.None)]
        [TestCase(HitFlags.Annihilation, false, false, true, NegationReason.None)]
        public void Matrix_NegationByFlag(HitFlags flags, bool iFrames, bool parry, bool dodge, NegationReason expected)
        {
            IncomingDefense d = Plain();
            d.HasIFrames = iFrames;
            d.ParryActive = parry;
            d.DodgeChance = dodge ? 1f : 0f;
            IncomingResult r = NewResolver().Resolve(Hit(100f, flags), d, Ctx(), Rng());
            Assert.That(r.Negation, Is.EqualTo(expected));
            Assert.That(r.HealthDamage, Is.EqualTo(expected == NegationReason.None ? 100f : 0f));
        }

        [TestCase(Normal, true)]
        [TestCase(HitFlags.Unavoidable, false)]
        [TestCase(HitFlags.Unblockable, false)]
        [TestCase(HitFlags.Annihilation, false)]
        public void Matrix_BlockByFlag(HitFlags flags, bool canBlock)
        {
            IncomingDefense d = Plain();
            d.BlockChance = 1f;
            // Many hits: with cap 50% a blockable flag blocks some; a non-blockable never does.
            var rng = Rng();
            var resolver = NewResolver();
            int blocked = 0;
            for (int i = 0; i < 200; i++)
                if (resolver.Resolve(Hit(100f, flags), d, Ctx(telegraphed: true), rng).Blocked) blocked++;
            Assert.That(blocked > 0, Is.EqualTo(canBlock));
        }

        [Test]
        public void Block_RequiresFrontalArc()
        {
            IncomingDefense d = Plain();
            d.BlockChance = 1f;
            var rng = Rng();
            var resolver = NewResolver();
            for (int i = 0; i < 200; i++)
                Assert.That(resolver.Resolve(Hit(100f), d, Ctx(telegraphed: true, arc: false), rng).Blocked, Is.False);
        }

        [Test]
        public void Dodge_DoesNotApplyToTelegraphedHits()
        {
            IncomingDefense d = Plain();
            d.DodgeChance = 1f;
            IncomingResult r = NewResolver().Resolve(Hit(100f), d, Ctx(telegraphed: true), Rng());
            Assert.That(r.Negation, Is.EqualTo(NegationReason.None));
        }

        [Test]
        public void Tick_IsNegatedByIFrames_ButNotByParry()
        {
            // 06 3.3 step 0 says "o Hit"; Tick handling is an open reading, logged in M03.
            // Chosen reading: i-frames negate any incoming damage (Ticks included); Parry only parries Hits.
            IncomingDefense iframes = Plain();
            iframes.HasIFrames = true;
            IncomingResult r = NewResolver().Resolve(Hit(10f, kind: HitKind.Tick), iframes, Ctx(), Rng());
            Assert.That(r.Negation, Is.EqualTo(NegationReason.IFrames));
            Assert.That(r.HealthDamage, Is.EqualTo(0f));

            IncomingDefense parry = Plain();
            parry.ParryActive = true;
            r = NewResolver().Resolve(Hit(10f, kind: HitKind.Tick), parry, Ctx(), Rng());
            Assert.That(r.Negation, Is.EqualTo(NegationReason.None));
            Assert.That(r.HealthDamage, Is.EqualTo(10f));
        }

        [Test]
        public void Tick_IsNotDodgedBlockedOrParried()
        {
            IncomingDefense d = Plain();
            d.DodgeChance = 1f;
            d.BlockChance = 1f;
            d.ParryActive = true;
            var rng = Rng();
            var resolver = NewResolver();
            for (int i = 0; i < 200; i++)
            {
                IncomingResult r = resolver.Resolve(Hit(10f, kind: HitKind.Tick), d, Ctx(), rng);
                Assert.That(r.Negation, Is.EqualTo(NegationReason.None));
                Assert.That(r.Blocked, Is.False);
                Assert.That(r.HealthDamage, Is.EqualTo(10f));
            }
        }

        [Test]
        public void Dodge_ChanceIsCapped()
        {
            IncomingDefense d = Plain();
            d.DodgeChance = 1f; // clamps to 40%
            var rng = Rng();
            var resolver = NewResolver();
            int dodged = 0;
            const int n = 4000;
            for (int i = 0; i < n; i++)
                if (resolver.Resolve(Hit(10f), d, Ctx(), rng).Negation == NegationReason.Dodge) dodged++;
            Assert.That(dodged / (float)n, Is.EqualTo(0.40f).Within(0.05f));
        }

        [Test]
        public void Annihilation_StillGetsMitigation()
        {
            IncomingDefense d = Plain();
            d.Armor = 550f; // 50% at eLvl 1
            d.HasIFrames = true;
            IncomingResult r = NewResolver().Resolve(Hit(100f, HitFlags.Annihilation), d, Ctx(), Rng());
            Assert.That(r.HealthDamage, Is.EqualTo(50f).Within(1e-4f));
        }
    }
}
