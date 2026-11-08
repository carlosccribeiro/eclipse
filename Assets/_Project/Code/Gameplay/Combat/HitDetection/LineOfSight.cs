using Eclipse.Gameplay.Physics;
using UPhysics = UnityEngine.Physics;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>Melee/area line of sight: only the Wall layer blocks (06 3.9; LowCover does not).</summary>
    public static class LineOfSight
    {
        public static bool IsBlocked(Vector3 from, Vector3 to) =>
            UPhysics.Linecast(from, to, PhysicsLayers.LineOfSightMask, QueryTriggerInteraction.Ignore);
    }
}
