using System;
using System.IO;
using Xunit;

namespace KanjiNet.Tests;

public class PublicApiTests
{
    [Fact]
    public void IsHan_BasicAndExtensionB()
    {
        Assert.True(Kanji.IsHan('漢'));
        Assert.False(Kanji.IsHan('A'));
        Assert.True(Kanji.IsHan(0x20000));
    }

    [Fact]
    public void RegularUseDiscriminator_DisallowAndAllow()
    {
        var disallow = new Kanji.RegularUseDiscriminator().Disallow('漢');
        Assert.True(disallow.IsNotRegularUse('漢'));
        var replaced = disallow.ReplaceNotRegularUseAll("漢A漢", "_");
        Assert.Equal("_A_", replaced);

        var allow = new Kanji.RegularUseDiscriminator().Allow('漢');
        Assert.False(allow.IsNotRegularUse('漢'));
        var kept = allow.ReplaceNotRegularUseAll("漢A漢", "_");
        Assert.Equal("漢A漢", kept);
    }

    [Fact]
    public void ReplaceOldToNew_UsesMappingWhenAvailable()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "kanjidata", "golden_old-new.txt");
        if (File.Exists(path))
        {
            Assert.Equal("国体", Kanji.ReplaceOldToNew("國體"));
            Assert.Equal("旧", Kanji.ReplaceOldToNew("舊"));
        }
        else
        {
            Assert.Equal("abc", Kanji.ReplaceOldToNew("abc"));
        }
    }

    [Fact]
    public void To常用漢字()
    {
        Assert.Equal("吉田", Kanji.ReplaceOldToNew("𠮷田"));
        Assert.Equal("富田", Kanji.ReplaceOldToNew("冨田"));
        Assert.Equal("峰", Kanji.ReplaceOldToNew("峯"));
        Assert.Equal("島崎", Kanji.ReplaceOldToNew("嶋﨑"));
        Assert.Equal("徳", Kanji.ReplaceOldToNew("德"));
        Assert.Equal("浜", Kanji.ReplaceOldToNew("濱"));
        Assert.Equal("浜", Kanji.ReplaceOldToNew("濵"));
        Assert.Equal("渡辺", Kanji.ReplaceOldToNew("渡邉"));
        Assert.Equal("渡辺", Kanji.ReplaceOldToNew("渡邊"));
        Assert.Equal("広", Kanji.ReplaceOldToNew("廣"));
        Assert.Equal("斉藤", Kanji.ReplaceOldToNew("齊藤"));
        Assert.Equal("斎藤", Kanji.ReplaceOldToNew("齋藤"));
        Assert.Equal("斎藤", Kanji.ReplaceOldToNew("斎藤"));
        Assert.Equal("桜井", Kanji.ReplaceOldToNew("櫻井"));
        Assert.Equal("沢", Kanji.ReplaceOldToNew("澤"));
        Assert.Equal("関", Kanji.ReplaceOldToNew("關"));
        Assert.Equal("高橋", Kanji.ReplaceOldToNew("髙橋"));
        Assert.Equal("柳", Kanji.ReplaceOldToNew("栁"));
    }

    [Fact]
    public void 名前用異体字_柳から栁へ()
    {
        Assert.Equal("二本栁", Kanji.ReplaceToNameVariant("二本柳"));
        Assert.Equal("栁", Kanji.ReplaceToNameVariant("柳"));
    }
}