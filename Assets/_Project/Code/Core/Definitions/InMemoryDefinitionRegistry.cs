using System;
using System.Collections.Generic;
using Eclipse.Core.Ids;

namespace Eclipse.Core.Definitions
{
    /// <summary>
    /// In-memory <see cref="IDefinitionRegistry"/> for tests and tools. Not thread-safe.
    /// <see cref="All{T}"/> lists are cached per type and rebuilt lazily after a registration;
    /// a returned list is a snapshot that is not mutated later.
    /// </summary>
    public sealed class InMemoryDefinitionRegistry : IDefinitionRegistry
    {
        private readonly Dictionary<StableId, IDefinition> _byId = new Dictionary<StableId, IDefinition>();
        private readonly List<IDefinition> _ordered = new List<IDefinition>();
        private readonly Dictionary<Type, object> _allCache = new Dictionary<Type, object>();

        public InMemoryDefinitionRegistry(IdRemapTable remap = null)
        {
            Remap = remap ?? new IdRemapTable();
        }

        public int Count => _ordered.Count;

        public IdRemapTable Remap { get; }

        /// <summary>
        /// Registers a definition. Throws ArgumentNullException (null), ArgumentException (empty id, or id that is
        /// the source of a remap entry, since an obsolete id cannot coexist with its remapping) and
        /// <see cref="DuplicateDefinitionException"/> (id already registered).
        /// </summary>
        public void Register(IDefinition def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            StableId id = def.Id;
            if (id.IsEmpty) throw new ArgumentException("Definition id is empty", nameof(def));
            if (Remap.Contains(id))
                throw new ArgumentException("Id '" + id + "' is obsolete (source of a remap entry)", nameof(def));
            if (_byId.ContainsKey(id)) throw new DuplicateDefinitionException(id);

            _byId.Add(id, def);
            _ordered.Add(def);
            _allCache.Clear();
        }

        /// <summary>Registers each definition in order; stops at the first failure (earlier ones stay registered).</summary>
        public void RegisterRange(IEnumerable<IDefinition> defs)
        {
            if (defs == null) throw new ArgumentNullException(nameof(defs));
            foreach (IDefinition d in defs) Register(d);
        }

        public bool Contains(StableId id) => _byId.ContainsKey(Remap.Resolve(id));

        public bool TryGet<T>(StableId id, out T definition) where T : class, IDefinition
        {
            if (_byId.TryGetValue(Remap.Resolve(id), out IDefinition found) && found is T typed)
            {
                definition = typed;
                return true;
            }
            definition = null;
            return false;
        }

        public T Get<T>(StableId id) where T : class, IDefinition
        {
            if (TryGet(id, out T def)) return def;
            throw new KeyNotFoundException("No definition of type " + typeof(T).Name + " with id '" + id + "'");
        }

        public IReadOnlyList<T> All<T>() where T : class, IDefinition
        {
            if (_allCache.TryGetValue(typeof(T), out object cached)) return (IReadOnlyList<T>)cached;
            var list = new List<T>();
            for (int i = 0; i < _ordered.Count; i++)
                if (_ordered[i] is T t) list.Add(t);
            _allCache[typeof(T)] = list;
            return list;
        }
    }
}
