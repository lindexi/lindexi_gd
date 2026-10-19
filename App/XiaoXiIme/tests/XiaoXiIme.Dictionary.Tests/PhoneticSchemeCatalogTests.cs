using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class PhoneticSchemeCatalogTests
{
    [Fact]
    public void WhenCatalogIsReadThenProductOrderContainsExactlyEightDoublePinyinSchemes()
    {
        Assert.Equal(new[] { "fullPinyin", "sogouDoublePinyin", "microsoftDoublePinyin", "naturalCodeDoublePinyin", "abcDoublePinyin", "daniuDoublePinyin", "jiajiaDoublePinyin", "xiaoheDoublePinyin", "xiaoyueDoublePinyin" },
            PhoneticSchemeCatalog.All.Select(scheme => scheme.SchemeId));
    }

    [Theory]
    [InlineData("fullPinyin", ImeProductInputScheme.FullPinyin)]
    [InlineData("xiaoheDoublePinyin", ImeProductInputScheme.XiaoheDoublePinyin)]
    public void WhenExistingSchemeIsReadThenPersistedIdentityIsPreserved(string id, ImeProductInputScheme expected)
    {
        Assert.Equal(expected, PhoneticSchemeCatalog.All.Single(scheme => scheme.SchemeId == id).ProductScheme);
    }

    [Theory]
    [InlineData("daniuDoublePinyin")]
    [InlineData("xiaoyueDoublePinyin")]
    public void WhenSourceIsUnavailableThenSchemeCannotBeSelected(string id)
    {
        Assert.Null(PhoneticSchemeCatalog.All.Single(scheme => scheme.SchemeId == id).ProductScheme);
    }

    [Fact]
    public void WhenXiaoheBaselineIsReadThenCompleteRulesHaveAReproducibleFingerprint()
    {
        var scheme = PhoneticSchemeCatalog.All.Single(scheme => scheme.SchemeId == "xiaoheDoublePinyin");
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(scheme.Rules!))), scheme.ContentSha256);
    }

    [Theory]
    [InlineData("initials:b=b")]
    [InlineData("zh=v")]
    [InlineData("ch=i")]
    [InlineData("sh=u")]
    [InlineData("ve=t")]
    [InlineData("iong=s")]
    [InlineData("zero:a,ai,an,ang,ao,e,ei,en,eng,er,o,ou")]
    [InlineData("zero-rule:single=repeat;pair=literal;long=first+final")]
    [InlineData("normalize:lowerInvariant;u:=v;ü=v;y/w=ordinary-initials")]
    public void WhenXiaoheBaselineIsReadThenMappingAndSpecialRulesAreRecorded(string rule)
    {
        Assert.Contains(rule, PhoneticSchemeCatalog.All.Single(scheme => scheme.SchemeId == "xiaoheDoublePinyin").Rules);
    }

    [Theory]
    [InlineData("a", "aa")]
    [InlineData("ai", "ai")]
    [InlineData("an", "an")]
    [InlineData("ang", "ah")]
    [InlineData("ao", "ao")]
    [InlineData("e", "ee")]
    [InlineData("ei", "ei")]
    [InlineData("en", "en")]
    [InlineData("eng", "eg")]
    [InlineData("er", "er")]
    [InlineData("o", "oo")]
    [InlineData("ou", "ou")]
    [InlineData("zhong", "vs")]
    [InlineData("chi", "ii")]
    [InlineData("shi", "ui")]
    [InlineData("yue", "yt")]
    [InlineData("wo", "wo")]
    [InlineData("lü", "lv")]
    [InlineData("lu:", "lv")]
    [InlineData("lv", "lv")]
    [InlineData("lve", "lt")]
    public void WhenBaselineSpecialSpellingIsCompiledThenDocumentedCodeFindsCandidate(string reading, string code)
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-catalog-tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile([new PhoneticDictionaryEntry("fixture", reading, 1)], path,
            new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" });
        var dictionary = DictionaryPackageLoader.Load(path);
        Assert.Equal("fixture", Assert.Single(dictionary.Query(new ImeDictionaryQuery(code))).Text);
    }

    [Theory]
    [InlineData(ImeProductInputScheme.FullPinyin, 0)]
    [InlineData(ImeProductInputScheme.XiaoheDoublePinyin, 1)]
    public void WhenCatalogIsAddedThenLegacyEnumNumbersDoNotChange(ImeProductInputScheme scheme, int number)
    {
        Assert.Equal(number, (int)scheme);
    }
}
