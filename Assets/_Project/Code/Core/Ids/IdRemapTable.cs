using System;
using System.Collections.Generic;

namespace Eclipse.Core.Ids
{
    /// <summary>
    /// Migration table mapping renamed ids (old to new). Chains are followed on resolve.
    /// Cycles are rejected at <see cref="Add"/> time (InvalidOperationException), so the table is always acyclic.
    /// </summary>
    public sealed class IdRemapTable
    {
        private readonly Dictionary<StableId, StableId> _map = new Dictionary<StableId, StableId>();

        /// <summary>Number of remap entries.</summary>
        public int Count => _map.Count;

        /// <summary>Adds from-to. Throws ArgumentException if empty, from == to or from already mapped; InvalidOperationException if it closes a cycle.</summary>
        public void Add(StableId from, StableId to)
        {
            if (from.IsEmpty) throw new ArgumentException("'from' is empty", nameof(from));
            if (to.IsEmpty) throw new ArgumentException("'to' is empty", nameof(to));
            if (from == to) throw new ArgumentException("Self-map not allowed: " + from, nameof(to));
            if (_map.ContainsKey(from)) throw new ArgumentException("Duplicate remap source: " + from, nameof(from));

            // Walk the chain from 'to'; reaching 'from' means the new edge closes a cycle.
            StableId cur = to;
            int guard = _map.Count + 1;
            while (_map.TryGetValue(cur, out var next))
            {
                if (next == from)
                    throw new InvalidOperationException("Remap cycle detected: " + from + " -> " + to);
                cur = next;
                if (--guard < 0) throw new InvalidOperationException("Remap cycle detected");
            }
            _map.Add(from, to);
        }

        /// <summary>Follows the chain to its final id; returns the input if it has no entry.</summary>
        public StableId Resolve(StableId id)
        {
            TryResolve(id, out var result);
            return result;
        }

        /// <summary>Returns true if <paramref name="id"/> was remapped; <paramref name="resolved"/> is the final id (or the input).</summary>
        public bool TryResolve(StableId id, out StableId resolved)
        {
            resolved = id;
            bool found = false;
            int guard = _map.Count + 1;
            while (_map.TryGetValue(resolved, out var next))
            {
                resolved = next;
                found = true;
                if (--guard < 0) throw new InvalidOperationException("Remap cycle detected");
            }
            return found;
        }

        /// <summary>True if the id has a direct entry.</summary>
        public bool Contains(StableId id) => _map.ContainsKey(id);
    }
}
