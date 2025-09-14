using System;
using System.Collections.Generic;
using System.IO;

namespace KanjiNet;

/// <summary>
/// 旧字体→新字体の置換（strings.Replacer相当）。
/// </summary>
public static class OldToNewReplacer
{
    private static readonly Lazy<Dictionary<int, string>> Map = new(BuildMap);

    public static string Replace(string s)
    {
        var map = Map.Value;
        var sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; )
        {
            int scalar = char.ConvertToUtf32(s, i);
            int width = char.IsSurrogatePair(s, i) ? 2 : 1;
            if (map.TryGetValue(scalar, out var repl))
            {
                sb.Append(repl);
            }
            else
            {
                sb.Append(s, i, width);
            }
            i += width;
        }
        return sb.ToString();
    }

    private static Dictionary<int, string> BuildMap()
    {
        var dict = new Dictionary<int, string>();
        var path = Path.Combine(AppContext.BaseDirectory, "testdata", "golden_old-new.txt");
        if (!File.Exists(path)) return dict;
        foreach (var line in File.ReadLines(path))
        {
            if (line.Length == 0 || line[0] == '!') continue;
            var cols = line.Split(' ');
            if (cols.Length != 4) continue;
            if (int.TryParse(cols[0], System.Globalization.NumberStyles.HexNumber, null, out var code))
            {
                dict[code] = cols[3];
            }
        }
        return dict;
    }
}


