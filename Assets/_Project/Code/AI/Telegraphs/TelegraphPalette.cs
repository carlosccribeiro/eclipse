using System;
using UnityEngine;

namespace Eclipse.AI.Telegraphs
{
    /// <summary>Telegraph colors by kind (06 section 3.10). Created by TelegraphPaletteCreator; orange is forbidden.</summary>
    public sealed class TelegraphPalette : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TelegraphVisual visual;
            public Color fill;
            public Color border;
        }

        [SerializeField] private Entry[] entries = new Entry[0];

        public Entry[] Entries => entries;

        public bool TryGet(TelegraphVisual visual, out Entry entry)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].visual == visual) { entry = entries[i]; return true; }
            }
            entry = default;
            return false;
        }

        public Color Fill(TelegraphVisual visual) => TryGet(visual, out Entry e) ? e.fill : Color.magenta;
        public Color Border(TelegraphVisual visual) => TryGet(visual, out Entry e) ? e.border : Color.magenta;

        /// <summary>
        /// True for orange hues (reserved to Ember, 06 section 3.10). Hue 10 to 48 degrees with real saturation and brightness;
        /// saturated yellow (about 55 degrees, the Annihilation dashed border) and the pale white-gold are not orange.
        /// </summary>
        public static bool IsOrange(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            float degrees = h * 360f;
            return degrees >= 10f && degrees <= 48f && s >= 0.5f && v >= 0.3f;
        }

        /// <summary>Editor/creator only.</summary>
        public void EditorSetEntries(Entry[] value) => entries = value;
    }
}
