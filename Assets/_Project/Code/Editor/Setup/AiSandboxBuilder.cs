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
    /// Idempotent builder of the AI Sandbox (M07 T8): the four enemy prefabs, the test wave asset and the AISandbox scene (floor, walls
    /// on the Wall layer, Kael greybox with basic combat, CombatWorld, isometric camera rig, spawn points and the composition root).
    /// Reuses the prefabs of SandboxBuilder / CombatSandboxContentBuilder without editing them. An existing scene is kept (object
    /// fileIDs are random per creation): delete the .unity to regenerate.
    /// Run: -executeMethod Eclipse.Editor.Setup.AiSandboxBuilder.Run
    /// </summary>
    public static class AiSandboxBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/AISandbox.unity";
        public const string WavePath = Root + "/TestWave_AshenMix.asset";
        private const string ScalingPath = "Assets/_Project/Content/Enemies/Scaling/EnemyScaling.asset";
        private const string DifficultyPath = "Assets/_Project/Content/Shared/Difficulty/normal.asset";
        private const string GreyboxFolder = "Assets/_Project/Art/Greybox";
        private const string DebugKeysType = "Eclipse.AI.Sandbox.AiSandboxDebugKeys, Eclipse.AI.Sandbox";

        // SANDBOX wave: a mixed group, small live cap so the token director is visible (not balance data).
        private static readonly int[] WaveCounts = { 6, 3, 2, 1 };

        [MenuItem("Eclipse/Setup/Build AI Sandbox")]
        public static void Run()
        {
            TelegraphPaletteCreator.Run();
            EnemyPrefabCreator.Run();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SandboxBuilder.KaelPrefabPath) == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(CombatSandboxContentBuilder.WorldPrefabPath) == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(SandboxBuilder.BootstrapPrefabPath) == null)
                SandboxBuilder.Run(); // creates the shared prefabs (and the CombatSandbox scene) on a fresh checkout

            TestWaveDefinition wave = EnsureWave();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) BuildScene(wave);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static TestWaveDefinition EnsureWave()
        {
            var wave = AssetDatabase.LoadAssetAtPath<TestWaveDefinition>(WavePath);
            if (wave != null) return wave;
            wave = ScriptableObject.CreateInstance<TestWaveDefinition>();
            AssetDatabase.CreateAsset(wave, WavePath);
            var so = new SerializedObject(wave);
            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = Specs.Length;
            for (int i = 0; i < Specs.Length; i++)
            {
                SerializedProperty e = entries.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Specs[i].PrefabPath);
                e.FindPropertyRelative("count").intValue = WaveCounts[i];
            }
            so.FindProperty("maxAlive").intValue = 6;
            so.FindProperty("spawnIntervalSeconds").floatValue = 0.75f;
            so.FindProperty("monsterLevel").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wave);
            return wave;
        }

        private static void BuildScene(TestWaveDefinition wave)
        {
            EnsureFolder("Assets/_Project/Scenes/Sandbox");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Material floorM = LoadMat("Greybox_Floor");
            Material wallM = LoadMat("Greybox_Wall");
            Material coverM = LoadMat("Greybox_Cover");
            var arena = new GameObject("Arena").transform;
            Block(arena, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 30f), 0, floorM);
            Block(arena, "Wall_N", new Vector3(0f, 2f, 15.5f), new Vector3(32f, 4f, 1f), PhysicsLayers.Wall, wallM);
            Block(arena, "Wall_S", new Vector3(0f, 2f, -15.5f), new Vector3(32f, 4f, 1f), PhysicsLayers.Wall, wallM);
            Block(arena, "Wall_E", new Vector3(15.5f, 2f, 0f), new Vector3(1f, 4f, 32f), PhysicsLayers.Wall, wallM);
            Block(arena, "Wall_W", new Vector3(-15.5f, 2f, 0f), new Vector3(1f, 4f, 32f), PhysicsLayers.Wall, wallM);
            Block(arena, "LowCover_A", new Vector3(6f, 0.5f, -5f), new Vector3(4f, 1f, 1f), PhysicsLayers.LowCover, coverM);
            Block(arena, "LowCover_B", new Vector3(-6f, 0.5f, 5f), new Vector3(1f, 1f, 4f), PhysicsLayers.LowCover, coverM);

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CombatSandboxContentBuilder.WorldPrefabPath));
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SandboxBuilder.BootstrapPrefabPath));
            var kael = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SandboxBuilder.KaelPrefabPath));
            kael.transform.position = Vector3.zero;

            // Spawn points on the arena edge, away from the player.
            var spawnRoot = new GameObject("SpawnPoints").transform;
            var points = new SpawnPoint[4];
            Vector3[] positions = { new Vector3(11f, 0f, 11f), new Vector3(-11f, 0f, 11f), new Vector3(11f, 0f, -11f), new Vector3(-11f, 0f, -11f) };
            for (int i = 0; i < positions.Length; i++)
            {
                var go = new GameObject("SpawnPoint_" + (i + 1));
                go.transform.SetParent(spawnRoot, false);
                go.transform.position = positions[i];
                points[i] = go.AddComponent<SpawnPoint>();
                points[i].SetRadius(2f);
            }

            var ai = new GameObject("AI");
            ai.AddComponent<LocomotionSystem>();
            var spawner = ai.AddComponent<SimpleWaveSpawner>();
            var bootstrap = ai.AddComponent<AiSandboxBootstrap>();
            SetRefs(spawner, "points", System.Array.ConvertAll(points, p => (Object)p));

            GameObject[] prefabs = new GameObject[Specs.Length];
            for (int i = 0; i < Specs.Length; i++) prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(Specs[i].PrefabPath);
            SetRef(bootstrap, "scaling", AssetDatabase.LoadAssetAtPath<EnemyScalingDefinition>(ScalingPath));
            SetRef(bootstrap, "difficulty", AssetDatabase.LoadAssetAtPath<DifficultyDefinition>(DifficultyPath));
            SetRef(bootstrap, "palette", AssetDatabase.LoadAssetAtPath<TelegraphPalette>(TelegraphPaletteCreator.AssetPath));
            SetRef(bootstrap, "telegraphShader", Shader.Find("Eclipse/TelegraphFill"));
            SetRef(bootstrap, "spawner", spawner);
            SetRef(bootstrap, "wave", wave);
            SetRefs(bootstrap, "enemyPrefabs", System.Array.ConvertAll(prefabs, p => (Object)p));

            System.Type keysType = System.Type.GetType(DebugKeysType);
            if (keysType == null) throw new System.InvalidOperationException("AiSandboxDebugKeys not found (assembly Eclipse.AI.Sandbox).");
            var keys = (Component)ai.AddComponent(keysType);
            SetRef(keys, "bootstrap", bootstrap);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        }

        private static Material LoadMat(string name)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(GreyboxFolder + "/" + name + ".mat");
            if (m == null) throw new System.InvalidOperationException("Greybox material missing: " + name + " (run SandboxBuilder.Run first).");
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
