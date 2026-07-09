namespace XiaoXiIme.ImeUi.Avalonia;

public sealed record CandidateWindowViewState(
    bool IsVisible,
    string CompositionText,
    IReadOnlyList<CandidateWindowCandidateViewModel> Candidates,
    int Selection,
    int PageStart,
    int PageSize,
    int CurrentPage,
    int TotalPages,
    string GuidelineText,
    int AnchorX,
    int AnchorY)
{
    public static CandidateWindowViewState Hidden { get; } = new(
        IsVisible: false,
        CompositionText: string.Empty,
        Array.Empty<CandidateWindowCandidateViewModel>(),
        Selection: 0,
        PageStart: 0,
        PageSize: 0,
        CurrentPage: 0,
        TotalPages: 0,
        GuidelineText: string.Empty,
        AnchorX: 0,
        AnchorY: 0);
}
