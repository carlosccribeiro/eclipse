using Eclipse.AI.Enemies;
using Eclipse.Data.Enemies;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Feedback;
using Eclipse.Gameplay.Physics;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Setup
{
    /// <summary>
    /// Idempotent creator of the four greybox enemy prefabs of the Ashen Wastes (M07 T7): capsule silhouettes per archetype with the
    /// components <see cref="EnemyController"/> needs (CombatActor as Dummy kind, Hurtbox, ImpactFlash, SquashEffect). No
    /// <c>MassBodyMarker</c> (the controller is the IMassBody). An existing prefab that already points at the right definition is
    /// kept, so a second run changes no file; delete the .prefab to regenerate.
    /// Run: -executeMethod Eclipse.Editor.Setup.EnemyPrefabCreator.Run
    /// </summary>
    public static class EnemyPrefabCreator
    {
        public const string Root = "Assets/_Project/Content/Enemies/AshenWastes";
        private const string MaterialsFolder = "Assets/_Project/Art/Greybox";
        private const string LitShader = "Universal Render Pipeline/Lit";

        public readonly struct Spec
        {
            public readonly string Folder, DefinitionFile;
            public readonly float Radius, Height;
            public readonly Color Color;
            /// <summary>Front plate width/height/thickness; zero width = none (Kiln Brute only, 09 section 5.1).</summary>
            public readonly Vector3 Plate;

            public Spec(string folder, string definitionFile, float radius, float height, Color color, Vector3 plate)
            { Folder = folder; DefinitionFile = definitionFile; Radius = radius; Height = height; Color = color; Plate = plate; }

            public string PrefabPath => Root + "/" + Folder + "/" + Folder + ".prefab";
            public string DefinitionPath => Root + "/Enemies/" + DefinitionFile + ".asset";
        }

        // Greybox sizes are visual only (D4); no orange here, that hue is reserved for Ember telegraph/VFX language.
        public static readonly Spec[] Specs =
        {
            new Spec("CinderHusk", "cinder_husk", 0.35f, 1.3f, new Color(0.45f, 0.30f, 0.22f), Vector3.zero),
            new Spec("AshbornMarauder", "marauder", 0.45f, 1.8f, new Color(0.45f, 0.52f, 0.62f), Vector3.zero),
            new Spec("SlagHurler", "slag_hurler", 0.45f, 1.7f, new Color(0.75f, 0.70f, 0.25f), Vector3.zero),
            new Spec("KilnBrute", "kiln_brute", 0.85f, 2.3f, new Color(0.45f, 0.12f, 0.12f), new Vector3(1.5f, 1.6f, 0.2f)),
        };

        public static string PrefabPath(string folder) => Root + "/" + folder + "/" + folder + ".prefab";

        [MenuItem("Eclipse/Setup/Build Enemy Prefabs")]
        public static void Run()
        {
            for (int i = 0; i < Specs.Length; i++) Build(Specs[i]);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Build(Spec spec)
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(spec.DefinitionPath);
            if (definition == null)
                throw new System.InvalidOperationException("EnemyDefinition missing (run the data pipeline first): " + spec.DefinitionPath);

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
            if (existing != null)
            {
                var controller = existing.GetComponent<EnemyController>();
                if (controller != null && controller.Definition == definition) return;
            }

            EnsureFolder(Root + "/" + spec.Folder);
            Material body = Mat("Greybox_Enemy_" + spec.Folder, spec.Color);
            Material nose = Mat("Greybox_EnemyNose", new Color(0.95f, 0.95f, 0.95f));
            Material plateMat = Mat("Greybox_EnemyPlate", new Color(0.22f, 0.22f, 0.25f));

            var root = new GameObject(spec.Folder) { layer = PhysicsLayers.Character };
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, spec.Height * 0.5f, 0f);
            col.radius = spec.Radius;
            col.height = spec.Height;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Body";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.layer = PhysicsLayers.Character;
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, spec.Height * 0.5f, 0f);
            visual.transform.localScale = new Vector3(spec.Radius * 2f, spec.Height * 0.5f, spec.Radius * 2f);
            visual.GetComponent<Renderer>().sharedMaterial = body;

            var face = Part(root.transform, "Nose", new Vector3(0f, spec.Height * 0.75f, spec.Radius), new Vector3(0.2f, 0.2f, 0.4f), nose);
            if (spec.Plate.x > 0f)
                Part(root.transform, "FrontPlate", new Vector3(0f, spec.Height * 0.5f, spec.Radius + spec.Plate.z * 0.5f), spec.Plate, plateMat);

            var hgo = new GameObject("Hurtbox");
            hgo.transform.SetParent(root.transform, false);
            hgo.AddComponent<CapsuleCollider>();
            var hurtbox = hgo.AddComponent<Hurtbox>();
            hurtbox.Configure(spec.Radius, spec.Height, false);

            var flash = root.AddComponent<ImpactFlash>();
            var squash = root.AddComponent<SquashEffect>();
            var actor = root.AddComponent<CombatActor>();
            var enemy = root.AddComponent<EnemyController>();

            SetRenderers(flash, "renderers", visual.GetComponent<Renderer>(), face.GetComponent<Renderer>());
            SetRef(squash, "visual", visual.transform);
            SetRef(actor, "hurtbox", hurtbox);
            SetRef(actor, "flash", flash);
            SetRef(actor, "squash", squash);
            SetInt(actor, "kind", (int)ActorKind.Dummy);
            SetRef(enemy, "definition", definition);
            SetRef(enemy, "actor", actor);
            SetRef(enemy, "hurtbox", hurtbox);

            PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            Object.DestroyImmediate(root);
        }

        private static GameObject Part(Transform parent, string name, Vector3 pos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.layer = PhysicsLayers.Character;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        // ---------------------------------------------------------------- shared editor helpers (also used by AiSandboxBuilder)

        internal static Material Mat(string name, Color color)
        {
            EnsureFolder(MaterialsFolder);
            string path = MaterialsFolder + "/" + name + ".mat";
            Shader shader = Shader.Find(LitShader);
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

        internal static SerializedProperty Prop(SerializedObject so, Object target, string property)
        {
            SerializedProperty p = so.FindProperty(property);
            if (p == null) throw new System.InvalidOperationException("Property not found: " + property + " on " + target.GetType().Name);
            return p;
        }

        internal static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            Prop(so, target, property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetInt(Object target, string property, int value)
        {
            var so = new SerializedObject(target);
            Prop(so, target, property).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetFloat(Object target, string property, float value)
        {
            var so = new SerializedObject(target);
            Prop(so, target, property).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetRefs(Object target, string property, params Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Prop(so, target, property);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRenderers(Object target, string property, params Renderer[] renderers) =>
            SetRefs(target, property, System.Array.ConvertAll(renderers, r => (Object)r));

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
