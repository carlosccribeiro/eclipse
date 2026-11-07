using System.Collections.Generic;
using Eclipse.Data.Definitions;

namespace Eclipse.Data.Validation.Tests
{
    public class FakeTaggedDef : Definition, ITaggedDefinition
    {
        public List<string> Tags = new List<string>();
        public IEnumerable<string> TagNames => Tags;
    }
}
