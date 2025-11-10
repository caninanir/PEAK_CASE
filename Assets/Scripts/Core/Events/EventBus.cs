using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    private static readonly Dictionary<Type, List<object>> Subscribers = new Dictionary<Type, List<object>>();

    public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        Type eventType = typeof(T);
        
        if (!Subscribers.ContainsKey(eventType))
        {
            Subscribers[eventType] = new List<object>();
        }
        
        Subscribers[eventType].Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        Type eventType = typeof(T);
        
        if (Subscribers.ContainsKey(eventType))
        {
            Subscribers[eventType].Remove(handler);
        }
    }

    public static void Publish<T>(T eventData) where T : IGameEvent
    {
        Type eventType = typeof(T);
        
        if (!Subscribers.ContainsKey(eventType))
        {
            return;
        }
        
        List<object> handlers = Subscribers[eventType];
        
        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            Action<T> handler = handlers[i] as Action<T>;
            handler?.Invoke(eventData);
        }
    }

    public static void Clear()
    {
        Subscribers.Clear();
    }
}

public interface IGameEvent { }



