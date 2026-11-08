using Eclipse.AI.Diagnostics;
using Eclipse.AI.Locomotion;
using Eclipse.AI.Spawning;
using Eclipse.AI.Telegraphs;
using Eclipse.Data.Difficulties;
using Eclipse.Data.Enemies;
using Eclipse.Gameplay.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Eclipse.Editor.Setup.EnemyPrefabCreator;

namespace Eclipse.Editor.Setup
{
    /// <summary>
    /// Idempotent builder of the soak scene (M07 T9): a 64x64 walled room, the shared Kael/CombatWorld/bootstrap prefabs of the AI
    /// Sandbox, the composition root with empty pools (the <see cref="SoakTestRunner"/> warms them), the bot and the runner.
    /// Reuses AiSandboxBuilder.Run for the prefabs and assets without editing it. An existing scene is kept: delete the .unity to
    /// regenerate. Run: -executeMethod Eclipse.Editor.Setup.SoakSceneBuilder.Run
    /// </summary>
    public static class SoakSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Perf/SoakTest.unity";
        private const string ScalingPath = "Assets/_Project/Content/Enemies/Scaling/EnemyScaling.asset";
        private const string DifficultyPath = "Assets/_Project/Content/Shared/Difficulty/normal.asset";
        private const string GreyboxFolder = "Assets/_Project/Art/Greybox";
        private const float HalfRoom = 32f;

        [MenuItem("Eclipse/Setup/Build Soak Scene")]
        public static void Run()
        {
            AiSandboxBuilder.Run(); // prefabs, palette, materials (idempotent)
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildScene()
        {
            EnsureFolder("Assets/_Project/Scenes/Perf");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Material floorM = LoadMat("Greybox_Floor");
            Material wallM = LoadMat("Greybox_Wall");
            var arena = new GameObject("Arena").transform;
            float w = HalfRoom * 2f + 2f;
            Block(arena, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(HalfRoom * 2f, 0.2f, HalfRoom * 2f), 0, floorM);
            Block(arena, "Wall_N", new Vector3(0f, 2f, HalfRoom + 0.5f), new Vector3(w, 4f, 1f), PhysicsLayers.Wall, wallM);
            Block(arena, "Wall_S", new Vector3(0f, 2f, -HalfRoom - 0.5f), new Vector3(w, 4f, 1f), PhysicsLayers.Wall, wallM);
            Block(arena, "Wall_E", new Vector3(HalfRoom + 0.5f, 2f, 0f), new Vector3(1f, 4f, w), PhysicsLayers.Wall, wallM);
            Block(arena, "Wall_W", new Vector3(-HalfRoom - 0.5f, 2f, 0f), new Vector3(1f, 4f, w), PhysicsLayers.Wall, wallM);

            // The debug overlay's OnGUI allocates every frame (IMGUI) and would drown the GC measurement: off in the soak scene.
            var world = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CombatSandboxContentBuilder.WorldPrefabPath));
            foreach (var overlay in world.GetComponentsInChildren<Eclipse.Gameplay.Combat.Integration.CombatDebugOverlay>(true)) overlay.enabled = false;
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SandboxBuilder.BootstrapPrefabPath));
            var kael = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SandboxBuilder.KaelPrefabPath));
            kael.transform.position = new Vector3(7f, 0f, 0f);

            var ai = new GameObject("AI");
            ai.AddComponent<LocomotionSystem>();
            var spawner = ai.AddComponent<SimpleWaveSpawner>();
            var bootstrap = ai.AddComponent<AiSandboxBootstrap>();
            SetRef(bootstrap, "scaling", AssetDatabase.LoadAssetAtPath<EnemyScalingDefinition>(ScalingPath));
            SetRef(bootstrap, "difficulty", AssetDatabase.LoadAssetAtPath<DifficultyDefinition>(DifficultyPath));
            SetRef(bootstrap, "palette", AssetDatabase.LoadAssetAtPath<TelegraphPalette>(TelegraphPaletteCreator.AssetPath));
            SetRef(bootstrap, "telegraphShader", Shader.Find("Eclipse/TelegraphFill"));
            SetRef(bootstrap, "spawner", spawner);
            var so = new SerializedObject(bootstrap);
            so.FindProperty("arenaHalfExtent").floatValue = HalfRoom + 2f;
            so.FindProperty("telegraphPoolSize").intValue = 64;
            so.ApplyModifiedPropertiesWithoutUndo();

            var bot = ai.AddComponent<SoakBot>();
            var runner = ai.AddComponent<SoakTestRunner>();
            SetRef(runner, "bootstrap", bootstrap);
            SetRef(runner, "spawner", spawner);
            SetRef(runner, "bot", bot);
            SetRef(runner, "huskPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Specs[0].PrefabPath));
            SetRef(runner, "marauderPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Specs[1].PrefabPath));

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        }

        private static Material LoadMat(string name)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(GreyboxFolder + "/" + name + ".mat");
            if (m == null) throw new System.InvalidOperationException("Greybox material missing: " + name + ".");
            return m;
        }

        private static void Block(Transform parent, string name, Vector3 pos, Vector3 scale, int layer, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }
    }
}
