using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KanjiNet;

#nullable enable

/// <summary>
/// CYO向け 異体/旧字/互換漢字 → 常用(JIS X 0208) 正規化ユーティリティ（Hotset対応）
/// - Unicodeの1文字（Rune）単位で置換（𠮷などのサロゲートも安全）
/// - 二段構え: まず Hotset(人名など頻出) → 次に Main(全体表) の順に検索
/// - CSV形式: target_char,target_codepoint,variant_char,variant_codepoint,category,notes
/// - スレッドセーフ: 生成後は読み取り専用（不変）
/// </summary>
public sealed class ShiftJISNormalizer
{
    private readonly Dictionary<Rune, Rune> _hotMap;   // variant -> target
    private readonly Dictionary<Rune, Rune> _mainMap;  // variant -> target

    private ShiftJISNormalizer(Dictionary<Rune, Rune> hot, Dictionary<Rune, Rune> main)
    {
        _hotMap  = hot;
        _mainMap = main;
    }

    /// <summary>
    /// ホットセット + メインCSVからノーマライザを構築します。
    /// </summary>
    /// <param name="mainCsvPath">全体マップ（例: normalization_map_namefirst_491.csv）</param>
    /// <param name="hotCsvPath">人名ホットセット（例: normalization_map_hotset_names.csv）</param>
    /// <param name="skipHeaderMain">メインCSVの先頭行をヘッダとしてスキップするか</param>
    /// <param name="skipHeaderHot">ホットCSVの先頭行をヘッダとしてスキップするか</param>
    public static ShiftJISNormalizer FromCsvWithHotset(
        string mainCsvPath,
        string hotCsvPath,
        bool skipHeaderMain = true,
        bool skipHeaderHot  = true)
    {
        var hot  = LoadVariantToTargetMapFlexible(hotCsvPath,  skipHeaderHot);
        var main = LoadVariantToTargetMapFlexible(mainCsvPath, skipHeaderMain);

        // Hotsetのエントリはmainと重複していてもOK（Hotset優先で参照される）
        return new ShiftJISNormalizer(hot, main);
    }

    /// <summary>
    /// メインCSVのみから構築（Hotset無し）。
    /// </summary>
    public static ShiftJISNormalizer FromCsv(string mainCsvPath, bool skipHeader = true)
    {
        var main = LoadVariantToTargetMapFlexible(mainCsvPath, skipHeader);
        return new ShiftJISNormalizer(new Dictionary<Rune, Rune>(0), main);
    }

    /// <summary>
    /// 小さな内蔵ホットセット + 空のメイン（お試し用）。
    /// 実運用では FromCsvWithHotset の利用を推奨。
    /// </summary>
    public static ShiftJISNormalizer CreateBuiltinHotsetMinimal()
    {
        var hot = new Dictionary<Rune, Rune>
        {
            [new Rune(0x9AD9)] = new Rune('高'), // 髙→高
            [new Rune(0xFA11)] = new Rune('崎'), // 﨑→崎
            [new Rune(0x20BB7)] = new Rune('吉'), // 𠮷→吉
            [new Rune('濵')] = new Rune('浜'),
            [new Rune('濱')] = new Rune('浜'),
            [new Rune('邊')] = new Rune('辺'),
            [new Rune('邉')] = new Rune('辺'),
            [new Rune('嶋')] = new Rune('島'),
            [new Rune('嶌')] = new Rune('島'),
            [new Rune('齋')] = new Rune('斎'),
            [new Rune('齊')] = new Rune('斉'),
            [new Rune('澤')] = new Rune('沢'),
            [new Rune('德')] = new Rune('徳'),
            [new Rune('櫻')] = new Rune('桜'),
            [new Rune('廣')] = new Rune('広'),
            [new Rune('關')] = new Rune('関'),
            [new Rune('冨')] = new Rune('富'),
            [new Rune('峯')] = new Rune('峰'),
        };
        return new ShiftJISNormalizer(hot, new Dictionary<Rune, Rune>(0));
    }

