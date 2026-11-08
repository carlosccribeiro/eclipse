using Eclipse.Core.Stats;
using Eclipse.Gameplay.Simulation;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>Anything that takes part in combat (player, enemy, boss, summon, dummy). Frozen contract (M05 T1).</summary>
    public interface ICombatEntity
    {
        EntityId Id { get; }
        Team Team { get; }
        bool IsAlive { get; }
        Vector3 Position { get; }
        /// <summary>Per-entity clock (hit-stop freezes only this entity - 06 3.11).</summary>
        EntityClock Clock { get; }
        /// <summary>Final stats of the entity (Core StatBlock).</summary>
        StatBlock Stats { get; }
    }
}
