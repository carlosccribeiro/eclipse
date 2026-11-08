using Eclipse.Gameplay.Physics;
using UPhysics = UnityEngine.Physics;
using UnityEngine;

namespace Eclipse.Gameplay.Combat
{
    /// <summary>Nearest-target search for Chain/Homing over the hurtbox layers. Pre-allocated buffer, no GC.</summary>
    public sealed class PhysicsProjectileTargetSearch : IProjectileTargetSearch
    {
        private readonly Collider[] _buffer;

        public PhysicsProjectileTargetSearch(int bufferSize = 32)
        {
            _buffer = new Collider[bufferSize];
        }

        public bool TryFindNearest(Vector3 from, float radius, Team sourceTeam, HitRegistry alreadyHit, int hitGroup, out Vector3 point)
        {
            point = default;
            int mask = sourceTeam == Team.Player ? PhysicsLayers.PlayerAttackTargetsMask : PhysicsLayers.EnemyAttackTargetsMask;
            int n = UPhysics.OverlapSphereNonAlloc(from, radius, _buffer, mask, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                if (!_buffer[i].TryGetComponent(out Hurtbox hb) || hb.Entity == null || !hb.Entity.IsAlive) continue;
                if (hb.Entity.Team == sourceTeam) continue;
                if (alreadyHit != null && alreadyHit.Contains(hitGroup, hb.Entity.Id)) continue;
                Vector3 c = hb.Center;
                float d = (c - from).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                point = c;
                found = true;
            }
            return found;
        }
    }
}
