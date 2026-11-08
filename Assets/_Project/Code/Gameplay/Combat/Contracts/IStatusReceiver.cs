using Eclipse.Core.Ids;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Holds statuses of an entity. M05 ships a StatusStub; M11 replaces it without changing this contract.
    /// Frozen contract (M05 T1).
    /// </summary>
    public interface IStatusReceiver
    {
        EntityId Id { get; }
        /// <summary>Tries to apply a status; false if rejected (immune, Generation rules, cap).</summary>
        bool TryApply(in StatusRequest request);
        int GetStacks(StableId status);
        void Remove(StableId status);
    }
}
