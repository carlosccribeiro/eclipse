namespace Eclipse.Gameplay.Combat
{
    /// <summary>
    /// Resolves a runtime <see cref="EntityId"/> to the entity and its damage receiver. Implemented by the spawner /
    /// combat registry (later modules); used by the periodic damage runners so they never hold scene references.
    /// </summary>
    public interface ICombatDirectory
    {
        /// <summary>False when the id is unknown (despawned, pooled).</summary>
        bool TryGet(EntityId id, out ICombatEntity entity, out IDamageReceiver receiver);
    }
}
