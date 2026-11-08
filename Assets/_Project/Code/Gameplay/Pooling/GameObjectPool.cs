using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eclipse.Gameplay.Pooling
{
    /// <summary>Pool of prefab instances. Get/Release do not allocate after warm-up.</summary>
    public sealed class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly Stack<GameObject> _idle;
        private readonly HashSet<GameObject> _idleSet = new HashSet<GameObject>();
        private readonly Dictionary<GameObject, IPoolable[]> _poolables = new Dictionary<GameObject, IPoolable[]>();
        private readonly int _maxIdle;

        public int IdleCount => _idle.Count;
        public int TotalCreated { get; private set; }

        public GameObjectPool(GameObject prefab, Transform root, PoolConfig config)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _root = root;
            _maxIdle = config.MaxIdle;
            _idle = new Stack<GameObject>(Math.Max(config.Prewarm, 4));
            for (int i = 0; i < config.Prewarm; i++)
            {
                GameObject go = Create();
                go.SetActive(false);
                _idle.Push(go);
                _idleSet.Add(go);
            }
        }

        private GameObject Create()
        {
            TotalCreated++;
            GameObject go = UnityEngine.Object.Instantiate(_prefab, _root);
            _poolables[go] = go.GetComponentsInChildren<IPoolable>(true);
            return go;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject go;
            if (_idle.Count > 0)
            {
                go = _idle.Pop();
                _idleSet.Remove(go);
            }
            else
            {
                go = Create();
            }
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            IPoolable[] list = _poolables[go];
            for (int i = 0; i < list.Length; i++) list[i].OnSpawned();
            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null) throw new ArgumentNullException(nameof(go));
            if (!_poolables.TryGetValue(go, out IPoolable[] list))
                throw new InvalidOperationException("Object does not belong to this pool.");
            if (_idleSet.Contains(go)) return; // double release ignored
            for (int i = 0; i < list.Length; i++) list[i].OnDespawned();
            if (_maxIdle > 0 && _idle.Count >= _maxIdle)
            {
                _poolables.Remove(go);
                UnityEngine.Object.Destroy(go);
                return;
            }
            go.SetActive(false);
            if (_root != null) go.transform.SetParent(_root, false);
            _idle.Push(go);
            _idleSet.Add(go);
        }
    }
}
