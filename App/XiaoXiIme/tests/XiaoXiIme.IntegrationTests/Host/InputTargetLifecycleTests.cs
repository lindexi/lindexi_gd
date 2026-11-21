using System.Text.Json;
using Avalonia;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

public class InputTargetLifecycleTests
{
    private static ImeHostService CreateHost() => new
    (
        productSettingsPath: Path.Combine
        (
            Path.GetTempPath(), "XiaoXiIme-target-tests", Guid.NewGuid().ToString("N"), "settings.json"
        )
    );

    private static ImeInputTargetUpdate Update
    (
        string session, long sequence, bool active = true,
        string context = "context", int x = 10
    ) => new
    (
        "owner", sequence, new(session), context, active,
        new
        (
            new(session), 123, 456, 789, x, 30, x, 30, 20, 96, "candidate-exclude", true,
            ClientExclusion: new(0, 20, 100, 30)
        )
    );

    [Fact]
    public async Task NextGenerationDiscardsPreviousComposition()
    {
        using var host = CreateHost();
        var session = new ImeSessionId("first");
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(ImeKey.FromCharacter('n'), session));

        var resumed = await host.ProcessKeyAsync(new ImeProcessKeyRequest(ImeKey.FromCharacter('i'), session, Generation: 1));

        Assert.Equal("i", resumed.Result.Snapshot.Composition.Reading);
    }

    [Fact]
    public async Task PreviousGenerationCannotChangeResumedComposition()
    {
        using var host = CreateHost();
        var session = new ImeSessionId("first");
        await host.ProcessKeyAsync(new ImeProcessKeyRequest(ImeKey.FromCharacter('n'), session, Generation: 1));

        await host.ProcessKeyAsync(new ImeProcessKeyRequest(ImeKey.FromCharacter('i'), session));
        var snapshot = await host.GetSnapshotAsync(session);

        Assert.Equal("n", snapshot.Composition.Reading);
    }

    [Fact]
    public async Task ActivationWithoutKeyPublishesAnchor()
    {
        using var host = CreateHost();
        await host.UpdateInputTargetAsync(Update("first", 1));
        Assert.Equal
        (
            10, (await host.GetUiStateAsync
            (
                new ImeUiStateRequest
                (
                    FocusWindowHandle: 123, FocusProcessId: 456
                )
            )).Anchor?.ScreenX
        );
    }

    [Fact]
    public async Task SameWindowSwitchFollowsActiveContextWithoutAnotherKey()
    {
        using var host = CreateHost();
        await host.UpdateInputTargetAsync(Update("first", 1, context: "a"));
        await host.UpdateInputTargetAsync(Update("second", 2, context: "b", x: 70));
        await host.UpdateInputTargetAsync(Update("first", 3, context: "a"));
        Assert.Equal
        (
            new ImeSessionId("first"), (await host.GetUiStateAsync
            (
                new ImeUiStateRequest
                (
                    FocusWindowHandle: 123, FocusProcessId: 456
                )
            )).SessionId
        );
    }

    [Fact]
    public async Task DeactivationWithoutAnchorClearsActiveTarget()
    {
        using var host = CreateHost();
        await host.UpdateInputTargetAsync(Update("first", 1));
        await host.UpdateInputTargetAsync(Update("first", 2, false) with { Anchor = null });
        Assert.Null
        (
            (await host.GetUiStateAsync
            (
                new ImeUiStateRequest
                (
                    FocusWindowHandle: 123, FocusProcessId: 456
                )
            )).Anchor
        );
    }

    [Fact]
    public async Task DeactivationAndReactivationPreserveComposition()
    {
        using var host = CreateHost();
        await host.UpdateInputTargetAsync(Update("first", 1));
        await host.SetCompositionAsync("ni", new ImeSessionId("first"));
        await host.UpdateInputTargetAsync(Update("first", 2, false));
        await host.UpdateInputTargetAsync(Update("first", 3));
        Assert.Equal("ni", (await host.GetSnapshotAsync(new ImeSessionId("first"))).Composition.Reading);
    }

    [Fact]
    public async Task OldObservationCannotReplaceNewAnchor()
    {
        using var host = CreateHost();
        await host.UpdateInputTargetAsync(Update("first", 5, x: 70));
        await host.UpdateInputTargetAsync(Update("first", 4, x: 10));
        Assert.Equal(70, (await host.GetUiStateAsync(new ImeSessionId("first"))).Anchor?.ScreenX);
    }

    [Fact]
    public async Task ReusedContextHandleRejectsOldActivation()
    {
        using var host = CreateHost();
        await host.UpdateInputTargetAsync(Update("first", 1));
        await host.UpdateInputTargetAsync(Update("replacement", 2));
        Assert.False((await host.UpdateInputTargetAsync(Update("first", 3))).Accepted);
    }

    [Fact]
    public async Task ReusedContextHandleRejectsOldKey()
    {
        using var host = CreateHost();
        await host.UpdateInputTargetAsync(Update("first", 1));
        await host.UpdateInputTargetAsync(Update("replacement", 2));
        var response = await host.ProcessKeyAsync
        (
            new ImeProcessKeyRequest
            (
                new(ImeKeyKind.Character, 'n'), new("first")
            )
        );
        Assert.False(response.Result.Handled);
    }

    [Fact]
    public async Task ReusedContextHandleRejectsOldComposition()
    {
        using var host = CreateHost();
        await host.UpdateInputTargetAsync(Update("first", 1));
        await host.UpdateInputTargetAsync(Update("replacement", 2));
        var response = await host.SetCompositionAsync(new ImeSetCompositionRequest("ni", new("first")));
        Assert.False(response.Result.Handled);
    }

    [Fact]
    public void TargetUpdateAotRoundTripPreservesFullExclusionRectangle()
    {
        var update = Update("first", 1);
        var json = JsonSerializer.Serialize(update, XiaoXiImeIpcJsonSerializerContext.Default.ImeInputTargetUpdate);
        Assert.Equal
            (update, JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeInputTargetUpdate));
    }

    [Fact]
    public void PlacementClampedAtBottomAvoidsExclusionAbove()
    {
        Assert.Equal
        (
            new PixelPoint(100, 600), CandidateWindowPlacement.Calculate
            (
                new PixelPoint(100, 850), 800, new Size(200, 200), 1,
                new PixelRect(0, 0, 1000, 900), new PixelRect(0, 800, 500, 100)
            )
        );
    }

    [Fact]
    public void PlacementUsesFreeSideWhenNeitherVerticalSideFits()
    {
        Assert.Equal
        (
            new PixelPoint(500, 400), CandidateWindowPlacement.Calculate
            (
                new PixelPoint(100, 400), 0, new Size(200, 200), 1,
                new PixelRect(0, 0, 1000, 900), new PixelRect(0, 0, 500, 900)
            )
        );
    }
}