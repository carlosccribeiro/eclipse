using Eclipse.Data.Combat;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Attacks;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Entities;
using Eclipse.Gameplay.Feedback;
using Eclipse.Gameplay.Physics;
using Eclipse.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Setup
{
    /// <summary>
    /// Idempotent builder of the combat content of the sandbox (M05 T10): CombatWorld prefab, combat components and the
    /// procedural Greatsword on Kael_Greybox, and the four TrainingDummy prefabs. Called by SandboxBuilder.Run after the
    /// Kael prefab is (re)created. Run alone: -executeMethod Eclipse.Editor.Setup.CombatSandboxContentBuilder.Run
    /// </summary>
    public static class CombatSandboxContentBuilder
    {
        public const string WorldPrefabPath = "Assets/_Project/Content/Shared/Prefabs/CombatWorld.prefab";
        public const string DummyFolder = "Assets/_Project/Content/Shared/Prefabs";
        public const string WeaponPath = "Assets/_Project/Content/Heroes/Kael/Weapons/greatsword.asset";
        private const string MaterialsFolder = "Assets/_Project/Art/Greybox";
        private const string LitShader = "Universal Render Pipeline/Lit";
        /// <summary>Provisional Attack Speed cap (06 3.5 / 01), set on the hero prefab, never in code.</summary>
        private const float ProvisionalMaxAttackSpeed = 2.0f;

        public readonly struct DummySpec
        {
            public readonly string Name;
            public readonly MassClass Mass;
            public readonly StaggerTier Tier;
            public readonly float Radius, Height;
            public readonly Color Color;

            public DummySpec(string name, MassClass mass, StaggerTier tier, float radius, float height, Color color)
            { Name = name; Mass = mass; Tier = tier; Radius = radius; Height = height; Color = color; }
        }

        // Greybox sizes are visual. Colossal uses the Guardian Stagger bar (400): gap, 06 3.8.2 names no tier for Colossal.
        public static readonly DummySpec[] Dummies =
        {
            new DummySpec("TrainingDummy_Light", MassClass.Light, StaggerTier.None, 0.4f, 1.4f, new Color(0.35f, 0.75f, 0.35f)),
            new DummySpec("TrainingDummy_Medium", MassClass.Medium, StaggerTier.Medium, 0.5f, 1.8f, new Color(0.85f, 0.75f, 0.2f)),
            new DummySpec("TrainingDummy_Heavy", MassClass.Heavy, StaggerTier.Heavy, 0.8f, 2.0f, new Color(0.75f, 0.2f, 0.2f)),
            new DummySpec("TrainingDummy_Colossal", MassClass.Colossal, StaggerTier.Guardian, 1.4f, 3.6f, new Color(0.55f, 0.3f, 0.75f)),
        };

        public static string DummyPath(string name) => DummyFolder + "/" + name + ".prefab";

        [MenuItem("Eclipse/Setup/Build Combat Content")]
        public static void Run()
        {
            BuildPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>Creates the config, world prefab and dummies and augments Kael_Greybox (must already exist).</summary>
        public static void BuildPrefabs()
        {
            var config = CombatRuntimeConfigCreator.Run();
            EnsureFolder(DummyFolder);
            BuildWorldPrefab(config);
            AugmentKael();
            for (int i = 0; i < Dummies.Length; i++) BuildDummy(Dummies[i]);
        }

        // ---------------------------------------------------------------- world

        private static void BuildWorldPrefab(CombatRuntimeConfig config)
        {
            var root = new GameObject("CombatWorld");
            var world = root.AddComponent<CombatWorld>();
            var overlay = root.AddComponent<CombatDebugOverlay>();
            var controls = root.AddComponent<SandboxDebugControls>();
            SetRef(world, "config", config);
            SetRef(controls, "overlay", overlay);
            PrefabUtility.SaveAsPrefabAsset(root, WorldPrefabPath);
            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- Kael

        private static void AugmentKael()
        {
            var root = PrefabUtility.LoadPrefabContents(SandboxBuilder.KaelPrefabPath);
            if (root.GetComponent<AttackController>() != null)
            {
                PrefabUtility.UnloadPrefabContents(root); // already augmented: stay idempotent (delete the prefab to regenerate)
                return;
            }
            try
            {
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WeaponPath);
                var motor = root.GetComponent<PlayerMotor>();
                var dodge = root.GetComponent<DodgeController>();

                var hurtbox = BuildHurtbox(root.transform, 0.4f, 1.8f, true);
                var actor = root.AddComponent<CombatActor>();
                var attack = root.AddComponent<AttackController>();
                var rig = root.AddComponent<PlayerCombatRig>();
                var visual = root.AddComponent<WeaponVisualDriver>();

                SetRef(actor, "hurtbox", hurtbox);
                SetRef(actor, "motor", motor);
                SetRef(actor, "dodge", dodge);
                SetEnum(actor, "kind", (int)ActorKind.Player);
                SetFloat(attack, "maxAttackSpeed", ProvisionalMaxAttackSpeed);
                SetRef(rig, "actor", actor);
                SetRef(rig, "attack", attack);
                SetRef(rig, "motor", motor);
                SetRef(rig, "dodge", dodge);
                SetRef(rig, "weapon", weapon);

                Transform pivot = BuildGreatsword(root.transform);
                SetRef(visual, "attack", attack);
                SetRef(visual, "pivot", pivot);

                PrefabUtility.SaveAsPrefabAsset(root, SandboxBuilder.KaelPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform BuildGreatsword(Transform parent)
        {
            Material metal = Mat("Greybox_Weapon", new Color(0.78f, 0.80f, 0.86f));
            Material grip = Mat("Greybox_WeaponGrip", new Color(0.25f, 0.18f, 0.12f));
            var pivot = new GameObject("WeaponPivot").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(0.45f, 1.05f, 0.15f);
            Part(pivot, "Grip", PrimitiveType.Cube, new Vector3(0f, 0f, 0.1f), new Vector3(0.08f, 0.08f, 0.4f), grip);
            Part(pivot, "Guard", PrimitiveType.Cube, new Vector3(0f, 0f, 0.32f), new Vector3(0.4f, 0.07f, 0.08f), grip);
            Part(pivot, "Blade", PrimitiveType.Cube, new Vector3(0f, 0f, 1.2f), new Vector3(0.16f, 0.05f, 1.7f), metal);
            return pivot;
        }

        private static void Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.layer = PhysicsLayers.Character;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        private static Hurtbox BuildHurtbox(Transform parent, float radius, float height, bool player)
        {
            var go = new GameObject("Hurtbox");
            go.transform.SetParent(parent, false);
            go.AddComponent<CapsuleCollider>();
            var hb = go.AddComponent<Hurtbox>();
            hb.Configure(radius, height, player);
            return hb;
        }

        // ---------------------------------------------------------------- dummies

        private static void BuildDummy(DummySpec spec)
        {
            Material body = Mat("Greybox_" + spec.Name, spec.Color);
            Material nose = Mat("Greybox_DummyNose", new Color(0.95f, 0.95f, 0.95f));
            Material outer = Mat("Greybox_TelegraphOuter", new Color(0.45f, 0.05f, 0.05f));
            Material fill = Mat("Greybox_TelegraphFill", new Color(0.95f, 0.1f, 0.1f));

            var root = new GameObject(spec.Name) { layer = PhysicsLayers.Character };
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

            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Nose";
            Object.DestroyImmediate(face.GetComponent<Collider>());
            face.layer = PhysicsLayers.Character;
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0f, spec.Height * 0.75f, spec.Radius);
            face.transform.localScale = new Vector3(0.2f, 0.2f, 0.4f);
            face.GetComponent<Renderer>().sharedMaterial = nose;

            var telegraphOuter = Telegraph(root.transform, "TelegraphOuter", 0.015f, outer);
            var telegraphFill = Telegraph(root.transform, "TelegraphFill", 0.03f, fill);

            var hurtbox = BuildHurtbox(root.transform, spec.Radius, spec.Height, false);
            var mass = root.AddComponent<MassBodyMarker>();
            mass.MassClass = spec.Mass;
            var flash = root.AddComponent<ImpactFlash>();
            var squash = root.AddComponent<SquashEffect>();
            var actor = root.AddComponent<CombatActor>();
            var attacker = root.AddComponent<TrainingAttacker>();

            SetRenderers(flash, "renderers", visual.GetComponent<Renderer>(), face.GetComponent<Renderer>());
            SetRef(squash, "visual", visual.transform);
            SetRef(actor, "hurtbox", hurtbox);
            SetRef(actor, "flash", flash);
            SetRef(actor, "squash", squash);
            SetEnum(actor, "kind", (int)ActorKind.Dummy);
            SetEnum(actor, "staggerTier", (int)spec.Tier);
            SetBool(actor, "infiniteHealth", true);
            SetRef(attacker, "actor", actor);
            SetBool(attacker, "attackEnabled", false);
            SetRef(attacker, "telegraphFill", telegraphFill);
            SetRef(attacker, "telegraphOuter", telegraphOuter);

            PrefabUtility.SaveAsPrefabAsset(root, DummyPath(spec.Name));
            Object.DestroyImmediate(root);
        }

        private static Transform Telegraph(Transform parent, string name, float y, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            go.transform.localScale = new Vector3(1f, 0.01f, 1f);
            go.GetComponent<Renderer>().sharedMaterial = m;
            go.SetActive(false);
            return go.transform;
        }

        // ---------------------------------------------------------------- scene helpers (used by SandboxBuilder)

        /// <summary>Instantiates the world prefab and the four dummies on a ring around the spawn point.</summary>
        public static void PlaceCombatContent(Transform parent)
        {
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath));
            PlaceDummy(parent, "TrainingDummy_Light", new Vector3(4f, 0f, 3f), false);
            PlaceDummy(parent, "TrainingDummy_Medium", new Vector3(0f, 0f, 6f), true);
            PlaceDummy(parent, "TrainingDummy_Heavy", new Vector3(6f, 0f, 0f), false);
            PlaceDummy(parent, "TrainingDummy_Colossal", new Vector3(-7f, 0f, 0f), false);
        }

        private static void PlaceDummy(Transform parent, string name, Vector3 pos, bool attackEnabled)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DummyPath(name)), parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(-pos.normalized, Vector3.up);
            SetBool(go.GetComponent<TrainingAttacker>(), "attackEnabled", attackEnabled);
        }

        // ---------------------------------------------------------------- utilities

        private static Material Mat(string name, Color color)
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

        private static SerializedProperty Prop(SerializedObject so, Object target, string property)
        {
            SerializedProperty p = so.FindProperty(property);
            if (p == null) throw new System.InvalidOperationException("Property not found: " + property + " on " + target.GetType().Name);
            return p;
        }

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            Prop(so, target, property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string property, int value)
        {
            var so = new SerializedObject(target);
            Prop(so, target, property).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string property, float value)
        {
            var so = new SerializedObject(target);
            Prop(so, target, property).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string property, bool value)
        {
            var so = new SerializedObject(target);
            Prop(so, target, property).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRenderers(Object target, string property, params Renderer[] renderers)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Prop(so, target, property);
            p.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
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
