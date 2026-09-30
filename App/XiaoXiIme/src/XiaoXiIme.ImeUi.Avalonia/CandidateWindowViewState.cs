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
    int AnchorY,
    string AttributionText = "",
    string DiagnosticText = "")
{
    public string PageText => TotalPages > 1 ? $"{CurrentPage}/{TotalPages}" : string.Empty;

    public bool HasPageText => !string.IsNullOrWhiteSpace(PageText);

    public bool HasGuideline => !string.IsNullOrWhiteSpace(GuidelineText);

    public bool HasAttribution => !string.IsNullOrWhiteSpace(AttributionText);

    public bool HasDiagnostic => !string.IsNullOrWhiteSpace(DiagnosticText);

    public bool HasFooter => HasGuideline || HasAttribution || HasDiagnostic;

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
