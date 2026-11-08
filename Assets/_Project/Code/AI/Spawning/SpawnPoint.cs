using UnityEngine;

namespace Eclipse.AI.Spawning
{
    /// <summary>Marker of a place where the test spawner may put enemies (M07 T8). Position on the XZ plane, spread inside a radius.</summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private float radius = 1f;

        public float Radius => radius;
        public void SetRadius(float value) => radius = value;

        /// <summary>Point for the n-th spawn here: a deterministic golden-angle spiral inside the radius (no RNG in the sandbox spawner).</summary>
        public Vector3 PointFor(int n)
        {
            const float GoldenAngle = 2.39996323f;
            float r = radius * Mathf.Sqrt(((n % 7) + 0.5f) / 7f);
            float a = n * GoldenAngle;
            Vector3 p = transform.position;
            return new Vector3(p.x + Mathf.Cos(a) * r, 0f, p.z + Mathf.Sin(a) * r);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.05f, radius);
        }
    }
}
