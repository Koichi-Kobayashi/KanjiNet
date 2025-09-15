using System;
using System.IO;

namespace KanjiNet;

internal static class DataPathResolver
{
    /// <summary>
    /// kanjidata 配下のファイルを以下の優先順で探索し、最初に見つかったパスを返します。
    /// 1) 引数がフルパスで存在
    /// 2) 実行ディレクトリ直下
    /// 3) 実行ディレクトリの kanjidata/ 下
    /// 4) リポジトリ構成の KanjiNet/kanjidata/ 下（開発時）
    /// 見つからなければ null。
    /// </summary>
    public static string? TryResolvePath(string pathOrName)
    {
        if (File.Exists(pathOrName)) return pathOrName;

        var baseDir = AppContext.BaseDirectory;

        var candidate = Path.Combine(baseDir, pathOrName);
        if (File.Exists(candidate)) return candidate;

        candidate = Path.Combine(baseDir, "kanjidata", pathOrName);
        if (File.Exists(candidate)) return candidate;

        // リポジトリを想定: .../KanjiNet/KanjiNet/bin/(Debug|Release)/net8.0 → プロジェクトルート/KanjiNet/kanjidata
        try
        {
            var dir = new DirectoryInfo(baseDir);
            // net8.0 → bin → KanjiNet → (プロジェクトルート)
            var projectDir = dir.Parent?.Parent?.Parent; // net8.0 -> bin -> KanjiNet
            if (projectDir is not null)
            {
                candidate = Path.Combine(projectDir.FullName, "kanjidata", pathOrName);
                if (File.Exists(candidate)) return candidate;
            }
        }
        catch { }

        return null;
    }
}


