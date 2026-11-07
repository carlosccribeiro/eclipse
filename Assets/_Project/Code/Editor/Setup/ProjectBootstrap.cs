using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Eclipse.Editor.Setup
{
    /// <summary>
    /// Idempotent project bootstrap (M00 T5): URP Forward+ profiles, player settings,
    /// fixed timestep and folder tree. Run via -executeMethod Eclipse.Editor.Setup.ProjectBootstrap.Run.
    /// </summary>
    public static class ProjectBootstrap
    {
        const string UrpDir = "Assets/_Project/Settings/URP";
        const string LowName = "PC_Low";
        const string HighName = "PC_High";

        static readonly string[] Folders =
        {
            "Assets/_Project/Code/Core", "Assets/_Project/Code/Data", "Assets/_Project/Code/Gameplay",
            "Assets/_Project/Code/AI", "Assets/_Project/Code/Run", "Assets/_Project/Code/Meta",
            "Assets/_Project/Code/Save", "Assets/_Project/Code/UI", "Assets/_Project/Code/Audio",
            "Assets/_Project/Code/Platform", "Assets/_Project/Code/Editor",
            "Assets/_Project/Code/Tests/EditMode", "Assets/_Project/Code/Tests/PlayMode",
            "Assets/_Project/Content/Heroes", "Assets/_Project/Content/Items", "Assets/_Project/Content/Modifiers",
            "Assets/_Project/Content/Affinities", "Assets/_Project/Content/Enemies", "Assets/_Project/Content/Bosses",
            "Assets/_Project/Content/Regions", "Assets/_Project/Content/Sanctuary",
            "Assets/_Project/Art", "Assets/_Project/Audio", "Assets/_Project/UI",
            "Assets/_Project/Settings", "Assets/_Project/Scenes", "Assets/Plugins",
            "DataSource",
        };

        public static void Run()
        {
            CreateFolders();
            var low = EnsureUrpProfile(LowName);
            var high = EnsureUrpProfile(HighName);
            ConfigureGraphicsAndQuality(low, high);
            ConfigurePlayer();
            ConfigureInputHandler();
            ConfigureFixedTimestep();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectBootstrap] Done.");
        }

        static void CreateFolders()
        {
            foreach (var folder in Folders)
            {
                Directory.CreateDirectory(folder);
                var keep = Path.Combine(folder, ".gitkeep");
                if (!File.Exists(keep)) File.WriteAllText(keep, string.Empty);
            }
            Directory.CreateDirectory(UrpDir);
            AssetDatabase.Refresh();
        }

        static UniversalRenderPipelineAsset EnsureUrpProfile(string name)
        {
            var assetPath = $"{UrpDir}/{name}.asset";
            var rendererPath = $"{UrpDir}/{name}_Renderer.asset";

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            renderer.renderingMode = RenderingMode.ForwardPlus;
            EditorUtility.SetDirty(renderer);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, assetPath);
            }
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            return pipeline;
        }

        static void ConfigureGraphicsAndQuality(RenderPipelineAsset low, RenderPipelineAsset high)
        {
            GraphicsSettings.defaultRenderPipeline = high;

            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            var so = new SerializedObject(qs);
            var levels = so.FindProperty("m_QualitySettings");
            while (levels.arraySize > 2) levels.DeleteArrayElementAtIndex(levels.arraySize - 1);
            while (levels.arraySize < 2) levels.InsertArrayElementAtIndex(levels.arraySize - 1 < 0 ? 0 : levels.arraySize - 1);

            Apply(levels.GetArrayElementAtIndex(0), LowName, low);
            Apply(levels.GetArrayElementAtIndex(1), HighName, high);

            so.FindProperty("m_CurrentQuality").intValue = 1;
            var defaults = so.FindProperty("m_PerPlatformDefaultQuality");
            if (defaults != null)
            {
                for (int i = 0; i < defaults.arraySize; i++)
                    defaults.GetArrayElementAtIndex(i).FindPropertyRelative("second").intValue = 1;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        static void Apply(SerializedProperty level, string name, RenderPipelineAsset rp)
        {
            level.FindPropertyRelative("name").stringValue = name;
            level.FindPropertyRelative("customRenderPipeline").objectReferenceValue = rp;
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "PROJECT ECLIPSE";
            PlayerSettings.productName = "PROJECT ECLIPSE";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            var standalone = NamedBuildTarget.Standalone;
            PlayerSettings.SetScriptingBackend(standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(standalone, ApiCompatibilityLevel.NET_Standard);
        }

        static void ConfigureInputHandler()
        {
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
            var so = new SerializedObject(ps);
            so.FindProperty("activeInputHandler").intValue = 1; // Input System Package (New)
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureFixedTimestep()
        {
            var tm = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset")[0];
            var so = new SerializedObject(tm);
            // Unity 6.3 stores the fixed timestep as a rational (count / rate), not a float.
            // Rate 141120000 Hz is Unity's default tick; 141120000 / 60 = 2352000 ticks = 1/60 s.
            var ts = so.FindProperty("Fixed Timestep");
            var rate = ts.FindPropertyRelative("m_Rate");
            rate.FindPropertyRelative("m_Numerator").longValue = 141120000;
            rate.FindPropertyRelative("m_Denominator").longValue = 1;
            ts.FindPropertyRelative("m_Count").longValue = 141120000L / 60L;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }
}
