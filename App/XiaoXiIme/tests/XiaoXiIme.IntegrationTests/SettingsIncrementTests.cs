using Avalonia.Input;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

public class SettingsIncrementTests
{
    [Fact]
    public async Task WhenUnchangedSettingsAreSubmittedThenRevisionRemainsZero()
    {
        var path = Path.Combine
            (Path.GetTempPath(), "XiaoXiIme-settings-increment", Guid.NewGuid().ToString("N"), "settings.json");
        using var host = new ImeHostService(productSettingsPath: path, userDictionaryPath: path + ".user");
        var state = await host.GetProductSettingsAsync();
        var saved = await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings));
        Assert.Equal(0, saved.Settings.Revision);
    }
}

[Collection(nameof(ProductionDictionaryCollection))]
public class DebugSettingsIncrementTests(ProductionDictionaryFixture fixture)
{
    [Fact]
    public void WhenPreparingSchemeChangeThenActiveInputIsPreserved()
    {
        var session = new DictionaryDebugSession(Load("fullPinyin"));
        session.ProcessKey(Key.N);
        session.ProcessKey(Key.I);
        var prepared = session.PrepareDictionaryChange(Load("xiaoheDoublePinyin"));
        Assert.Equal("ni", prepared.Candidates.CompositionText);
    }

    [Fact]
    public void WhenPreparingSchemeChangeThenCommittedOutputIsPreserved()
    {
        var session = new DictionaryDebugSession(Load("fullPinyin"));
        session.ProcessKey(Key.N);
        session.ProcessKey(Key.I);
        session.ProcessKey(Key.Space);
        var prepared = session.PrepareDictionaryChange(Load("xiaoheDoublePinyin"));
        Assert.Equal(session.CommittedText, prepared.CommittedText);
    }

    [Fact]
    public async Task WhenPreparingFuzzyInBackgroundThenPendingInputIsPreserved()
    {
        var session = new DictionaryDebugSession
        (
            new InMemoryImeDictionary
                (new ImeLexicon([new("张", "zhang", 123)], [], ImeProjectionDefinition.FullPinyin, 0))
        );
        session.ProcessKey(Key.Z);
        session.ProcessKey(Key.A);
        session.ProcessKey(Key.N);
        var prepared = await session.PrepareSettingsChangeAsync(null, 5, CancellationToken.None);
        Assert.Equal("zan", prepared.Candidates.CompositionText);
    }

    [Fact]
    public async Task WhenPreparationIsCancelledThenNoReplacementIsReturned()
    {
        var session = new DictionaryDebugSession
        (
            new InMemoryImeDictionary
                (new ImeLexicon([new("张", "zhang", 123)], [], ImeProjectionDefinition.FullPinyin, 0))
        );
        await Assert.ThrowsAnyAsync<OperationCanceledException>
            (() => session.PrepareSettingsChangeAsync(null, 5, new CancellationToken(true)));
    }

    private IImeDictionary Load(string scheme) => DictionaryPackageLoader.Load
    (
        DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: scheme, baseDirectory: fixture.HostRoot)
    );
}