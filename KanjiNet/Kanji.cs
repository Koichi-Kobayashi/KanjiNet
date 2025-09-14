using System;
using System.Runtime.CompilerServices;

namespace KanjiNet;

/// <summary>
/// 公開API: IsHan ほか（後続で常用・人名用を追加）。
/// </summary>
public static class Kanji
{
    /// <summary>
    /// 文字が漢字ブロック（簡易）に含まれるか。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHan(char ch)
    {
        var scalar = char.ConvertToUtf32(ch.ToString(), 0);
        return UnicodeHelpers.IsHan(scalar);
    }

    /// <summary>
    /// Runeを直接判定したい場合。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHan(int scalar) => UnicodeHelpers.IsHan(scalar);

    // 常用漢字系
    public static bool IsRegularUse(char ch) => RegularUse.IsRegularUse(char.ConvertToUtf32(ch.ToString(), 0));
    public static bool IsStandardRegularUse(char ch) => RegularUse.IsStandardRegularUse(char.ConvertToUtf32(ch.ToString(), 0));
    public static bool IsOldFormRegularUse(char ch) => RegularUse.IsOldFormRegularUse(char.ConvertToUtf32(ch.ToString(), 0));
    public static bool IsTolerableRegularUse(char ch) => RegularUse.IsTolerableRegularUse(char.ConvertToUtf32(ch.ToString(), 0));
    public static bool IsNotRegularUse(char ch) => RegularUse.IsNotRegularUse(char.ConvertToUtf32(ch.ToString(), 0));
    public static string ReplaceNotRegularUseAll(string s, string replacement) => RegularUse.ReplaceNotRegularUseAll(s, replacement);

    // 人名用漢字
    public static bool IsForPersonalNames(char ch) => PersonalNames.IsForPersonalNames(char.ConvertToUtf32(ch.ToString(), 0));
    public static bool IsNotForPersonalNames(char ch) => PersonalNames.IsNotForPersonalNames(char.ConvertToUtf32(ch.ToString(), 0));

    // 旧→新置換
    public static string ReplaceOldToNew(string s) => OldToNewReplacer.Replace(s);

    // Discriminator（Allow/Disallow）
    public sealed class RegularUseDiscriminator
    {
        private readonly RegularUse.Discriminator _inner = new();
        public RegularUseDiscriminator Allow(params char[] chars)
        {
            var scalars = new int[chars.Length];
            for (int i = 0; i < chars.Length; i++) scalars[i] = char.ConvertToUtf32(chars[i].ToString(), 0);
            _inner.Allow(scalars);
            return this;
        }
        public RegularUseDiscriminator Disallow(params char[] chars)
        {
            var scalars = new int[chars.Length];
            for (int i = 0; i < chars.Length; i++) scalars[i] = char.ConvertToUtf32(chars[i].ToString(), 0);
            _inner.Disallow(scalars);
            return this;
        }
        public bool IsNotRegularUse(char ch) => _inner.IsNotRegularUse(char.ConvertToUtf32(ch.ToString(), 0));
        public string ReplaceNotRegularUseAll(string s, string replacement) => _inner.ReplaceNotRegularUseAll(s, replacement);
    }
}


