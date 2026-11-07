using Eclipse.Core.Ids;
using Eclipse.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Definitions
{
    /// <summary>Shows the stable id read-only and offers "Rename ID..." which records an old-to-new remap pair.</summary>
    [CustomEditor(typeof(Definition), true)]
    public sealed class DefinitionInspector : UnityEditor.Editor
    {
        private string _newId = string.Empty;
        private string _error;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty idProp = serializedObject.FindProperty("stableId");

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(idProp);

            EditorGUILayout.BeginHorizontal();
            _newId = EditorGUILayout.TextField("New ID", _newId);
            if (GUILayout.Button("Rename ID...", GUILayout.Width(100)))
                _error = TryRename((Definition)target, _newId);
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);

            DrawPropertiesExcluding(serializedObject, "m_Script", "stableId");
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Renames the definition's id; returns an error message or null on success.</summary>
        public static string TryRename(Definition def, string newId)
        {
            if (!StableId.IsValid(newId)) return "Invalid StableId: '" + newId + "'";
            string old = def.StableIdString;
            if (old == newId) return "New ID equals the current ID.";
            if (!StableId.IsValid(old)) return "Current ID is empty/invalid; set it directly instead of renaming.";

            foreach (string guid in AssetDatabase.FindAssets("t:Definition", new[] { RegistryBuilder.ContentFolder }))
            {
                var other = AssetDatabase.LoadAssetAtPath<Definition>(AssetDatabase.GUIDToAssetPath(guid));
                if (other != null && other != def && other.StableIdString == newId)
                    return "ID already used by " + AssetDatabase.GetAssetPath(other);
            }

            IdRemapAsset remap = RegistryBuilder.LoadOrCreateRemap(RegistryBuilder.RemapPath);
            try
            {
                Undo.RecordObject(remap, "Rename ID");
                remap.AddPair(old, newId);
            }
            catch (System.Exception e)
            {
                return e.Message;
            }
            Undo.RecordObject(def, "Rename ID");
            def.EditorSetStableId(newId);
            EditorUtility.SetDirty(remap);
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssets();
            return null;
        }
    }
}
