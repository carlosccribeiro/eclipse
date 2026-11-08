using Eclipse.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Setup
{
    /// <summary>Idempotent creator of Kael_MovementProfile.asset (08 section 3.1: 6.0 m/s; accel/decel = Infinity = instant).</summary>
    public static class KaelMovementProfileCreator
    {
        public const string AssetPath = "Assets/_Project/Content/Heroes/Kael/Data/Kael_MovementProfile.asset";

        [MenuItem("Eclipse/Setup/Create Kael Movement Profile")]
        public static void Run()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MovementProfile>(AssetPath);
            if (asset == null)
            {
                string dir = System.IO.Path.GetDirectoryName(AssetPath).Replace('\\', '/');
                EnsureFolder(dir);
                asset = ScriptableObject.CreateInstance<MovementProfile>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.baseSpeed = 6.0f;
            asset.acceleration = MovementProfile.Instant;
            asset.deceleration = MovementProfile.Instant;
            asset.speedFloorFraction = 0.4f;
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
