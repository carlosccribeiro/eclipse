using System;
using Eclipse.Core.Ids;

namespace Eclipse.Core.Definitions
{
    /// <summary>Thrown when a definition id is registered twice.</summary>
    public sealed class DuplicateDefinitionException : Exception
    {
        /// <summary>The duplicated id.</summary>
        public StableId Id { get; }

        public DuplicateDefinitionException(StableId id)
            : base("Duplicate definition id: '" + id + "'")
        {
            Id = id;
        }
    }
}
