using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RomajiTyping.Internal;

internal class SparseUShortBitSet(CharKeyFrozenDictionary<ulong> frozenDictionary)
{
    // 1ブロックあたり 64ビット扱う
    const int BitsPerBlock = sizeof(ulong) * 8;

    /// <summary>
    /// value を表すビットを立てる（true にする）。
    /// </summary>
    internal static void Set(ref CharKeyFrozenDictionary<ulong>.Builder builder, ushort value)
    {
        int blockIndex = value >> sizeof(ulong);
        int offset = value & (BitsPerBlock - 1); // 0..63

        // ブロックが存在しなければ新規作成（=0）
        ref var block = ref builder.GetValueRefOrAddDefault((ushort)blockIndex, out _);

        // 指定ビットを立てる
        block |= (1UL << offset);
    }


    /// <summary>
    /// value を表すビットが立っているかどうか判定する。
    /// </summary>
    public bool IsSet(ushort value)
    {
        int blockIndex = value >> sizeof(ulong);
        int offset = value & (BitsPerBlock - 1); // 0..63

        if (frozenDictionary.TryGetValue((ushort)blockIndex, out ulong block))
        {
            return (block & (1UL << offset)) != 0;
        }

        Console.WriteLine($"Is not set: {(char)value}");

        return false;
    }
}