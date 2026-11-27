using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class NaturalCodeProductionTests
{
    [Theory]
    [InlineData("a", "a")]
    [InlineData("b", "ou")]
    [InlineData("c", "iao")]
    [InlineData("d", "iang uang")]
    [InlineData("e", "e")]
    [InlineData("f", "en")]
    [InlineData("g", "eng")]
    [InlineData("h", "ang")]
    [InlineData("i", "i")]
    [InlineData("j", "an")]
    [InlineData("k", "ao")]
    [InlineData("l", "ai")]
    [InlineData("m", "ian")]
    [InlineData("n", "in")]
    [InlineData("o", "o uo")]
    [InlineData("p", "un")]
    [InlineData("q", "iu")]
    [InlineData("r", "er uan")]
    [InlineData("s", "iong ong")]
    [InlineData("t", "ue ve")]
    [InlineData("u", "u")]
    [InlineData("v", "ui v")]
    [InlineData("w", "ia ua")]
    [InlineData("x", "ie")]
    [InlineData("y", "ing uai")]
    [InlineData("z", "ei")]
    public void EachKeyHasTheIndependentBaselineFinals(string key, string finals)
    {
        var layout = PhoneticSchemeCatalog.All.Single(s => s.SchemeId == "naturalCodeDoublePinyin").Layout!;
        Assert.Equal
        (
            finals.Split(' '),
            layout.Finals.Where(p => p.Value == key[0]).Select(p => p.Key).Order(StringComparer.Ordinal)
        );
    }

    [Theory]
    [InlineData("lü", "lv")]
    [InlineData("lu:", "lv")]
    [InlineData("lve", "lt")]
    [InlineData("jv", "ju")]
    [InlineData("qve", "qt")]
    [InlineData("xve", "xt")]
    [InlineData("yve", "yt")]
    [InlineData("wo", "wo")]
    [InlineData("zhong", "vs")]
    [InlineData("chi", "ii")]
    [InlineData("shi", "ui")]
    public void SpecialSpellingSurvivesCompileLoad(string reading, string code)
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-natural", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            [new PhoneticDictionaryEntry("fixture", reading, 1)], path,
            new DictionaryPackageParameters { InputScheme = "naturalCodeDoublePinyin" }
        );
        Assert.Equal
            ("fixture", Assert.Single(DictionaryPackageLoader.Load(path).Query(new ImeDictionaryQuery(code))).Text);
    }

    [Fact]
    public void CollidingSyllablesBothSurviveCompileLoad()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-natural", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            [new PhoneticDictionaryEntry("咯", "lo", 2), new PhoneticDictionaryEntry("罗", "luo", 1)], path,
            new DictionaryPackageParameters { InputScheme = "naturalCodeDoublePinyin" }
        );
        Assert.Equal
        (
            new[] { "咯", "罗" },
            DictionaryPackageLoader.Load(path).Query(new ImeDictionaryQuery("lo")).Select(c => c.Text).Order
                (StringComparer.Ordinal)
        );
    }

    [Theory]
    [InlineData("projectionVersion", null)]
    [InlineData("projectionVersion", "tampered")]
    [InlineData("rulesSha256", null)]
    [InlineData("rulesSha256", "tampered")]
    public void WhenRequiredPackageIdentityIsRemovedOrChangedThenLoadRejectsIt(string field, string? value)
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-natural", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            [new PhoneticDictionaryEntry("fixture", "er", 1)], path,
            new DictionaryPackageParameters { InputScheme = "naturalCodeDoublePinyin" }
        );
        var manifestPath = Path.Combine(path, "manifest.json");
        var manifest = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(manifestPath))!;
        var parameters = manifest["parameters"]!.AsObject();
        if (value is null)
        {
            parameters.Remove(field);
        }
        else
        {
            parameters[field] = value;
        }

        File.WriteAllText(manifestPath, manifest.ToJsonString());
        Assert.Throws<DictionaryPackageException>(() => DictionaryPackageLoader.Load(path));
    }

    [Theory]
    [InlineData("dd", false)]
    [InlineData("dj", true)]
    [InlineData("jd", true)]
    [InlineData("vd", true)]
    [InlineData("er", true)]
    [InlineData("aa", true)]
    [InlineData("lt", true)]
    [InlineData("lo", true)]
    public void NaturalCodeSyllableLegalityIsIndependentOfLexicon(string code, bool expected)
    {
        Assert.Equal(expected, ImeProjectionDefinition.IsNaturalCodeSyllable(code));
    }

    [Theory]
    [InlineData('d', true)]
    [InlineData('v', true)]
    [InlineData('e', false)]
    [InlineData('\'', false)]
    public void NaturalCodeInitialKeysComeFromLayout(char key, bool expected)
    {
        Assert.Equal(expected, ImeProjectionDefinition.IsNaturalCodeInitial(key));
    }

    [Fact]
    public void NaturalCodeHasStablePersistedIdentity()
    {
        Assert.Equal
        (
            (ImeProductInputScheme)6,
            PhoneticSchemeCatalog.All.Single(s => s.SchemeId == "naturalCodeDoublePinyin").ProductScheme
        );
    }
}