using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeIpc;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule.Tests;

[Collection("Lifecycle environment")]
public class ImeHostBridgeTests
{
    [Fact(Timeout = 2_000)]
    public Task ProcessKey_WhenHostIsUnavailableThenLeavesInputUnhandledAndKeepsDiagnostic()
    {
        using var bridge = new ImeHostBridge(new UnavailableBridgeClient());

        var first = bridge.ProcessKey(ImeKey.FromCharacter('n'));
        var second = bridge.ProcessKey(ImeKey.FromCharacter('i'));
        var uiState = bridge.GetUiState();
        var commit = bridge.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.False(first.Handled);
        Assert.False(second.Handled);
        Assert.False(second.Snapshot.IsComposing);
        Assert.NotNull(bridge.LastError);
        Assert.True(bridge.IsUsingFallback);
        Assert.False(uiState.CandidateWindowVisible);
        Assert.Equal(ImeAboutState.SeWzc, uiState.EffectiveAbout);
        Assert.True(uiState.IsHostUnavailable);
        Assert.False(uiState.IsUsingFallbackDictionary);
        Assert.Contains("Host unavailable", uiState.DiagnosticText, StringComparison.Ordinal);
        Assert.Null(commit.CommitText);
        Assert.False(commit.Snapshot.IsComposing);

        return Task.CompletedTask;
    }

    [Fact]
    public void ProcessKey_WhenClientHasProgrammingErrorThenDoesNotHideFailure()
    {
        using var bridge = new ImeHostBridge(new InvalidBridgeClient());

        Assert.Throws<InvalidOperationException>(() => bridge.ProcessKey(ImeKey.FromCharacter('n')));
    }

    [Fact]
    public void IsHostUnavailable_WhenPeerDisconnectsThenReturnsTrue()
    {
        var exception = new dotnetCampus.Ipc.Exceptions.IpcPeerConnectionBrokenException();

        Assert.True(ImeHostBridge.IsHostUnavailable(exception));
    }

    [Fact]
    public void IsHostUnavailable_WhenIpcTransportIsUnsupportedThenReturnsTrue()
    {
        var result = ImeHostBridge.IsHostUnavailable(new NotSupportedException());

        Assert.True(result);
    }

    [Fact(Timeout = 2_000)]
    public Task QueryConversionList_WhenHostIsUnavailableThenReturnsNoCandidates()
    {
        using var bridge = new ImeHostBridge(new UnavailableBridgeClient());

        var composing = bridge.ProcessKey(ImeKey.FromCharacter('n'));
        var candidates = bridge.QueryConversionList("ni");
        var reverseCandidates = bridge.QueryReverseConversionList("你");
        var snapshot = bridge.GetSnapshot();

        Assert.False(composing.Handled);
        Assert.Empty(candidates);
        Assert.Empty(reverseCandidates);
        Assert.False(snapshot.IsComposing);
        Assert.True(bridge.IsUsingFallback);

        return Task.CompletedTask;
    }

    [Fact(Timeout = 2_000)]
    public Task RegisterWord_WhenHostIsUnavailableThenReportsFailure()
    {
        using var bridge = new ImeHostBridge(new UnavailableBridgeClient());

        var composing = bridge.ProcessKey(ImeKey.FromCharacter('n'));
        var registered = bridge.RegisterWord
            (new ImeRegisterWordRequest(ImeRegisterWordAction.Register, "zidingyi", "自定义"));
        var enumerated = bridge.RegisterWord
            (new ImeRegisterWordRequest(ImeRegisterWordAction.Enumerate, "zidingyi", "自定义"));
        var snapshot = bridge.GetSnapshot();

        Assert.False(composing.Handled);
        Assert.False(registered.Succeeded);
        Assert.False(enumerated.Succeeded);
        Assert.Empty(enumerated.Entries);
        Assert.False(snapshot.IsComposing);
        Assert.True(bridge.IsUsingFallback);

        return Task.CompletedTask;
    }

    [Fact(Timeout = 2_000)]
    public Task ProcessKey_WhenIpcTimesOutThenReturnsNoComposition()
    {
        using var bridge = new ImeHostBridge(new TimeoutBridgeClient());

        var result = bridge.ProcessKey(ImeKey.FromCharacter('n'));
        var uiState = bridge.GetUiState();

        Assert.False(result.Handled);
        Assert.False(result.Snapshot.IsComposing);
        Assert.True(bridge.IsUsingFallback);
        Assert.True(uiState.IsHostUnavailable);
        Assert.False(uiState.IsUsingFallbackDictionary);
        Assert.Contains("Host unavailable", uiState.DiagnosticText, StringComparison.Ordinal);
        Assert.Contains("timed out", bridge.LastError, StringComparison.OrdinalIgnoreCase);

        return Task.CompletedTask;
    }

    [Fact]
    public void WhenProcessedKeyResponseTimesOutThenNextKeyStartsCleanComposition()
    {
        using var host = CreateHost();
        using var bridge = new ImeHostBridge(new TimeoutBridgeClient(host.Service));
        var session = new ImeSessionId("first");
        bridge.ProcessKey(ImeKey.FromCharacter('n'), session);

        var result = bridge.ProcessKey(ImeKey.FromCharacter('i'), session);

        Assert.Equal("i", result.Snapshot.Composition.Reading);
    }

    [Fact]
    public void NextKeyUsesNewGenerationOnlyForTheTimedOutSession()
    {
        var client = new TimeoutBridgeClient();
        using var bridge = new ImeHostBridge(client);
        var session = new ImeSessionId("first");
        var key = ImeKey.FromCharacter('n');
        bridge.ProcessKey(key, session);

        bridge.ProcessKey(key, new ImeSessionId("second"));
        bridge.ProcessKey(key, session);

        Assert.Equal(new long[] { 0, 0, 1 }, client.Generations);
    }

