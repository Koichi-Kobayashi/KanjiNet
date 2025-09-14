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
        var path = Path.Combine(AppContext.BaseDirectory, "testdata", "golden_old-new.txt");
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
}