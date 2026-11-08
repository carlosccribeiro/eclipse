namespace Eclipse.AI.Utility
{
    /// <summary>
    /// Attack permission gate (09 section 3.2 / 11 section 3.10). Implemented by the AttackTokenDirector (M07 T3);
    /// tests use a stub. A brain only attacks after <see cref="TryAcquire"/> returns true and calls
    /// <see cref="Release"/> when the attack ends.
    /// </summary>
    public interface ITokenGate
    {
        bool TryAcquire(int ownerId, bool ranged);
        void Release(int ownerId);
    }
}
