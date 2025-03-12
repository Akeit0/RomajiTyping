using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RomajiTyping
{
    [StructLayout(LayoutKind.Auto)]
    public sealed class SimpleStringBuilder
    {
        char[] array;
        int tailIndex;
        public SimpleStringBuilder(int capacity = 8)
        {
            array = new char[capacity];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(char element)
        {
            if (array.Length == tailIndex)
            {
                Array.Resize(ref array, tailIndex * 2);
            }

            array[tailIndex] = element;
            tailIndex++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(ReadOnlySpan<char> elements)
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

        public ref char this[int index]
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

        public Span<char> AsSpan() => array.AsSpan(0, tailIndex);

        public Span<char> AsSpan(int start) => AsSpan(start, tailIndex - start);

        public Span<char> AsSpan(int start, int length)
        {
            CheckIndex(start + length - 1);
            return array.AsSpan(start, length);
        }

        public char[] GetArray() => array;

        void CheckIndex(int index)
        {
            if (index < 0 || index > tailIndex) throw new IndexOutOfRangeException();
        }
        
        public override string ToString()
        {
            return AsSpan().ToString();
        }
    }
}