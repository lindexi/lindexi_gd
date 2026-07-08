namespace XiaoXiIme.Foundation;

public sealed record ImeUiState(
    bool CandidateWindowVisible,
    CompositionText Composition,
    IReadOnlyList<ImeCandidate> Candidates,
    ImeCandidateWindowState CandidateWindow,
    ImeGuideline Guideline,
    int AnchorX = 0,
    int AnchorY = 0)
{
    public static ImeUiState Empty { get; } = new(
        CandidateWindowVisible: false,
        CompositionText.Empty,
        Array.Empty<ImeCandidate>(),
        ImeCandidateWindowState.Empty,
        ImeGuideline.Empty);

    public static ImeUiState FromSnapshot(ImeSessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new ImeUiState(
            CandidateWindowVisible: snapshot.IsComposing && snapshot.Candidates.Count > 0,
            snapshot.Composition,
            snapshot.Candidates,
            snapshot.CandidateWindow,
            snapshot.EffectiveGuideline);
    }
}
