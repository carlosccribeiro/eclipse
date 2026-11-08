using System;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Runtime id of a combat entity (not a StableId: never saved). 0 is <see cref="None"/>.
    /// Frozen contract (M05 T1): extend only through new files.
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public static readonly EntityId None = default;

        public readonly int Value;

        public EntityId(int value) { Value = value; }

        public bool IsValid => Value != 0;

        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId o && Equals(o);
        public override int GetHashCode() => Value;
        public override string ToString() => "E" + Value;
        public static bool operator ==(EntityId a, EntityId b) => a.Value == b.Value;
        public static bool operator !=(EntityId a, EntityId b) => a.Value != b.Value;
    }
}
