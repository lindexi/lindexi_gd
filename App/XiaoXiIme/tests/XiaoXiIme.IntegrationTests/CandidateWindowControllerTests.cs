using XiaoXiIme.Foundation;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

public class CandidateWindowControllerTests
{
    [Fact(Timeout = 2_000)]
    public Task Update_VisibleUiState_ShowsCurrentPageAndSelectedCandidate()
    {
        var controller = new CandidateWindowController();
        var uiState = new ImeUiState(
            CandidateWindowVisible: true,
            new CompositionText("ni", "ni", 2),
            CreateCandidates(12),
            new ImeCandidateWindowState(10, 9, 3),
            new ImeGuideline(ImeGuidelineLevel.Reading, "ni"),
            AnchorX: 100,
            AnchorY: 200);

        var state = controller.Update(uiState);

        Assert.True(state.IsVisible);
        Assert.Equal("ni", state.CompositionText);
        Assert.Equal(9, state.PageStart);
        Assert.Equal(3, state.PageSize);
        Assert.Equal(4, state.CurrentPage);
        Assert.Equal(4, state.TotalPages);
        Assert.Equal(3, state.Candidates.Count);
        Assert.Equal(10, state.Selection);
        Assert.Equal(2, state.Candidates.Single(candidate => candidate.IsSelected).DisplayIndex);
        Assert.Equal(100, state.AnchorX);
        Assert.Equal(200, state.AnchorY);

        return Task.CompletedTask;
    }

    [Fact(Timeout = 2_000)]
    public Task Update_HiddenUiState_HidesCandidateWindowButKeepsCompositionAndGuideline()
    {
        var controller = new CandidateWindowController();
        var uiState = new ImeUiState(
            CandidateWindowVisible: false,
            new CompositionText("zz", "zz", 2),
            Array.Empty<ImeCandidate>(),
            ImeCandidateWindowState.Empty,
            new ImeGuideline(ImeGuidelineLevel.NoCandidate, "无候选：zz"));

        var state = controller.Update(uiState);

        Assert.False(state.IsVisible);
        Assert.Empty(state.Candidates);
        Assert.Equal("zz", state.CompositionText);
        Assert.Equal("无候选：zz", state.GuidelineText);

        return Task.CompletedTask;
    }

    [Fact(Timeout = 2_000)]
    public Task Update_SelectionOutsidePage_NormalizesPageToSelection()
    {
        var controller = new CandidateWindowController();
        var uiState = new ImeUiState(
            CandidateWindowVisible: true,
            new CompositionText("test", "test", 4),
            CreateCandidates(10),
            new ImeCandidateWindowState(8, 0, 3),
            ImeGuideline.Empty);

        var state = controller.Update(uiState);

        Assert.True(state.IsVisible);
        Assert.Equal(6, state.PageStart);
        Assert.Equal(3, state.PageSize);
        Assert.Equal(3, state.CurrentPage);
        Assert.Equal(4, state.TotalPages);
        Assert.Equal(8, state.Candidates.Single(candidate => candidate.IsSelected).CandidateIndex);

        return Task.CompletedTask;
    }

    [Fact(Timeout = 2_000)]
    public Task Hide_ReturnsHiddenState()
    {
        var controller = new CandidateWindowController();
        controller.Update(new ImeUiState(
            CandidateWindowVisible: true,
            new CompositionText("ni", "ni", 2),
            CreateCandidates(1),
            new ImeCandidateWindowState(0, 0, 1),
            ImeGuideline.Empty));

        var state = controller.Hide();

        Assert.False(state.IsVisible);
        Assert.Empty(state.Candidates);
        Assert.Same(state, controller.CurrentState);

        return Task.CompletedTask;
    }

    private static ImeCandidate[] CreateCandidates(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new ImeCandidate($"候选{index}", $"read{index}"))
            .ToArray();
    }
}
