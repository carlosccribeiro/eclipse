using UnityEngine;

namespace Eclipse.AI.Enemies
{
    /// <summary>Planar (XZ) helpers for "does anything stand on the line of fire" and on-screen tests. Pure; no allocation.</summary>
    public static class ShotLine
    {
        /// <summary>True when the circle (an ally body) intersects the segment from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static bool SegmentHitsCircle(Vector3 from, Vector3 to, Vector3 center, float radius)
        {
            float dx = to.x - from.x, dz = to.z - from.z;
            float len2 = dx * dx + dz * dz;
            float t = 0f;
            if (len2 > 1e-8f)
            {
                t = ((center.x - from.x) * dx + (center.z - from.z) * dz) / len2;
                t = t < 0f ? 0f : (t > 1f ? 1f : t);
            }
            float px = from.x + dx * t - center.x;
            float pz = from.z + dz * t - center.z;
            return px * px + pz * pz <= radius * radius;
        }

        /// <summary>
        /// True when <paramref name="worldPoint"/> projects inside the viewport grown by <paramref name="margin"/>
        /// (viewport fractions) and in front of the camera. <paramref name="viewProjection"/> = projection x worldToCamera.
        /// </summary>
        public static bool IsOnScreen(in Matrix4x4 viewProjection, Vector3 worldPoint, float margin)
        {
            Vector4 clip = viewProjection * new Vector4(worldPoint.x, worldPoint.y, worldPoint.z, 1f);
            if (clip.w <= 0f) return false;
            float x = clip.x / clip.w * 0.5f + 0.5f;
            float y = clip.y / clip.w * 0.5f + 0.5f;
            return x >= -margin && x <= 1f + margin && y >= -margin && y <= 1f + margin;
        }
    }
}
