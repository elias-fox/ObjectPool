using System;
using System.Collections.Concurrent;

/// <summary>
/// Maintains a pool of objects for storage prior to reuse. Useful for situations where automatic 
/// garbage collection must be minimized or if creating new objects is expensive.
/// </summary>
/// <typeparam name="T">The type of object to store</typeparam>
public class ObjectPool<T> where T : class
{
    ConcurrentBag<T> pool = new ConcurrentBag<T>();
    Func<T> create = null;
    Action<T> reset = null, activate = null;

    /// <summary>
    /// Create a new object pool containing the given type T.
    /// </summary>
    /// <param name="Create">Function to create and return a new object</param>
    /// <param name="Activate">Method to activate the object when it's requested for use</param>
    /// <param name="Reset">Method to reset the object so it can be reused</param>
    public ObjectPool(Func<T> Create, Action<T> Activate = null, Action<T> Reset = null)
    {
        create = Create ?? throw new ArgumentNullException("Create");
        activate = Activate;
        reset = Reset;
    }

    /// <summary>
    /// Request an item from the pool. If the pool is empty, a new item will be created. 
    /// If an Activate delegate was set the item will be passed to it prior to returning.
    /// </summary>
    /// <returns>The requested object</returns>
    public T Take()
    {
        T item;
        if(pool.IsEmpty) item = create();
        else if(!pool.TryTake(out item)) throw new Exception("TryTake failed on a non-empty bag");
        activate?.Invoke(item);
        return item;
    }

    /// <summary>
    /// Store the item(s) in the object pool for later use. If a Reset delegate was set the item
    /// will be passed to it prior to storage.
    /// </summary>
    /// <param name="item">The items to store.</param>
    public void Return(params T[] item)
    {
        foreach(var t in item)
        {
            reset?.Invoke(t);
            pool.Add(t);
        }
    }

    /// <summary>
    /// Inflate the pool so that it contains at least n items. Any new objects created will not have the 
    /// Activate or Reset methods used on them. 
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public int Prime(int n)
    {
        var diff = n - pool.Count;
        while(pool.Count < n) pool.Add(create());
        return diff;
    }
}
