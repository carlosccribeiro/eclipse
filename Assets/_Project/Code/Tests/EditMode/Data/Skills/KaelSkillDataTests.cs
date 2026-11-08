using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using Eclipse.Data.Heroes;
using Eclipse.Data.Skills;
using NUnit.Framework;
using UnityEditor;

namespace Eclipse.Tests.EditMode.Data.Skills
{
    /// <summary>Checks the imported Kael skill, resource and hero assets against 08 sections 3.1, 3.2, 4.1 and 4.4.</summary>
    public class KaelSkillDataTests
    {
        private const string Folder = "Assets/_Project/Content/Heroes/Kael";

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

        private static SkillDefinition Skill(string name) => Load<SkillDefinition>("skill.kael." + name);

        private static void Param(SkillDefinition s, string key, float expected) =>
            Assert.AreEqual(expected, s.GetParam(key), 1e-5f, s.StableIdString + " " + key);

        private static void Header(SkillDefinition s, SkillSlot slot, string tags, float cost, float cooldown, float coef, int modSlots)
        {
            Assert.AreEqual("hero.kael", s.HeroId);
            Assert.AreEqual(slot, s.Slot);
            Assert.AreEqual(TagMask.Parse(tags), s.Tags);
            Assert.AreEqual(cost, s.CostAmount, 1e-5f);
            Assert.AreEqual(cooldown, s.CooldownSeconds, 1e-5f);
            Assert.AreEqual(coef, s.DamageCoefficient, 1e-5f);
            Assert.AreEqual(modSlots, s.ModifierSlots);
        }

        [Test]
        public void CleavingStrike()
        {
            SkillDefinition s = Skill("cleaving_strike");
            Header(s, SkillSlot.Q, "Attack|Melee|Area|Physical", 20f, 0f, 2.2f, 3);
            Assert.AreEqual("resource.fury", s.CostResourceId);
            Assert.IsFalse(s.CostAllResource);
            Param(s, "arc_deg", 180f);
            Param(s, "radius_m", 3.5f);
        }

        [Test]
        public void InfernalLeap()
        {
            SkillDefinition s = Skill("infernal_leap");
            Header(s, SkillSlot.Shift, "Attack|Movement|Area|Ember", 0f, 9f, 1.8f, 3);
            Assert.AreEqual(15f, s.ResourceGain, 1e-5f);
            Assert.IsEmpty(s.CostResourceId);
            Param(s, "range_m", 8f);
            Param(s, "radius_m", 3f);
            Param(s, "unstoppable_s", 0.5f);
            Param(s, "ground_burning_s", 2f);
        }

        [Test]
        public void BurningChains()
        {
            SkillDefinition s = Skill("burning_chains");
            Header(s, SkillSlot.E, "Attack|Ranged|Ember|DoT", 15f, 12f, 1.2f, 3);
            Assert.AreEqual("resource.fury", s.CostResourceId);
            Param(s, "range_m", 8f);
            Param(s, "max_targets", 5f);
            Param(s, "root_s", 2f);
            Param(s, "break_distance_m", 10f);
            Param(s, "break_damage_coef_pct", 60f);
        }

        [Test]
        public void AshenGuard()
        {
            SkillDefinition s = Skill("ashen_guard");
            Header(s, SkillSlot.R, "Defensive|Attack|Melee|Physical", 0f, 14f, 2.5f, 3);
            Param(s, "stance_s", 1.5f);
            Param(s, "damage_reduction_pct", 60f);
            Param(s, "absorbed_to_counter_pct", 30f);
            Param(s, "counter_radius_m", 3f);
            Param(s, "resource_on_counter", 20f);
            Param(s, "parry_window_s", 0.2f);
            Param(s, "parry_window_f", 12f);
            Param(s, "parry_counter_bonus_pct", 50f);
        }

