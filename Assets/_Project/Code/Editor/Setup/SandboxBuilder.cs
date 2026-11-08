using Eclipse.Gameplay.Cameras;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Physics;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eclipse.Editor.Setup
{
    /// <summary>
    /// Idempotent builder of the Combat Sandbox greybox: materials, Kael_Greybox and GameplayBootstrap prefabs and
    /// the CombatSandbox scene (M04 T7). Run: -executeMethod Eclipse.Editor.Setup.SandboxBuilder.Run
    /// </summary>
    public static class SandboxBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/CombatSandbox.unity";
        public const string KaelPrefabPath = "Assets/_Project/Content/Heroes/Kael/Prefabs/Kael_Greybox.prefab";
        public const string BootstrapPrefabPath = "Assets/_Project/Content/Shared/Prefabs/GameplayBootstrap.prefab";
        private const string MaterialsFolder = "Assets/_Project/Art/Greybox";
        private const string ProfilePath = "Assets/_Project/Content/Heroes/Kael/Data/Kael_MovementProfile.asset";
        private const string DodgePath = "Assets/_Project/Content/Heroes/Shared/Data/DodgeConfig_Universal.asset";
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string DitherShader = "Eclipse/OcclusionDither";

        [MenuItem("Eclipse/Setup/Build Combat Sandbox")]
        public static void Run()
        {
            IsoCameraSettingsCreator.Run();
            DodgeConfigCreator.Run();
            KaelMovementProfileCreator.Run();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material floor = Mat("Greybox_Floor", LitShader, new Color(0.30f, 0.30f, 0.32f));
            Material wall = Mat("Greybox_Wall", DitherShader, new Color(0.50f, 0.50f, 0.54f));
            Material cover = Mat("Greybox_Cover", DitherShader, new Color(0.62f, 0.50f, 0.34f));
            Material pillar = Mat("Greybox_Pillar", DitherShader, new Color(0.45f, 0.55f, 0.65f));
            Material kael = Mat("Greybox_Kael", LitShader, new Color(0.25f, 0.45f, 0.85f));
            Material nose = Mat("Greybox_KaelNose", LitShader, new Color(0.95f, 0.95f, 0.95f));
            Material heavy = Mat("Greybox_DummyHeavy", LitShader, new Color(0.75f, 0.2f, 0.2f));
            Material medium = Mat("Greybox_DummyMedium", LitShader, new Color(0.85f, 0.75f, 0.2f));

            // Prefab internals get random fileIDs on every rebuild: keep the existing Kael prefab once it carries the combat
            // components (M05 T10) so a second Run changes no file; delete the .prefab to regenerate.
            if (!KaelPrefabHasCombat()) BuildKaelPrefab(kael, nose);
            BuildBootstrapPrefab();
            CombatSandboxContentBuilder.BuildPrefabs(); // M05 T10: combat components on Kael, CombatWorld and TrainingDummy prefabs
            // Scene object fileIDs are random per creation: rebuild only when missing or when it lacks the combat content
            // (M05 T10); otherwise delete the .unity to regenerate. Saving over the same path keeps the .meta (GUID).
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null || !SceneHasCombatContent())
                BuildScene(floor, wall, cover, pillar, heavy, medium);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool KaelPrefabHasCombat()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KaelPrefabPath);
            return prefab != null && prefab.GetComponent<Eclipse.Gameplay.Combat.Attacks.AttackController>() != null;
        }

        private static bool SceneHasCombatContent() =>
            System.Array.IndexOf(AssetDatabase.GetDependencies(ScenePath, false), CombatSandboxContentBuilder.WorldPrefabPath) >= 0;

        private static void BuildKaelPrefab(Material body, Material noseMat)
        {
            EnsureFolder("Assets/_Project/Content/Heroes/Kael/Prefabs");
            var root = new GameObject("Kael_Greybox") { layer = PhysicsLayers.Character };
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.4f;
            col.height = 1.8f;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Body";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            visual.GetComponent<Renderer>().sharedMaterial = body;

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(root.transform, false);
            nose.transform.localPosition = new Vector3(0f, 1.3f, 0.45f);
            nose.transform.localScale = new Vector3(0.2f, 0.2f, 0.4f);
            nose.GetComponent<Renderer>().sharedMaterial = noseMat;
            SetLayerRecursive(root, PhysicsLayers.Character);

            var motor = root.AddComponent<PlayerMotor>();
            var dodge = root.AddComponent<DodgeController>();
            var boot = root.AddComponent<PlayerBootstrap>();
            var profile = AssetDatabase.LoadAssetAtPath<MovementProfile>(ProfilePath);
            var dodgeCfg = AssetDatabase.LoadAssetAtPath<DodgeConfig>(DodgePath);
            SetRef(motor, "profile", profile);
            SetRef(dodge, "config", dodgeCfg);
            SetRef(dodge, "motor", motor);
            SetRef(boot, "motor", motor);
            SetRef(boot, "dodge", dodge);
            SetRef(boot, "movementProfile", profile);
            SetRef(boot, "dodgeConfig", dodgeCfg);

            PrefabUtility.SaveAsPrefabAsset(root, KaelPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildBootstrapPrefab()
        {
            EnsureFolder("Assets/_Project/Content/Shared/Prefabs");
            var root = new GameObject("GameplayBootstrap");
            var services = root.AddComponent<GameplayServices>();
            var rig = root.AddComponent<IsoCameraRig>();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CinemachineBrain>();
            var fader = camGo.AddComponent<OcclusionFader>();

            var vcamGo = new GameObject("IsoVirtualCamera");
            vcamGo.transform.SetParent(root.transform, false);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();

            var settings = AssetDatabase.LoadAssetAtPath<IsoCameraSettings>(IsoCameraSettingsCreator.AssetPath);
            SetRef(rig, "Settings", settings);
            SetRef(rig, "VirtualCamera", vcam);
            SetRef(fader, "Settings", settings);
            SetRef(services, "MainCamera", cam);
            SetRef(services, "CameraRig", rig);
            SetRef(services, "OcclusionFader", fader);

            PrefabUtility.SaveAsPrefabAsset(root, BootstrapPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildScene(Material floorM, Material wallM, Material coverM, Material pillarM, Material heavyM, Material mediumM)
        {
            EnsureFolder("Assets/_Project/Scenes/Sandbox");

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var arena = new GameObject("Arena").transform;
            Block(arena, "Floor", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 30f), 0, floorM);
            Block(arena, "Wall_N", PrimitiveType.Cube, new Vector3(0f, 2f, 15.5f), new Vector3(32f, 4f, 1f), PhysicsLayers.Wall, wallM);
            Block(arena, "Wall_S", PrimitiveType.Cube, new Vector3(0f, 2f, -15.5f), new Vector3(32f, 4f, 1f), PhysicsLayers.Wall, wallM);
            Block(arena, "Wall_E", PrimitiveType.Cube, new Vector3(15.5f, 2f, 0f), new Vector3(1f, 4f, 32f), PhysicsLayers.Wall, wallM);
            Block(arena, "Wall_W", PrimitiveType.Cube, new Vector3(-15.5f, 2f, 0f), new Vector3(1f, 4f, 32f), PhysicsLayers.Wall, wallM);
            Block(arena, "LowCover_A", PrimitiveType.Cube, new Vector3(8f, 0.5f, -6f), new Vector3(4f, 1f, 1f), PhysicsLayers.LowCover, coverM);
            Block(arena, "LowCover_B", PrimitiveType.Cube, new Vector3(-8f, 0.5f, 7f), new Vector3(1f, 1f, 4f), PhysicsLayers.LowCover, coverM);
            Block(arena, "LowCover_C", PrimitiveType.Cube, new Vector3(4f, 0.5f, 9f), new Vector3(3f, 1f, 1f), PhysicsLayers.LowCover, coverM);
            // Pillar sits on the camera line to the spawn point (yaw 45, pitch 50) to exercise the occlusion dither.
            Block(arena, "Pillar_Environment", PrimitiveType.Cylinder, new Vector3(-2f, 4f, -2f), new Vector3(2f, 4f, 2f), PhysicsLayers.Environment, pillarM);
            // M05 T10: the M04 primitive dummies are replaced by the TrainingDummy prefabs and the CombatWorld.
            CombatSandboxContentBuilder.PlaceCombatContent(arena);

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BootstrapPrefabPath));
            var kael = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(KaelPrefabPath));
            kael.transform.position = Vector3.zero;

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        }

        private static GameObject Block(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, int layer, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        private static void Dummy(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, MassClass mass, Material m)
        {
            var go = Block(parent, name, type, pos, scale, PhysicsLayers.Character, m);
            go.AddComponent<MassBodyMarker>().MassClass = mass;
        }

        private static Material Mat(string name, string shaderName, Color color)
        {
            EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + name + ".mat";
            Shader shader = Shader.Find(shaderName);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.shader != shader) m.shader = shader;
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p == null) throw new System.InvalidOperationException("Property not found: " + property + " on " + target.GetType().Name);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
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
