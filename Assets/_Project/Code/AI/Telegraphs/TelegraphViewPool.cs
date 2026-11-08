using UnityEngine;

namespace Eclipse.AI.Telegraphs
{
    /// <summary>Fixed-size pool of <see cref="TelegraphView"/>, all built at construction (no Instantiate at telegraph time).</summary>
    public sealed class TelegraphViewPool
    {
        private readonly TelegraphView[] _views;
        private readonly Material _material;
        private readonly Mesh _quad;
        private int _inUse;

        public int Capacity => _views.Length;
        public int InUseCount => _inUse;

        public TelegraphViewPool(Shader fillShader, int capacity, Transform parent = null)
        {
            _material = new Material(fillShader) { name = "TelegraphFill (pool)", renderQueue = 3100 };
            _quad = BuildQuad();
            _views = new TelegraphView[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var go = new GameObject("Telegraph " + i);
                if (parent != null) go.transform.SetParent(parent, false);
                var view = go.AddComponent<TelegraphView>();
                view.Init(_quad, _material);
                _views[i] = view;
            }
        }

        /// <summary>Returns a free view or null when the pool is exhausted (caller skips the visual; gameplay timing is unaffected).</summary>
        public TelegraphView Rent()
        {
            for (int i = 0; i < _views.Length; i++)
            {
                if (_views[i].InUse) continue;
                _views[i].InUse = true;
                _inUse++;
                return _views[i];
            }
            return null;
        }

        public void Return(TelegraphView view)
        {
            if (view == null || !view.InUse) return;
            view.Hide();
            view.InUse = false;
            _inUse--;
        }

        public void Destroy()
        {
            for (int i = 0; i < _views.Length; i++)
                if (_views[i] != null) Object.DestroyImmediate(_views[i].gameObject);
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_quad);
        }

        /// <summary>Unit quad on XZ, centered, UV 0..1, facing up.</summary>
        private static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "TelegraphQuad" };
            mesh.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 0.1f, 1f));
            return mesh;
        }
    }
}
