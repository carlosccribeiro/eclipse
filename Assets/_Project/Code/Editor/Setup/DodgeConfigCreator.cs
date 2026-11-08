using Eclipse.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Setup
{
    /// <summary>Idempotent creator of DodgeConfig_Universal.asset (06 section 3.7, R06).</summary>
    public static class DodgeConfigCreator
    {
        public const string AssetPath = "Assets/_Project/Content/Heroes/Shared/Data/DodgeConfig_Universal.asset";

        [MenuItem("Eclipse/Setup/Create Dodge Config")]
        public static void Run()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DodgeConfig>(AssetPath);
            if (asset == null)
            {
                EnsureFolder(System.IO.Path.GetDirectoryName(AssetPath).Replace('\\', '/'));
                asset = ScriptableObject.CreateInstance<DodgeConfig>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.totalFrames = 18;
            asset.distanceMeters = 4.5f;
            asset.easeOutPower = 2f;
            asset.invulnerableStartFrame = 2;
            asset.invulnerableEndFrame = 13;
            asset.recoveryFrames = 3;
            asset.cancelFromFrame = 15;
            asset.baseCharges = 2;
            asset.maxChargesCap = 3;
            asset.rechargeSeconds = 1.5f;
            asset.perfectDodgeWindowFrames = 6;
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
