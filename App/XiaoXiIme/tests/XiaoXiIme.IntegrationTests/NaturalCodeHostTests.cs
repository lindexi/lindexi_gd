using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;

namespace XiaoXiIme.IntegrationTests;

public class NaturalCodeHostTests
{
    [Theory]
    [InlineData("er", "er")]
    [InlineData("ang", "ah")]
    [InlineData("nüe", "nt")]
    [InlineData("lu:e", "lt")]
    [InlineData("jve", "jt")]
    [InlineData("yue", "yt")]
    [InlineData("zhong", "vs")]
    [InlineData("chi", "ii")]
    [InlineData("shi", "ui")]
    [InlineData("bing", "by")]
    public async Task WhenIndependentSpecialCodeIsTypedThenHostCommitsControlledEntry(string reading, string input)
    {
        var root = Path.Combine(Path.GetTempPath(), "XiaoXiIme-natural-host", Guid.NewGuid().ToString("N"));
        var package = DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: "naturalCodeDoublePinyin", baseDirectory: root);
        DictionaryPackageCompiler.Compile([new PhoneticDictionaryEntry("fixture", reading, 1)], package,
            new DictionaryPackageParameters { InputScheme = "naturalCodeDoublePinyin" });
        using var host = new ImeHostService(inputScheme: "naturalCodeDoublePinyin", hostBaseDirectory: root,
            productSettingsPath: Path.Combine(root, "settings.json"), userDictionaryPath: Path.Combine(root, "user.json"));
        foreach (var character in input)
            await host.ProcessKeyAsync(ImeKey.FromCharacter(character));
        Assert.Equal("fixture", (await host.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space))).CommitText);
    }
}
