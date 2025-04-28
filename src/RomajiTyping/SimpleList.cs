using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RomajiTyping
{
    [StructLayout(LayoutKind.Auto)]
    public sealed class SimpleList<T>(int capacity = 8)
    {
        T[] array = new T[capacity];
        int tailIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T element)
        {
            if (array.Length == tailIndex)
            {
                Array.Resize(ref array, tailIndex * 2);
            }

            array[tailIndex] = element;
            tailIndex++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ReadOnlySpan<T> elements)
        {
            if (array.Length < tailIndex + elements.Length)
            {
                EnsureCapacity(tailIndex + elements.Length);
            }

            elements.CopyTo(array.AsSpan(tailIndex));
            tailIndex += elements.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtSwapBack(int index)
        {
            CheckIndex(index);

            array![index] = array[tailIndex - 1];
            array[tailIndex - 1] = default!;
            tailIndex--;
        }


        public void RemoveLast(int count = 1)
        {
            CheckIndex(tailIndex - count);

            tailIndex -= count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            AsSpan().Clear();
            tailIndex = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EnsureCapacity(int capacity)
        {
            var newCapacity = array.Length;
            while (newCapacity < capacity)
            {
                newCapacity *= 2;
            }

            Array.Resize(ref array, newCapacity);
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckIndex(index);
                return ref array[index];
            }
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => tailIndex;
        }

        public int Count => tailIndex;

        public Span<T> AsSpan() => array.AsSpan(0, tailIndex);

        public Span<T> AsSpan(int start) => AsSpan(start, tailIndex - start);

        public Span<T> AsSpan(int start, int length)
        {
            CheckIndex(start + length - 1);
            return array.AsSpan(start, length);
        }

        public T[] GetArray() => array;

        void CheckIndex(int index)
        {
            if (index < 0 || index > tailIndex)
                throwIndexOutOfRangeException();

            void throwIndexOutOfRangeException() => throw new IndexOutOfRangeException();
        }


        public override string ToString()
        {
            return AsSpan().ToString();
        }
    }
}