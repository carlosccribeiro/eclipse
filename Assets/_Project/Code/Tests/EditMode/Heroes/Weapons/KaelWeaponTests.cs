using Eclipse.Data.Combat;
using Eclipse.Data.Definitions;
using Eclipse.Gameplay.Heroes;
using NUnit.Framework;
using UnityEditor;

namespace Eclipse.Tests.EditMode.Heroes.Weapons
{
    /// <summary>Frame data of the 4 non-Greatsword Kael weapons (06 3.6.2), sheet DPS (02 3.4) and kit bonuses (08 4.6).</summary>
    public class KaelWeaponTests
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

        private static AttackStep A(string id) => Load<AttackChainDefinition>(id).Step;

        private static void Check(string id, int s, int a, int r, AttackHitShape shape, float stagger, int hitStop, float proc)
        {
            AttackStep st = A(id);
            Assert.AreEqual(s, st.StartupFrames, id + " startup");
            Assert.AreEqual(a, st.ActiveFrames, id + " active");
            Assert.AreEqual(r, st.RecoveryFrames, id + " recovery");
            Assert.AreEqual(shape, st.Shape, id + " shape");
            Assert.AreEqual(stagger, st.Stagger, id + " stagger");
            Assert.AreEqual(hitStop, st.HitStopFrames, id + " hitstop");
            Assert.AreEqual(proc, st.ProcCoefficient, 1e-5f, id + " proc");
        }

        [TestCase("dual_axes.1"), TestCase("dual_axes.2"), TestCase("dual_axes.3"), TestCase("dual_axes.4")]
        public void DualAxesStrikes(string n)
        {
            Check("attack.kael." + n, 10, 3, 24, AttackHitShape.Sector, 5, 2, 0.8f);
            AttackStep s = A("attack.kael." + n);
            Assert.AreEqual(2.2f, s.Radius, 1e-5f);
            Assert.AreEqual(110f, s.ArcDegrees, 1e-5f);
        }

        [Test]
        public void DualAxesSpinAndWhirlwind()
        {
            Check("attack.kael.dual_axes.5", 14, 10, 16, AttackHitShape.Circle, 12, 4, 0.8f);
            Assert.AreEqual(2.2f, A("attack.kael.dual_axes.5").Radius, 1e-5f);
            Check("attack.kael.dual_axes.heavy", 8, 72, 14, AttackHitShape.Circle, 4, 1, 0.3f);
            AttackStep w = A("attack.kael.dual_axes.heavy");
            Assert.AreEqual(2.2f, w.Radius, 1e-5f);
            Assert.AreEqual(9, w.RehitIntervalFrames, "0.15 s at 60 Hz");
        }

        [Test]
        public void Warhammer()
        {
            Check("attack.kael.warhammer.1", 28, 8, 50, AttackHitShape.Sector, 30, 7, 1f);
            Assert.AreEqual(140f, A("attack.kael.warhammer.1").ArcDegrees, 1e-5f);
            Check("attack.kael.warhammer.2", 32, 8, 46, AttackHitShape.Sector, 40, 8, 1f);
            Assert.AreEqual(160f, A("attack.kael.warhammer.2").ArcDegrees, 1e-5f);
            Assert.AreEqual(2.5f, A("attack.kael.warhammer.2").Radius, 1e-5f);
            Check("attack.kael.warhammer.heavy", 40, 6, 34, AttackHitShape.Circle, 90, 10, 1f);
            Assert.AreEqual(4f, A("attack.kael.warhammer.heavy").Radius, 1e-5f);
        }

