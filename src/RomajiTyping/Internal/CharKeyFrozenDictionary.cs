using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RomajiTyping.Internal;

internal sealed class CharKeyFrozenDictionary<TValue>
{
    readonly Entry[] entries;
    readonly short[] buckets; // bucket is index of entries, 1-based(0 for empty).
    readonly int bucketsLength; // power of 2

    CharKeyFrozenDictionary(Entry[] entries, short[] buckets)
    {
        this.entries = entries;
        this.buckets = buckets;
        this.bucketsLength = buckets.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(ushort key, [NotNullWhen(returnValue: true)] out TValue? value)
    {
        ref var bucket = ref buckets[GetBucketIndex(key)];
        var index = bucket - 1;

        // lookup phase
        while (index != -1)
        {
            ref var entry = ref entries[index];
            if (entry.Key == key)
            {
                value = entry.Value!;
                return true;
            }

            index = entry.Next;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    short GetBucketIndex(uint hashCode)
    {
        return (short)(hashCode & (bucketsLength - 1));
    }

    internal struct Builder
    {
        const int MinimumSize = 16; // minimum arraypool size(power of 2)
        const double LoadFactor = 0.72;

        Entry[] entries;
        short[] buckets; // bucket is index of entries, 1-based(0 for empty).
        int bucketsLength; // power of 2
        int entryIndex;
        int resizeThreshold;
        readonly ArrayPool<Entry> entryPool;

        public Builder() : this(ArrayPool<Entry>.Shared)
        {
        }

        public Builder(ArrayPool<Entry> entryPool)
        {
            this.entryPool = entryPool;
            this.buckets = ArrayPool<short>.Shared.Rent(MinimumSize);
            this.entries = this.entryPool.Rent(MinimumSize);
            this.bucketsLength = MinimumSize;
            this.resizeThreshold = (int)(MinimumSize * LoadFactor);
            buckets.AsSpan().Clear(); // 0-clear.
        }


        public ref TValue? GetValueRefOrAddDefault(ushort key, out bool exists)
        {
            ref var bucket = ref buckets[GetBucketIndex(key)];
            var index = bucket - 1;

            // lookup phase
            while (index != -1)
            {
                ref var entry = ref entries[index];
                if (entry.Key == (key))
                {
                    exists = true;
                    return ref entry.Value!;
                }

                index = entry.Next;
            }

            // add phase
            exists = false;
            if (entryIndex > resizeThreshold)
            {
                Resize();
                // Need to recalculate bucket after resize
                bucket = ref buckets[GetBucketIndex(key)];
            }

            ref var newEntry = ref entries[entryIndex];
            newEntry.Key = key;
            newEntry.Value = default!;
            newEntry.Next = (short)(bucket - 1);

            bucket = (short)(entryIndex + 1);
            entryIndex++;

            return ref newEntry.Value!;
        }

        void Resize()
        {
            var newSize = System.Numerics.BitOperations.RoundUpToPowerOf2((uint)entries.Length * 2);
            var newEntries = entryPool.Rent((int)newSize);
            var newBuckets = ArrayPool<short>.Shared.Rent((int)newSize);
            bucketsLength = (int)newSize; // guarantees PowerOf2
            resizeThreshold = (int)(bucketsLength * LoadFactor);
            newBuckets.AsSpan().Clear(); // 0-clear.

            // Copy entries
            Array.Copy(entries, newEntries, entryIndex);

            for (int i = 0; i < entryIndex; i++)
            {
                ref var entry = ref newEntries[i];
                var bucketIndex = GetBucketIndex(entry.Key);

                ref var bucket = ref newBuckets[bucketIndex];
                entry.Next = (short)(bucket - 1);
                bucket = (short)(i + 1);
            }

            // return old arrays
            ArrayPool<short>.Shared.Return(buckets, clearArray: false);
            entryPool.Return(entries, clearArray: false);

            // assign new arrays
            entries = newEntries;
            buckets = newBuckets;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetBucketIndex(ushort hashCode)
        {
            return (hashCode & (bucketsLength - 1));
        }


        public CharKeyFrozenDictionary<TValue> Build()
        {
            var newBuckets = new short[bucketsLength];
            buckets.AsSpan(0, bucketsLength).CopyTo(newBuckets);
            buckets = null!;


            var newEntries = new Entry[entryIndex];
            entries.AsSpan(0, entryIndex).CopyTo(newEntries);
            entryPool.Return(entries, RuntimeHelpers.IsReferenceOrContainsReferences<TValue>());
            entries = null!;

            return new(newEntries, newBuckets);
        }
    }

    public Enumerator GetEnumerator() => new(this);


    [StructLayout(LayoutKind.Auto)]
    public struct Entry
    {
        public ushort Key;
        public short Next; // next is index of entries, -1 is end of chain
        public TValue Value;
    }

    public struct Enumerator(CharKeyFrozenDictionary<TValue> frozenDictionary)
    {
        int index;
        KeyValuePair<char, TValue> pair;

        public bool MoveNext()
        {
            return TryGetNext(out pair);
        }

        public KeyValuePair<char, TValue> Current => pair;


        public bool TryGetNext(out KeyValuePair<char, TValue> current)
        {
            if (index < frozenDictionary.entries.Length)
            {
                ref var entry = ref frozenDictionary.entries[index];
                index++;
                current = new((char)entry.Key, entry.Value!);
                return true;
            }

            current = default;
            return false;
        }
    }
}