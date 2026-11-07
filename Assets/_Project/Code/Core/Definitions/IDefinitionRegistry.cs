using System.Collections.Generic;
using Eclipse.Core.Ids;

namespace Eclipse.Core.Definitions
{
    /// <summary>
    /// Read-only lookup of definitions by <see cref="StableId"/>. All lookups first resolve the id
    /// through <see cref="Remap"/> (old id returns the new definition).
    /// </summary>
    public interface IDefinitionRegistry
    {
        /// <summary>Total number of registered definitions.</summary>
        int Count { get; }

        /// <summary>Migration table applied to every lookup.</summary>
        IdRemapTable Remap { get; }

        /// <summary>True if a definition exists for the (remapped) id, regardless of type.</summary>
        bool Contains(StableId id);

        /// <summary>Returns false if the id is absent or the definition is not a <typeparamref name="T"/>.</summary>
        bool TryGet<T>(StableId id, out T definition) where T : class, IDefinition;

        /// <summary>Returns the definition or throws <see cref="KeyNotFoundException"/> (message includes the id) if absent or of a different type.</summary>
        T Get<T>(StableId id) where T : class, IDefinition;

        /// <summary>All definitions assignable to <typeparamref name="T"/>, in registration order. Implementations should cache per type to avoid per-call allocation.</summary>
        IReadOnlyList<T> All<T>() where T : class, IDefinition;
    }
}