        [Test]
        public void Longsword()
        {
            Check("attack.kael.longsword.1", 14, 4, 32, AttackHitShape.Sector, 8, 3, 1f);
            Check("attack.kael.longsword.2", 14, 4, 32, AttackHitShape.Sector, 8, 3, 1f);
            Check("attack.kael.longsword.3", 16, 5, 29, AttackHitShape.Sector, 10, 3, 1f);
            Assert.AreEqual(140f, A("attack.kael.longsword.3").ArcDegrees, 1e-5f);
            Assert.AreEqual(2.8f, A("attack.kael.longsword.3").Radius, 1e-5f);
            Check("attack.kael.longsword.4", 20, 6, 24, AttackHitShape.Box, 25, 6, 1f);
            AttackStep t = A("attack.kael.longsword.4");
            Assert.AreEqual(3.5f, t.Length, 1e-5f);
            Assert.AreEqual(1f, t.Width, 1e-5f);
            Assert.AreEqual(3f, t.LungeMaxMeters, 1e-5f);
            Check("attack.kael.longsword.heavy", 22, 6, 30, AttackHitShape.Box, 45, 7, 1f);
            Assert.AreEqual(5f, A("attack.kael.longsword.heavy").Length, 1e-5f);
            Assert.AreEqual(1f, A("attack.kael.longsword.heavy").Width, 1e-5f);
        }

        [Test]
        public void HeavyBlade()
        {
            Check("attack.kael.heavy_blade.1", 22, 7, 46, AttackHitShape.Sector, 18, 5, 1f);
            Check("attack.kael.heavy_blade.2", 22, 7, 46, AttackHitShape.Sector, 18, 5, 1f);
            Check("attack.kael.heavy_blade.3", 26, 8, 41, AttackHitShape.Sector, 28, 7, 1f);
            Assert.AreEqual(180f, A("attack.kael.heavy_blade.3").ArcDegrees, 1e-5f);
            Assert.AreEqual(2.6f, A("attack.kael.heavy_blade.3").Radius, 1e-5f);
            Check("attack.kael.heavy_blade.heavy", 30, 6, 36, AttackHitShape.Sector, 60, 10, 1f);
            Assert.AreEqual(3f, A("attack.kael.heavy_blade.heavy").Radius, 1e-5f);
            Assert.AreEqual(90f, A("attack.kael.heavy_blade.heavy").ArcDegrees, 1e-5f);
        }

        [TestCase("attack.kael.dual_axes.dash", 6, 4, 10, 6f, 3)]
        [TestCase("attack.kael.longsword.dash", 8, 5, 14, 12f, 4)]
        [TestCase("attack.kael.warhammer.dash", 10, 6, 18, 20f, 5)]
        [TestCase("attack.kael.heavy_blade.dash", 10, 6, 18, 20f, 5)]
        public void DashAttacksFollowWeightClass(string id, int s, int a, int r, float stagger, int hs)
        {
            AttackStep st = A(id);
            Assert.AreEqual(s, st.StartupFrames);
            Assert.AreEqual(a, st.ActiveFrames);
            Assert.AreEqual(r, st.RecoveryFrames);
            Assert.AreEqual(stagger, st.Stagger);
            Assert.AreEqual(hs, st.HitStopFrames);
        }

        [TestCase("weapon.kael.greatsword", 0.9f)]
        [TestCase("weapon.kael.dual_axes", 1.0f)]
        [TestCase("weapon.kael.warhammer", 0.9f)]
        [TestCase("weapon.kael.longsword", 1.0f)]
        [TestCase("weapon.kael.heavy_blade", 1.05f)]
        public void SheetDpsIsNormalizedAcrossWeapons(string id, float factor)
        {
            WeaponDefinition w = Load<WeaponDefinition>(id);
            // 02 3.4: DPS_ref(iLvl 1) = 10 x factor; the factor already encodes area -10% / specialist +5%.
            Assert.AreEqual(10f * factor, w.WeaponPowerILvl1, 1e-4f);
            Assert.AreEqual(w.WeaponPowerILvl1 / w.AttacksPerSecond, w.DamagePerHitILvl1, 1e-4f);
            // Factor-normalized DPS sits within +-5% of the 10.0 reference for all five weapons.
            Assert.AreEqual(10f, w.WeaponPowerILvl1 / factor, 0.5f);
        }

