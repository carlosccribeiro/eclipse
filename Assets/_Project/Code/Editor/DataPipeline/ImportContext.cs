using System;
using System.Collections.Generic;
using Eclipse.Data.Definitions;

namespace Eclipse.Editor.DataPipeline
{
    public enum ImportSeverity
    {
        Warning,
        Error,
    }

    /// <summary>One diagnostic with table, CSV line (0 if unknown) and definition id (may be null).</summary>
    public sealed class ImportMessage
    {
        public ImportSeverity Severity { get; }
        public string Table { get; }
        public int Line { get; }
        public string Id { get; }
        public string Text { get; }

        public ImportMessage(ImportSeverity severity, string table, int line, string id, string text)
        {
            Severity = severity;
            Table = table;
            Line = line;
            Id = id;
            Text = text;
        }

        public override string ToString()
        {
            string where = Table ?? "(pipeline)";
            if (Line > 0) where += ":" + Line;
            if (!string.IsNullOrEmpty(Id)) where += " [" + Id + "]";
            return (Severity == ImportSeverity.Error ? "ERROR " : "WARNING ") + where + ": " + Text;
        }
    }

    /// <summary>
    /// Shared state of one pipeline run: diagnostics, counters and the two-pass reference resolution.
    /// Pass 1: every importer creates/updates its assets and calls <see cref="Register"/>.
    /// Pass 2 (<see cref="RunDeferred"/>): actions queued with <see cref="Defer"/> resolve cross references by StableId.
    /// </summary>
    public sealed class ImportContext
    {
        private readonly List<ImportMessage> _messages = new List<ImportMessage>();
        private readonly List<Action> _deferred = new List<Action>();
        private readonly Dictionary<string, Definition> _definitions = new Dictionary<string, Definition>(StringComparer.Ordinal);

        public IReadOnlyList<ImportMessage> Messages => _messages;

        /// <summary>Table being imported; used by the overloads without a table argument.</summary>
        public string CurrentTable { get; set; }

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int TablesProcessed { get; internal set; }
        public int Created { get; internal set; }
        public int Updated { get; internal set; }
        public int Unchanged { get; internal set; }
        public int Orphans { get; internal set; }

        public bool HasErrors => ErrorCount > 0;

        public void Error(string table, int line, string id, string text) => Add(ImportSeverity.Error, table, line, id, text);
        public void Warning(string table, int line, string id, string text) => Add(ImportSeverity.Warning, table, line, id, text);
        public void Error(int line, string id, string text) => Add(ImportSeverity.Error, CurrentTable, line, id, text);
        public void Warning(int line, string id, string text) => Add(ImportSeverity.Warning, CurrentTable, line, id, text);
        public void Error(string text) => Add(ImportSeverity.Error, CurrentTable, 0, null, text);
        public void Warning(string text) => Add(ImportSeverity.Warning, CurrentTable, 0, null, text);

        private void Add(ImportSeverity severity, string table, int line, string id, string text)
        {
            _messages.Add(new ImportMessage(severity, table, line, id, text));
            if (severity == ImportSeverity.Error) ErrorCount++;
            else WarningCount++;
        }

        /// <summary>Makes a definition (new or existing) resolvable by id for pass 2. Called by <c>TableImporter</c>.</summary>
        public void Register(Definition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.StableIdString)) return;
            _definitions[definition.StableIdString] = definition;
        }

        /// <summary>Lets references that still use an old (remapped) id resolve to the renamed definition.</summary>
        public void RegisterAlias(string oldId, Definition definition)
        {
            if (definition == null || string.IsNullOrEmpty(oldId)) return;
            _definitions[oldId] = definition;
        }

        public bool TryResolve<T>(string id, out T definition) where T : Definition
        {
            if (id != null && _definitions.TryGetValue(id, out Definition d) && d is T typed)
            {
                definition = typed;
                return true;
            }
            definition = null;
            return false;
        }

        /// <summary>Resolves a reference by StableId; reports an error and returns null if missing or of another type.</summary>
        public T Require<T>(string id, int line, string ownerId, string column) where T : Definition
        {
            if (TryResolve(id, out T d)) return d;
            Error(line, ownerId, "column '" + column + "': reference '" + id + "' does not resolve to a " + typeof(T).Name);
            return null;
        }

        /// <summary>Queues a pass-2 action. It must mark assets dirty itself if it changes them.</summary>
        public void Defer(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            _deferred.Add(action);
        }

        /// <summary>Runs pass 2. Exceptions become errors.</summary>
        public void RunDeferred()
        {
            var actions = _deferred.ToArray();
            _deferred.Clear();
            foreach (Action a in actions)
            {
                try { a(); }
                catch (Exception ex) { Error("reference resolution failed: " + ex.Message); }
            }
        }
    }
}
