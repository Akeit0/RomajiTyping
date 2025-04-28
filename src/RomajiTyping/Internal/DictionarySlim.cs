// MIT License
// Copyright (c) Yoshifumi Kawai
// Copyright (c) Cysharp, Inc.
// borrowed from
//https://github.com/Cysharp/ZLinq/blob/main/src/ZLinq/Internal/DictionarySlim.cs

#pragma warning disable 0436
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RomajiTyping.Internal;

// Lightweight dictionary implementation optimized for performance
// with minimal API surface focused on the specific use case
internal struct DictionarySlim<TKey, TValue> : IDisposable where TKey : IEquatable<TKey> // allows TKey null
{
    const int MinimumSize = 16; // minimum arraypool size(power of 2)
    const double LoadFactor = 0.72;

    Entry[] entries;
    int[] buckets; // bucket is index of entries, 1-based(0 for empty).
    int bucketsLength; // power of 2
    int entryIndex;
    int resizeThreshold;

    public DictionarySlim()
    {
        this.buckets = ArrayPool<int>.Shared.Rent(MinimumSize);
        this.entries = ArrayPool<Entry>.Shared.Rent(MinimumSize);
        this.bucketsLength = MinimumSize;
        this.resizeThreshold = (int)(bucketsLength * LoadFactor);
        buckets.AsSpan().Clear(); // 0-clear.
    }

    public TValue this[TKey key] => GetValueRefOrAddDefault(key, out _)!;

    public ref TValue? GetValueRefOrAddDefault(TKey key, out bool exists)
    {
        var hashCode = InternalGetHashCode(key);
        ref var bucket = ref buckets[GetBucketIndex(hashCode)];
        var index = bucket - 1;

        // lookup phase
        while (index != -1)
        {
            ref var entry = ref entries[index];
            if (entry.HashCode == hashCode && entry.Key.Equals(key))
            {
                exists = true;
                return ref entry.Value;
            }

            index = entry.Next;
        }

        // add phase
        exists = false;
        if (entryIndex > resizeThreshold)
        {
            Resize();
            // Need to recalculate bucket after resize
            bucket = ref buckets[GetBucketIndex(hashCode)];
        }

        ref var newEntry = ref entries[entryIndex];
        newEntry.HashCode = hashCode;
        newEntry.Key = key;
        newEntry.Value = default;
        newEntry.Next = bucket - 1;

        bucket = entryIndex + 1;
        entryIndex++;

        return ref newEntry.Value;
    }

    void Resize()
    {
        var newSize = System.Numerics.BitOperations.RoundUpToPowerOf2((uint)entries.Length * 2);
        var newEntries = ArrayPool<Entry>.Shared.Rent((int)newSize);
        var newBuckets = ArrayPool<int>.Shared.Rent((int)newSize);
        bucketsLength = (int)newSize; // guarantees PowerOf2
        resizeThreshold = (int)(bucketsLength * LoadFactor);
        newBuckets.AsSpan().Clear(); // 0-clear.

        // Copy entries
        Array.Copy(entries, newEntries, entryIndex);

        for (int i = 0; i < entryIndex; i++)
        {
            ref var entry = ref newEntries[i];
            var bucketIndex = GetBucketIndex(entry.HashCode);

            ref var bucket = ref newBuckets[bucketIndex];
            entry.Next = bucket - 1;
            bucket = i + 1;
        }

        // return old arrays
        ArrayPool<int>.Shared.Return(buckets, clearArray: false);
        ArrayPool<Entry>.Shared.Return(entries, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<Entry>());

        // assign new arrays
        entries = newEntries;
        buckets = newBuckets;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    uint InternalGetHashCode(TKey key)
    {
        // allows null.
        return (uint)(key.GetHashCode() & 0x7FFFFFFF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int GetBucketIndex(uint hashCode)
    {
        return (int)(hashCode & (bucketsLength - 1));
    }

    // return to pool
    public void Dispose()
    {
        if (buckets != null)
        {
            ArrayPool<int>.Shared.Return(buckets, clearArray: false);
            buckets = null!;
        }

        if (entries != null)
        {
            ArrayPool<Entry>.Shared.Return(entries, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<Entry>());
            entries = null!;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [DebuggerDisplay("HashCode = {HashCode}, Key = {Key}, Value =  {Value}, Next = {Next}")]
    struct Entry
    {
        public uint HashCode;
        public TKey Key;
        public TValue? Value;
        public int Next; // next is index of entries, -1 is end of chain
    }
}