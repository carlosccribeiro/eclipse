using Eclipse.Core.Ids;

namespace Eclipse.Core.Definitions
{
    /// <summary>A static game definition identified by an immutable <see cref="StableId"/>.</summary>
    public interface IDefinition
    {
        /// <summary>Immutable id. Renaming it requires an <see cref="IdRemapTable"/> entry.</summary>
        StableId Id { get; }
    }
}
