using System.Collections.Generic;
using Eclipse.Core.Items;
using Eclipse.Core.Tags;
using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Data.Items
{
    /// <summary>
    /// Named Legendary, set piece or Unique (02 sections 3.11, 3.12; 11 section 3.4.2). Source: items_named.csv.
    /// Modifier ids are 03 codes kept as text (<c>KAE-CS-01</c>); they are not StableIds.
    /// </summary>
    public sealed class NamedItemDefinition : Definition
    {
        [SerializeField] private string catalogNo;
        [SerializeField] private ItemKind itemKind;
        [SerializeField] private string baseId;
        [SerializeField] private string heroId;
        [SerializeField] private string effectId;
        [SerializeField] private List<NamedAffixEntry> fixedAffixes = new List<NamedAffixEntry>();
        [SerializeField] private List<NamedAffixEntry> openOptions = new List<NamedAffixEntry>();
        [SerializeField] private List<string> modifierIds = new List<string>();
        [SerializeField] private ulong affinityBits;
        [SerializeField] private string setId;
        [SerializeField] private UniqueSourceKind uniqueSourceKind;
        [SerializeField] private string uniqueSourceRef;
        [SerializeField] private int minDropILvl;
        [SerializeField] private string subtitlePt;
        [SerializeField] private string subtitleEn;
        [SerializeField] private string lorePt;
        [SerializeField] private string loreEn;
        [SerializeField] private string build;
        [SerializeField] private float effectMoreDeclared;

        public string CatalogNo => catalogNo;
        public ItemKind ItemKind => itemKind;
        public string BaseId => baseId;
        public string HeroId => heroId;
        public string EffectId => effectId;
        public IReadOnlyList<NamedAffixEntry> FixedAffixes => fixedAffixes;
        public IReadOnlyList<NamedAffixEntry> OpenOptions => openOptions;
        public IReadOnlyList<string> ModifierIds => modifierIds;
        public TagMask Affinity => TagMask.FromBits(affinityBits);
        public string SetId => setId;
        public UniqueSourceKind UniqueSourceKind => uniqueSourceKind;
        public string UniqueSourceRef => uniqueSourceRef;
        public int MinDropILvl => minDropILvl;
        public string SubtitlePt => subtitlePt;
        public string SubtitleEn => subtitleEn;
        public string LorePt => lorePt;
        public string LoreEn => loreEn;
        public string Build => build;
        /// <summary>Declared "more" multiplier of the item effect in percent (R03), 0 when none.</summary>
        public float EffectMoreDeclared => effectMoreDeclared;

#if UNITY_EDITOR
        /// <summary>Editor/importer only: core structure.</summary>
        public void EditorSet(string catalog, ItemKind kind, string baseDefinitionId, string hero, string effect,
            List<NamedAffixEntry> fixedList, List<NamedAffixEntry> openList, List<string> modifiers, TagMask affinity,
            string set, UniqueSourceKind sourceKind, string sourceRef, int minILvl)
        {
            catalogNo = catalog;
            itemKind = kind;
            baseId = baseDefinitionId;
            heroId = hero;
            effectId = effect;
            fixedAffixes = fixedList;
            openOptions = openList;
            modifierIds = modifiers;
            affinityBits = affinity.Bits;
            setId = set;
            uniqueSourceKind = sourceKind;
            uniqueSourceRef = sourceRef;
            minDropILvl = minILvl;
        }

        /// <summary>Editor/importer only: optional presentation columns.</summary>
        public void EditorSetExtras(string subPt, string subEn, string lorePtText, string loreEnText, string buildText, float effectMore)
        {
            subtitlePt = subPt;
            subtitleEn = subEn;
            lorePt = lorePtText;
            loreEn = loreEnText;
            build = buildText;
            effectMoreDeclared = effectMore;
        }
#endif
    }
}
