using System;
using System.Collections.Concurrent;

namespace RomajiTyping;

public sealed class Pool<T>(Func<T> provider, Action<T>? onRelease = null)
{
    readonly ConcurrentBag<T> bag = new();

    public readonly struct Lease(Pool<T> pool, T value) : IDisposable
    {
        public readonly T Value = value;
        public void Dispose() => pool.Return(Value);
    }

    public Lease Get()
    {
        if (bag.TryTake(out var item)) return new(this, item);
        return new(this, provider());
    }

    public void Return(T value)
    {
        onRelease?.Invoke(value);
        bag.Add(value);
    }
}