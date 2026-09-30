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
        Assert.Equal(2, state.CurrentPage);
        Assert.Equal(2, state.TotalPages);
        Assert.Equal(3, state.Candidates.Count);
        Assert.Equal(10, state.Selection);
        Assert.Equal(2, state.Candidates.Single(candidate => candidate.IsSelected).DisplayIndex);
        Assert.Equal(100, state.AnchorX);
        Assert.Equal(200, state.AnchorY);
        Assert.Equal(ImeAboutState.SeWzcNotice, state.AttributionText);
        Assert.True(state.HasAttribution);

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
            new ImeGuideline(ImeGuidelineLevel.NoCandidate, "无候选：zz"),
            DiagnosticText: "Using the minimal fallback dictionary.",
            IsUsingFallbackDictionary: true);

        var state = controller.Update(uiState);

        Assert.False(state.IsVisible);
        Assert.Empty(state.Candidates);
        Assert.Equal("zz", state.CompositionText);
        Assert.Equal("无候选：zz", state.GuidelineText);
        Assert.Equal(ImeAboutState.SeWzcNotice, state.AttributionText);
        Assert.Equal("Using the minimal fallback dictionary.", state.DiagnosticText);

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
            new ImeCandidateWindowState(9, 0, 1),
            ImeGuideline.Empty);

        var state = controller.Update(uiState);

        Assert.True(state.IsVisible);
        Assert.Equal(9, state.PageStart);
        Assert.Equal(1, state.PageSize);
        Assert.Equal(2, state.CurrentPage);
        Assert.Equal(2, state.TotalPages);
        Assert.Equal(9, state.Candidates.Single(candidate => candidate.IsSelected).CandidateIndex);

        return Task.CompletedTask;
    }

    [Fact(Timeout = 2_000)]
    public Task Update_WhenSelectionMovesAcrossPageBoundaryThenUpdatesPageAndHighlight()
    {
        var controller = new CandidateWindowController();
        var candidates = CreateCandidates(12);

        var firstPage = controller.Update(new ImeUiState(
            CandidateWindowVisible: true,
            new CompositionText("test", "test", 4),
            candidates,
            new ImeCandidateWindowState(8, 0, 9),
            ImeGuideline.Empty));
        var secondPage = controller.Update(new ImeUiState(
            CandidateWindowVisible: true,
            new CompositionText("test", "test", 4),
            candidates,
            new ImeCandidateWindowState(9, 9, 3),
            ImeGuideline.Empty));
        var returnedPage = controller.Update(new ImeUiState(
            CandidateWindowVisible: true,
            new CompositionText("test", "test", 4),
            candidates,
            new ImeCandidateWindowState(8, 0, 9),
            ImeGuideline.Empty));

        Assert.Equal(1, firstPage.CurrentPage);
        Assert.Equal(9, firstPage.Candidates.Single(candidate => candidate.IsSelected).DisplayIndex);
        Assert.Equal(2, secondPage.CurrentPage);
        Assert.Equal(1, secondPage.Candidates.Single(candidate => candidate.IsSelected).DisplayIndex);
        Assert.Equal(1, returnedPage.CurrentPage);
        Assert.Equal(8, returnedPage.Candidates.Single(candidate => candidate.IsSelected).CandidateIndex);
        Assert.Single(returnedPage.Candidates, candidate => candidate.IsSelected);

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