    [Fact]
    public void GetUiState_WhenHostUsesFallbackDictionaryThenDoesNotReportHostUnavailable()
    {
        using var bridge = new ImeHostBridge(new FallbackDictionaryBridgeClient());

        var uiState = bridge.GetUiState();

        Assert.False(bridge.IsUsingFallback);
        Assert.Null(bridge.LastError);
        Assert.False(uiState.IsHostUnavailable);
        Assert.True(uiState.IsUsingFallbackDictionary);
        Assert.Contains("minimal fallback dictionary", uiState.DiagnosticText, StringComparison.Ordinal);
    }

    [Fact(Timeout = 5_000)]
    public Task ProcessKey_WhenHostLayoutContainsProjectTermsThenFullPinyinCommitsCanonicalTerm()
    {
        using var host = CreateHost();
        using var bridge = new ImeHostBridge(host.Options);

        host.Start();
        ImeProcessResult result = default!;
        foreach (var character in "xiaoxiaimuyi")
        {
            result = bridge.ProcessKey(ImeKey.FromCharacter(character));
        }

        var commit = bridge.ProcessKey(new ImeKey(ImeKeyKind.Space));
        var uiState = bridge.GetUiState();

        Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);
        Assert.True(commit.Handled);
        Assert.Equal("XiaoXiIme", commit.CommitText);
        Assert.False(commit.Snapshot.IsComposing);
        Assert.False(bridge.IsUsingFallback);
        Assert.False(uiState.IsHostUnavailable);
        Assert.False(uiState.IsUsingFallbackDictionary);
        return Task.CompletedTask;
    }

    [Fact(Timeout = 5_000)]
    public Task ProcessKey_WhenHostLayoutRequestsXiaoheThenProjectTermCommitsProjectedKey()
    {
        using var host = CreateHost(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
        using var bridge = new ImeHostBridge(host.Options);

        host.Start();
        ImeProcessResult result = default!;
        foreach (var character in "xnxiaimuyi")
        {
            result = bridge.ProcessKey(ImeKey.FromCharacter(character));
        }

        var commit = bridge.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);
        Assert.True(commit.Handled);
        Assert.Equal("XiaoXiIme", commit.CommitText);
        Assert.False(commit.Snapshot.IsComposing);
        Assert.False(bridge.IsUsingFallback);
        return Task.CompletedTask;
    }

    [Fact(Timeout = 5_000)]
    public Task ProcessVirtualKey_WhenHostLayoutContainsProjectTermsThenFullPinyinCommitsCanonicalTerm()
    {
        using var host = CreateHost();
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));

        try
        {
            host.Start();
            ImeProcessResult result = default!;
            foreach (var character in "xiaoxiaimuyi")
            {
                result = ImeModuleRuntime.ProcessVirtualKey((ushort)('A' + (character - 'a')));
            }

            var commit = ImeModuleRuntime.ProcessVirtualKey(ImeConstants.VkSpace);

            Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);
            Assert.Equal("XiaoXiIme", commit.CommitText);
            Assert.False(commit.Snapshot.IsComposing);
        }
        finally
        {
            ImeModuleRuntime.SetBridgeForTesting(null);
        }

        return Task.CompletedTask;
    }

    [Fact(Timeout = 5_000)]
    public Task ProcessVirtualKey_WhenHostLayoutRequestsXiaoheThenProjectTermCommitsProjectedKey()
    {
        using var host = CreateHost(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));

        try
        {
            host.Start();
            ImeProcessResult result = default!;
            foreach (var character in "xnxiaimuyi")
            {
                result = ImeModuleRuntime.ProcessVirtualKey((ushort)('A' + (character - 'a')));
            }

            var commit = ImeModuleRuntime.ProcessVirtualKey(ImeConstants.VkSpace);

            Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);
            Assert.Equal("XiaoXiIme", commit.CommitText);
            Assert.False(commit.Snapshot.IsComposing);
        }
        finally
        {
            ImeModuleRuntime.SetBridgeForTesting(null);
        }

        return Task.CompletedTask;
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeToAsciiEx_WhenHostLayoutContainsProjectTermsThenFullPinyinCommitsCanonicalTerm()
    {
        using var host = CreateHost();
        return CommitProjectTermThroughImeToAsciiEx(host, "xiaoxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeToAsciiEx_WhenHostLayoutRequestsXiaoheThenProjectTermCommitsProjectedKey()
    {
        using var host = CreateHost(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
        return CommitProjectTermThroughImeToAsciiEx(host, "xnxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeProcessKey_WhenHostLayoutContainsProjectTermsThenFullPinyinCommitsCanonicalTerm()
    {
        using var host = CreateHost();
        return CommitProjectTermThroughImmPath(host, "xiaoxiaimuyi", eatKeysFirst: true);
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeProcessKey_WhenHostLayoutRequestsXiaoheThenProjectTermCommitsProjectedKey()
    {
        using var host = CreateHost(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
        return CommitProjectTermThroughImmPath(host, "xnxiaimuyi", eatKeysFirst: true);
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeSetCompositionString_WhenHostLayoutContainsProjectTermsThenWritesCanonicalComposition()
    {
        using var host = CreateHost();
        return SetProjectTermThroughImeSetCompositionString(host, "xiaoxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeSetCompositionString_WhenHostLayoutRequestsXiaoheThenWritesProjectedComposition()
    {
        using var host = CreateHost(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
        return SetProjectTermThroughImeSetCompositionString(host, "xnxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task
        ImeConversionList_WhenHostLayoutContainsProjectTermsThenWritesCanonicalCandidateWithoutChangingComposition()
    {
        using var host = CreateHost();
        return QueryProjectTermThroughImeConversionList(host, "xiaoxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task
        ImeConversionList_WhenHostLayoutRequestsXiaoheThenWritesProjectedCandidateWithoutChangingComposition()
    {
        using var host = CreateHost(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
        return QueryProjectTermThroughImeConversionList(host, "xnxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task
        ImeConversionList_WhenHostLayoutContainsProjectTermsThenWritesCanonicalReadingWithoutChangingComposition()
    {
        using var host = CreateHost();
        return QueryProjectTermThroughImeReverseConversionList(host);
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task
        ImeConversionList_WhenHostLayoutRequestsXiaoheThenWritesCanonicalReadingWithoutChangingComposition()
    {
        using var host = CreateHost(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
        return QueryProjectTermThroughImeReverseConversionList(host);
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task
        ImeConversionList_WhenHostLayoutContainsProjectTermsThenReportsReverseLengthWithoutChangingComposition()
    {
        using var host = CreateHost();
        return QueryProjectTermThroughImeReverseLength(host);
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeConfigure_WhenRegisterWordThenPersistsWithoutChangingComposition()
    {
        using var host = CreateHost();
        return RegisterWordThroughImeConfigure(host, "xiaoxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeSelect_WhenDeselectedThenCancelsFullPinyinProjectTermComposition()
    {
        using var host = CreateHost();
        return CancelProjectTermThroughImeSelect(host, "xiaoxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public async Task ImeSelect_WhenOneHimcIsDeselectedThenPreservesOtherHimcCompositionAsync()
    {
        using var host = CreateHost();
        using var target = new InMemoryImmContext();
        using var other = new InMemoryImmContext();
        var otherSessionId = ImeSessionId.FromHimc(other.Handle);
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));
        ImeExports.SetCompositionContextWriterForTesting(new CompositeImmContextAccessor(target, other).CreateWriter());

        try
        {
            host.Start();
            unsafe
            {
                var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
                foreach (var (himc, input) in new[] { (target, "xiaoxiaimuyi"), (other, "aimuyi") })
                {
                    foreach (var character in input)
                    {
                        var virtualKey = (uint)('A' + (character - 'a'));
                        Assert.True(ImeExports.ImeProcessKeyManaged(himc.Handle, virtualKey, 0, null));
                        ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)buffer, 0, himc.Handle);
                    }
                }
            }

            Assert.Equal("xiaoxiaimuyi", target.ReadCompositionText());
            Assert.Equal("aimuyi", other.ReadCompositionText());
            Assert.Equal("IME", other.ReadFirstCandidateText());

            Assert.Equal(1, ImeExports.ImeSelectManaged(target.Handle, 0));

            Assert.Empty(target.ReadCompositionText());
            Assert.Equal("aimuyi", other.ReadCompositionText());
            Assert.Equal("IME", other.ReadFirstCandidateText());
            Assert.True(ImeModuleRuntime.GetSnapshotForTesting(otherSessionId).IsComposing);
            var otherHostSnapshot = await host.Service.GetSnapshotAsync(otherSessionId);
            Assert.Equal("aimuyi", otherHostSnapshot.Composition.Reading);
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task NotifyIme_WhenCancelThenClearsFullPinyinProjectTermComposition()
    {
        using var host = CreateHost();
        return CancelProjectTermThroughNotifyIme(host, "xiaoxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task NotifyIme_WhenCompleteThenCommitsXiaoheProjectTerm()
    {
        using var host = CreateHost(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
        return CompleteProjectTermThroughNotifyIme(host, "xnxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task NotifyIme_WhenRevertThenCommitsFullPinyinReading()
    {
        using var host = CreateHost();
        return RevertProjectTermThroughNotifyIme(host, "xiaoxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task NotifyIme_WhenSelectCandidateThenCommitsFullPinyinProjectTerm()
    {
        using var host = CreateHost();
        return SelectProjectTermThroughNotifyIme(host, "xiaoxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeToAsciiEx_WhenSecondHimcCancelsRecomposesAndCommitsThenKeepsFirstSession()
    {
        using var host = CreateHost();
        return ComposeIndependentHimcSessions(host);
    }

    private static unsafe Task ComposeIndependentHimcSessions(HostFixture host)
    {
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));
        using var first = new InMemoryImmContext();
        using var second = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(new CompositeImmContextAccessor(first, second).CreateWriter());
        var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
        var list = (TransMsgList*)buffer;

        try
        {
            host.Start();
            foreach (var character in "xiaoxiaimuyi")
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                Assert.True(ImeExports.ImeProcessKeyManaged(first.Handle, virtualKey, 0, null));
                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, first.Handle);
            }

            foreach (var character in "aimuyi")
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                Assert.True(ImeExports.ImeProcessKeyManaged(second.Handle, virtualKey, 0, null));
                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, second.Handle);
            }

            Assert.Equal("xiaoxiaimuyi", first.ReadCompositionText());
            Assert.Equal("XiaoXiIme", first.ReadFirstCandidateText());
            Assert.Equal("aimuyi", second.ReadCompositionText());
            Assert.Equal("IME", second.ReadFirstCandidateText());

            Assert.True(ImeExports.ImeProcessKeyManaged(second.Handle, ImeConstants.VkEscape, 0, null));
            ImeExports.ImeToAsciiExManaged(ImeConstants.VkEscape, 0, null, (nint)list, 0, second.Handle);

            Assert.Equal(string.Empty, second.ReadCompositionText());
            Assert.Equal(string.Empty, second.ReadResultText());
            Assert.False(ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(second.Handle)).IsComposing);
            Assert.False
            (
                host.Service.GetSnapshotAsync(ImeSessionId.FromHimc(second.Handle)).GetAwaiter().GetResult().IsComposing
            );
            Assert.Equal("xiaoxiaimuyi", first.ReadCompositionText());
            Assert.Equal("XiaoXiIme", first.ReadFirstCandidateText());
            Assert.Equal
            (
                "xiaoxiaimuyi",
                host.Service.GetSnapshotAsync
                    (ImeSessionId.FromHimc(first.Handle)).GetAwaiter().GetResult().Composition.Reading
            );

            foreach (var character in "aimuyi")
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                Assert.True(ImeExports.ImeProcessKeyManaged(second.Handle, virtualKey, 0, null));
                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, second.Handle);
            }

            Assert.Equal("aimuyi", second.ReadCompositionText());
            Assert.Equal("IME", second.ReadFirstCandidateText());
            Assert.True(ImeExports.ImeProcessKeyManaged(second.Handle, ImeConstants.VkSpace, 0, null));
            var secondCommit = ImeExports.ImeToAsciiExManaged
                (ImeConstants.VkSpace, 0, null, (nint)list, 0, second.Handle);
            var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();

            Assert.Equal(2u, secondCommit);
            Assert.Equal("IME", lastResult.CommitText);
            Assert.Equal("IME", second.ReadResultText());
            Assert.Equal(0u, second.CompositionString->CompStrLength);
            Assert.Equal("xiaoxiaimuyi", first.ReadCompositionText());
            Assert.Equal("XiaoXiIme", first.ReadFirstCandidateText());
            Assert.True(ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(first.Handle)).IsComposing);
            Assert.False(ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(second.Handle)).IsComposing);
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }

        return Task.CompletedTask;
    }

    private static unsafe Task CommitProjectTermThroughImeToAsciiEx(HostFixture host, string input) =>
        CommitProjectTermThroughImmPath(host, input, eatKeysFirst: false);

    private static unsafe Task CancelProjectTermThroughImeSelect(HostFixture host, string input)
    {
        ComposeProjectTerm(host, input, eatKeysFirst: true, out var himc);
        using (himc)
        {
            try
            {
                Assert.Equal(1, ImeExports.ImeSelectManaged(himc.Handle, 0));
                var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();
                Assert.Null(lastResult.CommitText);
                Assert.False(lastResult.Snapshot.IsComposing);
                Assert.Equal(0u, himc.CompositionString->CompStrLength);
                Assert.Equal(0u, himc.CompositionString->ResultStrLength);
                Assert.Equal(0u, himc.CandidateInfo->Count);
                Assert.False(ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(himc.Handle)).IsComposing);
                Assert.False
                (
                    (host.Service.GetSnapshotAsync(ImeSessionId.FromHimc(himc.Handle)).GetAwaiter().GetResult())
                    .IsComposing
                );
            }
            finally
            {
                ImeExports.SetCompositionContextWriterForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }

        return Task.CompletedTask;
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeSetActiveContext_WhenDeactivatedAndReactivatedThenPreservesFullPinyinProjectTermSession()
    {
        using var host = CreateHost();
        return CancelProjectTermThroughImeSetActiveContext(host, "xiaoxiaimuyi");
    }

    [Fact(Timeout = 5_000)]
    public unsafe Task ImeDestroy_WhenCalledThenResetsAllSessionsAndLeavesNoComposition()
    {
        using var host = CreateHost();
        return ResetAllSessionsThroughImeDestroy(host);
    }

    private static unsafe Task CancelProjectTermThroughNotifyIme(HostFixture host, string input)
    {
        ComposeProjectTerm(host, input, eatKeysFirst: true, out var himc);
        using (himc)
        {
            try
            {
                Assert.Equal
                (
                    1,
                    ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiCompositionStr, 0, ImeConstants.CpsCancel)
                );
                var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();
                Assert.Null(lastResult.CommitText);
                Assert.False(lastResult.Snapshot.IsComposing);
                Assert.Equal(0u, himc.CompositionString->CompStrLength);
                Assert.Equal(0u, himc.CompositionString->ResultStrLength);
                Assert.Equal(0u, himc.CandidateInfo->Count);
            }
            finally
            {
                ImeExports.SetCompositionContextWriterForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }

        return Task.CompletedTask;
    }

    private static unsafe Task CompleteProjectTermThroughNotifyIme(HostFixture host, string input)
    {
        ComposeProjectTerm(host, input, eatKeysFirst: true, out var himc);
        using (himc)
        {
            try
            {
                Assert.Equal
                (
                    1,
                    ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiCompositionStr, 0, ImeConstants.CpsComplete)
                );
                var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();
                Assert.Equal("XiaoXiIme", lastResult.CommitText);
                Assert.False(lastResult.Snapshot.IsComposing);
                Assert.Equal("XiaoXiIme", himc.ReadResultText());
                Assert.Equal(0u, himc.CompositionString->CompStrLength);
                Assert.Equal(0u, himc.CandidateInfo->Count);
            }
            finally
            {
                ImeExports.SetCompositionContextWriterForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }

        return Task.CompletedTask;
    }

    private static unsafe Task SelectProjectTermThroughNotifyIme(HostFixture host, string input)
    {
        ComposeProjectTerm(host, input, eatKeysFirst: true, out var himc);
        using (himc)
        {
            try
            {
                Assert.Equal(1, ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiSelectCandidateStr, 0, 0));
                var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();
                Assert.Equal("XiaoXiIme", lastResult.CommitText);
                Assert.False(lastResult.Snapshot.IsComposing);
                Assert.Equal("XiaoXiIme", himc.ReadResultText());
                Assert.Equal(0u, himc.CompositionString->CompStrLength);
                Assert.Equal(0u, himc.CandidateInfo->Count);
            }
            finally
            {
                ImeExports.SetCompositionContextWriterForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }

        return Task.CompletedTask;
    }

    private static unsafe Task RevertProjectTermThroughNotifyIme(HostFixture host, string input)
    {
        ComposeProjectTerm(host, input, eatKeysFirst: true, out var himc);
        using (himc)
        {
            try
            {
                Assert.Equal
                (
                    1,
                    ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiCompositionStr, 0, ImeConstants.CpsRevert)
                );
                var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();
                Assert.Equal(input, lastResult.CommitText);
                Assert.False(lastResult.Snapshot.IsComposing);
                Assert.Equal(input, himc.ReadResultText());
                Assert.Equal(0u, himc.CompositionString->CompStrLength);
                Assert.Equal(0u, himc.CandidateInfo->Count);
            }
            finally
            {
                ImeExports.SetCompositionContextWriterForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }

        return Task.CompletedTask;
    }

    private static unsafe Task CancelProjectTermThroughImeSetActiveContext(HostFixture host, string input)
    {
        ComposeProjectTerm(host, input, eatKeysFirst: true, out var himc);
        using (himc)
        {
            try
            {
                var session = ImeSessionId.FromHimc(himc.Handle);
                var before = host.Service.GetSnapshotAsync(session).GetAwaiter().GetResult();
                Assert.Equal(1, ImeExports.ImeSetActiveContextManaged(himc.Handle, 0));
                Assert.Equal
                    (input, (host.Service.GetSnapshotAsync(session).GetAwaiter().GetResult()).Composition.Reading);
                Assert.Equal((uint)input.Length, himc.CompositionString->CompStrLength);
                Assert.Equal(1, ImeExports.ImeSetActiveContextManaged(himc.Handle, 1));
                Assert.Equal
                    (before.Composition, (host.Service.GetSnapshotAsync(session).GetAwaiter().GetResult()).Composition);
                Assert.True(ImeModuleRuntime.GetSnapshotForTesting(session).IsComposing);
            }
            finally
            {
                ImeExports.SetCompositionContextWriterForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }

        return Task.CompletedTask;
    }

    private static unsafe Task ResetAllSessionsThroughImeDestroy(HostFixture host)
    {
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));
        using var first = new InMemoryImmContext();
        using var second = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(new CompositeImmContextAccessor(first, second).CreateWriter());
        var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
        var list = (TransMsgList*)buffer;

        try
        {
            host.Start();
            foreach (var character in "xiaoxiaimuyi")
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                Assert.True(ImeExports.ImeProcessKeyManaged(first.Handle, virtualKey, 0, null));
                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, first.Handle);
            }

            foreach (var character in "aimuyi")
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                Assert.True(ImeExports.ImeProcessKeyManaged(second.Handle, virtualKey, 0, null));
                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, second.Handle);
            }

            Assert.Equal(1, ImeExports.ImeDestroyManaged(0));
            Assert.False(ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(first.Handle)).IsComposing);
            Assert.False(ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(second.Handle)).IsComposing);
            Assert.False
            (
                (host.Service.GetSnapshotAsync(ImeSessionId.FromHimc(first.Handle)).GetAwaiter().GetResult())
                .IsComposing
            );
            Assert.False
            (
                (host.Service.GetSnapshotAsync(ImeSessionId.FromHimc(second.Handle)).GetAwaiter().GetResult())
                .IsComposing
            );
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }

        return Task.CompletedTask;
    }

    private static unsafe void ComposeProjectTerm
        (HostFixture host, string input, bool eatKeysFirst, out InMemoryImmContext himc)
    {
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));
        himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        try
        {
            host.Start();
            var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
            var list = (TransMsgList*)buffer;
            foreach (var character in input)
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                if (eatKeysFirst)
                {
                    Assert.True(ImeExports.ImeProcessKeyManaged(himc.Handle, virtualKey, 0, null));
                }

                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, himc.Handle);
            }

            Assert.Equal(input, himc.ReadCompositionText());
            Assert.Equal("XiaoXiIme", himc.ReadFirstCandidateText());
        }
        catch
        {
            himc.Dispose();
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
            throw;
        }
    }

    private static unsafe Task SetProjectTermThroughImeSetCompositionString(HostFixture host, string input)
    {
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());

        try
        {
            host.Start();
            fixed (char* compositionPointer = input)
            {
                Assert.Equal
                (
                    1,
                    ImeExports.ImeSetCompositionStringManaged
                    (
                        himc.Handle,
                        ImeConstants.ScsSetStr,
                        compositionPointer,
                        (uint)(input.Length * sizeof(char)),
                        null,
                        0
                    )
                );
            }

            var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();
            Assert.True(lastResult.Handled);
            Assert.Null(lastResult.CommitText);
            Assert.Equal(input, lastResult.Snapshot.Composition.Reading);
            Assert.Equal("XiaoXiIme", lastResult.Snapshot.Candidates[0].Text);
            Assert.Equal(input, himc.ReadCompositionText());
            Assert.Equal("XiaoXiIme", himc.ReadFirstCandidateText());
            Assert.Equal(0u, himc.CompositionString->ResultStrLength);

            fixed (char* emptyPointer = string.Empty)
            {
                Assert.Equal
                (
                    1,
                    ImeExports.ImeSetCompositionStringManaged
                    (
                        himc.Handle,
                        ImeConstants.ScsSetStr,
                        emptyPointer,
                        0,
                        null,
                        0
                    )
                );
            }

            var cleared = ImeModuleRuntime.GetLastProcessResultForTesting();
            Assert.True(cleared.Handled);
            Assert.False(cleared.Snapshot.IsComposing);
            Assert.Equal(0u, himc.CompositionString->CompStrLength);
            Assert.Equal(0u, himc.CompositionString->ResultStrLength);
            Assert.Equal(0u, himc.CandidateInfo->Count);
            Assert.False
            (
                (host.Service.GetSnapshotAsync(ImeSessionId.FromHimc(himc.Handle)).GetAwaiter().GetResult()).IsComposing
            );
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }

        return Task.CompletedTask;
    }

    private static unsafe Task QueryProjectTermThroughImeConversionList(HostFixture host, string input)
    {
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        const string currentComposition = "n";
        var buffer = new byte[256];
        var scheme = input == "xiaoxiaimuyi" ? "fullPinyin" : "xiaoheDoublePinyin";
        var packagePath = Path.Combine(Path.GetTempPath(), "ConversionListSemantics", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            XiaoXiImeProjectTerms.Parse(), packagePath,
            new DictionaryPackageParameters { InputScheme = scheme }
        );
        var context = new ImeContext(DictionaryPackageLoader.Load(packagePath));
        var interpretations = context.QueryInterpretations(input).Candidates;
        Assert.Equal(new[] { "XiaoXiIme", "小希IME", "小希" }, interpretations.Select(c => c.Candidate.Text));
        Assert.Equal
        (
            new[] { input.Length, input.Length, scheme == "fullPinyin" ? 6 : 4 },
            interpretations.Select(c => c.Interpretations[0].CoverageEnd)
        );
        Assert.All(interpretations, c => Assert.Equal(0, c.Interpretations[0].Segments[0].InputStart));
        Assert.Equal(new[] { 1, 2, 1 }, interpretations.Select(c => c.Interpretations[0].Segments.Count));
        context.SetComposition(input);
        var selection = context.QueryInterpretations(input).Candidates.Single(c => c.Candidate.Text == "小希");
        var partial = context.SelectCandidate(selection.SelectionId!.Value);
        Assert.Equal(("小希", "aimuyi"), (partial.CommitText, partial.Snapshot.Composition.Reading));

        try
        {
            host.Start();
            fixed (char* compositionPointer = currentComposition)
            {
                Assert.Equal
                (
                    1,
                    ImeExports.ImeSetCompositionStringManaged
                    (
                        himc.Handle,
                        ImeConstants.ScsSetStr,
                        compositionPointer,
                        (uint)(currentComposition.Length * sizeof(char)),
                        null,
                        0
                    )
                );
            }

            Assert.Equal(currentComposition, himc.ReadCompositionText());
            fixed (char* sourcePointer = input)
            fixed (byte* destination = buffer)
            {
                var required = ImeExports.ImeConversionListManaged
                (
                    himc.Handle,
                    sourcePointer,
                    null,
                    0,
                    ImeConstants.GclConversion
                );
                Assert.True(required > 0);
                Assert.Equal
                (
                    required,
                    ImeExports.ImeConversionListManaged
                    (
                        himc.Handle,
                        sourcePointer,
                        destination,
                        (uint)buffer.Length,
                        ImeConstants.GclConversion
                    )
                );

                var candidateList = (CandidateList*)destination;
                var texts = new List<string>();
                for (var index = 0; index < candidateList->Count; index++)
                {
                    texts.Add(new string((char*)(destination + candidateList->Offset[index])));
                }

                Assert.Equal(new[] { "XiaoXiIme", "小希IME", "小希" }, texts);
            }

            Assert.Equal(currentComposition, himc.ReadCompositionText());
            Assert.Equal
            (
                currentComposition,
                host.Service.GetSnapshotAsync
                    (ImeSessionId.FromHimc(himc.Handle)).GetAwaiter().GetResult().Composition.Reading
            );
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }

        return Task.CompletedTask;
    }

    private static unsafe Task QueryProjectTermThroughImeReverseConversionList(HostFixture host)
    {
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        const string currentComposition = "n";
        const string source = "XiaoXiIme";
        var buffer = new byte[256];

        try
        {
            host.Start();
            fixed (char* compositionPointer = currentComposition)
            {
                Assert.Equal
                (
                    1,
                    ImeExports.ImeSetCompositionStringManaged
                    (
                        himc.Handle,
                        ImeConstants.ScsSetStr,
                        compositionPointer,
                        (uint)(currentComposition.Length * sizeof(char)),
                        null,
                        0
                    )
                );
            }

            Assert.Equal(currentComposition, himc.ReadCompositionText());
            fixed (char* sourcePointer = source)
            fixed (byte* destination = buffer)
            {
                var required = ImeExports.ImeConversionListManaged
                (
                    himc.Handle,
                    sourcePointer,
                    null,
                    0,
                    ImeConstants.GclReverseConversion
                );
                Assert.True(required > 0);
                Assert.Equal
                (
                    required,
                    ImeExports.ImeConversionListManaged
                    (
                        himc.Handle,
                        sourcePointer,
                        destination,
                        (uint)buffer.Length,
                        ImeConstants.GclReverseConversion
                    )
                );

                var candidateList = (CandidateList*)destination;
                Assert.Equal(1u, candidateList->Count);
                Assert.Equal("xiao xi ai mu yi", new string((char*)(destination + candidateList->Offset[0])));
            }

            Assert.Equal(currentComposition, himc.ReadCompositionText());
            Assert.Equal
            (
                currentComposition,
                host.Service.GetSnapshotAsync
                    (ImeSessionId.FromHimc(himc.Handle)).GetAwaiter().GetResult().Composition.Reading
            );
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }

        return Task.CompletedTask;
    }

    private static unsafe Task QueryProjectTermThroughImeReverseLength(HostFixture host)
    {
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        const string currentComposition = "n";
        const string source = "XiaoXiIme";
        var buffer = new byte[256];
        Array.Fill(buffer, (byte)0xCC);

        try
        {
            host.Start();
            fixed (char* compositionPointer = currentComposition)
            {
                Assert.Equal
                (
                    1,
                    ImeExports.ImeSetCompositionStringManaged
                    (
                        himc.Handle,
                        ImeConstants.ScsSetStr,
                        compositionPointer,
                        (uint)(currentComposition.Length * sizeof(char)),
                        null,
                        0
                    )
                );
            }

            Assert.Equal(currentComposition, himc.ReadCompositionText());
            fixed (char* sourcePointer = source)
            fixed (byte* destination = buffer)
            {
                var required = ImeExports.ImeConversionListManaged
                (
                    himc.Handle,
                    sourcePointer,
                    destination,
                    (uint)buffer.Length,
                    ImeConstants.GclReverseLength
                );
                Assert.True(required > 0);
                Assert.Equal
                (
                    ImeCompositionContextWriter.GetRequiredConversionListSize
                    (
                        [new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")],
                        writeReading: true
                    ),
                    required
                );
                Assert.All(buffer, value => Assert.Equal((byte)0xCC, value));
            }

            Assert.Equal(currentComposition, himc.ReadCompositionText());
            Assert.Equal
            (
                currentComposition,
                host.Service.GetSnapshotAsync
                    (ImeSessionId.FromHimc(himc.Handle)).GetAwaiter().GetResult().Composition.Reading
            );
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }

        return Task.CompletedTask;
    }

    private static unsafe Task RegisterWordThroughImeConfigure(HostFixture host, string input)
    {
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var reading = "peizhi";
        var text = "配置词";

        try
        {
            host.Start();
            fixed (char* compositionPointer = input)
            {
                Assert.Equal
                (
                    1,
                    ImeExports.ImeSetCompositionStringManaged
                    (
                        himc.Handle,
                        ImeConstants.ScsSetStr,
                        compositionPointer,
                        (uint)(input.Length * sizeof(char)),
                        null,
                        0
                    )
                );
            }

            Assert.Equal(input, himc.ReadCompositionText());
            fixed (char* readingPointer = reading)
            fixed (char* textPointer = text)
            {
                var registerWord = stackalloc RegisterWord[1];
                registerWord->Reading = readingPointer;
                registerWord->Word = textPointer;
                Assert.Equal(1, ImeExports.ImeConfigureManaged(0, 0, ImeConstants.ImeConfigRegisterWord, registerWord));
            }

            string? enumeratedReading = null;
            string? enumeratedText = null;
            ImeExports.SetRegisterWordEnumProcForTesting
            ((readingPointer, _, textPointer, _) =>
                {
                    enumeratedReading = new string(readingPointer);
                    enumeratedText = new string(textPointer);
                    return 1;
                }
            );
            var enumerated = ImeExports.ImeEnumRegisterWordManaged(0, null, 0, null, null);
            var snapshot = host.Service.GetSnapshotAsync(ImeSessionId.FromHimc(himc.Handle)).GetAwaiter().GetResult();

            Assert.Equal(1u, enumerated);
            Assert.Equal(reading, enumeratedReading);
            Assert.Equal(text, enumeratedText);
            Assert.Equal(input, himc.ReadCompositionText());
            Assert.Equal(0u, himc.CompositionString->ResultStrLength);
            Assert.Equal(input, snapshot.Composition.Reading);
            Assert.True(snapshot.IsComposing);
        }
        finally
        {
            ImeExports.SetRegisterWordEnumProcForTesting(null);
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }

        return Task.CompletedTask;
    }

    private static unsafe Task CommitProjectTermThroughImmPath(HostFixture host, string input, bool eatKeysFirst)
    {
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(host.Options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
        var list = (TransMsgList*)buffer;

        try
        {
            host.Start();
            for (var index = 0; index < input.Length; index++)
            {
                var virtualKey = (uint)('A' + (input[index] - 'a'));
                if (eatKeysFirst)
                {
                    Assert.True(ImeExports.ImeProcessKeyManaged(himc.Handle, virtualKey, 0, null));
                }

                var result = ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, himc.Handle);
                var expectedMessages = index == 0
                    ? new[] { ImeConstants.WmImeStartComposition, ImeConstants.WmImeComposition }
                    : new[] { ImeConstants.WmImeComposition };
                Assert.Equal((uint)expectedMessages.Length, result);
                Assert.Equal((uint)expectedMessages.Length, list->Count);
                for (var messageIndex = 0; messageIndex < expectedMessages.Length; messageIndex++)
                {
                    Assert.Equal(expectedMessages[messageIndex], (&list->Message)[messageIndex].Message);
                }
            }

            Assert.Equal((uint)input.Length, himc.CompositionString->CompStrLength);
            Assert.Equal(input, himc.ReadCompositionText());
            Assert.Equal(1u, himc.CandidateInfo->Count);
            Assert.Equal("XiaoXiIme", himc.ReadFirstCandidateText());

            if (eatKeysFirst)
            {
                Assert.True(ImeExports.ImeProcessKeyManaged(himc.Handle, ImeConstants.VkSpace, 0, null));
            }

            var commit = ImeExports.ImeToAsciiExManaged(ImeConstants.VkSpace, 0, null, (nint)list, 0, himc.Handle);
            var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();

            Assert.Equal(2u, commit);
            Assert.Equal(ImeConstants.WmImeComposition, list->Message.Message);
            Assert.Equal((nuint)'X', list->Message.WParam);
            Assert.Equal((nint)ImeConstants.GcsResultStr, list->Message.LParam);
            Assert.Equal(ImeConstants.WmImeEndComposition, (&list->Message)[1].Message);
            Assert.Equal("XiaoXiIme", lastResult.CommitText);
            Assert.False(lastResult.Snapshot.IsComposing);
            Assert.Equal((uint)"XiaoXiIme".Length, himc.CompositionString->ResultStrLength);
            Assert.Equal("XiaoXiIme", himc.ReadResultText());
            Assert.Equal(0u, himc.CompositionString->CompStrLength);
            Assert.Equal(0u, himc.CandidateInfo->Count);
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }

        return Task.CompletedTask;
    }

    private static HostFixture CreateHost(string? inputScheme = null)
    {
        var hostRoot = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_ImeModule_{Guid.NewGuid():N}");
        var entries = XiaoXiImeProjectTerms.Parse();
        DictionaryPackageCompiler.Compile
        (
            entries,
            Path.Combine(hostRoot, DictionaryPackageLocations.FullPinyinPackageDirectoryName)
        );
        DictionaryPackageCompiler.Compile
        (
            entries,
            Path.Combine(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinPackageRelativePath),
            new DictionaryPackageParameters { InputScheme = DictionaryPackageLocations.XiaoheDoublePinyinInputScheme }
        );
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_ImeModule_{Guid.NewGuid():N}");
        return new HostFixture
        (
            new ImeHostService
            (
                options,
                inputScheme: inputScheme,
                hostBaseDirectory: hostRoot,
                userDictionaryPath: Path.Combine
                    (hostRoot, $"user-{inputScheme ?? DictionaryPackageLocations.FullPinyinInputScheme}.json")
            ),
            options
        );
    }

    private sealed class HostFixture : IDisposable
    {
        public HostFixture(ImeHostService service, XiaoXiImeIpcOptions options)
        {
            Service = service;
            Options = options;
        }

        public ImeHostService Service { get; }

        public XiaoXiImeIpcOptions Options { get; }

        public void Start() => Service.Start();

        public void Dispose() => Service.Dispose();
    }

    private sealed class UnavailableBridgeClient : ImeHostBridge.IImeHostBridgeClient
    {
        public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key, ImeSessionId sessionId)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("Host unavailable for test.");
        }

        public Task<ImeSessionSnapshot> GetSnapshotAsync(ImeSessionId sessionId)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("Host unavailable for test.");
        }

        public Task<ImeUiState> GetUiStateAsync(ImeSessionId sessionId)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("Host unavailable for test.");
        }

        public Task<ImeResetSessionResponse> ResetSessionAsync(ImeResetSessionRequest request)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("Host unavailable for test.");
        }

        public Task<ImeSetCompositionResponse> SetCompositionAsync(ImeSetCompositionRequest request)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("Host unavailable for test.");
        }

        public Task<ImeConversionListResponse> QueryConversionListAsync(ImeConversionListRequest request)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("Host unavailable for test.");
        }

        public Task<ImeRegisterWordResponse> RegisterWordAsync(ImeRegisterWordRequest request)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("Host unavailable for test.");
        }

        public void Dispose()
        {
        }
    }

    private sealed class FallbackDictionaryBridgeClient : ImeHostBridge.IImeHostBridgeClient
    {
        public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key, ImeSessionId sessionId)
        {
            return Task.FromResult(new ImeProcessResult(ImeSessionSnapshot.Empty, null, false));
        }

        public Task<ImeSessionSnapshot> GetSnapshotAsync(ImeSessionId sessionId)
        {
            return Task.FromResult(ImeSessionSnapshot.Empty);
        }

        public Task<ImeUiState> GetUiStateAsync(ImeSessionId sessionId)
        {
            return Task.FromResult
            (
                ImeUiState.FromSnapshot
                (
                    ImeSessionSnapshot.Empty,
                    "Using the minimal fallback dictionary.",
                    isHostUnavailable: false,
                    isUsingFallbackDictionary: true
                )
            );
        }

        public Task<ImeResetSessionResponse> ResetSessionAsync(ImeResetSessionRequest request)
        {
            return Task.FromResult(new ImeResetSessionResponse(request.SessionId, true));
        }

        public Task<ImeSetCompositionResponse> SetCompositionAsync(ImeSetCompositionRequest request)
        {
            return Task.FromResult
            (
                new ImeSetCompositionResponse
                (
                    new ImeProcessResult(ImeSessionSnapshot.Empty, null, false),
                    request.EffectiveSessionId
                )
            );
        }

        public Task<ImeConversionListResponse> QueryConversionListAsync(ImeConversionListRequest request)
        {
            return Task.FromResult(new ImeConversionListResponse([], request.EffectiveSessionId));
        }

        public Task<ImeRegisterWordResponse> RegisterWordAsync(ImeRegisterWordRequest request)
        {
            return Task.FromResult(new ImeRegisterWordResponse(false, [], request.EffectiveSessionId));
        }

        public void Dispose()
        {
        }
    }

    private sealed class TimeoutBridgeClient(ImeHostService? host = null) : ImeHostBridge.IImeHostBridgeClient
    {
        internal List<long> Generations { get; } = [];

        public async Task<ImeProcessResult> ProcessKeyAsync(ImeProcessKeyRequest request)
        {
            Generations.Add(request.Generation);
            if (host is not null)
            {
                var response = await host.ProcessKeyAsync(request);
                if (Generations.Count > 1)
                {
                    return response.Result;
                }
            }

            return await ProcessKeyAsync(request.Key, request.EffectiveSessionId);
        }

        public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key, ImeSessionId sessionId)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("IME IPC operation timed out.");
        }

        public Task<ImeSessionSnapshot> GetSnapshotAsync(ImeSessionId sessionId)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("IME IPC operation timed out.");
        }

        public Task<ImeUiState> GetUiStateAsync(ImeSessionId sessionId)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("IME IPC operation timed out.");
        }

        public Task<ImeResetSessionResponse> ResetSessionAsync(ImeResetSessionRequest request)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("IME IPC operation timed out.");
        }

        public Task<ImeSetCompositionResponse> SetCompositionAsync(ImeSetCompositionRequest request)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("IME IPC operation timed out.");
        }

        public Task<ImeConversionListResponse> QueryConversionListAsync(ImeConversionListRequest request)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("IME IPC operation timed out.");
        }

        public Task<ImeRegisterWordResponse> RegisterWordAsync(ImeRegisterWordRequest request)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("IME IPC operation timed out.");
        }

        public void Dispose()
        {
        }
    }

    private sealed class InvalidBridgeClient : ImeHostBridge.IImeHostBridgeClient
    {
        public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key, ImeSessionId sessionId)
        {
            throw new InvalidOperationException("Programming error for test.");
        }

        public Task<ImeSessionSnapshot> GetSnapshotAsync(ImeSessionId sessionId)
        {
            throw new InvalidOperationException("Programming error for test.");
        }

        public Task<ImeUiState> GetUiStateAsync(ImeSessionId sessionId)
        {
            throw new InvalidOperationException("Programming error for test.");
        }

        public Task<ImeResetSessionResponse> ResetSessionAsync(ImeResetSessionRequest request)
        {
            throw new InvalidOperationException("Programming error for test.");
        }

        public Task<ImeSetCompositionResponse> SetCompositionAsync(ImeSetCompositionRequest request)
        {
            throw new InvalidOperationException("Programming error for test.");
        }

        public Task<ImeConversionListResponse> QueryConversionListAsync(ImeConversionListRequest request)
        {
            throw new InvalidOperationException("Programming error for test.");
        }

        public Task<ImeRegisterWordResponse> RegisterWordAsync(ImeRegisterWordRequest request)
        {
            throw new InvalidOperationException("Programming error for test.");
        }

        public void Dispose()
        {
        }
    }
}