using System.Collections.Generic;

namespace Eclipse.Core.RunGen.Map
{
    /// <summary>Inputs of <see cref="MapGenerator.Generate"/> besides the seed.</summary>
    public sealed class MapGenContext
    {
        private static readonly string[] EmptyPool = new string[0];

        /// <summary>Region of the run; keys <see cref="RunHistory"/> (09 §11.3 rule 6).</summary>
        public string RegionId { get; set; } = string.Empty;

        /// <summary>Event ids available to Event rooms (content is M16). Ids are drawn, never invented.</summary>
        public IReadOnlyList<string> EventPool { get; set; } = EmptyPool;

        /// <summary>Optional history of previous runs (09 §11.3 rule 6, second clause).</summary>
        public RunHistory History { get; set; }

        /// <summary>Weights and counts; null means <see cref="MapGenConfig.CreateDefault"/>.</summary>
        public MapGenConfig Config { get; set; }
    }
}
