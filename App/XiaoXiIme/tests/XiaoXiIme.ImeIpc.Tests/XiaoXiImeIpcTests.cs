using System.Text.Json;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc.Tests;

public class XiaoXiImeIpcTests
{
    [Fact]
    public void Routes_AreStableDirectRouteNames()
    {
        Assert.Equal("XiaoXiIme.ProcessKey", XiaoXiImeIpcRoutes.ProcessKey);
        Assert.Equal("XiaoXiIme.GetSnapshot", XiaoXiImeIpcRoutes.GetSnapshot);
        Assert.Equal("XiaoXiIme.GetUiState", XiaoXiImeIpcRoutes.GetUiState);
        Assert.Equal("XiaoXiIme.GetHostStatus", XiaoXiImeIpcRoutes.GetHostStatus);
        Assert.Equal("XiaoXiIme.ResetSession", XiaoXiImeIpcRoutes.ResetSession);
        Assert.Equal("XiaoXiIme.SetComposition", XiaoXiImeIpcRoutes.SetComposition);
        Assert.Equal("XiaoXiIme.QueryConversionList", XiaoXiImeIpcRoutes.QueryConversionList);
        Assert.Equal("XiaoXiIme.RegisterWord", XiaoXiImeIpcRoutes.RegisterWord);
        Assert.Equal("XiaoXiIme.SnapshotChanged", XiaoXiImeIpcRoutes.NotifySnapshotChanged);
    }

    [Fact]
    public void DefaultOptions_UseImeHostServerName()
    {
        Assert.Equal("XiaoXiIme_ImeHost", XiaoXiImeIpcOptions.Default.ServerName);
    }

    [Fact]
    public void JsonSerializerContext_SerializesProcessKeyRequestForAot()
    {
        var request = new ImeProcessKeyRequest(ImeKey.FromCharacter('n'));

        var json = JsonSerializer.Serialize(request, XiaoXiImeIpcJsonSerializerContext.Default.ImeProcessKeyRequest);
        var deserialized = JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeProcessKeyRequest);

