namespace XiaoXiIme.Dictionary.Tests;

public class ProductionDoublePinyinTests
{
    public static IEnumerable<object[]> Codes()
    {
        var readings = "a ai an ang ao e ei en eng er o ou nue lue nve lve ju qu xu yu jue que xue yue bing ni hao"
            .Split(' ');
        var tables = new[]
        {
            ("naturalCodeDoublePinyin",
                "aa ai an ah ao ee ei en eg er oo ou nt lt nt lt ju qu xu yu jt qt xt yt by ni hk"),
            ("sogouDoublePinyin", "oa ol oj oh ok oe oz of og or oo ob nt lt nt lt ju qu xu yu jt qt xt yt b; ni hk"),
            ("microsoftDoublePinyin",
                "oa ol oj oh ok oe oz of og or oo ob nv lv nv lv ju qu xu yu jt qt xt yt b; ni hk"),
            ("abcDoublePinyin", "oa ol oj oh ok oe oq of og or oo ob nv lv nv lv ju qu xu yu jm qm xm ym by ni hk"),
            ("jiajiaDoublePinyin", "aa as af ag ad ee ew er et eq oo op nx lx nx lx ju qu xu yu jx qx xx yx bq ni hd")
        };
        foreach (var (scheme, codes) in tables)
        foreach (var pair in readings.Zip(codes.Split(' ')))
        {
            yield return [scheme, pair.First, pair.Second];
        }
    }

    [Theory]
    [MemberData(nameof(Codes))]
    public void WhenProductionPackageIsCompiledThenSchemeCodeFindsCanonicalEntry
        (string scheme, string reading, string code)
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-production-schemes", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            [new PhoneticDictionaryEntry("fixture", reading, 1)], path,
            new DictionaryPackageParameters { InputScheme = scheme }
        );
        var dictionary = DictionaryPackageLoader.Load(path);
        Assert.Equal("fixture", Assert.Single(dictionary.Query(new ImeDictionaryQuery(code))).Text);
    }
}