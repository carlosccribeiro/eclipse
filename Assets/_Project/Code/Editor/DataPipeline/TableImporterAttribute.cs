using System;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>
    /// Registers an <see cref="ITableImporter"/> class with the pipeline. Importers are discovered by
    /// <see cref="ImporterRegistry"/>; no central list needs editing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TableImporterAttribute : Attribute
    {
        public TableImporterAttribute(string name)
        {
            Name = name;
        }

        /// <summary>Table name (unique); referenced by <see cref="DependsOn"/> of other importers.</summary>
        public string Name { get; }

        /// <summary>Tie-breaker among importers with no dependency between them (lower first).</summary>
        public int Order { get; set; }

        /// <summary>Names of tables that must be imported before this one.</summary>
        public string[] DependsOn { get; set; } = Array.Empty<string>();
    }
}
