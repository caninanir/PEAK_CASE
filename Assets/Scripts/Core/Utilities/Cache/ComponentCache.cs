using System.Collections.Generic;
using UnityEngine;

public static class ComponentCache
{
    private static readonly Dictionary<GameObject, Dictionary<System.Type, Component>> cache = new Dictionary<GameObject, Dictionary<System.Type, Component>>();

    public static T GetComponent<T>(GameObject obj) where T : Component
    {
        if (obj == null) return null;

        if (!cache.ContainsKey(obj))
        {
            cache[obj] = new Dictionary<System.Type, Component>();
        }

        System.Type type = typeof(T);
        if (!cache[obj].ContainsKey(type))
        {
            T component = obj.GetComponent<T>();
            if (component != null)
            {
                cache[obj][type] = component;
            }
            return component;
        }

        return cache[obj][type] as T;
    }

    public static void ClearCache(GameObject obj)
    {
        if (obj != null && cache.ContainsKey(obj))
        {
            cache.Remove(obj);
        }
    }

    public static void ClearAll()
    {
        cache.Clear();
    }
}






