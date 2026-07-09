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
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsHostStatusResponseForAot()
    {
        var response = new ImeHostStatusResponse(new ImeHostStatus(IsRunning: true));

        var json = JsonSerializer.Serialize(response, XiaoXiImeIpcJsonSerializerContext.Default.ImeHostStatusResponse);
        var deserialized = JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeHostStatusResponse);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Status.IsRunning);
        Assert.Null(deserialized.Status.LastError);
    }

    [Fact]
    public void JsonSerializerContext_RoundTripsParameterlessRequestDtosForAot()
    {
        RoundTrip(new ImeSnapshotRequest(), XiaoXiImeIpcJsonSerializerContext.Default.ImeSnapshotRequest);
        RoundTrip(new ImeUiStateRequest(), XiaoXiImeIpcJsonSerializerContext.Default.ImeUiStateRequest);
        RoundTrip(new ImeHostStatusRequest(), XiaoXiImeIpcJsonSerializerContext.Default.ImeHostStatusRequest);

        static void RoundTrip<T>(T request, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo)
        {
            var json = JsonSerializer.Serialize(request, jsonTypeInfo);
            var deserialized = JsonSerializer.Deserialize(json, jsonTypeInfo);

            Assert.NotNull(deserialized);
        }
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

