using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace FlatVenture.NUH.Common.Pooling
{
    /// <summary>
    /// Component 프리팹을 재사용하는 범용 오브젝트 풀입니다.
    /// 검기, 화살, 마법 투사체처럼 반복 생성되는 플레이 오브젝트에 사용합니다.
    /// </summary>
    public sealed class ComponentObjectPool<T> : IDisposable where T : Component
    {
        private readonly T prefab;
        private readonly Transform container;
        private readonly ObjectPool<T> pool;

        public int CountInactive { get { return pool.CountInactive; } }
        public int CountActive { get { return pool.CountActive; } }
        public int CountAll { get { return pool.CountAll; } }

        public ComponentObjectPool(
            T prefab,
            Transform container,
            int defaultCapacity = 8,
            int maxSize = 64)
        {
            this.prefab = prefab != null
                ? prefab
                : throw new ArgumentNullException(nameof(prefab));
            this.container = container;

            pool = new ObjectPool<T>(
                Create,
                OnGet,
                OnRelease,
                OnDestroyItem,
                collectionCheck: true,
                defaultCapacity: Mathf.Max(1, defaultCapacity),
                maxSize: Mathf.Max(defaultCapacity, maxSize));
        }

        public T Get()
        {
            return pool.Get();
        }

        public void Release(T instance)
        {
            if (instance != null)
                pool.Release(instance);
        }

        public void Prewarm(int count)
        {
            int amount = Mathf.Max(0, count);
            List<T> instances = new List<T>(amount);
            for (int i = 0; i < amount; i++)
                instances.Add(pool.Get());

            for (int i = 0; i < instances.Count; i++)
                pool.Release(instances[i]);
        }

        public void Dispose()
        {
            pool.Dispose();
        }

        private T Create()
        {
            T instance = UnityEngine.Object.Instantiate(prefab, container);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private static void OnGet(T instance)
        {
            instance.gameObject.SetActive(true);
        }

        private void OnRelease(T instance)
        {
            instance.gameObject.SetActive(false);
            if (container != null)
                instance.transform.SetParent(container, false);
        }

        private static void OnDestroyItem(T instance)
        {
            if (instance != null)
                UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}
