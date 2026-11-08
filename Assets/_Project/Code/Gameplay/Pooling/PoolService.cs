using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eclipse.Gameplay.Pooling
{
    /// <summary>Registry of pools by prefab and by C# type (11 §3.14). Owns a root transform for idle instances.</summary>
    public sealed class PoolService
    {
        private readonly Transform _root;
        private readonly Dictionary<GameObject, GameObjectPool> _prefabPools = new Dictionary<GameObject, GameObjectPool>();
        private readonly Dictionary<Type, object> _typePools = new Dictionary<Type, object>();

        public PoolService(Transform root = null)
        {
            _root = root;
        }

        /// <summary>Returns the pool for a prefab, creating (and pre-warming) it on first use.</summary>
        public GameObjectPool ForPrefab(GameObject prefab, PoolConfig config = default)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (!_prefabPools.TryGetValue(prefab, out GameObjectPool pool))
            {
                pool = new GameObjectPool(prefab, _root, config);
                _prefabPools.Add(prefab, pool);
            }
            return pool;
        }

        /// <summary>Returns the pool for a C# type, creating it on first use with the given factory.</summary>
        public ObjectPool<T> For<T>(Func<T> factory, PoolConfig config = default) where T : class
        {
            if (!_typePools.TryGetValue(typeof(T), out object pool))
            {
                pool = new ObjectPool<T>(factory, config);
                _typePools.Add(typeof(T), pool);
            }
            return (ObjectPool<T>)pool;
        }

        /// <summary>Returns an already-registered C# type pool, or null.</summary>
        public ObjectPool<T> For<T>() where T : class
            => _typePools.TryGetValue(typeof(T), out object pool) ? (ObjectPool<T>)pool : null;
    }
}
