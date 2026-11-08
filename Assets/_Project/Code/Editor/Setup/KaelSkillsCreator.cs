using System;
using System.Collections.Generic;
using Eclipse.Core.Damage;
using Eclipse.Data.Heroes;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Heroes.Kael;
using Eclipse.Gameplay.Skills;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Setup
{
    /// <summary>
    /// Authors the structure of Kael's five skills (08 4.4) in the SkillDefinition assets: Delivery and Effects from the M08 T4 library.
    /// Numbers are never written here; every Delivery/Effect reads them by key from the skill params (Skills.csv, applied by the importer).
    /// Also assigns the M04 MovementProfile to the Kael hero asset. Idempotent: a skill that already has the expected Delivery type and
    /// effect count is left untouched (SerializeReference ids are random, so rewriting would dirty the file).
    /// Run: -executeMethod Eclipse.Editor.Setup.KaelSkillsCreator.Run
    /// </summary>
    public static class KaelSkillsCreator
    {
        public const string SkillsFolder = "Assets/_Project/Content/Heroes/Kael/Skills";
        public const string HeroPath = "Assets/_Project/Content/Heroes/Kael/Data/kael.asset";
        public const string MovementProfilePath = "Assets/_Project/Content/Heroes/Kael/Data/Kael_MovementProfile.asset";
        private const string Burning = "status.burning";

        [MenuItem("Eclipse/Setup/Build Kael Skills")]
        public static void Run()
        {
            bool changed = false;
            foreach (KeyValuePair<string, Func<(Delivery, Effect[])>> kv in Recipes())
            {
                string path = SkillsFolder + "/" + kv.Key.Substring("skill.kael.".Length) + ".asset";
                var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
                if (skill == null) throw new InvalidOperationException("Skill asset missing (run the data import first): " + path);
                (Delivery delivery, Effect[] effects) = kv.Value();
                if (skill.Delivery != null && skill.Delivery.GetType() == delivery.GetType() && skill.Effects.Count == effects.Length) continue;

                var so = new SerializedObject(skill);
                so.FindProperty("delivery").managedReferenceValue = delivery;
                SerializedProperty list = so.FindProperty("effects");
                list.arraySize = effects.Length;
                for (int i = 0; i < effects.Length; i++) list.GetArrayElementAtIndex(i).managedReferenceValue = effects[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(skill);
                changed = true;
            }

            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(HeroPath);
            var profile = AssetDatabase.LoadAssetAtPath<ScriptableObject>(MovementProfilePath);
            if (hero != null && profile != null && hero.MovementProfile != profile)
            {
                var so = new SerializedObject(hero);
                so.FindProperty("movementProfile").objectReferenceValue = profile;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hero);
                changed = true;
            }

            if (changed) AssetDatabase.SaveAssets();
            Debug.Log("[KaelSkillsCreator] " + (changed ? "assets updated" : "already up to date"));
        }

        private static Dictionary<string, Func<(Delivery, Effect[])>> Recipes() => new Dictionary<string, Func<(Delivery, Effect[])>>
        {
            // Q: sector in front, physical damage at the cast coefficient (220%).
            [KaelSkillIds.CleavingStrike] = () => (
                new MeleeArcDelivery(),
                new Effect[] { new DamageEffect(DamageType.Physical) }),

            // Shift: aimed jump, Unstoppable in the air (active phase = flight), impact area with Ember damage.
            // The persistent ground Burning is a gap (needs a status-carrying zone in Combat); the landing area applies Burning for ground_burning_s.
            [KaelSkillIds.InfernalLeap] = () => (
                new LeapDelivery("range_m", "radius_m"),
                new Effect[]
                {
                    new ApplyStateEffect(SkillStateKind.Unstoppable, "unstoppable_s").At(EffectTiming.ActiveStart, EffectTargeting.Caster),
                    new DamageEffect(DamageType.Ember),
                    new ApplyStatusEffect(Burning, 1, "ground_burning_s"),
                }),

            // E: cone of up to max_targets; damage + Burning + Root; the tether breaks on root end or distance and deals the break damage.
            [KaelSkillIds.BurningChains] = () => (
                new ConeMultiTargetDelivery(),
                new Effect[]
                {
                    new DamageEffect(DamageType.Ember),
                    new ApplyStatusEffect(Burning),
                    new ApplyStateEffect(SkillStateKind.Root, "root_s"),
                    new TetherEffect("root_s", 0f, "break_distance_m", 0f, new DamageEffect(DamageType.Ember, "break_damage_coef_pct")),
                }),

            // R: stance (Parry window at the start), counter-attack at the end/release with the Fury refund. DR, block and the
            // parry bonus live in AshenGuardReceiver.
            [KaelSkillIds.AshenGuard] = () => (
                new StanceDelivery(),
                new Effect[]
                {
                    new ApplyStateEffect(SkillStateKind.ParryWindow).At(EffectTiming.ActiveStart, EffectTargeting.Caster),
                    new DamageEffect(DamageType.Physical),
                    new GainResourceEffect("resource_on_counter"),
                }),

            // F: Unstoppable for the whole cast, six radial fissures, Ruin Vulnerability stacks per fissure, global hit-stop and shake.
            [KaelSkillIds.Worldbreaker] = () => (
                new RadialLinesDelivery(),
                new Effect[]
                {
                    new ApplyStateEffect(SkillStateKind.Unstoppable, "unstoppable_s").At(EffectTiming.CastStart, EffectTargeting.Caster),
                    new DamageEffect(DamageType.Physical),
                    ApplyTimedModifierEffect.ForVulnerability(KaelSkillIds.WorldbreakerRuinSource, "ruin_vulnerability_pct", 0f,
                        "ruin_vulnerability_s", 0f, "ruin_stacks_per_fissure", 0, "ruin_stacks_max", 0),
                    new GlobalHitStopEffect(),
                }),
        };
    }
}
