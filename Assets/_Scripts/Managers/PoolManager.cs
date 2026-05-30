using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1. 오브젝트 풀링을 관리하는 매니저
/// 2. 투사체, 몬스터 오브젝트 풀링 등을 처리
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    private Dictionary<Enums.PoolType, object> pools = new Dictionary<Enums.PoolType, object>();

    private void Awake()
    {
        // 씬 내에 오직 하나만 존재하도록 보장
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 오브젝트 풀링이 필요한 곳에서 풀 생성
    public void CreatePool<T>(Enums.PoolType key, 
        T prefab, int initialSize, Transform parent = null) where T : Component
    {
        if (pools.ContainsKey(key))
        {
            Debug.Log("이미 풀이 존재");
            return;
        }

        ObjectPool<T> pool = new ObjectPool<T>(prefab, initialSize, parent);
        pools[key] = pool;
    }

    // 풀에서 꺼내기
    public T Get<T>(Enums.PoolType key) where T : Component
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogError($"{key}에 대한 풀이 없음");
            return null;
        }

        return ((ObjectPool<T>)pools[key]).Get();
    }

    // 풀로 반환
    public void Return<T>(Enums.PoolType key, T obj) where T : Component
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogError($"{key}에 대한 풀이 없음");
            obj.gameObject.SetActive(false);
            return;
        }

        ((ObjectPool<T>)pools[key]).Return(obj);
    }

    private void OnDestroy()
    {
        // 씬이 파괴될 때 정적 참조를 비워주어 메모리 누수 방지
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
