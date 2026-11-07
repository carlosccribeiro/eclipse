using System;

namespace Eclipse.Core.Stats
{
    /// <summary>Identifies the origin of a set of mods (item, set, passive, buff) so they can be removed together.</summary>
    public readonly struct SourceHandle : IEquatable<SourceHandle>
    {
        public SourceHandle(int id) { Id = id; }

        /// <summary>Caller-chosen or <see cref="StatBlock.CreateSource"/>-issued id; 0 is invalid.</summary>
        public int Id { get; }

        public bool IsValid => Id != 0;

        public bool Equals(SourceHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is SourceHandle h && Equals(h);
        public override int GetHashCode() => Id;
        public override string ToString() => "Source#" + Id;
        public static bool operator ==(SourceHandle a, SourceHandle b) => a.Id == b.Id;
        public static bool operator !=(SourceHandle a, SourceHandle b) => a.Id != b.Id;
    }
}
