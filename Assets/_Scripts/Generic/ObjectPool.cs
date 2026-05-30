using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private Queue<T> pool = new Queue<T>();         // 미리 생성한 프리팹을 저장할 큐
    private T prefab;                               // 생성할 프리팹
    private Transform parent;                       // 프리팹이 생성될 부모 오브젝트 Transform

    // 생성자에서 처음 크기만큼 프리팹을 생성하여 pool에 넣어주기
    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }

    }

    // pool에 프리팹이 있다면 프리팹 전달, 없다면 새로 생성
    public T Get()
    {
        if (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        else
        {
            T newObj = Object.Instantiate(prefab, parent);
            return newObj;
        }
    }

    // 리턴된 오브젝트는 비활성화 시킨 뒤 pool에 넣음
    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }

}
