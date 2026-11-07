using System.Collections.Generic;
using Eclipse.Data.Definitions;

namespace Eclipse.Data.Validation.Tests
{
    public class FakeReferencingDef : Definition, IReferencingDefinition
    {
        public string Target;

        public IEnumerable<(string field, string targetId)> References
        {
            get { yield return ("target", Target); }
        }
    }
}
