using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KanjiNet;

/// <summary>
/// Unicode関連ヘルパ（Rune処理・範囲テーブル）。
/// </summary>
internal static class UnicodeHelpers
{
    internal readonly struct Range16(ushort lo, ushort hi, ushort stride)
    {
        public readonly ushort Lo = lo;
        public readonly ushort Hi = hi;
        public readonly ushort Stride = stride;
    }

    internal readonly struct Range32(uint lo, uint hi, uint stride)
    {
        public readonly uint Lo = lo;
        public readonly uint Hi = hi;
        public readonly uint Stride = stride;
    }

    internal sealed class RangeTable
    {
        public Range16[] R16 { get; }
        public Range32[] R32 { get; }

        public RangeTable(Range16[]? r16 = null, Range32[]? r32 = null)
        {
            R16 = r16 ?? Array.Empty<Range16>();
            R32 = r32 ?? Array.Empty<Range32>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsIn(this RangeTable table, int scalar)
    {
        if (scalar <= 0xFFFF)
        {
            var code = (ushort)scalar;
            foreach (var r in table.R16)
            {
                if (code < r.Lo || code > r.Hi) continue;
                var stride = r.Stride == 0 ? (ushort)1 : r.Stride;
                return ((code - r.Lo) % stride) == 0;
            }
            return false;
        }
        else
        {
            var code = (uint)scalar;
            foreach (var r in table.R32)
            {
                if (code < r.Lo || code > r.Hi) continue;
                var stride = r.Stride == 0 ? 1u : r.Stride;
                return ((code - r.Lo) % stride) == 0;
            }
            return false;
        }
    }

    // 簡易Han判定: CJK統合漢字の主要ブロック（U+4E00-U+9FFF）に加え拡張の一部を考慮
    // Goのunicode.Hanは多くのブロックを含むが、ここでは最小限＋必要拡張(Range32)を持つ
    internal static readonly RangeTable Han = new(
        r16: new[]
        {
            new Range16(0x4E00, 0x9FFF, 1), // CJK Unified Ideographs
            new Range16(0x3400, 0x4DBF, 1), // CJK Unified Ideographs Extension A
            new Range16(0xF900, 0xFAFF, 1), // CJK Compatibility Ideographs
        },
        r32: new[]
        {
            new Range32(0x20000, 0x2A6DF, 1), // Extension B
            new Range32(0x2A700, 0x2B73F, 1), // Extension C
            new Range32(0x2B740, 0x2B81F, 1), // Extension D
            new Range32(0x2B820, 0x2CEAF, 1), // Extension E
            new Range32(0x2CEB0, 0x2EBEF, 1), // Extension F
            new Range32(0x30000, 0x3134F, 1), // Extension G
        }
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsHan(int scalar) => Han.IsIn(scalar);
}


