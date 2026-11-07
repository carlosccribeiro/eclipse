using System;
using System.Collections.Generic;
using Eclipse.Core.Ids;
using UnityEngine;

namespace Eclipse.Data.Definitions
{
    /// <summary>Serialized old-to-new id pairs; feeds the runtime <see cref="IdRemapTable"/>.</summary>
    [CreateAssetMenu(menuName = "Eclipse/Id Remap", fileName = "IdRemap")]
    public sealed class IdRemapAsset : ScriptableObject
    {
        [Serializable]
        public struct Pair
        {
            public string from;
            public string to;
        }

        [SerializeField] private List<Pair> pairs = new List<Pair>();

        public IReadOnlyList<Pair> Pairs => pairs;

        /// <summary>Builds the table. Throws on invalid ids, duplicate sources or cycles.</summary>
        public IdRemapTable ToTable()
        {
            var table = new IdRemapTable();
            for (int i = 0; i < pairs.Count; i++)
                table.Add(StableId.Parse(pairs[i].from), StableId.Parse(pairs[i].to));
            return table;
        }

        /// <summary>Appends a pair after validating it against the current table (throws, leaving the asset unchanged).</summary>
        public void AddPair(string from, string to)
        {
            IdRemapTable table = ToTable();
            table.Add(StableId.Parse(from), StableId.Parse(to));
            pairs.Add(new Pair { from = from, to = to });
        }
    }
}