    /// <summary>
    /// 入力文字列を CYO検索用に正規化します（異体/旧字/互換→常用）。
    /// </summary>
    public string Normalize(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;

        ReadOnlySpan<char> span = input.AsSpan();
        var sb = new StringBuilder(input.Length);

        int i = 0;
        while (i < span.Length)
        {
            if (Rune.TryGetRuneAt(input, i, out var r))
            {
                if (_hotMap.TryGetValue(r, out var mappedHot))
                {
                    sb.Append(mappedHot);
                }
                else if (_mainMap.TryGetValue(r, out var mapped))
                {
                    sb.Append(mapped);
                }
                else
                {
                    sb.Append(r);
                }
                i += r.Utf16SequenceLength;
            }
            else
            {
                // 不正なサロゲート等はそのまま通す
                sb.Append(span[i]);
                i++;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// 2つの文字列が「CYO正規化後に等しいか」を判定します（大小文字などは区別）。
    /// </summary>
    public bool Equivalent(string? a, string? b)
        => string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal);

    /// <summary>
    /// ホットセットにエントリを追加（実行時拡張）。
    /// </summary>
    public ShiftJISNormalizer WithHotMapping(Rune variant, Rune target)
    {
        var hot = new Dictionary<Rune, Rune>(_hotMap);
        hot[variant] = target;
        return new ShiftJISNormalizer(hot, _mainMap);
    }

    /// <summary>
    /// メインマップにエントリを追加（実行時拡張）。
    /// </summary>
    public ShiftJISNormalizer WithMainMapping(Rune variant, Rune target)
    {
        var main = new Dictionary<Rune, Rune>(_mainMap);
        main[variant] = target;
        return new ShiftJISNormalizer(_hotMap, main);
    }

    /// <summary>
    /// 現在の登録件数を返します（デバッグ用）。
    /// </summary>
    public (int hotCount, int mainCount) Count => (_hotMap.Count, _mainMap.Count);

    // ---- CSV ロード ----

    private static Dictionary<Rune, Rune> LoadVariantToTargetMap(string csvPath, bool skipHeader)
    {
        var map = new Dictionary<Rune, Rune>(capacity: 1024);

        using var sr = new StreamReader(csvPath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        string? line;
        bool isFirst = true;

        while ((line = sr.ReadLine()) is not null)
        {
            if (isFirst && skipHeader)
            {
                isFirst = false;
                continue;
            }
            if (line.Length == 0) continue;

            var fields = ParseCsvLine(line);
            if (fields.Length < 3) continue;

            // 想定カラム:
            // 0=target_char, 1=target_codepoint, 2=variant_char, 3=variant_codepoint, 4=category, 5=notes
            var targetStr  = fields[0];
            var variantStr = fields[2];

            if (!TryGetSingleRune(targetStr, out var target))  continue;
            if (!TryGetSingleRune(variantStr, out var variant)) continue;
            if (target == variant) continue; // 無変換行をスキップ

            map[variant] = target;
        }

        return map;
    }

    private static Dictionary<Rune, Rune> LoadVariantToTargetMap(TextReader reader, bool skipHeader)
    {
        var map = new Dictionary<Rune, Rune>(capacity: 1024);
        string? line;
        bool isFirst = true;
        while ((line = reader.ReadLine()) is not null)
        {
            if (isFirst && skipHeader)
            {
                isFirst = false;
                continue;
            }
            if (line.Length == 0) continue;
            var fields = ParseCsvLine(line);
            if (fields.Length < 3) continue;
            var targetStr  = fields[0];
            var variantStr = fields[2];
            if (!TryGetSingleRune(targetStr, out var target))  continue;
            if (!TryGetSingleRune(variantStr, out var variant)) continue;
            if (target == variant) continue;
            map[variant] = target;
        }
        return map;
    }

    private static Dictionary<Rune, Rune> LoadVariantToTargetMapFlexible(string pathOrName, bool skipHeader)
    {
        // 1) パス解決ヘルパーで探索
        var resolved = DataPathResolver.TryResolvePath(pathOrName);
        if (resolved is not null)
        {
            return LoadVariantToTargetMap(resolved, skipHeader);
        }

        // 2) 埋め込みリソースからベース名一致で検索
        var fileName = Path.GetFileName(pathOrName);
        var asm = typeof(ShiftJISNormalizer).Assembly;
        string? resourceName = null;
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (name.EndsWith($"kanjidata.{fileName}", StringComparison.OrdinalIgnoreCase))
            {
                resourceName = name;
                break;
            }
        }
        if (resourceName is not null)
        {
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return LoadVariantToTargetMap(reader, skipHeader);
            }
        }

        throw new FileNotFoundException($"CSV not found (path or embedded): {pathOrName}");
    }

    private static bool TryGetSingleRune(string s, out Rune rune)
    {
        rune = default;
        if (string.IsNullOrEmpty(s)) return false;

        var span = s.AsSpan();
        if (!Rune.TryGetRuneAt(s, 0, out rune)) return false;
        return rune.Utf16SequenceLength == span.Length;
    }

    // 簡易CSVパーサ（カンマ/ダブルクォート対応）
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
                if (c == '\"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        sb.Append('\"');
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
                else if (c == '\"')
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

/* ======================= Usage Sample =======================
using System;
using System.Text;

class Program
{
    static void Main()
    {
        // 例: 実ファイル名は適宜変更してください
        var mainCsv = "cyo_normalization_map_namefirst_491.csv";
        var hotCsv  = "cyo_normalization_map_hotset_names.csv";

        var normalizer = CyoNormalizer.FromCsvWithHotset(mainCsv, hotCsv);

        var input = "髙橋﨑太郎と𠮷田さん（濵田）";
        var normalized = normalizer.Normalize(input);

        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine(input);
        Console.WriteLine(normalized);
        Console.WriteLine("Same after normalize? " + normalizer.Equivalent("髙橋", "高橋"));
        Console.WriteLine($"Counts: {normalizer.Count.hotCount} hot, {normalizer.Count.mainCount} main");
    }
}
============================================================== */
