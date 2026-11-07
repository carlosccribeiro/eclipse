using System;
using System.Collections.Generic;
using System.IO;
using Eclipse.Core.Definitions;
using Eclipse.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Eclipse.Editor.Definitions
{
    /// <summary>Scans Content for <see cref="Definition"/> assets and (re)builds the global registry asset.</summary>
    public static class RegistryBuilder
    {
        public const string ContentFolder = "Assets/_Project/Content";
        public const string RegistryPath = ContentFolder + "/DefinitionRegistry.asset";
        public const string RemapPath = ContentFolder + "/IdRemap.asset";

        [MenuItem("Eclipse/Data/Rebuild Registry")]
        public static void RebuildMenu() => Rebuild();

        /// <summary>Rebuilds the default registry. Throws <see cref="DuplicateDefinitionException"/> on duplicate ids.</summary>
        public static DefinitionRegistry Rebuild() => Rebuild(ContentFolder, RegistryPath, RemapPath);

        /// <summary>
        /// Testable variant: scans <paramref name="contentFolder"/>, writes the registry and remap assets at the given paths.
        /// Definitions are sorted by asset path (ordinal) for determinism; the registry file is only touched when its contents change.
        /// </summary>
        public static DefinitionRegistry Rebuild(string contentFolder, string registryPath, string remapPath)
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Definition", new[] { contentFolder }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);

            var defs = new List<Definition>(paths.Count);
            foreach (string p in paths)
            {
                var d = AssetDatabase.LoadAssetAtPath<Definition>(p);
                if (d != null) defs.Add(d);
            }
            return Rebuild(defs, registryPath, remapPath);
        }

        /// <summary>Rebuilds from an explicit, already ordered list of definition assets.</summary>
        public static DefinitionRegistry Rebuild(IReadOnlyList<Definition> defs, string registryPath, string remapPath)
        {
            // Validate first so a duplicate never leaves a half-written registry.
            IdRemapAsset remap = LoadOrCreateRemap(remapPath);
            var probe = new InMemoryDefinitionRegistry(remap.ToTable());
            foreach (Definition d in defs) probe.Register(d);

            var registry = AssetDatabase.LoadAssetAtPath<DefinitionRegistry>(registryPath);
            bool created = registry == null;
            if (created)
            {
                EnsureFolder(Path.GetDirectoryName(registryPath).Replace('\\', '/'));
                registry = ScriptableObject.CreateInstance<DefinitionRegistry>();
                AssetDatabase.CreateAsset(registry, registryPath);
            }

            bool same = !created && registry.RemapAsset == remap && registry.Definitions.Count == defs.Count;
            for (int i = 0; same && i < defs.Count; i++)
                same = registry.Definitions[i] == defs[i];

            if (!same)
            {
                registry.SetDefinitions(defs, remap);
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
            }
            return registry;
        }

        /// <summary>Loads the remap asset at <paramref name="path"/>, creating it if missing.</summary>
        public static IdRemapAsset LoadOrCreateRemap(string path)
        {
            var remap = AssetDatabase.LoadAssetAtPath<IdRemapAsset>(path);
            if (remap != null) return remap;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            remap = ScriptableObject.CreateInstance<IdRemapAsset>();
            AssetDatabase.CreateAsset(remap, path);
            return remap;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
