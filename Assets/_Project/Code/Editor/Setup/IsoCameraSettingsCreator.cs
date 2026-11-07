using Eclipse.Gameplay.Cameras;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Setup
{
    /// <summary>Idempotent creator of Settings/Camera/IsoCamera_Default.asset (06 section 3.11).</summary>
    public static class IsoCameraSettingsCreator
    {
        public const string AssetPath = "Assets/_Project/Settings/Camera/IsoCamera_Default.asset";

        [MenuItem("Eclipse/Setup/Create Iso Camera Settings")]
        public static void Run()
        {
            var asset = AssetDatabase.LoadAssetAtPath<IsoCameraSettings>(AssetPath);
            if (asset == null)
            {
                EnsureFolder("Assets/_Project/Settings/Camera");
                asset = ScriptableObject.CreateInstance<IsoCameraSettings>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.verticalFovDegrees = 30f;
            asset.pitchDegrees = 50f;
            asset.yawDegrees = 45f;
            asset.distance = 22f;
            asset.minDistance = 18f;
            asset.maxDistance = 26f;
            asset.lookAheadFraction = 0.15f;
            asset.lookAheadMaxMeters = 3f;
            asset.traumaDecayPerSecond = 1.5f;
            asset.defaultShakeIntensity = 0.7f;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
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
