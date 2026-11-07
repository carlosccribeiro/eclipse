using System;
using System.Collections.Generic;
using UnityEditor;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Discovers importers by <see cref="TableImporterAttribute"/> and orders them by dependency, then Order, then name.</summary>
    public static class ImporterRegistry
    {
        public sealed class Entry
        {
            public string Name;
            public int Order;
            public string[] DependsOn = Array.Empty<string>();
            public ITableImporter Importer;
        }

        /// <summary>Instantiates every attributed importer. Problems are reported to <paramref name="context"/>.</summary>
        public static List<ITableImporter> Discover(ImportContext context)
        {
            var entries = new List<Entry>();
            foreach (Type type in TypeCache.GetTypesWithAttribute<TableImporterAttribute>())
            {
                if (type.IsAbstract || !typeof(ITableImporter).IsAssignableFrom(type))
                {
                    context.Error("[TableImporter] on '" + type.FullName + "' requires a concrete ITableImporter class");
                    continue;
                }
                try
                {
                    var attr = (TableImporterAttribute)Attribute.GetCustomAttribute(type, typeof(TableImporterAttribute));
                    entries.Add(new Entry
                    {
                        Name = attr.Name,
                        Order = attr.Order,
                        DependsOn = attr.DependsOn ?? Array.Empty<string>(),
                        Importer = (ITableImporter)Activator.CreateInstance(type),
                    });
                }
                catch (Exception ex)
                {
                    context.Error("cannot instantiate importer '" + type.FullName + "': " + ex.Message);
                }
            }

            try
            {
                return Sort(entries);
            }
            catch (InvalidOperationException ex)
            {
                context.Error(ex.Message);
                return new List<ITableImporter>();
            }
        }

        /// <summary>Topological order (dependencies first); ties by Order then Name. Throws on duplicate names, unknown dependencies and cycles.</summary>
        public static List<ITableImporter> Sort(IReadOnlyList<Entry> entries)
        {
            var byName = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (Entry e in entries)
            {
                if (byName.ContainsKey(e.Name))
                    throw new InvalidOperationException("duplicate importer name '" + e.Name + "'");
                byName.Add(e.Name, e);
            }
            foreach (Entry e in entries)
                foreach (string dep in e.DependsOn)
                    if (!byName.ContainsKey(dep))
                        throw new InvalidOperationException("importer '" + e.Name + "' depends on unknown table '" + dep + "'");

            var pending = new List<Entry>(entries);
            var done = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<ITableImporter>(entries.Count);
            while (pending.Count > 0)
            {
                Entry best = null;
                foreach (Entry e in pending)
                {
                    bool ready = true;
                    foreach (string dep in e.DependsOn)
                        if (!done.Contains(dep)) { ready = false; break; }
                    if (!ready) continue;
                    if (best == null || e.Order < best.Order
                        || (e.Order == best.Order && string.CompareOrdinal(e.Name, best.Name) < 0))
                        best = e;
                }
                if (best == null)
                {
                    var names = new List<string>();
                    foreach (Entry e in pending) names.Add(e.Name);
                    throw new InvalidOperationException("dependency cycle among importers: " + string.Join(", ", names));
                }
                pending.Remove(best);
                done.Add(best.Name);
                result.Add(best.Importer);
            }
            return result;
        }
    }
}
