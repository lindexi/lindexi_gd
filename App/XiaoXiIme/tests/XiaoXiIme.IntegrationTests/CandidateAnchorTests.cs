using System.Text.Json;
using Avalonia;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;
using XiaoXiIme.ImeUi.Avalonia;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.IntegrationTests;

public class CandidateAnchorTests
{
    private static ImeHostService CreateHost() => new(productSettingsPath: Path.Combine(Path.GetTempPath(), "XiaoXiIme-anchor-tests", Guid.NewGuid().ToString("N"), "settings.json"));
    [Theory]
    [InlineData(1, 900, 900, 800, 700)]
    [InlineData(1.5, 900, 900, 700, 650)]
    [InlineData(2, 900, 900, 600, 600)]
    [InlineData(1, 0, 0, 0, 0)]
    public void Placement_UsesPhysicalSizeAndFlipsAtBottom(double scaling, int x, int y, int expectedX, int expectedY)
    {
        var result = CandidateWindowPlacement.Calculate(new PixelPoint(x, y), y - 100,
            new Size(200, 100), scaling, new PixelRect(0, 0, 1000, 900));
        Assert.Equal(new PixelPoint(expectedX, expectedY), result);
    }

    [Fact]
    public void Placement_NegativeMonitorOriginIsPreserved()
    {
        Assert.Equal(new PixelPoint(-1800, -100), CandidateWindowPlacement.Calculate(
            new PixelPoint(-1800, -100), -100, new Size(200, 100), 1,
            new PixelRect(-1920, -200, 1920, 1080)));
    }

    [Fact]
    public void Placement_OversizeWindowClampsToWorkAreaOrigin()
    {
        Assert.Equal(new PixelPoint(10, 20), CandidateWindowPlacement.Calculate(
            new PixelPoint(100, 100), 100, new Size(2000, 2000), 2,
            new PixelRect(10, 20, 1000, 900)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void Placement_InvalidScalingIsRejected(double scaling)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CandidateWindowPlacement.Calculate(
            default, 0, new Size(100, 100), scaling, new PixelRect(0, 0, 1000, 900)));
    }

    [Fact]
    public void Anchor_AotJsonRoundTripPreservesIdentityAndZeroCoordinate()
    {
        var request = new ImeProcessKeyRequest(new ImeKey(ImeKeyKind.Character, 'n'), new("first"),
            Anchor: CreateAnchor(new("first"), 0));
        var json = JsonSerializer.Serialize(request, XiaoXiImeIpcJsonSerializerContext.Default.ImeProcessKeyRequest);
        Assert.Equal(request, JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeProcessKeyRequest));
    }

    [Fact]
    public void Anchor_OldJsonOmitsOptionalPosition()
    {
        var request = JsonSerializer.Deserialize("{\"key\":{\"kind\":\"character\",\"character\":\"n\"},\"sessionId\":\"first\"}",
            XiaoXiImeIpcJsonSerializerContext.Default.ImeProcessKeyRequest);
        Assert.Null(request?.Anchor);
    }

    [Fact]
    public async Task Host_TwoSessionsKeepIndependentAnchors()
    {
        using var host = CreateHost();
        var first = new ImeSessionId("first");
        var second = new ImeSessionId("second");
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'n'), first, Anchor: CreateAnchor(first, 100)));
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'n'), second, Anchor: CreateAnchor(second, 500)));
        Assert.Equal(100, (await host.GetUiStateAsync(first)).Anchor?.ScreenX);
    }

    [Fact]
    public async Task Host_MismatchedIdentityDoesNotReuseOldAnchor()
    {
        using var host = CreateHost();
        var session = new ImeSessionId("first");
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'n'), session, Anchor: CreateAnchor(session, 100)));
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'i'), session, Anchor: CreateAnchor(new("second"), 500)));
        Assert.Null((await host.GetUiStateAsync(session)).Anchor);
    }

    [Fact]
    public async Task Host_ResetSessionClearsAnchor()
    {
        using var host = CreateHost();
        var session = new ImeSessionId("first");
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'n'), session, Anchor: CreateAnchor(session, 100)));
        await host.ResetSessionAsync(session);
        Assert.Null((await host.GetUiStateAsync(session)).Anchor);
    }

    [Fact]
    public void InvalidWindow_DoesNotProducePhysicalCoordinates()
    {
        if (!OperatingSystem.IsWindows())
            return;
        Assert.False(InputTargetMethods.TryGetPhysicalPoint(0, 100, 200, out _));
    }

    [Fact]
    public async Task Host_FocusReturnRestoresFirstSessionWithoutNewKey()
    {
        using var host = CreateHost();
        var first = new ImeSessionId("first");
        var second = new ImeSessionId("second");
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'n'), first, Anchor: CreateAnchor(first, 100)));
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'i'), second,
            Anchor: CreateAnchor(second, 500) with { WindowHandle = 456 }));

        var state = await host.GetUiStateAsync(new ImeUiStateRequest(FocusWindowHandle: 123, FocusProcessId: 1));

        Assert.Equal(first, state.SessionId);
    }

    [Fact]
    public async Task Host_UnknownFocusDoesNotReturnLastActiveCandidates()
    {
        using var host = CreateHost();
        var session = new ImeSessionId("first");
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'n'), session, Anchor: CreateAnchor(session, 100)));

        var state = await host.GetUiStateAsync(new ImeUiStateRequest(FocusWindowHandle: 123, FocusProcessId: 2));

        Assert.Null(state.Anchor);
    }

    [Fact]
    public async Task Host_ExplicitSessionIgnoresFocusFilter()
    {
        using var host = CreateHost();
        var session = new ImeSessionId("first");
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'n'), session, Anchor: CreateAnchor(session, 100)));

        var state = await host.GetUiStateAsync(new ImeUiStateRequest(session, 456, 2));

        Assert.Equal(100, state.Anchor?.ScreenX);
    }

    [Fact]
    public async Task Host_AmbiguousTargetDoesNotSelectAnotherSession()
    {
        using var host = CreateHost();
        var first = new ImeSessionId("first");
        var second = new ImeSessionId("second");
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'n'), first, Anchor: CreateAnchor(first, 100)));
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(new(ImeKeyKind.Character, 'i'), second, Anchor: CreateAnchor(second, 500)));

        var state = await host.GetUiStateAsync(new ImeUiStateRequest(FocusWindowHandle: 123, FocusProcessId: 1));

        Assert.Null(state.Anchor);
    }

    private static ImeCandidateAnchor CreateAnchor(ImeSessionId session, int x) =>
        new(session, 123, 1, 2, x, 200, x, 200, 180, 96, "caret", true);
}
