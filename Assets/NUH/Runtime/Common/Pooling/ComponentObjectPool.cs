using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Component 프리팹을 재사용하는 범용 오브젝트 풀입니다.
/// 검기, 화살, 마법 투사체처럼 반복 생성되는 플레이 오브젝트에 사용합니다.
/// </summary>
public sealed class ComponentObjectPool<T> : IDisposable where T : Component
{
    // 새 인스턴스가 필요할 때 복제할 원본 프리팹입니다.
    private readonly T prefab;
    // 반환된 오브젝트를 Hierarchy에서 모아 둘 부모입니다.
    private readonly Transform container;
    // Unity가 제공하는 실제 풀 자료구조입니다.
    private readonly ObjectPool<T> pool;

    public int CountInactive { get { return pool.CountInactive; } }
    public int CountActive { get { return pool.CountActive; } }
    public int CountAll { get { return pool.CountAll; } }

    /// <summary>프리팹과 생성·대여·반환·파괴 콜백을 Unity ObjectPool에 등록합니다.</summary>
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

    /// <summary>비활성 인스턴스를 재사용하거나 없으면 새로 생성해 반환합니다.</summary>
    public T Get()
    {
        return pool.Get();
    }

    /// <summary>사용이 끝난 인스턴스를 비활성화해 풀로 돌려보냅니다.</summary>
    public void Release(T instance)
    {
        if (instance != null)
            pool.Release(instance);
    }

    /// <summary>게임 시작 중 Instantiate 끊김을 줄이기 위해 지정 수만큼 미리 생성합니다.</summary>
    public void Prewarm(int count)
    {
        int amount = Mathf.Max(0, count);
        List<T> instances = new List<T>(amount);
        for (int i = 0; i < amount; i++)
            instances.Add(pool.Get());

        for (int i = 0; i < instances.Count; i++)
            pool.Release(instances[i]);
    }

    /// <summary>풀이 소유한 모든 인스턴스와 내부 자원을 정리합니다.</summary>
    public void Dispose()
    {
        pool.Dispose();
    }

    /// <summary>풀에 여유 인스턴스가 없을 때 프리팹을 새로 복제합니다.</summary>
    private T Create()
    {
        T instance = UnityEngine.Object.Instantiate(prefab, container);
        instance.gameObject.SetActive(false);
        return instance;
    }

    /// <summary>대여되는 오브젝트를 활성화합니다.</summary>
    private static void OnGet(T instance)
    {
        instance.gameObject.SetActive(true);
    }

    /// <summary>반환되는 오브젝트를 비활성화하고 풀 컨테이너 아래로 옮깁니다.</summary>
    private void OnRelease(T instance)
    {
        instance.gameObject.SetActive(false);
        if (container != null)
            instance.transform.SetParent(container, false);
    }

    /// <summary>최대 풀 크기를 넘거나 풀이 폐기될 때 GameObject를 파괴합니다.</summary>
    private static void OnDestroyItem(T instance)
    {
        if (instance != null)
            UnityEngine.Object.Destroy(instance.gameObject);
    }
}
