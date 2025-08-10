using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic object pooling utility for GameObjects.
/// </summary>
public static class SimplePool
{
    private static readonly Dictionary<GameObject, Queue<GameObject>> pools = new();
    private static readonly Dictionary<GameObject, bool> initialized = new();

    /// <summary>
    /// Preloads a number of instances for the given prefab.
    /// </summary>
    public static void Preload(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0)
            return;

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        if (initialized.ContainsKey(prefab))
            return;

        for (int i = 0; i < count; i++)
        {
            var obj = Object.Instantiate(prefab);
            obj.SetActive(false);
            queue.Enqueue(obj);
        }

        initialized[prefab] = true;
    }

    /// <summary>
    /// Requests an instance from the pool or instantiates a new one if empty.
    /// </summary>
    public static GameObject Get(GameObject prefab)
    {
        if (prefab == null)
            return null;

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        if (queue.Count > 0)
        {
            var obj = queue.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return Object.Instantiate(prefab);
    }

    /// <summary>
    /// Returns an instance back to the pool.
    /// </summary>
    public static void Return(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null)
            return;

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        instance.SetActive(false);
        queue.Enqueue(instance);
    }
}
