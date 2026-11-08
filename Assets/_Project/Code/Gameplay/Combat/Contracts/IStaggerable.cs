namespace Eclipse.Gameplay.Combat
{
    /// <summary>Entity with a Stagger bar (06 3.8). Frozen contract (M05 T1).</summary>
    public interface IStaggerable
    {
        EntityId Id { get; }
        float StaggerCurrent { get; }
        float StaggerMax { get; }
        bool IsStaggered { get; }
        /// <summary>Adds Stagger; returns true if this call filled the bar (Staggered/Break).</summary>
        bool AddStagger(float amount, EntityId source, int generation);
    }
}
