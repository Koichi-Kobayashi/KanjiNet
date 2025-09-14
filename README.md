[Kanji](https://github.com/ikawaha/kanji) のC#版です。

## Kanji（以下オリジナルを引用しています。）  
[![Go Reference](https://pkg.go.dev/badge/github.com/ikawaha/kanji.svg)](https://pkg.go.dev/github.com/ikawaha/kanji)

===
This package is a library for the Japanese kanji, including the regular-use kanji characters (常用漢字表), etc.

日本語漢字に関するパッケージです。

## 常用漢字

常用漢字は `一般の社会生活において現代の国語を書き表すための漢字使用の目安` として示される漢字の集合で、このパッケージでは [平成22年内閣告示第2号 (2010年11月30日)](https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/kanji/index.html) として告示されているものを対象にしています。

このパッケージで「常用漢字」は、標準字体 2136字、旧字体 364字、許容字体 5字からなる集合として扱っています。標準字体、旧字体、許容字体のそれぞれを `unicode.RangeTable` として定義していますので直接利用可能です。また、これらを扱う関数も定義されています。詳しくは [ドキュメント](https://pkg.go.dev/github.com/ikawaha/kanji) を参照してください。

## 人名用漢字

人名用漢字は、常用漢字以外で子の名に使える漢字の集合のことです。法務省のページに [子の名に使える漢字](http://www.moj.go.jp/MINJI/minji86.html) として定義されています。 人名用漢字を `unicode.RangeTable` として定義していますので直接利用可能です。また、人名に使える漢字であるかどうか（常用漢字であるかまたは人名用漢字であるか）をチェックする関数を用意しています。詳しくは [ドキュメント](https://pkg.go.dev/github.com/ikawaha/kanji) または [ブログ](https://zenn.dev/ikawaha/articles/20210801-e995d788c30ec1) を参照してください。

## 旧字体 -> 新字体 変換

旧字体を新字体に変換するための `strings.Replacer` を用意しています。

---
MIT

## ShiftJISNormalizer（異体/旧字の常用正規化）

人名で頻出する異体字・旧字体・互換漢字などを、常用に正規化するユーティリティです。

### 使用例

```csharp
using System;
using System.Text;

class Program
{
    static void Main()
    {
        // 例: 実ファイル名は適宜変更してください（埋め込み/出力/kanjidata/ から自動解決）
        var mainCsv = "normalization_map_namefirst_491.csv";
        var hotCsv  = "normalization_map_hotset_names.csv";

        var normalizer = ShiftJISNormalizer.FromCsvWithHotset(mainCsv, hotCsv);

        var input = "髙橋﨑太郎と𠮷田さん（濵田）";
        var normalized = normalizer.Normalize(input);

        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine(input);       // 元の文字列
        Console.WriteLine(normalized);  // 正規化後の文字列
        Console.WriteLine("Same after normalize? " + normalizer.Equivalent("髙橋", "高橋"));
        Console.WriteLine($"Counts: {normalizer.Count.hotCount} hot, {normalizer.Count.mainCount} main");
    }
}
```

### ローディングの解決順
- 指定パスがそのまま存在するファイル
- 出力ディレクトリ直下のファイル
- 出力ディレクトリ配下 `kanjidata/` のファイル
- アセンブリ埋め込みリソース `kanjidata.{ファイル名}`

`normalization_map_hotset_names.csv` は埋め込みリソースとして同梱されます。

## NuGet からの導入

[![NuGet](https://img.shields.io/nuget/vpre/Kanji.Net.svg)](https://www.nuget.org/packages/Kanji.Net)

### インストール（.NET CLI）

```bash
dotnet add package Kanji.Net --version 1.0.0-preview.1
```

### インストール（PackageReference）

```xml
<ItemGroup>
  <PackageReference Include="Kanji.Net" Version="1.0.0-preview.1" />
  <!-- 例: Release 版に移行する際は 1.0.0 などに変更してください -->
  
</ItemGroup>
```

インストール後は README の使用例に従って `ShiftJISNormalizer` や `Kanji` API をご利用ください。