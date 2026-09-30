namespace XiaoXiIme.Foundation;

public sealed record ImeUiState(
    bool CandidateWindowVisible,
    CompositionText Composition,
    IReadOnlyList<ImeCandidate> Candidates,
    ImeCandidateWindowState CandidateWindow,
    ImeGuideline Guideline,
    int AnchorX = 0,
    int AnchorY = 0,
    ImeAboutState? About = null,
    string? DiagnosticText = null,
    bool IsHostUnavailable = false,
    bool IsUsingFallbackDictionary = false)
{
    public ImeAboutState EffectiveAbout => About ?? ImeAboutState.SeWzc;

    public static ImeUiState Empty { get; } = new(
        CandidateWindowVisible: false,
        CompositionText.Empty,
        Array.Empty<ImeCandidate>(),
        ImeCandidateWindowState.Empty,
        ImeGuideline.Empty,
        About: ImeAboutState.SeWzc);

    public static ImeUiState FromSnapshot(
        ImeSessionSnapshot snapshot,
        string? diagnosticText = null,
        bool isHostUnavailable = false,
        bool isUsingFallbackDictionary = false)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new ImeUiState(
            CandidateWindowVisible: snapshot.IsComposing && snapshot.Candidates.Count > 0,
            snapshot.Composition,
            snapshot.Candidates,
            snapshot.CandidateWindow,
            snapshot.EffectiveGuideline,
            About: ImeAboutState.SeWzc,
            DiagnosticText: diagnosticText,
            IsHostUnavailable: isHostUnavailable,
            IsUsingFallbackDictionary: isUsingFallbackDictionary);
    }
}
