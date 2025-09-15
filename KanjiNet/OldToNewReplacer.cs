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

        // 1) 最低限のマッピングは組み込みリソースの hotset CSV を読み込む
        LoadHotsetFromEmbedded(dict);

        // 2) より包括的な golden_old-new.txt が存在すれば、その定義で上書き
        var path = DataPathResolver.TryResolvePath("golden_old-new.txt");
        if (path is not null)
        {
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
        }
        return dict;
    }

    private static void LoadHotsetFromEmbedded(Dictionary<int, string> dict)
    {
        var asm = typeof(OldToNewReplacer).Assembly;
        string? resourceName = null;
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (name.EndsWith("kanjidata.normalization_map_hotset_names.csv", StringComparison.OrdinalIgnoreCase))
            {
                resourceName = name;
                break;
            }
        }
        if (resourceName is null)
        {
            var filePath = DataPathResolver.TryResolvePath("normalization_map_hotset_names.csv");
            if (filePath is null) return;
            using var fileReader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true);
            LoadHotsetFromCsvReader(dict, fileReader, hasHeader: true);
            return;
        }

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream is null) return;
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        LoadHotsetFromCsvReader(dict, reader, hasHeader: true);
    }

    private static void LoadHotsetFromCsvReader(Dictionary<int, string> dict, TextReader reader, bool hasHeader)
    {
        string? line;
        bool isFirst = true;
        while ((line = reader.ReadLine()) is not null)
        {
            if (isFirst && hasHeader)
            {
                isFirst = false;
                if (line.StartsWith("target_char")) continue;
            }
            if (line.Length == 0) continue;
            var fields = ParseCsvLine(line);
            if (fields.Length < 3) continue;
            var target = fields[0];
            var variant = fields[2];
            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(variant)) continue;
            int code = char.ConvertToUtf32(variant, 0);
            dict[code] = target;
        }
    }

    private static string[] ParseCsvLine(string line)
    {
        var list = new System.Collections.Generic.List<string>(8);
        var sb = new System.Text.StringBuilder(line.Length);
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    list.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }
        list.Add(sb.ToString());
        return list.ToArray();
    }
}