        [TestCase("weapon.kael.dual_axes", 1.6f)]
        [TestCase("weapon.kael.warhammer", 0.7f)]
        [TestCase("weapon.kael.longsword", 1.2f)]
        [TestCase("weapon.kael.heavy_blade", 0.8f)]
        public void ComboLengthMatchesAps(string id, float aps)
        {
            WeaponDefinition w = Load<WeaponDefinition>(id);
            Assert.AreEqual(aps, w.AttacksPerSecond, 1e-5f);
            int total = 0;
            foreach (var c in w.BasicChain) total += c.Step.TotalFrames;
            Assert.AreEqual(60f / aps, (float)total / w.BasicChain.Count, 3f, "06 3.6 APS check: 60/APS +- 3 f");
        }

        [Test]
        public void ChainSizes()
        {
            Assert.AreEqual(5, Load<WeaponDefinition>("weapon.kael.dual_axes").BasicChain.Count);
            Assert.AreEqual(2, Load<WeaponDefinition>("weapon.kael.warhammer").BasicChain.Count);
            Assert.AreEqual(4, Load<WeaponDefinition>("weapon.kael.longsword").BasicChain.Count);
            Assert.AreEqual(3, Load<WeaponDefinition>("weapon.kael.heavy_blade").BasicChain.Count);
        }

        [Test]
        public void KitBonuses()
        {
            Assert.AreEqual(0.20f, WeaponKitBonus.ForWeapon("weapon.kael.greatsword").CleavingStrikeAreaIncrease);
            var da = WeaponKitBonus.ForWeapon("weapon.kael.dual_axes");
            Assert.AreEqual(2f, da.FuryPerBasic);
            Assert.AreEqual(1, da.BurningChainsExtraChains);
            var wh = WeaponKitBonus.ForWeapon("weapon.kael.warhammer");
            Assert.AreEqual(0.25f, wh.LeapWorldbreakerStaggerDamageIncrease);
            Assert.AreEqual(1f, wh.LeapRadiusBonusMeters);
            var ls = WeaponKitBonus.ForWeapon("weapon.kael.longsword");
            Assert.AreEqual(0.1f, ls.ParryWindowBonusSeconds);
            Assert.AreEqual(0.20f, ls.CounterAttackDamageIncrease);
            Assert.AreEqual(0.05f, WeaponKitBonus.ForWeapon("weapon.kael.heavy_blade").ExecuteThresholdBonus);
            Assert.AreEqual(0f, WeaponKitBonus.ForWeapon("weapon.unknown").FuryPerBasic);
        }

        [Test]
        public void AreaIncreaseScalesRadiusBySqrt()
        {
            Assert.AreEqual(3f * 1.0954451f, WeaponKitBonus.ScaleRadius(3f, 0.20f), 1e-4f);
            Assert.AreEqual(4f, WeaponKitBonus.ScaleRadius(2f, 3f), 1e-5f);
        }

        [Test]
        public void ParryWindowCapsAtThreeTenths()
        {
            var ls = WeaponKitBonus.ForWeapon("weapon.kael.longsword");
            Assert.AreEqual(0.25f, ls.ApplyParryWindow(0.15f), 1e-5f);
            Assert.AreEqual(0.3f, ls.ApplyParryWindow(0.25f), 1e-5f);
        }

        [Test]
        public void ExecuteThresholdCapAndElites()
        {
            var hb = WeaponKitBonus.ForWeapon("weapon.kael.heavy_blade");
            Assert.AreEqual(0.15f, hb.ApplyExecuteThreshold(0.10f, false), 1e-5f);
            Assert.AreEqual(0.125f, hb.ApplyExecuteThreshold(0.10f, true), 1e-5f);
            Assert.AreEqual(0.20f, hb.ApplyExecuteThreshold(0.18f, false), 1e-5f);
        }

        [Test]
        public void HeavyBladeHeavyDoublesDamageBelow35Percent()
        {
            Assert.AreEqual(2f, WeaponKitBonus.HeavyBladeHeavyMultiplier(0.34f));
            Assert.AreEqual(1f, WeaponKitBonus.HeavyBladeHeavyMultiplier(0.35f));
            Assert.AreEqual(1f, WeaponKitBonus.HeavyBladeHeavyMultiplier(0.9f));
        }
    }
}
