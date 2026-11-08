using System.Linq;
using Eclipse.Data.Combat;
using Eclipse.Data.Definitions;
using Eclipse.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Tests.EditMode.Data.Combat
{
    /// <summary>Checks the imported Greatsword assets against 06 sections 3.6.1-3.6.2, 08 section 4.3 and 02 section 3.4.</summary>
    public class WeaponDataTests
    {
        private const string Folder = "Assets/_Project/Content/Heroes/Kael/Weapons";

        private static T Load<T>(string id) where T : Definition
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Folder }))
            {
                var d = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null && d.StableIdString == id) return d;
            }
            Assert.Fail("asset not found: " + id + " (run the data import)");
            return null;
        }

        private static AttackChainDefinition Attack(string id) => Load<AttackChainDefinition>(id);

        private static void Frames(AttackStep s, int startup, int active, int recovery)
        {
            Assert.AreEqual(startup, s.StartupFrames, "startup");
            Assert.AreEqual(active, s.ActiveFrames, "active");
            Assert.AreEqual(recovery, s.RecoveryFrames, "recovery");
        }

        [TestCase("attack.kael.greatsword.1", 1)]
        [TestCase("attack.kael.greatsword.2", 2)]
        public void BasicStrikes1And2(string id, int index)
        {
            AttackChainDefinition a = Attack(id);
            AttackStep s = a.Step;
            Assert.AreEqual(AttackKind.Basic, a.Kind);
            Assert.AreEqual(index, a.ComboIndex);
            Frames(s, 20, 6, 40);
            Assert.AreEqual(AttackHitShape.Sector, s.Shape);
            Assert.AreEqual(2.8f, s.Radius, 1e-5f);
            Assert.AreEqual(120f, s.ArcDegrees, 1e-5f);
            Assert.AreEqual(15f, s.Stagger);
            Assert.AreEqual(5, s.HitStopFrames);
            Assert.AreEqual(1.0f, s.ProcCoefficient);
            Assert.AreEqual(1.10f, s.DamageMultiplier, 1e-5f);
            Assert.IsFalse(s.IsChargeable);
            Assert.AreEqual(60f, s.LungeConeDegrees);
        }

        [Test]
        public void BasicStrike3()
        {
            AttackStep s = Attack("attack.kael.greatsword.3").Step;
            Assert.AreEqual(3, Attack("attack.kael.greatsword.3").ComboIndex);
            Frames(s, 26, 8, 34);
            Assert.AreEqual(AttackHitShape.Sector, s.Shape);
            Assert.AreEqual(2.8f, s.Radius, 1e-5f);
            Assert.AreEqual(120f, s.ArcDegrees, 1e-5f);
            Assert.AreEqual(35f, s.Stagger);
            Assert.AreEqual(8, s.HitStopFrames);
            Assert.AreEqual(1.0f, s.ProcCoefficient);
            Assert.AreEqual(1.60f, s.DamageMultiplier, 1e-5f);
        }

        [Test]
        public void Heavy()
        {
            AttackChainDefinition a = Attack("attack.kael.greatsword.heavy");
            AttackStep s = a.Step;
            Assert.AreEqual(AttackKind.Heavy, a.Kind);
            Frames(s, 36, 6, 26);
            Assert.AreEqual(AttackHitShape.Cone, s.Shape);
            Assert.AreEqual(3f, s.Radius, 1e-5f);
            Assert.AreEqual(60f, s.Stagger);
            Assert.AreEqual(100f, s.StaggerMax);
            Assert.AreEqual(10, s.HitStopFrames);
            Assert.AreEqual(1.0f, s.ProcCoefficient);
            Assert.AreEqual(2.80f, s.DamageMultiplier, 1e-5f);
            Assert.AreEqual(4.20f, s.DamageMultiplierMax, 1e-5f);
            Assert.IsTrue(s.IsChargeable);
            Assert.AreEqual(36, s.ChargeMinFrames);
            Assert.AreEqual(72, s.ChargeMaxFrames);
        }

        [Test]
        public void DashAttackHeavyClass()
        {
            AttackChainDefinition a = Attack("attack.kael.greatsword.dash");
            AttackStep s = a.Step;
            Assert.AreEqual(AttackKind.Dash, a.Kind);
            Frames(s, 10, 6, 18);
            Assert.AreEqual(20f, s.Stagger);
            Assert.AreEqual(5, s.HitStopFrames);
            Assert.AreEqual(1.0f, s.DamageMultiplier, 1e-5f);
        }

        [Test]
        public void WeaponLinksChainAndWeaponPower()
        {
            WeaponDefinition w = Load<WeaponDefinition>("weapon.kael.greatsword");
            Assert.AreEqual(WeaponWeightClass.Heavy, w.WeightClass);
            Assert.AreEqual(0.9f, w.AttacksPerSecond, 1e-5f);
            Assert.AreEqual("base.kael.greatsword", w.BaseId);
            Assert.AreEqual(0.8f, w.ComboResetSeconds, 1e-5f);
            CollectionAssert.AreEqual(
                new[] { "attack.kael.greatsword.1", "attack.kael.greatsword.2", "attack.kael.greatsword.3" },
                w.BasicChain.Select(c => c.StableIdString).ToArray());
            Assert.AreEqual("attack.kael.greatsword.heavy", w.Heavy.StableIdString);
            Assert.AreEqual("attack.kael.greatsword.dash", w.DashAttack.StableIdString);
            // 02 section 3.4: DPS_ref(1) = 10 x 0.9 = 9; per hit = 9 / 0.9 = 10.
            Assert.AreEqual(9f, w.WeaponPowerILvl1, 1e-5f);
            Assert.AreEqual(10f, w.DamagePerHitILvl1, 1e-4f);
            Assert.AreEqual("feedback.shared.heavy", w.Feedback.StableIdString);
        }

        [Test]
        public void HeavyFeedbackProfile()
        {
            HitFeedbackProfile f = Load<HitFeedbackProfile>("feedback.shared.heavy");
            Assert.AreEqual(2, f.CritHitStopBonusFrames);
            Assert.AreEqual(100, f.MinHitStopSpacingMs);
            Assert.AreEqual(0.15f, f.ShakeTrauma, 1e-5f);
            Assert.AreEqual(0.25f, f.ShakeTraumaCrit, 1e-5f);
            Assert.AreEqual(1.5f, f.TraumaDecayPerSecond, 1e-5f);
            Assert.AreEqual(2, f.FlashFrames);
        }

        private static AttackChainDefinition Fake(string id, int s, int a, int r)
        {
            var d = ScriptableObject.CreateInstance<AttackChainDefinition>();
            d.EditorSetStableId(id);
            d.EditorSet("weapon.test.x", AttackKind.Basic, 1,
                new AttackStep(s, a, r, AttackHitShape.None, 0, 0, 0, 0, Vector3.zero, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, false, false, 0, 0));
            return d;
        }

        [Test]
        public void FrameRule_Passes_And_Fails()
        {
            AttackChainDefinition ok = Fake("attack.test.ok", 1, 1, 1);
            AttackChainDefinition bad = Fake("attack.test.bad", 0, 6, -3);
            try
            {
                ValidationReport good = DataValidator.Validate(new Definition[] { ok });
                Assert.AreEqual(0, good.Messages.Count(m => m.Rule == "AttackFrameData" && m.Severity == ValidationSeverity.Error));
                ValidationReport report = DataValidator.Validate(new Definition[] { bad });
                Assert.AreEqual(2, report.Messages.Count(m => m.Rule == "AttackFrameData" && m.Severity == ValidationSeverity.Error));
            }
            finally
            {
                Object.DestroyImmediate(ok);
                Object.DestroyImmediate(bad);
            }
        }
    }
}
