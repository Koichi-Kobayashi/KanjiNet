using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KanjiNet;

/// <summary>
/// 人名向けの異体字（例: 柳→栁）への変換を行うユーティリティ。
/// - CSVの category が "itaiji" の行のみを対象に、target(常用)→variant(異体) のマップを構築
/// - Hotset を優先（Hotsetにある異体を優先採用し、無い場合はメイン表から補完）
/// </summary>
internal static class NameVariantExpander
{
    private static readonly Lazy<Dictionary<int, string>> Map = new(BuildMap);

    public static string ReplaceToNameVariant(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var map = Map.Value;
        var sb = new StringBuilder(s.Length);
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
        // target(常用)→variant(異体). 1:Hotsetを先にロード（優先）、2:メインで補完
        var dict = new Dictionary<int, string>();

        // Hotset（例: normalization_map_hotset_names.csv）
        LoadItaijiTargetToVariant(dict, "normalization_map_hotset_names.csv", prefer: true);

        // メイン（例: normalization_map_namefirst_491.csv）
        LoadItaijiTargetToVariant(dict, "normalization_map_namefirst_491.csv", prefer: false);

        return dict;
    }

    private static void LoadItaijiTargetToVariant(Dictionary<int, string> dict, string pathOrName, bool prefer)
    {
        // 1) 物理パス解決
        var resolved = DataPathResolver.TryResolvePath(pathOrName);
        if (resolved is not null)
        {
            using var sr = new StreamReader(resolved, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            LoadFromCsvReader(dict, sr, skipHeader: true, prefer: prefer);
            return;
        }

        // 2) 埋め込みリソース
        var fileName = Path.GetFileName(pathOrName);
        var asm = typeof(NameVariantExpander).Assembly;
        string? resourceName = null;
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (name.EndsWith($"kanjidata.{fileName}", StringComparison.OrdinalIgnoreCase))
            {
                resourceName = name;
                break;
            }
        }
        if (resourceName is null) return;
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream is null) return;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        LoadFromCsvReader(dict, reader, skipHeader: true, prefer: prefer);
    }

    private static void LoadFromCsvReader(Dictionary<int, string> dict, TextReader reader, bool skipHeader, bool prefer)
    {
        string? line;
        bool isFirst = true;
        while ((line = reader.ReadLine()) is not null)
        {
            if (isFirst && skipHeader)
            {
                isFirst = false;
                // ヘッダ行に target_char が含まれる形式を想定
                if (line.StartsWith("target_char")) continue;
            }
            if (line.Length == 0) continue;

            var fields = ParseCsvLine(line);
            if (fields.Length < 5) continue;

            // 想定カラム: 0=target_char, 1=target_codepoint, 2=variant_char, 3=variant_codepoint, 4=category, 5=notes
            var category = fields[4];
            if (!IsItaijiCategory(category)) continue;

            var targetStr  = fields[0];
            var variantStr = fields[2];

            if (!TryGetSingleRune(targetStr, out var target))  continue;
            if (!TryGetSingleRune(variantStr, out var variant)) continue;
            if (target == variant) continue;

            int key = target.Value;
            string value = variant.ToString();

            if (prefer)
            {
                // Hotsetは常に上書き優先
                dict[key] = value;
            }
            else
            {
                // メインは未定義の時のみ補完
                if (!dict.ContainsKey(key)) dict[key] = value;
            }
        }
    }

    private static bool IsItaijiCategory(string category)
        => category?.Trim().Equals("itaiji", StringComparison.OrdinalIgnoreCase) == true;

    private static bool TryGetSingleRune(string s, out Rune rune)
    {
        rune = default;
        if (string.IsNullOrEmpty(s)) return false;
        if (!Rune.TryGetRuneAt(s, 0, out rune)) return false;
        return rune.Utf16SequenceLength == s.Length;
    }

    // 簡易CSVパーサ（ダブルクォート/カンマ対応）
    private static string[] ParseCsvLine(string line)
    {
        var list = new List<string>(8);
        var sb = new StringBuilder(line.Length);
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


