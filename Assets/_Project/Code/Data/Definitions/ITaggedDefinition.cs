using System.Collections.Generic;

namespace Eclipse.Data.Definitions
{
    /// <summary>Implemented by definitions that carry ability tags (00 section 3.4) by name; checked by TagVocabularyRule.</summary>
    public interface ITaggedDefinition
    {
        IEnumerable<string> TagNames { get; }
    }
}
