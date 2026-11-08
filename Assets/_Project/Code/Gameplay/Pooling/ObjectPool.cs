using System;
using System.Collections.Generic;

namespace Eclipse.Gameplay.Pooling
{
    /// <summary>Pool of plain C# objects. Get/Release do not allocate after warm-up.</summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Stack<T> _idle;
        private readonly HashSet<T> _idleSet;
        private readonly int _maxIdle;

        public int IdleCount => _idle.Count;
        public int TotalCreated { get; private set; }

        public ObjectPool(Func<T> factory, PoolConfig config)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _maxIdle = config.MaxIdle;
            int cap = Math.Max(config.Prewarm, 4);
            _idle = new Stack<T>(cap);
            _idleSet = new HashSet<T>();
            for (int i = 0; i < config.Prewarm; i++)
            {
                T o = Create();
                _idle.Push(o);
                _idleSet.Add(o);
            }
        }

        private T Create()
        {
            TotalCreated++;
            return _factory();
        }

        public T Get()
        {
            T o;
            if (_idle.Count > 0)
            {
                o = _idle.Pop();
                _idleSet.Remove(o);
            }
            else
            {
                o = Create();
            }
            (o as IPoolable)?.OnSpawned();
            return o;
        }

        public void Release(T o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));
            if (_idleSet.Contains(o)) return; // double release ignored
            (o as IPoolable)?.OnDespawned();
            if (_maxIdle > 0 && _idle.Count >= _maxIdle) return;
            _idle.Push(o);
            _idleSet.Add(o);
        }
    }
}
