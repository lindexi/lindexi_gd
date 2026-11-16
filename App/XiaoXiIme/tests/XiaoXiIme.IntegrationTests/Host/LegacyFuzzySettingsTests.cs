using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.IntegrationTests;

public class LegacyFuzzySettingsTests
{
    [Fact]
    public async Task WhenRulesAreSavedThenExistingNewAndRestartedSessionsInheritThem()
    {
        var root = Path.Combine(Path.GetTempPath(), "LegacyFuzzySettings", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            [new PhoneticDictionaryEntry("张", "zhang", 123)],
            Path.Combine(root, DictionaryPackageLocations.FullPinyinPackageDirectoryName)
        );
        var path = Path.Combine(root, "settings.json");
        using (var host = Create(root, path))
        {
            await host.SetCompositionAsync(new ImeSetCompositionRequest("zan", new ImeSessionId("first")));
            await host.SetCompositionAsync(new ImeSetCompositionRequest("zan", new ImeSessionId("second")));
            var state = await host.GetProductSettingsAsync();
            var saved = await host.SaveProductSettingsAsync
                (new(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { FuzzyRuleMask = 5 }));
            var first = await host.GetSnapshotAsync(new ImeSessionId("first"));
            var second = await host.GetSnapshotAsync(new ImeSessionId("second"));
            await host.SetCompositionAsync(new ImeSetCompositionRequest("zan", new ImeSessionId("new")));
            var fresh = await host.GetSnapshotAsync(new ImeSessionId("new"));
            Assert.Equal
            (
                (ImeProductSettingsError.None, "张", "张", "张"),
                (saved.Error, first.Candidates.Single().Text, second.Candidates.Single().Text,
                    fresh.Candidates.Single().Text)
            );
        }

        using var restarted = Create(root, path);
        await restarted.SetCompositionAsync("zan");
        Assert.Equal("张", (await restarted.GetSnapshotAsync()).Candidates.Single().Text);
    }

    [Fact]
    public async Task WhenDiskCommitFailsThenAllSessionsAndPreferencesRemainOld()
    {
        var root = Path.Combine(Path.GetTempPath(), "LegacyFuzzySettings", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            [new PhoneticDictionaryEntry("张", "zhang", 123)],
            Path.Combine(root, DictionaryPackageLocations.FullPinyinPackageDirectoryName)
        );
        var path = Path.Combine(root, "settings.json");
        using var host = Create(root, path);
        await host.SetCompositionAsync(new ImeSetCompositionRequest("zan", new ImeSessionId("first")));
        await host.SetCompositionAsync(new ImeSetCompositionRequest("zan", new ImeSessionId("second")));
        var firstBefore = await host.GetSnapshotAsync(new ImeSessionId("first"));
        var secondBefore = await host.GetSnapshotAsync(new ImeSessionId("second"));
        Directory.CreateDirectory(path);
        var state = await host.GetProductSettingsAsync();
        var saved = await host.SaveProductSettingsAsync
            (new(state.HostInstanceId, Guid.NewGuid(), 0, state.Settings with { FuzzyRuleMask = 5 }));
        var first = await host.GetSnapshotAsync(new ImeSessionId("first"));
        var second = await host.GetSnapshotAsync(new ImeSessionId("second"));
        Assert.Equal((ImeProductSettingsError.StorageUnavailable, 0), (saved.Error, saved.Settings.FuzzyRuleMask));
        Assert.Equal(firstBefore.Candidates, first.Candidates);
        Assert.Equal(secondBefore.Candidates, second.Candidates);
    }

    private static ImeHostService Create
        (string root, string path) => new
        (hostBaseDirectory: root, productSettingsPath: path, userDictionaryPath: path + ".user");
}