using System;
using System.Collections.Generic;
using Eclipse.Core.Definitions;
using Eclipse.Core.Ids;
using UnityEngine;

namespace Eclipse.Data.Definitions
{
    /// <summary>
    /// Global registry asset. Holds the serialized definition list; the lookup structures are
    /// rebuilt in <see cref="OnEnable"/>. Throws <see cref="DuplicateDefinitionException"/> on duplicate ids.
    /// </summary>
    [CreateAssetMenu(menuName = "Eclipse/Definition Registry", fileName = "DefinitionRegistry")]
    public sealed class DefinitionRegistry : ScriptableObject, IDefinitionRegistry
    {
        [SerializeField] private List<Definition> definitions = new List<Definition>();
        [SerializeField] private IdRemapAsset remapAsset;

        private InMemoryDefinitionRegistry _inner;

        public IReadOnlyList<Definition> Definitions => definitions;
        public IdRemapAsset RemapAsset => remapAsset;

        private InMemoryDefinitionRegistry Inner
        {
            get
            {
                if (_inner == null) Rebuild();
                return _inner;
            }
        }

        public int Count => Inner.Count;
        public IdRemapTable Remap => Inner.Remap;

        private void OnEnable() => _inner = null;

        /// <summary>Replaces the definition list and remap asset, then rebuilds. Throws on duplicate ids (state is restored).</summary>
        public void SetDefinitions(IEnumerable<Definition> defs, IdRemapAsset remap = null)
        {
            if (defs == null) throw new ArgumentNullException(nameof(defs));
            var previousDefs = new List<Definition>(definitions);
            IdRemapAsset previousRemap = remapAsset;
            var next = new List<Definition>(defs);
            definitions.Clear();
            definitions.AddRange(next);
            remapAsset = remap;
            try
            {
                Rebuild();
            }
            catch
            {
                definitions.Clear();
                definitions.AddRange(previousDefs);
                remapAsset = previousRemap;
                _inner = null;
                throw;
            }
        }

        /// <summary>Rebuilds lookups from the serialized list. Throws <see cref="DuplicateDefinitionException"/>.</summary>
        public void Rebuild()
        {
            var inner = new InMemoryDefinitionRegistry(remapAsset != null ? remapAsset.ToTable() : null);
            for (int i = 0; i < definitions.Count; i++)
            {
                Definition d = definitions[i];
                if (d == null) continue;
                inner.Register(d);
            }
            _inner = inner;
        }

        public bool Contains(StableId id) => Inner.Contains(id);

        public bool TryGet<T>(StableId id, out T definition) where T : class, IDefinition =>
            Inner.TryGet(id, out definition);

        public T Get<T>(StableId id) where T : class, IDefinition => Inner.Get<T>(id);

        public IReadOnlyList<T> All<T>() where T : class, IDefinition => Inner.All<T>();
    }
}
