using System.Reflection;
using Eclipse.Data.Definitions;
using UnityEngine;
using UnityEngine.Localization;

namespace Eclipse.Data.Validation.Tests
{
    public class FakeValidationDef : Definition
    {
        public static T Make<T>(string id, string nameEn = null, string sourceDoc = null, bool withDisplayName = true)
            where T : Definition
        {
            var d = ScriptableObject.CreateInstance<T>();
            d.name = string.IsNullOrEmpty(id) ? "unnamed" : id;
            d.EditorSetStableId(id);
            d.EditorSetNameEn(nameEn);
            d.EditorSetSourceDoc(sourceDoc);
            if (withDisplayName)
            {
                typeof(Definition).GetField("displayName", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(d, new LocalizedString("Names", "key"));
            }
            return d;
        }
    }
}
