using System.Collections.Generic;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Affinities
{
    /// <summary>
    /// Affinity (Ember..Ruin): palette, tag, primary status and icon (11 section 3.4.2; palette in 07 section 10.1).
    /// </summary>
    [CreateAssetMenu(fileName = "Affinity", menuName = "Eclipse/Definitions/Affinity")]
    public sealed class AffinityDefinition : Definition, ITaggedDefinition
    {
        [SerializeField] private Tag affinityTag;
        [SerializeField] private Color primary;
        [SerializeField] private Color secondary;
        [SerializeField] private string primaryStatusId;
        [SerializeField] private Sprite icon;
        [SerializeField] private string shapeLanguage;

        public Tag AffinityTag => affinityTag;
        public Color Primary => primary;
        public Color Secondary => secondary;

        /// <summary>Textual id of the primary status (e.g. "Burning"); status definitions arrive in M11.</summary>
        public string PrimaryStatusId => primaryStatusId;

        public Sprite Icon => icon;
        public string ShapeLanguage => shapeLanguage;

        public IEnumerable<string> TagNames
        {
            get { yield return affinityTag.ToString(); }
        }

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(Tag tag, Color primaryColor, Color secondaryColor, string statusId, Sprite iconSprite, string shape)
        {
            affinityTag = tag;
            primary = primaryColor;
            secondary = secondaryColor;
            primaryStatusId = statusId;
            icon = iconSprite;
            shapeLanguage = shape;
        }
#endif
    }
}
