namespace Eclipse.Gameplay.Simulation
{
    /// <summary>
    /// Per-entity clock. Allows local freeze (hit-stop, 06 §3.11) without stopping the rest of the simulation.
    /// Call <see cref="Advance"/> once per global tick; tick the entity only when it returns true.
    /// </summary>
    public sealed class EntityClock
    {
        public int FrozenFramesRemaining { get; private set; }
        /// <summary>Number of ticks this entity actually advanced.</summary>
        public int LocalFrame { get; private set; }
        public bool IsFrozen => FrozenFramesRemaining > 0;

        /// <summary>Freezes for n ticks; overlapping freezes keep the longest (no stacking).</summary>
        public void Freeze(int frames)
        {
            if (frames > FrozenFramesRemaining) FrozenFramesRemaining = frames;
        }

        /// <summary>Consumes one global tick. Returns true if the entity should run this tick.</summary>
        public bool Advance()
        {
            if (FrozenFramesRemaining > 0)
            {
                FrozenFramesRemaining--;
                return false;
            }
            LocalFrame++;
            return true;
        }

        public void Reset()
        {
            FrozenFramesRemaining = 0;
            LocalFrame = 0;
        }
    }
}
