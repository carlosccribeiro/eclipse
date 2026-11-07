using Eclipse.Core.Definitions;
using Eclipse.Core.Ids;
using UnityEngine;
using UnityEngine.Localization;

namespace Eclipse.Data.Definitions
{
    /// <summary>
    /// Base of every static game definition (11 section 3.4.1). Identified by an immutable
    /// <see cref="StableId"/>; renames go through <see cref="IdRemapAsset"/>.
    /// </summary>
    public abstract class Definition : ScriptableObject, IDefinition
    {
        [SerializeField] private string stableId;
        [SerializeField] private LocalizedString displayName;
        [SerializeField] private LocalizedString description;
        [SerializeField, HideInInspector] private string importedHash;
        [SerializeField] private string sourceDoc;
        [SerializeField] private string nameEn;

        private StableId _id;
        private bool _idCached;

        /// <summary>Parsed id (cached). Empty if the serialized slug is blank or invalid.</summary>
        public StableId Id
        {
            get
            {
                if (!_idCached)
                {
                    StableId.TryParse(stableId, out _id);
                    _idCached = true;
                }
                return _id;
            }
        }

        /// <summary>Raw serialized slug.</summary>
        public string StableIdString => stableId;

        public LocalizedString DisplayName => displayName;
        public LocalizedString Description => description;

        /// <summary>Hash of the source CSV row that produced this asset; empty for hand-made assets.</summary>
        public string ImportedHash
        {
            get => importedHash;
            set => importedHash = value;
        }

        /// <summary>Owner doc of this definition (e.g. "07-resonance"); used by the reserved-name rule.</summary>
        public string SourceDoc => sourceDoc;

        /// <summary>English display name copied from the CSV name_en column; used by validation only (runtime text comes from localization).</summary>
        public string NameEn => nameEn;

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSetSourceDoc(string value) => sourceDoc = value;

        /// <summary>Editor/importer only.</summary>
        public void EditorSetNameEn(string value) => nameEn = value;

        /// <summary>Editor/importer only: sets the serialized id and invalidates the cache.</summary>
        public void EditorSetStableId(string value)
        {
            stableId = value;
            _idCached = false;
        }
#endif

        protected virtual void OnEnable() => _idCached = false;

        protected virtual void OnValidate() => _idCached = false;
    }
}