        Assert.NotNull(deserialized);
        Assert.Equal(ImeKeyKind.Character, deserialized.Key.Kind);
        Assert.Equal('n', deserialized.Key.Character);
        Assert.Equal(ImeSessionId.Default, deserialized.EffectiveSessionId);
        Assert.Contains("\"kind\":\"character\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonSerializerContext_SerializesSelectCandidateProcessKeyRequestForAot()
    {
        var request = new ImeProcessKeyRequest(ImeKey.SelectCandidate(0));

        var json = JsonSerializer.Serialize(request, XiaoXiImeIpcJsonSerializerContext.Default.ImeProcessKeyRequest);
        var deserialized = JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeProcessKeyRequest);

        Assert.NotNull(deserialized);
        Assert.Equal(ImeKeyKind.CandidateSelection, deserialized.Key.Kind);
        Assert.Equal(0, deserialized.Key.CandidateIndex);
        Assert.Contains("candidateSelection", json, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsSessionScopedSnapshotAndUiStateRequests()
    {
        var sessionId = ImeSessionId.FromHimc(0x1234);
        var snapshotJson = JsonSerializer.Serialize(new ImeSnapshotRequest(sessionId), XiaoXiImeIpcJsonSerializerContext.Default.ImeSnapshotRequest);
        var snapshot = JsonSerializer.Deserialize(snapshotJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeSnapshotRequest);
        var uiStateJson = JsonSerializer.Serialize(new ImeUiStateRequest(sessionId), XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateRequest);
        var uiState = JsonSerializer.Deserialize(uiStateJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateRequest);
        var processKeyJson = JsonSerializer.Serialize(new ImeProcessKeyRequest(ImeKey.FromCharacter('x'), sessionId), XiaoXiImeIpcJsonSerializerContext.Default.ImeProcessKeyRequest);
        var processKey = JsonSerializer.Deserialize(processKeyJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeProcessKeyRequest);

        Assert.Equal(sessionId, snapshot?.SessionId);
        Assert.Equal(sessionId, uiState?.SessionId);
        Assert.Equal(sessionId, processKey?.EffectiveSessionId);
        Assert.Equal("himc:1234", sessionId.Value);
        Assert.False(sessionId.IsUnspecified);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsUnspecifiedSessionAsLastActiveFallback()
    {
        var snapshotJson = JsonSerializer.Serialize(new ImeSnapshotRequest(), XiaoXiImeIpcJsonSerializerContext.Default.ImeSnapshotRequest);
        var snapshot = JsonSerializer.Deserialize(snapshotJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeSnapshotRequest);
        var uiStateJson = JsonSerializer.Serialize(new ImeUiStateRequest(), XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateRequest);
        var uiState = JsonSerializer.Deserialize(uiStateJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateRequest);

        Assert.True(snapshot?.SessionId.IsUnspecified);
        Assert.True(uiState?.SessionId.IsUnspecified);
        Assert.Equal(ImeSessionId.Unspecified, snapshot?.SessionId);
        Assert.Equal(ImeSessionId.Default, snapshot?.SessionId.Effective);
    }

    [Fact]
    public void CreateConfiguration_ReturnsAotJsonConfiguration()
    {
        var configuration = XiaoXiImeIpcProviderFactory.CreateConfiguration();

        Assert.IsType<XiaoXiImeAotJsonIpcObjectSerializer>(configuration.IpcObjectSerializer);
    }

    [Fact]
    public void AotJsonIpcObjectSerializer_RoundTripsProcessKeyRequest()
    {
        var serializer = new XiaoXiImeAotJsonIpcObjectSerializer(XiaoXiImeIpcJsonSerializerContext.Default);
        var request = new ImeProcessKeyRequest(ImeKey.FromCharacter('x'));

        var bytes = serializer.Serialize(request);
        var deserialized = serializer.Deserialize<ImeProcessKeyRequest>(bytes, 0, bytes.Length);

        Assert.Equal(ImeKeyKind.Character, deserialized.Key.Kind);
        Assert.Equal('x', deserialized.Key.Character);
        Assert.Equal(ImeSessionId.Default, deserialized.EffectiveSessionId);
    }

    [Fact]
    public void AotJsonIpcObjectSerializer_RoundTripsSessionScopedAndUnspecifiedRequests()
    {
        var serializer = new XiaoXiImeAotJsonIpcObjectSerializer(XiaoXiImeIpcJsonSerializerContext.Default);
        var sessionId = ImeSessionId.FromHimc(0x11);
        var processKeyBytes = serializer.Serialize(new ImeProcessKeyRequest(ImeKey.FromCharacter('x'), sessionId));
        var processKey = serializer.Deserialize<ImeProcessKeyRequest>(processKeyBytes, 0, processKeyBytes.Length);
        var snapshotBytes = serializer.Serialize(new ImeSnapshotRequest());
        var snapshot = serializer.Deserialize<ImeSnapshotRequest>(snapshotBytes, 0, snapshotBytes.Length);
        var uiStateBytes = serializer.Serialize(new ImeUiStateRequest(sessionId));
        var uiState = serializer.Deserialize<ImeUiStateRequest>(uiStateBytes, 0, uiStateBytes.Length);

        Assert.Equal(sessionId, processKey.EffectiveSessionId);
        Assert.True(snapshot.SessionId.IsUnspecified);
        Assert.Equal(sessionId, uiState.SessionId);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsResetSessionRequestForAot()
    {
        var sessionId = ImeSessionId.FromHimc(0x11);
        var requestJson = JsonSerializer.Serialize(new ImeResetSessionRequest(sessionId), XiaoXiImeIpcJsonSerializerContext.Default.ImeResetSessionRequest);
        var request = JsonSerializer.Deserialize(requestJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeResetSessionRequest);
        var resetAllJson = JsonSerializer.Serialize(new ImeResetSessionRequest(ResetAll: true), XiaoXiImeIpcJsonSerializerContext.Default.ImeResetSessionRequest);
        var resetAll = JsonSerializer.Deserialize(resetAllJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeResetSessionRequest);
        var responseJson = JsonSerializer.Serialize(new ImeResetSessionResponse(sessionId, true), XiaoXiImeIpcJsonSerializerContext.Default.ImeResetSessionResponse);
        var response = JsonSerializer.Deserialize(responseJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeResetSessionResponse);

        Assert.Equal(sessionId, request?.SessionId);
        Assert.False(request?.ResetAll);
        Assert.True(resetAll?.ResetAll);
        Assert.True(resetAll?.SessionId.IsUnspecified);
        Assert.Equal(sessionId, response?.SessionId);
        Assert.True(response?.Reset);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsSetCompositionRequestForAot()
    {
        var sessionId = ImeSessionId.FromHimc(0x22);
        var snapshot = new ImeSessionSnapshot(
            new CompositionText("xiaoxiaimuyi", "xiaoxiaimuyi", 12),
            [new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")],
            new ImeCandidateWindowState(0, 0, 1),
            IsComposing: true);
        var requestJson = JsonSerializer.Serialize(
            new ImeSetCompositionRequest("xiaoxiaimuyi", sessionId),
            XiaoXiImeIpcJsonSerializerContext.Default.ImeSetCompositionRequest);
        var request = JsonSerializer.Deserialize(requestJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeSetCompositionRequest);
        var responseJson = JsonSerializer.Serialize(
            new ImeSetCompositionResponse(new ImeProcessResult(snapshot, null, true), sessionId),
            XiaoXiImeIpcJsonSerializerContext.Default.ImeSetCompositionResponse);
        var response = JsonSerializer.Deserialize(responseJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeSetCompositionResponse);

        Assert.Equal("xiaoxiaimuyi", request?.Composition);
        Assert.Equal(sessionId, request?.EffectiveSessionId);
        Assert.True(response?.Result.Handled);
        Assert.Null(response?.Result.CommitText);
        Assert.Equal("xiaoxiaimuyi", response?.Result.Snapshot.Composition.Reading);
        Assert.Equal("XiaoXiIme", response?.Result.Snapshot.Candidates[0].Text);
        Assert.Equal(sessionId, response?.SessionId);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsConversionListRequestForAot()
    {
        var sessionId = ImeSessionId.FromHimc(0x33);
        var requestJson = JsonSerializer.Serialize(
            new ImeConversionListRequest("xiaoxiaimuyi", 9, sessionId),
            XiaoXiImeIpcJsonSerializerContext.Default.ImeConversionListRequest);
        var request = JsonSerializer.Deserialize(requestJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeConversionListRequest);
        var responseJson = JsonSerializer.Serialize(
            new ImeConversionListResponse([new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")], sessionId),
            XiaoXiImeIpcJsonSerializerContext.Default.ImeConversionListResponse);
        var response = JsonSerializer.Deserialize(responseJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeConversionListResponse);

        Assert.Equal("xiaoxiaimuyi", request?.Source);
        Assert.Equal(9, request?.MaxCount);
        Assert.Equal(ImeConversionListKind.Conversion, request?.Kind);
        Assert.Equal(sessionId, request?.EffectiveSessionId);
        Assert.Equal("XiaoXiIme", response?.Candidates[0].Text);
        Assert.Equal(sessionId, response?.SessionId);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsReverseConversionListRequestForAot()
    {
        var sessionId = ImeSessionId.FromHimc(0x34);
        var requestJson = JsonSerializer.Serialize(
            new ImeConversionListRequest("XiaoXiIme", 9, sessionId, ImeConversionListKind.ReverseConversion),
            XiaoXiImeIpcJsonSerializerContext.Default.ImeConversionListRequest);
        var request = JsonSerializer.Deserialize(requestJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeConversionListRequest);

        Assert.Equal("XiaoXiIme", request?.Source);
        Assert.Equal(9, request?.MaxCount);
        Assert.Equal(ImeConversionListKind.ReverseConversion, request?.Kind);
        Assert.Equal(sessionId, request?.EffectiveSessionId);
        Assert.Contains("reverseConversion", requestJson, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsRegisterWordRequestForAot()
    {
        var sessionId = ImeSessionId.FromHimc(0x44);
        var requestJson = JsonSerializer.Serialize(
            new ImeRegisterWordRequest(ImeRegisterWordAction.Register, "zidingyi", "自定义", 0x80000000, sessionId),
            XiaoXiImeIpcJsonSerializerContext.Default.ImeRegisterWordRequest);
        var request = JsonSerializer.Deserialize(requestJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeRegisterWordRequest);
        var responseJson = JsonSerializer.Serialize(
            new ImeRegisterWordResponse(true, [new ImeRegisterWordEntry("zidingyi", "自定义")], sessionId),
            XiaoXiImeIpcJsonSerializerContext.Default.ImeRegisterWordResponse);
        var response = JsonSerializer.Deserialize(responseJson, XiaoXiImeIpcJsonSerializerContext.Default.ImeRegisterWordResponse);

        Assert.Equal(ImeRegisterWordAction.Register, request?.Action);
        Assert.Equal("zidingyi", request?.Reading);
        Assert.Equal("自定义", request?.Text);
        Assert.Equal(0x80000000u, request?.Style);
        Assert.Equal(sessionId, request?.EffectiveSessionId);
        Assert.True(response?.Succeeded);
        Assert.Equal("zidingyi", response?.Entries[0].Reading);
        Assert.Equal("自定义", response?.Entries[0].Text);
        Assert.Equal(sessionId, response?.SessionId);
    }

    [Fact]
    public void AotJsonIpcObjectSerializer_RoundTripsProcessKeyRequestElement()
    {
        var serializer = new XiaoXiImeAotJsonIpcObjectSerializer(XiaoXiImeIpcJsonSerializerContext.Default);
        var request = new ImeProcessKeyRequest(ImeKey.FromCharacter('x'));

        var element = serializer.SerializeToElement(request);
        var deserialized = serializer.Deserialize<ImeProcessKeyRequest>(element);

        Assert.Equal(ImeKeyKind.Character, deserialized.Key.Kind);
        Assert.Equal('x', deserialized.Key.Character);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsSnapshotWithGuidelineAndCandidateWindow()
    {
        var snapshot = new ImeSessionSnapshot(
            new CompositionText("ni", "ni", 2),
            [new ImeCandidate("你", "ni")],
            new ImeCandidateWindowState(0, 0, 1),
            IsComposing: true,
            new ImeGuideline(ImeGuidelineLevel.Reading, "ni"));
        var response = new ImeSnapshotResponse(snapshot);

        var json = JsonSerializer.Serialize(response, XiaoXiImeIpcJsonSerializerContext.Default.ImeSnapshotResponse);
        var deserialized = JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeSnapshotResponse);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Snapshot.IsComposing);
        Assert.Equal("ni", deserialized.Snapshot.Composition.Reading);
        Assert.Equal("你", deserialized.Snapshot.Candidates[0].Text);
        Assert.Equal(1, deserialized.Snapshot.CandidateWindow.PageSize);
        Assert.Equal(ImeGuidelineLevel.Reading, deserialized.Snapshot.EffectiveGuideline.Level);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsUiStateResponseForAot()
    {
        var uiState = new ImeUiState(
            CandidateWindowVisible: true,
            new CompositionText("ni", "ni", 2),
            [new ImeCandidate("你", "ni")],
            new ImeCandidateWindowState(0, 0, 1),
            new ImeGuideline(ImeGuidelineLevel.Reading, "ni"));

        var json = JsonSerializer.Serialize(new ImeUiStateResponse(uiState), XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateResponse);
        var deserialized = JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateResponse);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.UiState.CandidateWindowVisible);
        Assert.Equal("ni", deserialized.UiState.Composition.Reading);
        Assert.Equal("你", deserialized.UiState.Candidates[0].Text);
        Assert.Equal(ImeAboutState.SeWzc, deserialized.UiState.EffectiveAbout);
        Assert.False(deserialized.UiState.IsHostUnavailable);
        Assert.False(deserialized.UiState.IsUsingFallbackDictionary);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsHostStatusResponseForAot()
    {
        var response = new ImeHostStatusResponse(new ImeHostStatus(
            IsRunning: true,
            LastError: null,
            DictionaryPackagePath: @"C:\XiaoXiIme.DictionaryPackage",
            IsUsingFallbackDictionary: true,
            About: ImeAboutState.SeWzc,
            DictionaryLoadError: "Fallback dictionary is active.",
            DictionaryInputScheme: "xiaoheDoublePinyin"));

        var json = JsonSerializer.Serialize(response, XiaoXiImeIpcJsonSerializerContext.Default.ImeHostStatusResponse);
        var deserialized = JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeHostStatusResponse);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Status.IsRunning);
        Assert.Null(deserialized.Status.LastError);
        Assert.Equal("Fallback dictionary is active.", deserialized.Status.DictionaryLoadError);
        Assert.Equal(@"C:\XiaoXiIme.DictionaryPackage", deserialized.Status.DictionaryPackagePath);
        Assert.True(deserialized.Status.IsUsingFallbackDictionary);
        Assert.Equal("xiaoheDoublePinyin", deserialized.Status.DictionaryInputScheme);
        Assert.Equal(ImeAboutState.SeWzc, deserialized.Status.EffectiveAbout);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsHostUnavailableUiStateForAot()
    {
        var uiState = ImeUiState.FromSnapshot(
            ImeSessionSnapshot.Empty,
            "Host unavailable; using the minimal fallback dictionary.",
            isHostUnavailable: true,
            isUsingFallbackDictionary: false);

        var json = JsonSerializer.Serialize(new ImeUiStateResponse(uiState), XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateResponse);
        var deserialized = JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateResponse);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.UiState.IsHostUnavailable);
        Assert.False(deserialized.UiState.IsUsingFallbackDictionary);
        Assert.Contains("Host unavailable", deserialized.UiState.DiagnosticText, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsParameterlessRequestDtosForAot()
    {
        var snapshot = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(new ImeSnapshotRequest(), XiaoXiImeIpcJsonSerializerContext.Default.ImeSnapshotRequest),
            XiaoXiImeIpcJsonSerializerContext.Default.ImeSnapshotRequest);
        var uiState = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(new ImeUiStateRequest(), XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateRequest),
            XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateRequest);
        RoundTrip(new ImeSnapshotRequest(), XiaoXiImeIpcJsonSerializerContext.Default.ImeSnapshotRequest);
        RoundTrip(new ImeUiStateRequest(), XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateRequest);
        RoundTrip(new ImeHostStatusRequest(), XiaoXiImeIpcJsonSerializerContext.Default.ImeHostStatusRequest);

        static void RoundTrip<T>(T request, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo)
        {
            var json = JsonSerializer.Serialize(request, jsonTypeInfo);
            var deserialized = JsonSerializer.Deserialize(json, jsonTypeInfo);

            Assert.NotNull(deserialized);
        }

        Assert.Equal(default(ImeSessionId), snapshot?.SessionId);
        Assert.Equal(default(ImeSessionId), uiState?.SessionId);
    }

    [Fact]
    public void AotJsonIpcObjectSerializer_ReturnsDefaultForUnknownDeserializeType()
    {
        var serializer = new XiaoXiImeAotJsonIpcObjectSerializer(XiaoXiImeIpcJsonSerializerContext.Default);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new UnknownSerializerProbe("value"));

        var deserialized = serializer.Deserialize<UnknownSerializerProbe>(bytes, 0, bytes.Length);

        Assert.Null(deserialized);
    }

    private sealed record UnknownSerializerProbe(string Value);
}

