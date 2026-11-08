using System.Collections.Generic;
using Eclipse.Gameplay.Physics;
using UnityEngine;

namespace Eclipse.Gameplay.Cameras
{
    /// <summary>
    /// Makes geometry between the camera and the player translucent by dither (06 section 3.11).
    /// Spherecast (NonAlloc) from the camera to the target; hit renderers get "_OcclusionFade" via MaterialPropertyBlock
    /// (shader Eclipse/OcclusionDither). No per-frame allocation after warm-up: buffers are pre-allocated.
    /// </summary>
    public sealed class OcclusionFader : MonoBehaviour
    {
        public static readonly int FadeId = Shader.PropertyToID("_OcclusionFade");
        public const int MaxHits = 32;

        /// <summary>Layers that may occlude: Environment, Wall, LowCover.</summary>
        public const int OccluderMask = PhysicsLayers.EnvironmentMask | PhysicsLayers.WallMask | PhysicsLayers.LowCoverMask;

        sealed class Entry
        {
            public Renderer Renderer;
            public float Fade;      // 0 opaque .. 1 fully faded
            public bool Seen;
        }

        public Transform Target;
        public IsoCameraSettings Settings;
        public float TargetHeight = 1.0f;

        readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
        readonly List<Entry> _active = new List<Entry>(MaxHits);
        readonly Dictionary<int, Entry> _byRenderer = new Dictionary<int, Entry>(64);
        readonly Dictionary<int, Renderer> _colliderRenderer = new Dictionary<int, Renderer>(64);
        MaterialPropertyBlock _block;

        /// <summary>Renderers currently faded or fading (for tests).</summary>
        public int ActiveCount => _active.Count;

        public bool IsFading(Renderer renderer)
        {
            return renderer != null && _byRenderer.TryGetValue(renderer.GetInstanceID(), out var e) && e.Fade > 0f;
        }

        void LateUpdate()
        {
            if (Target == null || Settings == null) return;
            Step(transform.position, Target.position + Vector3.up * TargetHeight, Time.deltaTime);
        }

        /// <summary>One occlusion pass between <paramref name="cameraPosition"/> and <paramref name="targetPoint"/>.</summary>
        public void Step(Vector3 cameraPosition, Vector3 targetPoint, float deltaTime)
        {
            _block ??= new MaterialPropertyBlock();
            for (int i = 0; i < _active.Count; i++) _active[i].Seen = false;

            Vector3 delta = targetPoint - cameraPosition;
            float dist = delta.magnitude;
            if (dist > 1e-3f)
            {
                int n = UnityEngine.Physics.SphereCastNonAlloc(cameraPosition, Settings.occlusionSphereRadius, delta / dist,
                    _hits, dist, OccluderMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    Renderer r = RendererOf(_hits[i].collider);
                    if (r == null) continue;
                    GetOrAdd(r).Seen = true;
                }
            }

            float speed = Settings.occlusionFadeSpeed * deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Entry e = _active[i];
                if (e.Renderer == null) { Remove(i); continue; }
                e.Fade = Mathf.MoveTowards(e.Fade, e.Seen ? 1f : 0f, speed);
                if (!e.Seen && e.Fade <= 0f)
                {
                    e.Renderer.SetPropertyBlock(null);
                    Remove(i);
                    continue;
                }
                e.Renderer.GetPropertyBlock(_block);
                _block.SetFloat(FadeId, e.Fade * (1f - Settings.occlusionMinOpacity));
                e.Renderer.SetPropertyBlock(_block);
            }
        }

        Renderer RendererOf(Collider c)
        {
            if (c == null) return null;
            int id = c.GetInstanceID();
            if (!_colliderRenderer.TryGetValue(id, out Renderer r))
            {
                c.TryGetComponent(out r);
                _colliderRenderer[id] = r;
            }
            return r;
        }

        Entry GetOrAdd(Renderer r)
        {
            int id = r.GetInstanceID();
            if (_byRenderer.TryGetValue(id, out Entry e)) return e;
            e = new Entry { Renderer = r };
            _byRenderer[id] = e;
            _active.Add(e);
            return e;
        }

        void Remove(int index)
        {
            Entry e = _active[index];
            if (e.Renderer != null) _byRenderer.Remove(e.Renderer.GetInstanceID());
            int last = _active.Count - 1;
            _active[index] = _active[last];
            _active.RemoveAt(last);
        }

        void OnDisable()
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].Renderer != null) _active[i].Renderer.SetPropertyBlock(null);
            _active.Clear();
            _byRenderer.Clear();
        }
    }
}
