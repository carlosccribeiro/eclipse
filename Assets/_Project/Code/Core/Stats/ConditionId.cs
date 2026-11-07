using System;

namespace Eclipse.Core.Stats
{
    /// <summary>Opaque id of a runtime condition ("target frozen", "Fury >= 50"). 0 means unconditional.</summary>
    public readonly struct ConditionId : IEquatable<ConditionId>
    {
        public ConditionId(int id) { Id = id; }

        public int Id { get; }

        public static ConditionId None => default;
        public bool IsNone => Id == 0;

        public bool Equals(ConditionId other) => Id == other.Id;
        public override bool Equals(object obj) => obj is ConditionId c && Equals(c);
        public override int GetHashCode() => Id;
        public override string ToString() => "Condition#" + Id;
        public static bool operator ==(ConditionId a, ConditionId b) => a.Id == b.Id;
        public static bool operator !=(ConditionId a, ConditionId b) => a.Id != b.Id;
    }

    /// <summary>Evaluates conditions at query time (per hit). Implemented by gameplay.</summary>
    public interface IConditionEvaluator
    {
        bool IsMet(ConditionId condition);
    }
}
