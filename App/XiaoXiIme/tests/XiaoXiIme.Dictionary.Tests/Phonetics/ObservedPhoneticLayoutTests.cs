namespace XiaoXiIme.Dictionary.Tests;

public class ObservedPhoneticLayoutTests
{
    [Theory]
    [InlineData
    (
        "sogouDoublePinyin", "zh=v ch=i sh=u",
        "a=a ou=b iao=c iang=d uang=d e=e en=f eng=g i=i an=j ao=k ai=l ian=m in=n o=o uo=o un=p iu=q uan=r er=r ong=s iong=s ve=t ue=t u=u ui=v ia=w ua=w ie=x uai=y v=y ei=z ang=h"
    )]
    [InlineData
    (
        "microsoftDoublePinyin", "zh=v ch=i sh=u",
        "a=a ou=b iao=c iang=d uang=d e=e en=f eng=g i=i an=j ao=k ai=l ian=m in=n o=o uo=o un=p iu=q uan=r er=r ong=s iong=s ue=t u=u ve=v ui=v ia=w ua=w ie=x uai=y v=y ei=z ang=h"
    )]
    [InlineData
    (
        "abcDoublePinyin", "zh=a ch=e sh=v",
        "a=a ou=b in=c uai=c ia=d ua=d e=e en=f eng=g ang=h i=i an=j ao=k ai=l ue=m ui=m un=n o=o uo=o uan=p ei=q iu=r er=r ong=s iong=s iang=t uang=t u=u ve=v v=v ian=w ie=x ing=y iao=z"
    )]
    [InlineData
    (
        "jiajiaDoublePinyin", "zh=v ch=u sh=i",
        "a=a ia=b ua=b uan=c ao=d e=e an=f ang=g iang=h uang=h i=i ian=j iao=k in=l ie=m iu=n o=o uo=o ou=p ing=q er=q en=r ai=s eng=t u=u ui=v v=v ei=w ue=x uai=x ong=y iong=y un=z"
    )]
    [InlineData
    (
        "xiaoheDoublePinyin", "zh=v ch=i sh=u",
        "a=a in=b ao=c ai=d e=e en=f eng=g ang=h i=i an=j uai=k ing=k iang=l uang=l ian=m iao=n o=o uo=o ie=p iu=q uan=r van=r ong=s iong=s ve=t ue=t u=u ui=v v=v ei=w ia=x ua=x vn=y un=y ou=z"
    )]
    public void WhenImageLayoutIsReadThenEveryVisibleMappingMatchesManualTranscription
        (string id, string specials, string finals)
    {
        var layout = PhoneticSchemeCatalog.All.Single(item => item.SchemeId == id).Layout!;
        var initials = "b=b p=p m=m f=f d=d t=t n=n l=l g=g k=k h=h j=j q=q x=x r=r z=z c=c s=s y=y w=w " + specials;
        Assert.Equal
        (
            Parse(initials).Concat(Parse(finals)).OrderBy(pair => pair.Key),
            layout.Initials.Concat
            (
                layout.ObservedFinals.Where
                    (pair => pair.Key != "ing" || id is not ("sogouDoublePinyin" or "microsoftDoublePinyin"))
            ).OrderBy(pair => pair.Key)
        );
    }

    [Theory]
    [InlineData("sogouDoublePinyin", "l", "ve", "lt")]
    [InlineData("microsoftDoublePinyin", "l", "ve", "lv")]
    [InlineData("abcDoublePinyin", "ch", "ue", "em")]
    [InlineData("abcDoublePinyin", "m", "ui", "mm")]
    [InlineData("jiajiaDoublePinyin", "ch", "uai", "ux")]
    [InlineData("jiajiaDoublePinyin", "sh", "ing", "iq")]
    public void WhenConfirmedComponentsAreProjectedThenImageCodeIsReturned
        (string id, string initial, string final, string expected)
    {
        var layout = PhoneticSchemeCatalog.All.Single(item => item.SchemeId == id).Layout!;
        Assert.Equal(expected, layout.TryProjectComponents(initial, final, out var code) ? code : null);
    }

    [Theory]
    [InlineData("jiajiaDoublePinyin", "l", "ve")]
    public void WhenRuleIsNotDefinedThenProjectionDoesNotGuess(string id, string initial, string final)
    {
        Assert.False
        (
            PhoneticSchemeCatalog.All.Single(item => item.SchemeId == id).Layout!.TryProjectComponents
                (initial, final, out _)
        );
    }

