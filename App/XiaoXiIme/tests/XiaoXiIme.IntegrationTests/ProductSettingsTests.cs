using System.Text.Json;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.IntegrationTests;

public class ProductSettingsTests
{
    private static string NewPath() => Path.Combine(Path.GetTempPath(), "XiaoXiIme-product-tests", Guid.NewGuid().ToString("N"), "product-settings.json");

    private static ImeHostService CreateHost(string path) => new(
        inputScheme: DictionaryPackageLocations.FullPinyinInputScheme,
        hostBaseDirectory: Path.GetDirectoryName(path),
        userDictionaryPath: path + ".user", productSettingsPath: path);

    [Fact]
    public void WhenNormalizedPathAlreadyHasWriterThenSecondHostIsRejected()
    {
        var path = NewPath();
        using var host = CreateHost(path);
        var alias = Path.Combine(Path.GetDirectoryName(path)!, ".", Path.GetFileName(path));
        Assert.Throws<IOException>(() => CreateHost(alias));
    }

    [Fact]
    public async Task WhenWriterExitsNormallyThenNextHostLoadsCommittedRevision()
    {
        var path = NewPath();
        using (var host = CreateHost(path))
        {
            var state = await host.GetProductSettingsAsync();
            await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { StatusBarVisible = false }));
        }
        using var next = CreateHost(path);
        Assert.Equal(1, (await next.GetProductSettingsAsync()).Settings.Revision);
    }

    [Fact]
    public async Task WhenNoSettingsExistThenDefaultsAreVisibleFullPinyin()
    {
        using var host = CreateHost(NewPath());
        Assert.Equal(new ImeProductSettings(), (await host.GetProductSettingsAsync()).Settings);
    }

    [Fact]
    public async Task WhenPreferencesAreSavedThenRestartLoadsThem()
    {
        var path = NewPath();
        using (var host = CreateHost(path))
        {
            var state = await host.GetProductSettingsAsync();
            await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { StatusBarVisible = false }));
        }
        using var restarted = CreateHost(path);
        Assert.Equal(new ImeProductSettings(Revision: 1, StatusBarVisible: false), (await restarted.GetProductSettingsAsync()).Settings);
    }

    [Fact]
    public async Task WhenOperationIsRepeatedThenRevisionDoesNotAdvance()
    {
        using var host = CreateHost(NewPath());
        var state = await host.GetProductSettingsAsync();
        var request = new ImeSaveProductSettingsRequest(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { StatusBarVisible = false });
        var first = await host.SaveProductSettingsAsync(request);
        Assert.Equal(first, await host.SaveProductSettingsAsync(request));
    }

    [Fact]
    public async Task WhenMoreThan128OperationsCommitThenFirstOperationRemainsIdempotent()
    {
        using var host = CreateHost(NewPath());
        var state = await host.GetProductSettingsAsync();
        var first = new ImeSaveProductSettingsRequest(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { StatusBarVisible = false });
        var expected = await host.SaveProductSettingsAsync(first);
        for (var index = 0; index < 130; index++)
        {
            state = await host.GetProductSettingsAsync();
            await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), state.Settings.Revision, state.Settings with { StatusBarVisible = !state.Settings.StatusBarVisible }));
        }
        Assert.Equal(expected, await host.SaveProductSettingsAsync(first));
    }

    [Fact]
    public async Task WhenOperationIdIsReusedWithDifferentContentThenItIsRejected()
    {
        using var host = CreateHost(NewPath());
        var state = await host.GetProductSettingsAsync();
        var request = new ImeSaveProductSettingsRequest(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { StatusBarVisible = false });
        await host.SaveProductSettingsAsync(request);
        Assert.Equal(ImeProductSettingsError.Conflict, (await host.SaveProductSettingsAsync(request with { Settings = state.Settings })).Error);
    }

    [Fact]
    public async Task WhenCommitIsReadThenOperationIdAndRevisionBelongToSameSnapshot()
    {
        using var host = CreateHost(NewPath());
        var state = await host.GetProductSettingsAsync();
        var operation = Guid.NewGuid();
        await host.SaveProductSettingsAsync(new(state.HostInstanceId, operation, 0, state.Settings with { StatusBarVisible = false }));
        var read = await host.GetProductSettingsAsync();
        Assert.Equal((1L, (Guid?)operation), (read.Settings.Revision, read.LastOperationId));
    }

    [Fact]
    public async Task WhenRevisionIsStaleThenSaveIsRejected()
    {
        using var host = CreateHost(NewPath());
        var state = await host.GetProductSettingsAsync();
        Assert.Equal(ImeProductSettingsError.Conflict, (await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 4, state.Settings))).Error);
    }

    [Fact]
    public async Task WhenHostInstanceChangesThenSaveIsRejected()
    {
        using var host = CreateHost(NewPath());
        Assert.Equal(ImeProductSettingsError.HostChanged, (await host.SaveProductSettingsAsync(new(Guid.NewGuid(), Guid.NewGuid(), 0, new()))).Error);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(2, 0)]
    [InlineData(1, 42)]
    public async Task WhenSettingsAreInvalidThenSaveIsRejected(int schema, int scheme)
    {
        using var host = CreateHost(NewPath());
        var state = await host.GetProductSettingsAsync();
        Assert.Equal(ImeProductSettingsError.InvalidSettings, (await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 0,
            new(SchemaVersion: schema, InputScheme: (ImeProductInputScheme)scheme)))).Error);
    }

    [Fact]
    public async Task WhenTargetPackageIsMissingThenSavedConfigurationIsUnchanged()
    {
        using var host = CreateHost(NewPath());
        var state = await host.GetProductSettingsAsync();
        await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { InputScheme = ImeProductInputScheme.XiaoheDoublePinyin }));
        Assert.Equal(state.Settings, (await host.GetProductSettingsAsync()).Settings);
    }

    [Fact]
    public async Task WhenStorageFailsThenSavedConfigurationIsUnchanged()
    {
        var path = NewPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path + ".blocker", "blocker");
        using var host = CreateHost(Path.Combine(path + ".blocker", "settings.json"));
        var state = await host.GetProductSettingsAsync();
        await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { StatusBarVisible = false }));
        Assert.Equal(state.Settings, (await host.GetProductSettingsAsync()).Settings);
    }

    [Fact]
    public async Task WhenSchemaIsUnknownThenFileCannotBeOverwritten()
    {
        var path = NewPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        const string original = "{\"schemaVersion\":2}";
        File.WriteAllText(path, original);
        using var host = CreateHost(path);
        var state = await host.GetProductSettingsAsync();
        await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 0, new()));
        Assert.Equal(original, File.ReadAllText(path));
    }

    [Fact]
    public async Task WhenConfigurationIsCorruptThenItIsIsolated()
    {
        var path = NewPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "not json");
        using var host = CreateHost(path);
        Assert.Equal(ImeProductSettingsError.InvalidSettings, (await host.GetProductSettingsAsync()).LoadError);
    }

    [Fact]
    public async Task WhenPreferencesAreSavedThenTwoActiveSessionsRemainUnchanged()
    {
        using var host = CreateHost(NewPath());
        var first = new ImeSessionId("product-first");
        var second = new ImeSessionId("product-second");
        await host.ProcessKeyAsync(ImeKey.FromCharacter('x'), first);
        await host.ProcessKeyAsync(ImeKey.FromCharacter('x'), second);
        var before = await host.GetSnapshotAsync(new ImeSnapshotRequest(first));
        var state = await host.GetProductSettingsAsync();
        await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { StatusBarVisible = false }));
        Assert.Equal(before.Composition, (await host.GetSnapshotAsync(new ImeSnapshotRequest(second))).Composition);
    }

    [Theory]
    [InlineData(ImeProductInputScheme.FullPinyin, ImeProductInputScheme.XiaoheDoublePinyin, "fullPinyin", "xiaoheDoublePinyin")]
    [InlineData(ImeProductInputScheme.XiaoheDoublePinyin, ImeProductInputScheme.FullPinyin, "xiaoheDoublePinyin", "fullPinyin")]
    public async Task WhenSchemeIsSavedThenRunningSchemeRemainsUntilRestart(
        ImeProductInputScheme initial, ImeProductInputScheme target, string initialName, string targetName)
    {
        var path = NewPath();
        var root = Path.GetDirectoryName(path)!;
        var entries = new[] { new PhoneticDictionaryEntry("你好", "ni hao", 200) };
        DictionaryPackageCompiler.Compile(entries, Path.Combine(root, DictionaryPackageLocations.FullPinyinPackageDirectoryName));
        DictionaryPackageCompiler.Compile(entries, Path.Combine(root, DictionaryPackageLocations.XiaoheDoublePinyinPackageRelativePath),
            new DictionaryPackageParameters { InputScheme = DictionaryPackageLocations.XiaoheDoublePinyinInputScheme });
        File.WriteAllText(path, JsonSerializer.Serialize(new ImeProductSettings(InputScheme: initial), XiaoXiImeIpcJsonSerializerContext.Default.ImeProductSettings));
        using (var host = new ImeHostService(hostBaseDirectory: root, userDictionaryPath: path + ".user", productSettingsPath: path))
        {
            var first = new ImeSessionId("scheme-first");
            var second = new ImeSessionId("scheme-second");
            await host.ProcessKeyAsync(ImeKey.FromCharacter('n'), first);
            await host.ProcessKeyAsync(ImeKey.FromCharacter('n'), second);
            var before = await host.GetSnapshotAsync(new ImeSnapshotRequest(first));
            var state = await host.GetProductSettingsAsync();
            await host.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { InputScheme = target }));
            await host.ProcessKeyAsync(ImeKey.FromCharacter('n'), new ImeSessionId("scheme-new"));
            Assert.Equal(before.Composition, (await host.GetSnapshotAsync(new ImeSnapshotRequest(second))).Composition);
            Assert.Equal(initialName, (await host.GetProductSettingsAsync()).CurrentInputScheme);
        }
        using var restarted = new ImeHostService(hostBaseDirectory: root, userDictionaryPath: path + ".user", productSettingsPath: path);
        Assert.Equal(targetName, (await restarted.GetProductSettingsAsync()).CurrentInputScheme);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WhenExplicitSchemeOrPathIsProvidedThenSavedSchemeCannotOverrideIt(bool explicitPath)
    {
        var path = NewPath();
        var root = Path.GetDirectoryName(path)!;
        var package = Path.Combine(root, DictionaryPackageLocations.FullPinyinPackageDirectoryName);
        DictionaryPackageCompiler.Compile([new PhoneticDictionaryEntry("你好", "ni hao", 200)], package);
        File.WriteAllText(path, JsonSerializer.Serialize(new ImeProductSettings(Revision: 1, InputScheme: ImeProductInputScheme.XiaoheDoublePinyin),
            XiaoXiImeIpcJsonSerializerContext.Default.ImeProductSettings));
        using var host = new ImeHostService(dictionaryPackagePath: explicitPath ? package : null,
            inputScheme: explicitPath ? null : "fullPinyin", hostBaseDirectory: root, productSettingsPath: path, userDictionaryPath: path + ".user");
        var state = await host.GetProductSettingsAsync();
        Assert.Equal(("fullPinyin", explicitPath ? "explicitPath" : "explicitParameter"), (state.CurrentInputScheme, state.InputSchemeSource));
    }

    [Fact(Timeout = 5000)]
    public async Task WhenHostIsAbsentThenSettingsReadTimesOutWithoutCreatingConfiguration()
    {
        var options = new XiaoXiImeIpcOptions("ProductTimeout_" + Guid.NewGuid().ToString("N"), TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
        using var client = new XiaoXiImeIpcClient(options);
        await Assert.ThrowsAsync<TimeoutException>(() => client.GetProductSettingsAsync());
    }

    [Fact]
    public void WhenContractIsSerializedThenAotMetadataSupportsRoundTrip()
    {
        var request = new ImeSaveProductSettingsRequest(Guid.NewGuid(), Guid.NewGuid(), 4,
            new(Revision: 4, InputScheme: ImeProductInputScheme.XiaoheDoublePinyin, StatusBarPosition: new(120, 240)));
        var json = JsonSerializer.Serialize(request, XiaoXiImeIpcJsonSerializerContext.Default.ImeSaveProductSettingsRequest);
        Assert.Equal(request, JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeSaveProductSettingsRequest));
    }

    [Fact(Timeout = 10_000)]
    public async Task WhenSettingsAreSavedThroughIpcThenAuthorityIsReturned()
    {
        var path = NewPath();
        var options = new XiaoXiImeIpcOptions("XiaoXiIme_Product_" + Guid.NewGuid().ToString("N"));
        using var host = new ImeHostService(options, inputScheme: DictionaryPackageLocations.FullPinyinInputScheme,
            hostBaseDirectory: Path.GetDirectoryName(path), userDictionaryPath: path + ".user", productSettingsPath: path);
        using var client = new XiaoXiImeIpcClient(options);
        host.Start();
        var state = await client.GetProductSettingsAsync();
        var saved = await client.SaveProductSettingsAsync(new(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { StatusBarVisible = false }));
        Assert.Equal(new ImeProductSettings(Revision: 1, StatusBarVisible: false), saved.Settings);
    }
}
