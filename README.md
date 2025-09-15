[Kanji](https://github.com/ikawaha/kanji) のC#版です。

## Kanji（以下オリジナルを引用しています。）  
[![Go Reference](https://pkg.go.dev/badge/github.com/ikawaha/kanji.svg)](https://pkg.go.dev/github.com/ikawaha/kanji)

---
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
        // 例: 実ファイル名は適宜変更してください（出力/kanjidata/ などから自動解決）
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

### ホットセット優先/メイン優先の切替

```csharp
var mainCsv = "normalization_map_namefirst_491.csv";
var hotCsv  = "normalization_map_hotset_names.csv";

// デフォルト（ホットセット優先）
var normalizerHotFirst = ShiftJISNormalizer.FromCsvWithHotset(mainCsv, hotCsv, preferHotset: true);

// メイン優先にしたい場合
var normalizerMainFirst = ShiftJISNormalizer.FromCsvWithHotset(mainCsv, hotCsv, preferHotset: false);
```

### ローディングの解決順
- 指定パスがそのまま存在するファイル
- 出力ディレクトリ直下のファイル
- 出力ディレクトリ配下 `kanjidata/` のファイル
- リポジトリの `KanjiNet/kanjidata/` のファイル（開発時）

注: NuGet から導入した場合は、パッケージに含まれる `kanjidata/` がビルド時に出力フォルダ直下へ自動展開されます（contentFiles）。そのため使用例ではファイル名だけで解決できます。


## 人名向け 異体字展開（常用→異体）

人名で好まれる異体字に変換するAPIを提供します。例: 「柳」→「栁」。

### 使用例

```csharp
// 常用→人名向け異体に置換（Hotset優先、CSVから自動解決）
var s1 = Kanji.ReplaceToNameVariant("二本柳"); // => "二本栁"
var s2 = Kanji.ReplaceToNameVariant("柳");     // => "栁"
```

- 対象はCSVのカテゴリーが「itaiji」の行です。
  - ホットセット: `normalization_map_hotset_names.csv`
  - メイン表: `normalization_map_namefirst_491.csv`
  - 解決順は `DataPathResolver` に従います（出力直下 → 出力/kanjidata → リポジトリの `KanjiNet/kanjidata/` → 埋め込み）。
  - ホットセットに定義がある場合はそちらを優先し、無い場合にメイン表で補完します。

注意:
- 既存の `Kanji.ReplaceOldToNew` は逆方向（異体/旧→常用）です。用途に応じて使い分けてください。

## NuGet からの導入

[![NuGet](https://img.shields.io/nuget/vpre/Kanji.Net.svg)](https://www.nuget.org/packages/Kanji.Net)

### インストール（.NET CLI）

```bash
dotnet add package Kanji.Net --version 1.0.0-preview.4
```

### インストール（PackageReference）

```xml
<ItemGroup>
  <PackageReference Include="Kanji.Net" Version="1.0.0-preview.4" />
  <!-- 例: Release 版に移行する際は 1.0.0 などに変更してください -->
  
  
  
  
</ItemGroup>
```

インストール後は README の使用例に従って `ShiftJISNormalizer` や `Kanji` API をご利用ください。

### kanjidata の自動展開（NuGet）

NuGet パッケージには `kanjidata/` 配下のデータが含まれており、ビルド時に出力フォルダ直下の `kanjidata/` に自動コピーされます。したがって、使用例のとおりファイル名（例: `normalization_map_namefirst_491.csv`）だけを渡せば `DataPathResolver` が見つけます。

## ファイルとSQL Server上の異体字を照合する実装例

### 前提
- `ShiftJISNormalizer` を使い、異体/旧字/互換漢字を常用に正規化します。
  - ホットセット（人名頻出）+ メインCSVを指定すると網羅性が上がります。
  - ファイル解決は「指定パス → 出力直下 → 出力/kanjidata → リポジトリの `KanjiNet/kanjidata/`（開発時）」の順で自動解決されます。NuGet 導入時はビルドで `kanjidata/` が自動配置されます。

```csharp
var mainCsv = "normalization_map_namefirst_491.csv";
var hotCsv  = "normalization_map_hotset_names.csv";
var normalizer = ShiftJISNormalizer.FromCsvWithHotset(mainCsv, hotCsv);
// メイン優先にしたい場合: ShiftJISNormalizer.FromCsvWithHotset(mainCsv, hotCsv, preferHotset: false);
```

### 1) その場照合（アプリ側で等価判定）
- ファイルに「高橋」とあっても、DB側が「髙橋」でも一致させたい場合:

```csharp
// ファイル側の値
string text = "高橋";

// DBから取得した候補行に対して正規化等価判定
foreach (var row in rowsFromDb)
{
    if (normalizer.Equivalent(row.Name, text))
    {
        // 「髙橋」「﨑」「𠮷」など異体字でも一致
        // ヒット処理...
    }
}

// 確認例
bool same = normalizer.Equivalent("髙橋", "高橋"); // true
```

### 2) 正規化列を持って索引検索（推奨）
- 事前に正規化済み列を用意し、= 検索で高速一致させます。

```sql
-- 例: 正規化列とインデックスを追加
ALTER TABLE dbo.People ADD Name_TextNorm NVARCHAR(200) NULL;
CREATE INDEX IX_People_Name_TextNorm ON dbo.People(Name_TextNorm);
```

```csharp
// 初期移行（全行）
foreach (var row in rowsFromDb)
{
    row.Name_TextNorm = normalizer.Normalize(row.Name);
    // UPDATE dbo.People SET Name_TextNorm = @row.Name_TextNorm WHERE Id = @row.Id;
}

// 照合時
string key = normalizer.Normalize("高橋");
// SELECT * FROM dbo.People WHERE Name_TextNorm = @key;
```

ポイント:
- `Normalize` は文字列を常用に寄せ、`Equivalent(a,b)` は「正規化後に等しいか」を判定します。
- CSVを用意できない場合の簡易用途は `ShiftJISNormalizer.CreateBuiltinHotsetMinimal()` でも検証可能です。

---
MIT