    [Fact]
    public void WhenExplicitSymbolMappingIsConfiguredThenProjectionPreservesTheSymbol()
    {
        var layout = new PhoneticLayout
        (
            new Dictionary<string, char> { ["b"] = 'b' },
            new Dictionary<string, char> { ["ing"] = ';' }
        );
        Assert.Equal("b;", layout.TryProjectComponents("b", "ing", out var code) ? code : null);
    }

    [Fact]
    public void WhenSourceDictionariesChangeThenLayoutRemainsImmutable()
    {
        var finals = new Dictionary<string, char> { ["ing"] = ';' };
        var layout = new PhoneticLayout(new Dictionary<string, char> { ["b"] = 'b' }, finals);
        finals["ing"] = 'x';
        Assert.Equal(';', layout.Finals["ing"]);
    }

    [Theory]
    [InlineData("daniuDoublePinyin")]
    [InlineData("xiaoyueDoublePinyin")]
    public void WhenNoTargetDefinitionExistsThenLayoutRemainsUndefined(string id)
    {
        Assert.Null(PhoneticSchemeCatalog.All.Single(item => item.SchemeId == id).Layout);
    }

    [Theory]
    [InlineData("sogouDoublePinyin")]
    [InlineData("microsoftDoublePinyin")]
    [InlineData("abcDoublePinyin")]
    [InlineData("jiajiaDoublePinyin")]
    public void WhenProductionLayoutExistsThenTwelveZeroInitialCodesAreDefined(string id)
    {
        Assert.Equal(12, PhoneticSchemeCatalog.All.Single(item => item.SchemeId == id).Layout!.ZeroInitialCodes!.Count);
    }

    [Theory]
    [InlineData("n", "van", "nr")]
    [InlineData("n", "vn", "ny")]
    public void WhenXiaoheImageExtensionIsReadThenItIsObservedButNotImplicitlyEnabled
        (string initial, string final, string expected)
    {
        var layout = PhoneticSchemeCatalog.All.Single(item => item.SchemeId == "xiaoheDoublePinyin").Layout!;
        Assert.Equal(expected, string.Concat(layout.Initials[initial], layout.ObservedFinals[final]));
        Assert.False(layout.TryProjectComponents(initial, final, out _));
    }

    [Theory]
    [InlineData("sogouDoublePinyin", "uurufahexn")]
    [InlineData("microsoftDoublePinyin", "uurufahexn")]
    [InlineData("abcDoublePinyin", "vurufahexc")]
    [InlineData("jiajiaDoublePinyin", "iurufahexl")]
    [InlineData("xiaoheDoublePinyin", "uurufahexb")]
    public void WhenRealProjectTermUsesConfirmedComponentsThenManualImageInputMatches(string id, string expected)
    {
        var entry = XiaoXiImeProjectTerms.Parse().Single(item => item.Text == "输入法核心");
        var layout = PhoneticSchemeCatalog.All.Single(item => item.SchemeId == id).Layout!;
        var codes = entry.Reading.Split(' ').Select
        (syllable =>
            {
                var initialLength = syllable.StartsWith("sh", StringComparison.Ordinal) ? 2 : 1;
                return layout.TryProjectComponents
                    (syllable[..initialLength], syllable[initialLength..], out var code)
                    ? code
                    : null;
            }
        );
        Assert.Equal(expected, string.Concat(codes));
    }

    [Theory]
    [InlineData("uurufahexb", "输入法核心")]
    [InlineData("hzxrilkz", "候选窗口")]
    [InlineData("yshuciku", "用户词库")]
    [InlineData("cikubmyi", "词库编译")]
    public void WhenRealProjectLexiconIsCompiledAndLoadedThenXiaoheInputFindsExpectedTerm(string input, string expected)
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-observed-layout-tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            XiaoXiImeProjectTerms.Parse(), path,
            new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" }
        );
        var dictionary = DictionaryPackageLoader.Load(path);
        Assert.Equal(expected, Assert.Single(dictionary.Query(new ImeDictionaryQuery(input))).Text);
    }

    private static IEnumerable<KeyValuePair<string, char>> Parse(string mappings) =>
        mappings.Split(' ').Select(item => new KeyValuePair<string, char>(item[..item.IndexOf('=')], item[^1]));
}