        [Test]
        public void Worldbreaker()
        {
            SkillDefinition s = Skill("worldbreaker");
            Header(s, SkillSlot.F, "Attack|Area|Ultimate|Physical|Ruin", 0f, 90f, 6f, 2);
            Assert.IsTrue(s.CostAllResource);
            Assert.AreEqual(50f, s.CostMinimum, 1e-5f);
            Assert.AreEqual("resource.fury", s.CostResourceId);
            Assert.AreEqual(0.06f, s.DamageCoefficientPerResource, 1e-5f);
            Param(s, "radius_m", 7f);
            Param(s, "fissure_count", 6f);
            Param(s, "ruin_vulnerability_s", 6f);
            Param(s, "hitstop_f", 6f);
            Param(s, "shake", 0.5f);
            // Provisional (M08 premises): 1 stack per fissure, max 5.
            Param(s, "ruin_stacks_per_fissure", 1f);
            Param(s, "ruin_stacks_max", 5f);
        }

        [Test]
        public void SkillsAreUnflaggedWhereDocIsSilent()
        {
            foreach (string n in new[] { "cleaving_strike", "infernal_leap", "burning_chains", "ashen_guard", "worldbreaker" })
            {
                SkillDefinition s = Skill(n);
                Assert.AreEqual(0, s.MoveDuringCastPercent, n);
                Assert.IsFalse(s.Commit, n);
                Assert.IsFalse(s.Tracking, n);
            }
        }

        [Test]
        public void StructureIsEmptyUntilAuthored()
        {
            SkillDefinition s = Skill("cleaving_strike");
            Assert.IsNotNull(s.Effects);
            Assert.IsNotNull(s.Triggers);
        }

        [Test]
        public void FuryResource()
        {
            ResourceDefinition r = Load<ResourceDefinition>("resource.fury");
            Assert.AreEqual(ResourceKind.Pool, r.Kind);
            Assert.AreEqual(100f, r.BaseMaximum);
            Assert.AreEqual(150f, r.AbsoluteCap);
            Assert.AreEqual(1f, r.MaximumPerMaxResourcePercent);
            Assert.AreEqual(4f, r.DecayDelaySeconds);
            Assert.AreEqual(8f, r.DecayPerSecond);
            Assert.AreEqual(0f, r.StartOfRoomValue);
            Assert.AreEqual(5f, r.GetParam("basic_first_target"));
            Assert.AreEqual(1f, r.GetParam("basic_extra_target"));
            Assert.AreEqual(9f, r.GetParam("basic_max_per_hit"));
            Assert.AreEqual(10f, r.GetParam("heavy_tap"));
            Assert.AreEqual(15f, r.GetParam("heavy_charged"));
            Assert.AreEqual(1f, r.GetParam("damage_taken_per_pct_hp"));
            Assert.AreEqual(15f, r.GetParam("damage_taken_max_per_s"));
            Assert.AreEqual(3f, r.GetParam("kill"));
        }

        [Test]
        public void KaelHero()
        {
            HeroDefinition h = Load<HeroDefinition>("hero.kael");
            Assert.AreEqual(HeroPrimaryAttribute.Strength, h.PrimaryAttribute);
            Assert.AreEqual(1.2f, h.HealthRelative, 1e-5f);
            Assert.AreEqual(1.3f, h.ArmorRelative, 1e-5f);
            Assert.AreEqual(6.0f, h.MoveSpeed, 1e-5f);
            Assert.AreEqual(0f, h.EngagementMinMeters);
            Assert.AreEqual(4f, h.EngagementMaxMeters);
            Assert.AreEqual("Warbrand", h.OffHandCategory);
            CollectionAssert.AreEqual(new[] { "Greatsword", "Dual Axes", "Warhammer", "Longsword", "Heavy Blade" }, h.WeaponCategories);
            CollectionAssert.AreEqual(new[] { "passive.kael.embers_of_rage", "passive.kael.ashbound_resolve" }, h.PassiveIds);
            Assert.AreSame(Load<ResourceDefinition>("resource.fury"), h.Resource);
            Assert.AreSame(Skill("cleaving_strike"), h.GetSkill(SkillSlot.Q));
            Assert.AreSame(Skill("burning_chains"), h.GetSkill(SkillSlot.E));
            Assert.AreSame(Skill("ashen_guard"), h.GetSkill(SkillSlot.R));
            Assert.AreSame(Skill("infernal_leap"), h.GetSkill(SkillSlot.Shift));
            Assert.AreSame(Skill("worldbreaker"), h.GetSkill(SkillSlot.F));
            Assert.IsNull(h.GetSkill(SkillSlot.Basic));
            Assert.IsNull(h.GetSkill(SkillSlot.Heavy));
        }
    }
}
