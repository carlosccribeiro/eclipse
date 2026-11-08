using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Eclipse.AI.Locomotion
{
    /// <summary>
    /// Global path per room via NavMesh (waypoints only). Called on demand (spawn, target change), never per frame
    /// per agent; per-frame movement is the locomotion jobs. Reuses one NavMeshPath and one corner buffer.
    /// </summary>
    public sealed class RoomPathProvider
    {
        readonly NavMeshPath _path = new NavMeshPath();
        readonly Vector3[] _corners = new Vector3[64];

        /// <summary>Fills the list (cleared first) with the corners from start to end. False if there is no complete path.</summary>
        public bool TryGetWaypoints(Vector3 from, Vector3 to, List<Vector3> waypoints, int areaMask = NavMesh.AllAreas)
        {
            waypoints.Clear();
            if (!NavMesh.CalculatePath(from, to, areaMask, _path) || _path.status != NavMeshPathStatus.PathComplete)
                return false;
            int n = _path.GetCornersNonAlloc(_corners);
            for (int i = 0; i < n; i++) waypoints.Add(_corners[i]);
            return n > 0;
        }
    }

    /// <summary>Walks an agent along waypoints: returns the current steering target and advances on arrival.</summary>
    public struct WaypointCursor
    {
        public int Index;

        public Vector3 Current(List<Vector3> waypoints, Vector3 position, float arriveRadius)
        {
            if (waypoints.Count == 0) return position;
            while (Index < waypoints.Count - 1)
            {
                Vector3 d = waypoints[Index] - position;
                d.y = 0f;
                if (d.sqrMagnitude > arriveRadius * arriveRadius) break;
                Index++;
            }
            return waypoints[Index];
        }
    }
}
