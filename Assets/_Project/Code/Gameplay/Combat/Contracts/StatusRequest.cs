using Eclipse.Core.Ids;
using Eclipse.Core.Tags;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>Request to apply a status (07). Resolved by an <see cref="IStatusReceiver"/>. Frozen contract (M05 T1).</summary>
    public readonly struct StatusRequest
    {
        /// <summary>StableId of the status definition (e.g. "burning").</summary>
        public readonly StableId Status;
        public readonly EntityId Source;
        public readonly EntityId Target;
        /// <summary>Generation of the originating event (R07).</summary>
        public readonly int Generation;
        /// <summary>Duration in simulation ticks (60 Hz); 0 = status default.</summary>
        public readonly int DurationFrames;
        /// <summary>Stacks to add.</summary>
        public readonly int Stacks;
        /// <summary>Intensity/buildup multiplier (Status Potency already applied by the caller; 1 = neutral).</summary>
        public readonly float Potency;
        /// <summary>When true the application skips buildup (guaranteed, used by the provisional Burning).</summary>
        public readonly bool Guaranteed;
        public readonly HitFlags Flags;

        public StatusRequest(StableId status, EntityId source, EntityId target, int generation, int durationFrames,
            int stacks, float potency, bool guaranteed, HitFlags flags)
        {
            Status = status; Source = source; Target = target; Generation = generation; DurationFrames = durationFrames;
            Stacks = stacks; Potency = potency; Guaranteed = guaranteed; Flags = flags;
        }
    }
}
