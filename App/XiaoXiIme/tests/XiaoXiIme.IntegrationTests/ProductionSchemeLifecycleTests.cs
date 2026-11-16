using Avalonia.Input;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(ProductionDictionaryCollection))]
public class ProductionSchemeLifecycleTests(ProductionDictionaryFixture fixture)
{
    [Theory]
    [InlineData("fullPinyin", ImeProductInputScheme.FullPinyin, "nihao")]
    [InlineData("xiaoheDoublePinyin", ImeProductInputScheme.XiaoheDoublePinyin, "nihc")]
    [InlineData("sogouDoublePinyin", ImeProductInputScheme.SogouDoublePinyin, "nihk")]
    [InlineData("microsoftDoublePinyin", ImeProductInputScheme.MicrosoftDoublePinyin, "nihk")]
    [InlineData("abcDoublePinyin", ImeProductInputScheme.AbcDoublePinyin, "nihk")]
    [InlineData("jiajiaDoublePinyin", ImeProductInputScheme.JiajiaDoublePinyin, "nihd")]
    [InlineData("naturalCodeDoublePinyin", (ImeProductInputScheme)6, "nihk")]
    public async Task WhenSavedHostRestartsThenTargetCompletePackageIsQueried
        (string scheme, ImeProductInputScheme product, string input)
    {
        var path = Path.Combine(fixture.HostRoot, Guid.NewGuid().ToString("N"), "settings.json");
        using (var host = new ImeHostService
                   (hostBaseDirectory: fixture.HostRoot, productSettingsPath: path, userDictionaryPath: path + ".user"))
        {
            var state = await host.GetProductSettingsAsync();
            await host.SaveProductSettingsAsync
            (
                new
                (
                    state.HostInstanceId, Guid.NewGuid(), state.Settings.Revision,
                    state.Settings with { InputScheme = product }
                )
            );
        }

        using var restarted = new ImeHostService
            (hostBaseDirectory: fixture.HostRoot, productSettingsPath: path, userDictionaryPath: path + ".user");
        var status = await restarted.GetHostStatusAsync();
        Assert.Equal(scheme, status.DictionaryInputScheme);
        Assert.False(status.IsUsingFallbackDictionary);
        foreach (var character in input)
        {
            await restarted.ProcessKeyAsync(ImeKey.FromCharacter(character));
        }

        Assert.Equal("你好", (await restarted.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space))).CommitText);
        var dictionary = Load(scheme);
        Assert.Contains(dictionary.Query(new ImeDictionaryQuery(input)), candidate => candidate.Text == "你好");
        Assert.Contains(new ImeLookupAlias("小希", "xiao xi", "xx"), dictionary.GetLexicon().LookupAliases);
        Assert.Contains(dictionary.Query(new ImeDictionaryQuery("xx")), candidate => candidate.Text == "小希");
    }

    [Theory]
    [InlineData("xiaoheDoublePinyin")]
    [InlineData("sogouDoublePinyin")]
    [InlineData("microsoftDoublePinyin")]
    [InlineData("abcDoublePinyin")]
    [InlineData("jiajiaDoublePinyin")]
    [InlineData("naturalCodeDoublePinyin")]
    public void WhenDebugPreparesTargetPackageThenRawInputCaretAndOutputArePreserved(string scheme)
    {
        var session = new DictionaryDebugSession(Load("fullPinyin"));
        session.ProcessKey(Key.N);
        session.ProcessKey(Key.I);
        session.ProcessKey(Key.Space);
        session.ProcessKey(Key.N);
        session.ProcessKey(Key.I);
        session.ProcessKey(Key.Left);
        var prepared = session.PrepareDictionaryChange(Load(scheme));
        Assert.Equal("ni", prepared.Candidates.CompositionText);
        Assert.Equal(session.CommittedText, prepared.CommittedText);
        prepared.ProcessKey(Key.Back);
        Assert.Equal("i", prepared.Candidates.CompositionText);
        Assert.Equal("ni", session.Candidates.CompositionText);
    }

    [Theory]
    [InlineData(0, "fullPinyin")]
    [InlineData(1, "xiaoheDoublePinyin")]
    public async Task WhenLegacyNumericSchemeIsLoadedThenHostUsesOriginalPackage(int number, string scheme)
    {
        var directory = Path.Combine(fixture.HostRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText
        (
            path,
            $"{{\"schemaVersion\":1,\"revision\":1,\"inputScheme\":{number},\"statusBarVisible\":true,\"fuzzyRuleMask\":0}}"
        );
        using var host = new ImeHostService
            (hostBaseDirectory: fixture.HostRoot, productSettingsPath: path, userDictionaryPath: path + ".user");
        Assert.Equal(scheme, (await host.GetHostStatusAsync()).DictionaryInputScheme);
    }

    private IImeDictionary Load(string scheme) => DictionaryPackageLoader.Load
    (
        DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: scheme, baseDirectory: fixture.HostRoot)
    );
}