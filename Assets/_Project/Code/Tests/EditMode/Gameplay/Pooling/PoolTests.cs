using System;
using NUnit.Framework;
using UnityEngine;
using Eclipse.Gameplay.Pooling;

namespace Eclipse.Tests.EditMode.Gameplay.Pooling
{
    public class PoolTests
    {
        private sealed class Item : IPoolable
        {
            public int Spawned, Despawned;
            public void OnSpawned() => Spawned++;
            public void OnDespawned() => Despawned++;
        }

        [Test]
        public void Prewarm_CreatesInstancesUpFront()
        {
            var pool = new ObjectPool<Item>(() => new Item(), new PoolConfig(8));
            Assert.AreEqual(8, pool.IdleCount);
            Assert.AreEqual(8, pool.TotalCreated);
        }

        [Test]
        public void GetRelease_ReusesSameInstanceAndFiresCallbacks()
        {
            var pool = new ObjectPool<Item>(() => new Item(), new PoolConfig(1));
            Item a = pool.Get();
            Assert.AreEqual(1, a.Spawned);
            pool.Release(a);
            Assert.AreEqual(1, a.Despawned);
            Item b = pool.Get();
            Assert.AreSame(a, b);
            Assert.AreEqual(1, pool.TotalCreated);
        }

        [Test]
        public void DoubleRelease_IsIgnored()
        {
            var pool = new ObjectPool<Item>(() => new Item(), new PoolConfig(0));
            Item a = pool.Get();
            pool.Release(a);
            pool.Release(a);
            Assert.AreEqual(1, a.Despawned);
            Assert.AreEqual(1, pool.IdleCount);
        }

        [Test]
        public void MaxIdle_DiscardsExtra()
        {
            var pool = new ObjectPool<Item>(() => new Item(), new PoolConfig(0, 1));
            Item a = pool.Get();
            Item b = pool.Get();
            pool.Release(a);
            pool.Release(b);
            Assert.AreEqual(1, pool.IdleCount);
        }

        [Test]
        public void PoolService_ReturnsSamePoolPerType()
        {
            var svc = new PoolService();
            var p1 = svc.For(() => new Item(), new PoolConfig(2));
            var p2 = svc.For<Item>();
            Assert.AreSame(p1, p2);
        }

        [Test]
        public void GameObjectPool_ReusesInstanceAndPrewarms()
        {
            var prefab = new GameObject("PrefabTest");
            var rootGo = new GameObject("PoolRoot");
            try
            {
                var svc = new PoolService(rootGo.transform);
                GameObjectPool pool = svc.ForPrefab(prefab, new PoolConfig(2));
                Assert.AreEqual(2, pool.IdleCount);
                Assert.AreSame(pool, svc.ForPrefab(prefab));

                GameObject a = pool.Get(new Vector3(1, 2, 3), Quaternion.identity);
                Assert.IsTrue(a.activeSelf);
                Assert.AreEqual(new Vector3(1, 2, 3), a.transform.position);
                pool.Release(a);
                Assert.IsFalse(a.activeSelf);
                GameObject b = pool.Get(Vector3.zero, Quaternion.identity);
                Assert.AreSame(a, b);
                Assert.AreEqual(2, pool.TotalCreated);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootGo);
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

#if !UNITY_5_3_OR_NEWER
        [Test]
        public void GetRelease_AfterWarmup_AllocatesZeroBytes()
        {
            var pool = new ObjectPool<Item>(() => new Item(), new PoolConfig(16));
            for (int i = 0; i < 100; i++) pool.Release(pool.Get());
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) pool.Release(pool.Get());
            long after = GC.GetAllocatedBytesForCurrentThread();
            Assert.AreEqual(0, after - before);
        }
#endif
    }
}
