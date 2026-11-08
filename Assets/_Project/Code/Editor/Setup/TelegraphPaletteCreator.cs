using Eclipse.AI.Telegraphs;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Setup
{
    /// <summary>Idempotent creator of TelegraphPalette.asset (06 section 3.10). No orange entry: that hue is Ember only.</summary>
    public static class TelegraphPaletteCreator
    {
        public const string AssetPath = "Assets/_Project/Content/Shared/Telegraphs/TelegraphPalette.asset";

        [MenuItem("Eclipse/Setup/Create Telegraph Palette")]
        public static void Run()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TelegraphPalette>(AssetPath);
            if (asset == null)
            {
                EnsureFolder(System.IO.Path.GetDirectoryName(AssetPath).Replace('\\', '/'));
                asset = ScriptableObject.CreateInstance<TelegraphPalette>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.EditorSetEntries(new[]
            {
                E(TelegraphVisual.Red, new Color(0.85f, 0.10f, 0.10f, 0.60f), new Color(1.00f, 0.20f, 0.20f, 1f)),
                E(TelegraphVisual.Purple, new Color(0.55f, 0.20f, 0.85f, 0.60f), new Color(0.75f, 0.45f, 1.00f, 1f)),
                E(TelegraphVisual.WhiteGold, new Color(1.00f, 0.93f, 0.62f, 0.65f), new Color(1.00f, 1.00f, 0.90f, 1f)),
                E(TelegraphVisual.CyanRing, new Color(0.10f, 0.90f, 0.95f, 0.35f), new Color(0.10f, 0.90f, 0.95f, 1f)),
                E(TelegraphVisual.Annihilation, new Color(0.85f, 0.10f, 0.10f, 0.65f), new Color(1.00f, 0.92f, 0.10f, 1f)),
                E(TelegraphVisual.UnstoppableOutline, new Color(1f, 1f, 1f, 0.20f), new Color(1f, 1f, 1f, 1f)),
                E(TelegraphVisual.SniperLine, new Color(0.95f, 0.10f, 0.10f, 0.70f), new Color(1.00f, 0.30f, 0.30f, 1f)),
                E(TelegraphVisual.Grab, new Color(0.70f, 0.75f, 0.85f, 0.50f), new Color(0.85f, 0.90f, 1.00f, 1f)),
                E(TelegraphVisual.EdgeIndicator, new Color(0.90f, 0.15f, 0.15f, 0.80f), new Color(1.00f, 0.35f, 0.35f, 1f)),
            });
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static TelegraphPalette.Entry E(TelegraphVisual visual, Color fill, Color border)
        {
            return new TelegraphPalette.Entry { visual = visual, fill = fill, border = border };
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
