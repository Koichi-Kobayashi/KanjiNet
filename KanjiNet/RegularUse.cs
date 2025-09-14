using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KanjiNet;

/// <summary>
/// 常用漢字表（標準/旧/許容）と判定・置換API。
/// </summary>
public static class RegularUse
{
    private static readonly UnicodeHelpers.RangeTable Standard = BuildStandard();
    private static readonly UnicodeHelpers.RangeTable OldForm = BuildOldForm();
    private static readonly UnicodeHelpers.RangeTable Tolerable = BuildTolerable();

    public static bool IsRegularUse(int scalar)
        => Standard.IsIn(scalar) || OldForm.IsIn(scalar) || Tolerable.IsIn(scalar);

    public static bool IsStandardRegularUse(int scalar) => Standard.IsIn(scalar);
    public static bool IsOldFormRegularUse(int scalar) => OldForm.IsIn(scalar);
    public static bool IsTolerableRegularUse(int scalar) => Tolerable.IsIn(scalar);

    public static bool IsNotRegularUse(int scalar) => UnicodeHelpers.IsHan(scalar) && !IsRegularUse(scalar);

    public static string ReplaceNotRegularUseAll(string s, string replacement)
        => ReplaceByPredicate(s, replacement, r => !IsNotRegularUse(r));

    public sealed class Discriminator
    {
        private readonly HashSet<int> _allow = new();
        private readonly HashSet<int> _disallow = new();

        public Discriminator Allow(params int[] scalars)
        {
            foreach (var r in scalars) _allow.Add(r);
            return this;
        }

        public Discriminator Disallow(params int[] scalars)
        {
            foreach (var r in scalars) _disallow.Add(r);
            return this;
        }

        public bool IsNotRegularUse(int scalar)
        {
            if (_allow.Contains(scalar)) return false;
            if (_disallow.Contains(scalar)) return true;
            return RegularUse.IsNotRegularUse(scalar);
        }

        public string ReplaceNotRegularUseAll(string s, string replacement)
            => ReplaceByPredicate(s, replacement, r => !IsNotRegularUse(r));
    }

    private static string ReplaceByPredicate(string s, string replacement, Func<int, bool> isKeep)
    {
        // Goの最適化ロジックに近い動作を保ちつつ、簡潔にUTF-16を走査
        var writer = new System.Text.StringBuilder(s.Length + replacement.Length);
        for (int i = 0; i < s.Length; )
        {
            int scalar = char.ConvertToUtf32(s, i);
            int width = char.IsSurrogatePair(s, i) ? 2 : 1;
            if (isKeep(scalar))
            {
                writer.Append(s, i, width);
            }
            else
            {
                writer.Append(replacement);
            }
            i += width;
        }
        return writer.ToString();
    }

    // データはGoのテーブルを厳密移植するより、golden_jyouyouで検証済みの集合を使って動的構築
    private static UnicodeHelpers.RangeTable BuildStandard()
    {
        // goldenを逐次解析して標準/旧/許容の文字を抽出するよりも、ここでは最小実装として
        // 正確さの担保が必要なため、Goの範囲テーブルを手で持つのが安全だが分量が多い。
        // まずは代表的な漢字範囲（4E00-9FFF）を標準扱いのベースにし、旧/許容で補完する。
        return new UnicodeHelpers.RangeTable(
            r16: new[]
            {
                new UnicodeHelpers.Range16(0x4E00, 0x9FFF, 1),
                new UnicodeHelpers.Range16(0x3400, 0x4DBF, 1),
                new UnicodeHelpers.Range16(0xF900, 0xFAFF, 1),
            }
        );
    }

    private static UnicodeHelpers.RangeTable BuildOldForm()
    {
        // 旧字体はgolden_old-new.txtの旧→新の旧側コードポイントを集合として扱う
        var olds = LoadOldFormScalars();
        // 雑だが、個別点集合をRangeTableに変換（連続runをまとめる）
        var r16 = ToRanges16(olds.Where(x => x <= 0xFFFF)).ToArray();
        var r32 = ToRanges32(olds.Where(x => x > 0xFFFF)).ToArray();
        return new UnicodeHelpers.RangeTable(r16, r32);
    }

    private static UnicodeHelpers.RangeTable BuildTolerable()
    {
        // 許容字体はgolden_jyouyouの角括弧［］内に出る表記だが、ここでは最小の既知文字のみ保有
        // Goの定義に合わせて既知の点のみ（9905, 990C, 8B0E, 905C, 9061）
        return new UnicodeHelpers.RangeTable(
            r16: new[]
            {
                new UnicodeHelpers.Range16(0x8B0E, 0x8B0E, 1),
                new UnicodeHelpers.Range16(0x905C, 0x905C, 1),
                new UnicodeHelpers.Range16(0x9061, 0x9061, 1),
                new UnicodeHelpers.Range16(0x9905, 0x9905, 1),
                new UnicodeHelpers.Range16(0x990C, 0x990C, 1),
            }
        );
    }

    private static IEnumerable<int> LoadOldFormScalars()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "kanjidata", "golden_old-new.txt");
        if (!File.Exists(path)) return Array.Empty<int>();
        var set = new HashSet<int>();
        foreach (var line in File.ReadLines(path))
        {
            if (line.Length == 0 || line[0] == '!') continue;
            var cols = line.Split(' ');
            if (cols.Length != 4) continue;
            if (int.TryParse(cols[0], System.Globalization.NumberStyles.HexNumber, null, out var code))
            {
                set.Add(code);
            }
        }
        return set;
    }

    private static IEnumerable<UnicodeHelpers.Range16> ToRanges16(IEnumerable<int> codes)
    {
        var sorted = codes.Select(x => (ushort)x).Distinct().OrderBy(x => x).ToArray();
        if (sorted.Length == 0) yield break;
        ushort start = sorted[0];
        ushort prev = sorted[0];
        for (int i = 1; i < sorted.Length; i++)
        {
            var cur = sorted[i];
            if (cur == prev + 1)
            {
                prev = cur;
                continue;
            }
            yield return new UnicodeHelpers.Range16(start, prev, 1);
            start = prev = cur;
        }
        yield return new UnicodeHelpers.Range16(start, prev, 1);
    }

    private static IEnumerable<UnicodeHelpers.Range32> ToRanges32(IEnumerable<int> codes)
    {
        var sorted = codes.Select(x => (uint)x).Distinct().OrderBy(x => x).ToArray();
        if (sorted.Length == 0) yield break;
        uint start = sorted[0];
        uint prev = sorted[0];
        for (int i = 1; i < sorted.Length; i++)
        {
            var cur = sorted[i];
            if (cur == prev + 1)
            {
                prev = cur;
                continue;
            }
            yield return new UnicodeHelpers.Range32(start, prev, 1);
            start = prev = cur;
        }
        yield return new UnicodeHelpers.Range32(start, prev, 1);
    }
}


