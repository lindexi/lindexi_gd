using XiaoXiIme.Dictionary;

namespace XiaoXiIme.Dictionary.Tests;

public class XiaoXiImeProjectTermsTests
{
    [Fact]
    public void Parse_WhenCanonicalSourceIsEmbeddedThenCoversRequiredTerms()
    {
        var entries = XiaoXiImeProjectTerms.Parse();

        Assert.Contains(entries, entry => entry.Text == "小希" && entry.Reading == "xiao xi");
        Assert.Contains(entries, entry => entry.Text == "小希" && entry.Reading == "xx");
        Assert.Contains(entries, entry => entry.Text == "小希输入法" && entry.Reading == "xiao xi shu ru fa");
        Assert.Contains(entries, entry => entry.Text == "XiaoXiIme" && entry.Reading == "xiao xi ai mu yi");
        Assert.Contains(entries, entry => entry.Text == "输入法核心" && entry.Reading == "shu ru fa he xin");
        Assert.Contains(entries, entry => entry.Text == "候选窗口" && entry.Reading == "hou xuan chuang kou");
        Assert.Contains(entries, entry => entry.Text == "用户词库" && entry.Reading == "yong hu ci ku");
        Assert.Contains(entries, entry => entry.Text == "词库编译" && entry.Reading == "ci ku bian yi");
        Assert.Contains(entries, entry => entry.Text == "TSF" && entry.Reading == "ti e si e fu");
        Assert.Contains(entries, entry => entry.Text == "IME" && entry.Reading == "ai mu yi");
    }

    [Fact]
    public void CompileAndLoad_WhenCanonicalTermsAreCompiledThenXiaoheUsesProjectedLookupKeys()
    {
        var entries = XiaoXiImeProjectTerms.Parse();
        var root = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.Tests", Guid.NewGuid().ToString("N"));
        var fullPinyinPath = Path.Combine(root, "fullPinyin");
        var xiaohePath = Path.Combine(root, "xiaoheDoublePinyin");
        DictionaryPackageCompiler.Compile(entries, fullPinyinPath);
        DictionaryPackageCompiler.Compile(
            entries,
            xiaohePath,
            new DictionaryPackageParameters { InputScheme = DictionaryPackageLocations.XiaoheDoublePinyinInputScheme });
        var fullPinyin = DictionaryPackageLoader.Load(fullPinyinPath);
        var xiaohe = DictionaryPackageLoader.Load(xiaohePath);

        Assert.Equal("XiaoXiIme", Assert.Single(fullPinyin.Query(new ImeDictionaryQuery("xiaoxiaimuyi"))).Text);
        Assert.Equal("XiaoXiIme", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("xnxiaimuyi"))).Text);
        Assert.Empty(xiaohe.Query(new ImeDictionaryQuery("xiaoxiaimuyi")));
        Assert.Equal("小希", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("xx"))).Text);
        Assert.Equal("IME", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("aimuyi"))).Text);
    }

    [Fact]
    public void WriteTo_WhenPathIsProvidedThenCreatesParseableNativeSource()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.Tests", Guid.NewGuid().ToString("N"), XiaoXiImeProjectTerms.OutputRelativePath);

        XiaoXiImeProjectTerms.WriteTo(path);

        using var reader = File.OpenText(path);
        var entries = PhoneticDictionarySourceParser.Parse(reader, path);
        Assert.Equal(XiaoXiImeProjectTerms.Parse(), entries);
    }
}
