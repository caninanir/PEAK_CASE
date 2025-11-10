using System.Collections.Generic;
using UnityEngine;

public class GenericPool<T> where T : Component, IPoolable
{
    private readonly Queue<T> pool = new Queue<T>();
    private readonly T prefab;
    private readonly Transform parent;
    private readonly int initialSize;
    private int totalCreated = 0;

    public GenericPool(T prefab, Transform parent, int initialSize = 10)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.initialSize = initialSize;
        
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    public T Get()
    {
        T obj = null;
        
        while (pool.Count > 0 && obj == null)
        {
            obj = pool.Dequeue();
            if (obj == null || obj.gameObject == null)
            {
                obj = null;
                continue;
            }
        }
        
        if (obj == null)
        {
            obj = CreateNewObject();
        }
        
        obj.gameObject.SetActive(true);
        obj.OnSpawn();
        return obj;
    }

    public void Return(T obj)
    {
        if (obj == null || obj.gameObject == null) return;
        
        obj.OnDespawn();
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }

    private T CreateNewObject()
    {
        T obj = Object.Instantiate(prefab, parent);
        obj.gameObject.SetActive(false);
        totalCreated++;
        return obj;
    }

    public void Clear()
    {
        while (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            if (obj != null)
            {
                Object.Destroy(obj.gameObject);
            }
        }
        totalCreated = 0;
    }

    public void WarmUp(int count)
    {
        int needed = count - pool.Count;
        for (int i = 0; i < needed; i++)
        {
            CreateNewObject();
        }
    }

    public int PoolSize => pool.Count;
    public int TotalCreated => totalCreated;
    public int ActiveCount => totalCreated - pool.Count;
}




