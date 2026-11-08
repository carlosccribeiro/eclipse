using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Setup
{
    /// <summary>
    /// Idempotent creator of CombatRuntimeConfig.asset (M05 T10): the scene-level combat numbers that have no CSV yet,
    /// copied from the docs (section named next to each value). SANDBOX values are test-rig tuning no doc defines.
    /// </summary>
    public static class CombatRuntimeConfigCreator
    {
        public const string AssetPath = "Assets/_Project/Content/Shared/Combat/CombatRuntimeConfig.asset";
        private const string StatesPath = "Assets/_Project/Content/Shared/CombatStates/default.asset";

        [MenuItem("Eclipse/Setup/Create Combat Runtime Config")]
        public static CombatRuntimeConfig Run()
        {
            var asset = AssetDatabase.LoadAssetAtPath<CombatRuntimeConfig>(AssetPath);
            if (asset == null)
            {
                EnsureFolder(System.IO.Path.GetDirectoryName(AssetPath).Replace('\\', '/'));
                asset = ScriptableObject.CreateInstance<CombatRuntimeConfig>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.combatStates = AssetDatabase.LoadAssetAtPath<CombatStatesDefinition>(StatesPath);

            // 01 3.4.1 / 3.6 caps
            asset.armorMitigationCap = 0.75f;
            asset.physicalResistanceCap = 0.30f;
            asset.resistanceCap = 0.75f;
            asset.resistanceFloor = -0.50f;
            asset.dodgeCap = 0.40f;
            asset.blockCap = 0.50f;
            asset.damageReductionCap = 0.40f;
            asset.blockAmount = 0.40f;          // 06 3.3 step 4
            asset.difficultyResPenalty = 0f;    // 01 3.4.1, Normal
            asset.resistanceFloorUnderStatus = -0.20f; // 01 3.4.2

            // 06 3.14
            asset.graceSeconds = 0.25f;
            asset.heavyGraceSeconds = 0.8f;
            asset.heavyHitHealthFraction = 0.20f;
            asset.tickIntervalSeconds = 0.5f;
            asset.lowHealthFraction = 0f;       // gap: no doc defines the OnLowHealth threshold (0 = off)
            asset.deathSlowMotionScale = 0.3f;
            asset.deathSlowMotionSeconds = 0.5f;
            asset.reviveHealthFraction = 0.5f;
            asset.reviveInvulnerabilitySeconds = 3f;
            asset.maxRevivesPerBossRoom = 1;

            // 06 3.12
            asset.flaskCharges = 3;
            asset.flaskHealFraction = 0.35f;
            asset.flaskHealSeconds = 1f;

            // 01 3.3, Kael level 1
            asset.kaelMaxHealth = 240f;
            asset.kaelArmor = 100f;
            asset.kaelCriticalChance = 0.05f;
            asset.kaelCriticalDamage = 0.50f;

            // SANDBOX (gap: not defined by the docs)
            asset.difficulty = DifficultyTier.Normal;
            asset.rngSeed = 20260101UL;
            asset.attackerLevel = 1f;
            asset.dummyMaxHealth = 1000f;
            asset.dummyArmorPercent = 0f;
            asset.testAttackIntervalSeconds = 2f;   // task brief
            asset.testAttackWindupSeconds = 0.6f;   // 06 3.10 common wind-up
            asset.testAttackActiveFrames = 4;
            asset.testAttackRecoveryFrames = 20;
            asset.testAttackRadius = 2.5f;
            asset.testAttackArcDegrees = 120f;
            asset.testAttackHealthFraction = 0.10f;

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
