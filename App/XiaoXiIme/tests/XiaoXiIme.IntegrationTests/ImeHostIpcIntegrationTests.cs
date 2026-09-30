using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.IntegrationTests;

public class ImeHostIpcIntegrationTests
{
    [Fact(Timeout = 5_000)]
    public async Task IpcClient_ProcessesKeysThroughImeHostService()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(options, hostBaseDirectory: hostRoot);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();

        await client.ConnectAsync();
        await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var composingResult = await client.ProcessKeyAsync(ImeKey.FromCharacter('i'));

        Assert.True(composingResult.Handled);
        Assert.True(composingResult.Snapshot.IsComposing);
        Assert.Equal("ni", composingResult.Snapshot.Composition.Reading);
        Assert.Equal("你", composingResult.Snapshot.Candidates[0].Text);

        var commitResult = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));

        Assert.True(commitResult.Handled);
        Assert.Equal("你", commitResult.CommitText);
        Assert.False(commitResult.Snapshot.IsComposing);

        var snapshot = await client.GetSnapshotAsync();

        Assert.False(snapshot.IsComposing);
        Assert.Empty(snapshot.Candidates);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_WhenHostLayoutContainsProjectTermsThenFullPinyinCommitsCanonicalTerm()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(options, hostBaseDirectory: hostRoot);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();
        await client.ConnectAsync();

        ImeProcessResult result = default!;
        foreach (var character in "xiaoxiaimuyi")
        {
            result = await client.ProcessKeyAsync(ImeKey.FromCharacter(character));
        }

        Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);

        var commitResult = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));

        Assert.True(commitResult.Handled);
        Assert.Equal("XiaoXiIme", commitResult.CommitText);
        Assert.False(commitResult.Snapshot.IsComposing);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_WhenHostLayoutRequestsXiaoheThenProjectTermCommitsProjectedKey()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(
            options,
            inputScheme: DictionaryPackageLocations.XiaoheDoublePinyinInputScheme,
            hostBaseDirectory: hostRoot);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();
        await client.ConnectAsync();

        ImeProcessResult result = default!;
        foreach (var character in "xnxiaimuyi")
        {
            result = await client.ProcessKeyAsync(ImeKey.FromCharacter(character));
        }

        Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);

        var commitResult = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));

        Assert.True(commitResult.Handled);
        Assert.Equal("XiaoXiIme", commitResult.CommitText);
        Assert.False(commitResult.Snapshot.IsComposing);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_WhenSetCompositionReplacesReadingThenDoesNotAutoCommitAbbreviation()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(options, hostBaseDirectory: hostRoot);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();
        await client.ConnectAsync();

        var result = await client.SetCompositionAsync("xiaoxiaimuyi");
        var abbreviation = await client.SetCompositionAsync("xx");
        var cleared = await client.SetCompositionAsync(string.Empty);
        var snapshot = await client.GetSnapshotAsync();

        Assert.True(result.Handled);
        Assert.Null(result.CommitText);
        Assert.Equal("xiaoxiaimuyi", result.Snapshot.Composition.Reading);
        Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);
        Assert.True(abbreviation.Handled);
        Assert.Null(abbreviation.CommitText);
        Assert.Equal("xx", abbreviation.Snapshot.Composition.Reading);
        Assert.Equal("小希", abbreviation.Snapshot.Candidates[0].Text);
        Assert.True(cleared.Handled);
        Assert.False(cleared.Snapshot.IsComposing);
        Assert.False(snapshot.IsComposing);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_WhenQueryConversionListThenLeavesCurrentCompositionUnchanged()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(options, hostBaseDirectory: hostRoot);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();
        await client.ConnectAsync();

        await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var candidates = await client.QueryConversionListAsync("xiaoxiaimuyi");
        var snapshot = await client.GetSnapshotAsync();

        Assert.Equal("XiaoXiIme", candidates[0].Text);
        Assert.Equal("n", snapshot.Composition.Reading);
        Assert.True(snapshot.IsComposing);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_WhenRegisterWordThenPersistsWithoutChangingComposition()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        var hostRoot = CreateHostLayout();
        var userDictionaryPath = Path.Combine(hostRoot, "user-dictionary.json");
        using var host = new ImeHostService(options, hostBaseDirectory: hostRoot, userDictionaryPath: userDictionaryPath);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();
        await client.ConnectAsync();

        await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var registered = await client.RegisterWordAsync("zidingyi", "自定义");
        var snapshot = await client.GetSnapshotAsync();
        var enumerated = await client.EnumerateRegisterWordsAsync("zidingyi");

        Assert.True(registered.Succeeded);
        Assert.Equal("n", snapshot.Composition.Reading);
        Assert.True(snapshot.IsComposing);
        Assert.Equal("zidingyi", enumerated.Entries[0].Reading);
        Assert.Equal("自定义", enumerated.Entries[0].Text);
    }

    [Fact]
    public async Task ImeHostService_WhenWordIsRegisteredThenPersistsForNewHost()
    {
        var packagePath = CreatePackage(
        [
            new PhoneticDictionaryEntry("你", "ni", 100),
        ]);
        var userDictionaryPath = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_User_{Guid.NewGuid():N}", "user-dictionary.json");
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");

        using (var host = new ImeHostService(options, dictionaryPackagePath: packagePath, userDictionaryPath: userDictionaryPath))
        using (var client = new XiaoXiImeIpcClient(options))
        {
            host.Start();
            await client.ConnectAsync();
            await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
            var registered = await client.RegisterWordAsync("zidingyi", "自定义");
            Assert.True(registered.Succeeded);
        }

        using var reloadedHost = new ImeHostService(dictionaryPackagePath: packagePath, userDictionaryPath: userDictionaryPath);
        var result = await reloadedHost.SetCompositionAsync("zidingyi");
        var unregistered = await reloadedHost.RegisterWordAsync(new ImeRegisterWordRequest(ImeRegisterWordAction.Unregister, "zidingyi", "自定义"));

        Assert.Equal("自定义", result.Snapshot.Candidates[0].Text);
        Assert.True(unregistered.Succeeded);
        Assert.Empty((await reloadedHost.RegisterWordAsync(new ImeRegisterWordRequest(ImeRegisterWordAction.Enumerate, "zidingyi", "自定义"))).Entries);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_WhenTwoSessionsComposeThenKeepsIndependentSnapshots()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(options, hostBaseDirectory: hostRoot);
        using var client = new XiaoXiImeIpcClient(options);
        var firstSession = ImeSessionId.FromHimc(0x11);
        var secondSession = ImeSessionId.FromHimc(0x22);

        host.Start();
        await client.ConnectAsync();

        var first = await ProcessCharactersAsync(client, "xiaoxiaimuyi", firstSession);
        var second = await ProcessCharactersAsync(client, "aimuyi", secondSession);
        var lastActiveSnapshot = await client.GetSnapshotAsync();
        var lastActiveUiState = await client.GetUiStateAsync();
        var firstSnapshot = await client.GetSnapshotAsync(firstSession);
        var secondSnapshot = await client.GetSnapshotAsync(secondSession);

        Assert.Equal("XiaoXiIme", first.Snapshot.Candidates[0].Text);
        Assert.Equal("xiaoxiaimuyi", first.Snapshot.Composition.Reading);
        Assert.Equal("IME", second.Snapshot.Candidates[0].Text);
        Assert.Equal("aimuyi", second.Snapshot.Composition.Reading);
        Assert.Equal("aimuyi", lastActiveSnapshot.Composition.Reading);
        Assert.Equal("IME", lastActiveSnapshot.Candidates[0].Text);
        Assert.Equal("aimuyi", lastActiveUiState.Composition.Reading);
        Assert.Equal("IME", lastActiveUiState.Candidates[0].Text);
        Assert.Equal("xiaoxiaimuyi", firstSnapshot.Composition.Reading);
        Assert.Equal("XiaoXiIme", firstSnapshot.Candidates[0].Text);
        Assert.Equal("aimuyi", secondSnapshot.Composition.Reading);
        Assert.Equal("IME", secondSnapshot.Candidates[0].Text);

        var commit = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space), secondSession);
        var firstAfterCommit = await client.GetSnapshotAsync(firstSession);
        var secondAfterCommit = await client.GetSnapshotAsync(secondSession);
        var lastActiveAfterCommit = await client.GetSnapshotAsync();

        Assert.Equal("IME", commit.CommitText);
        Assert.False(commit.Snapshot.IsComposing);
        Assert.True(firstAfterCommit.IsComposing);
        Assert.Equal("XiaoXiIme", firstAfterCommit.Candidates[0].Text);
        Assert.False(secondAfterCommit.IsComposing);
        Assert.False(lastActiveAfterCommit.IsComposing);
        Assert.Empty(lastActiveAfterCommit.Candidates);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_WhenSessionIsResetThenDropsOnlyThatContext()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(options, hostBaseDirectory: hostRoot);
        using var client = new XiaoXiImeIpcClient(options);
        var firstSession = ImeSessionId.FromHimc(0x11);
        var secondSession = ImeSessionId.FromHimc(0x22);

        host.Start();
        await client.ConnectAsync();

        await ProcessCharactersAsync(client, "xiaoxiaimuyi", firstSession);
        await ProcessCharactersAsync(client, "aimuyi", secondSession);
        var reset = await client.ResetSessionAsync(secondSession);
        var firstAfterReset = await client.GetSnapshotAsync(firstSession);
        var secondAfterReset = await client.GetSnapshotAsync(secondSession);
        var lastActiveAfterReset = await client.GetSnapshotAsync();

        Assert.True(reset.Reset);
        Assert.Equal(secondSession, reset.SessionId);
        Assert.True(firstAfterReset.IsComposing);
        Assert.Equal("XiaoXiIme", firstAfterReset.Candidates[0].Text);
        Assert.False(secondAfterReset.IsComposing);
        Assert.Empty(secondAfterReset.Candidates);
        Assert.False(lastActiveAfterReset.IsComposing);
        Assert.Empty(lastActiveAfterReset.Candidates);
    }

    [Fact]
    public async Task ImeHostService_WhenPackageIsValidThenLoadsProductionDictionary()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("测试词", "ce", 500)]);
        using var host = new ImeHostService(dictionaryPackagePath: packagePath);

        var first = await host.ProcessKeyAsync(ImeKey.FromCharacter('c'));
        var result = await host.ProcessKeyAsync(ImeKey.FromCharacter('e'));
        var status = await host.GetHostStatusAsync();

        Assert.True(first.Handled);
        Assert.Equal("测试词", result.Snapshot.Candidates[0].Text);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Null(status.LastError);
        Assert.Null(status.DictionaryLoadError);
        Assert.Equal(ImeAboutState.SeWzc, status.EffectiveAbout);
        Assert.Equal(Path.GetFullPath(packagePath), status.DictionaryPackagePath);
        Assert.Equal(DictionaryPackageLocations.FullPinyinInputScheme, status.DictionaryInputScheme);
    }

    [Fact]
    public async Task ImeHostService_WhenXiaohePackageIsLoadedThenQueriesProjectedKeys()
    {
        var packagePath = CreatePackage(
            [new PhoneticDictionaryEntry("你好", "ni hao", 200)],
            new DictionaryPackageParameters { InputScheme = DictionaryPackageLocations.XiaoheDoublePinyinInputScheme });
        using var host = new ImeHostService(dictionaryPackagePath: packagePath);

        await host.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        await host.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        await host.ProcessKeyAsync(ImeKey.FromCharacter('h'));
        var result = await host.ProcessKeyAsync(ImeKey.FromCharacter('c'));
        var status = await host.GetHostStatusAsync();

        Assert.Equal("你好", result.Snapshot.Candidates[0].Text);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Equal(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, status.DictionaryInputScheme);
        Assert.Equal(Path.GetFullPath(packagePath), status.DictionaryPackagePath);
    }

    [Fact]
    public async Task ImeHostService_WhenXiaoheSchemeIsRequestedWithoutPackageThenKeepsRequestedSchemeOnFallback()
    {
        using var host = new ImeHostService(inputScheme: DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);

        var status = await host.GetHostStatusAsync();

        Assert.True(status.IsUsingFallbackDictionary);
        Assert.Equal(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, status.DictionaryInputScheme);
        Assert.Equal(
            DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: DictionaryPackageLocations.XiaoheDoublePinyinInputScheme),
            status.DictionaryPackagePath);
    }

    [Fact]
    public async Task ImeHostService_WhenHostLayoutContainsBothPackagesThenLoadsFullPinyinByDefault()
    {
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(hostBaseDirectory: hostRoot);

        await host.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        await host.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        await host.ProcessKeyAsync(ImeKey.FromCharacter('h'));
        await host.ProcessKeyAsync(ImeKey.FromCharacter('a'));
        var result = await host.ProcessKeyAsync(ImeKey.FromCharacter('o'));
        var status = await host.GetHostStatusAsync();

        Assert.Equal("你好", result.Snapshot.Candidates[0].Text);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Equal(DictionaryPackageLocations.FullPinyinInputScheme, status.DictionaryInputScheme);
        Assert.Equal(
            DictionaryPackageLocations.ResolvePackageDirectory(baseDirectory: hostRoot),
            status.DictionaryPackagePath);
    }

    [Fact]
    public async Task ImeHostService_WhenHostLayoutRequestsXiaoheThenLoadsProjectedPackage()
    {
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(
            inputScheme: DictionaryPackageLocations.XiaoheDoublePinyinInputScheme,
            hostBaseDirectory: hostRoot);

        await host.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        await host.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        await host.ProcessKeyAsync(ImeKey.FromCharacter('h'));
        var result = await host.ProcessKeyAsync(ImeKey.FromCharacter('c'));
        var status = await host.GetHostStatusAsync();

        Assert.Equal("你好", result.Snapshot.Candidates[0].Text);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Equal(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, status.DictionaryInputScheme);
        Assert.Equal(
            DictionaryPackageLocations.ResolvePackageDirectory(
                inputScheme: DictionaryPackageLocations.XiaoheDoublePinyinInputScheme,
                baseDirectory: hostRoot),
            status.DictionaryPackagePath);
    }

    [Fact]
    public async Task ImeHostService_WhenHostLayoutContainsProjectTermsThenFullPinyinQueryHitsCanonicalKey()
    {
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, "user-dictionary.json"));

        ImeProcessResult result = default!;
        foreach (var character in "xiaoxiaimuyi")
        {
            result = await host.ProcessKeyAsync(ImeKey.FromCharacter(character));
        }

        var status = await host.GetHostStatusAsync();

        Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Equal(DictionaryPackageLocations.FullPinyinInputScheme, status.DictionaryInputScheme);
        Assert.Equal("XiaoXiIme", (await host.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space))).CommitText);
    }

    [Fact]
    public async Task ImeHostService_WhenHostLayoutRequestsXiaoheThenProjectTermUsesProjectedKey()
    {
        var hostRoot = CreateHostLayout();
        using var host = new ImeHostService(
            inputScheme: DictionaryPackageLocations.XiaoheDoublePinyinInputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, "user-dictionary.json"));

        ImeProcessResult result = default!;
        foreach (var character in "xnxiaimuyi")
        {
            result = await host.ProcessKeyAsync(ImeKey.FromCharacter(character));
        }

        var status = await host.GetHostStatusAsync();

        Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Equal(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, status.DictionaryInputScheme);
        Assert.Equal("XiaoXiIme", (await host.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space))).CommitText);
    }

    [Fact]
    public async Task ImeHostService_WhenPackageIsMissingThenReportsMinimalFallback()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_Missing_{Guid.NewGuid():N}");
        using var host = new ImeHostService(dictionaryPackagePath: packagePath);

        await host.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var result = await host.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        var status = await host.GetHostStatusAsync();

        Assert.Equal("你", result.Snapshot.Candidates[0].Text);
        Assert.True(status.IsUsingFallbackDictionary);
        Assert.Null(status.LastError);
        Assert.NotNull(status.DictionaryLoadError);
        Assert.Equal(ImeAboutState.SeWzc, status.EffectiveAbout);
        Assert.Equal(Path.GetFullPath(packagePath), status.DictionaryPackagePath);
        Assert.Equal(DictionaryPackageLocations.FullPinyinInputScheme, status.DictionaryInputScheme);

        var uiState = await host.GetUiStateAsync();
        Assert.Equal(ImeAboutState.SeWzcNotice, uiState.EffectiveAbout.Notice);
        Assert.False(uiState.IsHostUnavailable);
        Assert.True(uiState.IsUsingFallbackDictionary);
        Assert.Contains("minimal fallback dictionary", uiState.DiagnosticText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImeHostService_WhenCandidateSelectedThenPersistsLearningForNewHost()
    {
        var packagePath = CreatePackage(
        [
            new PhoneticDictionaryEntry("你", "ni", 100),
            new PhoneticDictionaryEntry("呢", "ni", 60),
        ]);
        var userDictionaryPath = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_User_{Guid.NewGuid():N}", "user-dictionary.json");

        using (var host = new ImeHostService(dictionaryPackagePath: packagePath, userDictionaryPath: userDictionaryPath))
        {
            await host.ProcessKeyAsync(ImeKey.FromCharacter('n'));
            await host.ProcessKeyAsync(ImeKey.FromCharacter('i'));
            await host.ProcessKeyAsync(ImeKey.NextCandidate());
            await host.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));
        }

        using var reloadedHost = new ImeHostService(dictionaryPackagePath: packagePath, userDictionaryPath: userDictionaryPath);
        await reloadedHost.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var result = await reloadedHost.ProcessKeyAsync(ImeKey.FromCharacter('i'));

        Assert.Equal("呢", result.Snapshot.Candidates[0].Text);
    }

    [Fact]
    public async Task ImeHostService_WhenUserDictionaryIsCorruptThenReportsIsolationAndContinues()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("你", "ni", 100)]);
        var userDictionaryPath = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_User_{Guid.NewGuid():N}", "user-dictionary.json");
        Directory.CreateDirectory(Path.GetDirectoryName(userDictionaryPath)!);
        File.WriteAllText(userDictionaryPath, "invalid-json");

        using var host = new ImeHostService(dictionaryPackagePath: packagePath, userDictionaryPath: userDictionaryPath);
        await host.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var result = await host.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        var status = await host.GetHostStatusAsync();

        Assert.Equal("你", result.Snapshot.Candidates[0].Text);
        Assert.NotNull(status.UserDictionaryError);
        Assert.NotNull(status.IsolatedUserDictionaryPath);
        Assert.Equal(Path.GetFullPath(userDictionaryPath), status.UserDictionaryPath);

        var uiState = await host.GetUiStateAsync();
        Assert.Equal(ImeAboutState.SeWzc, uiState.EffectiveAbout);
        Assert.False(uiState.IsHostUnavailable);
        Assert.False(uiState.IsUsingFallbackDictionary);
        Assert.Contains("User dictionary error", uiState.DiagnosticText, StringComparison.Ordinal);
        Assert.DoesNotContain("minimal fallback dictionary", uiState.DiagnosticText, StringComparison.Ordinal);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_GetsUiStateAndHostStatusThroughImeHostService()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        using var host = new ImeHostService(options);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();

        var status = await client.WaitUntilReadyAsync();
        await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var result = await client.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        var uiState = await client.GetUiStateAsync();

        Assert.True(status.IsRunning);
        Assert.True(result.Snapshot.IsComposing);
        Assert.True(uiState.CandidateWindowVisible);
        Assert.False(uiState.IsHostUnavailable);
        Assert.Equal("ni", uiState.Composition.Reading);
        Assert.Equal("你", uiState.Candidates[0].Text);
        Assert.Equal(uiState.Candidates.Count, uiState.CandidateWindow.PageSize);
        Assert.Equal(ImeGuidelineLevel.Reading, uiState.Guideline.Level);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_WhenHostIsMissingThenWaitUntilReadyTimesOut()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Missing_{Guid.NewGuid():N}");
        using var client = new XiaoXiImeIpcClient(options);

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => client.WaitUntilReadyAsync(TimeSpan.FromMilliseconds(200)));

        Assert.Contains("did not become ready", exception.Message, StringComparison.Ordinal);
    }

    private static async Task<ImeProcessResult> ProcessCharactersAsync(
        XiaoXiImeIpcClient client,
        string input,
        ImeSessionId sessionId)
    {
        ImeProcessResult result = default!;
        foreach (var character in input)
        {
            result = await client.ProcessKeyAsync(ImeKey.FromCharacter(character), sessionId);
        }

        return result;
    }

    private static string CreatePackage(
        IReadOnlyList<PhoneticDictionaryEntry> entries,
        DictionaryPackageParameters? parameters = null)
    {
        var packagePath = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_HostPackage_{Guid.NewGuid():N}");
        DictionaryPackageCompiler.Compile(entries, packagePath, parameters);
        return packagePath;
    }

    private static string CreateHostLayout()
    {
        var hostRoot = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_HostLayout_{Guid.NewGuid():N}");
        var entries = new List<PhoneticDictionaryEntry> { new("你好", "ni hao", 200) };
        entries.AddRange(XiaoXiImeProjectTerms.Parse());
        DictionaryPackageCompiler.Compile(
            entries,
            Path.Combine(hostRoot, DictionaryPackageLocations.FullPinyinPackageDirectoryName));
        DictionaryPackageCompiler.Compile(
            entries,
            Path.Combine(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinPackageRelativePath),
            new DictionaryPackageParameters { InputScheme = DictionaryPackageLocations.XiaoheDoublePinyinInputScheme });
        return hostRoot;
    }
}

