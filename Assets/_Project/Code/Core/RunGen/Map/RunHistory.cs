using System;
using System.Collections.Generic;

namespace Eclipse.Core.RunGen.Map
{
    /// <summary>
    /// Cross-run memory needed by 09 §11.3 rule 6: event ids seen in the last run, per region.
    /// Persistence is owned by M16/M18; the generator only reads it.
    /// </summary>
    public sealed class RunHistory
    {
        private static readonly string[] None = new string[0];

        /// <summary>Region id -> event ids of the last run in that region.</summary>
        public Dictionary<string, string[]> LastRunEventIds { get; } = new Dictionary<string, string[]>(StringComparer.Ordinal);

        public IReadOnlyList<string> GetLastRunEventIds(string regionId)
        {
            if (regionId != null && LastRunEventIds.TryGetValue(regionId, out string[] ids)) return ids;
            return None;
        }

        public void SetLastRunEventIds(string regionId, IEnumerable<string> eventIds)
        {
            LastRunEventIds[regionId ?? string.Empty] = new List<string>(eventIds).ToArray();
        }
    }
}
