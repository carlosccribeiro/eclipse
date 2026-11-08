using Eclipse.Data.Combat;
using Eclipse.Data.Heroes;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Heroes;
using Eclipse.Gameplay.Heroes.Kael;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Eclipse.Editor.Setup
{
    /// <summary>
    /// Idempotent builder of Kael's complete kit on the Combat Sandbox (M08 T7): placeholder VFX (materials + disc prefabs by affinity),
    /// Fury / skills / weapon swap components on Kael_Greybox and a dedicated attacker for Ashen Guard and Parry in the scene.
    /// Runs after SandboxBuilder.Run (needs the Kael prefab, the dummies and the scene). Gameplay/Combat is not touched.
    /// Run: -executeMethod Eclipse.Editor.Setup.KaelSandboxBuilder.Run
    /// </summary>
    public static class KaelSandboxBuilder
    {
        public const string VfxFolder = "Assets/_Project/Content/Heroes/Kael/VFX";
        public const string WeaponsFolder = "Assets/_Project/Content/Heroes/Kael/Weapons";
        public const string GuardAttackerName = "TrainingAttacker_AshenGuard";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";

        // Order = debug keys F5..F9 (see KaelHeroRig).
        private static readonly string[] WeaponNames = { "greatsword", "dual_axes", "warhammer", "longsword", "heavy_blade" };

        // 06 3.11 colour per damage type. Physical white, Ember orange, Ruin sickly green.
        private static readonly Color Ember = new Color(1.00f, 0.50f, 0.05f);
        private static readonly Color Physical = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color Ruin = new Color(0.45f, 0.60f, 0.15f);

        [MenuItem("Eclipse/Setup/Build Kael Sandbox")]
        public static void Run()
        {
            KaelSkillsCreator.Run();
            GameObject ember = BuildVfx("Ember", Ember);
            GameObject physical = BuildVfx("Physical", Physical);
            GameObject ruin = BuildVfx("Ruin", Ruin);
            AugmentKael(ember, physical, ruin);
            PlaceGuardAttacker();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ---------------------------------------------------------------- VFX placeholders

        private static GameObject BuildVfx(string affinity, Color color)
        {
            EnsureFolder(VfxFolder);
            string matPath = VfxFolder + "/VFX_Disc_" + affinity + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find(UnlitShader);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            if (mat.shader != shader) mat.shader = shader;
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);

            string prefabPath = VfxFolder + "/VFX_Disc_" + affinity + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "VFX_Disc_" + affinity;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var saved = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return saved;
        }

        // ---------------------------------------------------------------- Kael prefab

        private static void AugmentKael(GameObject ember, GameObject physical, GameObject ruin)
        {
            var root = PrefabUtility.LoadPrefabContents(SandboxBuilder.KaelPrefabPath);
            try
            {
                if (root.GetComponent<KaelHeroRig>() != null) return; // already augmented (delete the components/prefab to regenerate)

                var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(KaelSkillsCreator.HeroPath);
                var weapons = new Object[WeaponNames.Length];
                for (int i = 0; i < weapons.Length; i++)
                {
                    weapons[i] = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WeaponsFolder + "/" + WeaponNames[i] + ".asset");
                    if (weapons[i] == null) throw new System.InvalidOperationException("Weapon asset missing: " + WeaponNames[i]);
                }
                if (hero == null) throw new System.InvalidOperationException("Hero asset missing: " + KaelSkillsCreator.HeroPath);

                var resource = root.AddComponent<ResourceComponent>();
                var skillRig = root.AddComponent<SkillCasterRig>();
                var kael = root.AddComponent<KaelHeroRig>();
                var vfx = root.AddComponent<KaelSkillVfx>();

                SetRef(resource, "definition", hero.Resource);
                SetRef(kael, "hero", hero);
                SetRefs(kael, "weapons", weapons);
                SetRef(kael, "actor", root.GetComponent<CombatActor>());
                SetRef(kael, "motor", root.GetComponent<PlayerMotor>());
                SetRef(kael, "dodge", root.GetComponent<DodgeController>());
                SetRef(kael, "attack", root.GetComponent<AttackController>());
                SetRef(kael, "resource", resource);
                SetRef(kael, "skillRig", skillRig);
                SetRef(vfx, "rig", kael);
                SetRef(vfx, "emberPrefab", ember);
                SetRef(vfx, "physicalPrefab", physical);
                SetRef(vfx, "ruinPrefab", ruin);

                PrefabUtility.SaveAsPrefabAsset(root, SandboxBuilder.KaelPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- scene

        private static void PlaceGuardAttacker()
        {
            var scene = EditorSceneManager.OpenScene(SandboxBuilder.ScenePath, OpenSceneMode.Single);
            if (GameObject.Find(GuardAttackerName) != null) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatSandboxContentBuilder.DummyPath("TrainingDummy_Medium"));
            GameObject arena = GameObject.Find("Arena");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, arena != null ? arena.transform : null);
            go.name = GuardAttackerName;
            // Close to the spawn point (the sector strike reaches ~3 m), looking at it: lets Ashen Guard / Parry (F1) be exercised at once.
            go.transform.position = new Vector3(0f, 0f, -3.5f);
            go.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            var so = new SerializedObject(go.GetComponent<TrainingAttacker>());
            so.FindProperty("attackEnabled").boolValue = false; // F4 enables every attacker (SandboxDebugControls)
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------------------------------------------------------------- utilities

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p == null) throw new System.InvalidOperationException("Property not found: " + property + " on " + target.GetType().Name);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefs(Object target, string property, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p == null) throw new System.InvalidOperationException("Property not found: " + property + " on " + target.GetType().Name);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
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
