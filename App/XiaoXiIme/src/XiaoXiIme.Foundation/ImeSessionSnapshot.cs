namespace XiaoXiIme.Foundation;

public sealed record ImeSessionSnapshot(
    CompositionText Composition,
    IReadOnlyList<ImeCandidate> Candidates,
    ImeCandidateWindowState CandidateWindow,
    bool IsComposing,
    ImeGuideline? Guideline = null,
    bool AcceptsSemicolon = false,
    bool EatsConfirmationSpace = false,
    bool AcceptsApostrophe = false)
{
    public ImeGuideline EffectiveGuideline => Guideline ?? ImeGuideline.Empty;

    public static ImeSessionSnapshot Empty { get; } = new(
        CompositionText.Empty,
        Array.Empty<ImeCandidate>(),
        ImeCandidateWindowState.Empty,
        false,
        ImeGuideline.Empty);
}