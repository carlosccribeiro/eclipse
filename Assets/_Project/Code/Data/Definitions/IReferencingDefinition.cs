using System.Collections.Generic;

namespace Eclipse.Data.Definitions
{
    /// <summary>Implemented by definitions that reference others by StableId; checked by ReferenceResolvesRule.</summary>
    public interface IReferencingDefinition
    {
        IEnumerable<(string field, string targetId)> References { get; }
    }
}
