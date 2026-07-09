namespace XiaoXiIme.Foundation;

public readonly record struct ImeKey(
    ImeKeyKind Kind,
    char Character = '\0',
    int CandidateIndex = -1)
{
    public static ImeKey FromCharacter(char character) => new(ImeKeyKind.Character, character);

    public static ImeKey PreviousCandidate() => new(ImeKeyKind.PreviousCandidate);

    public static ImeKey NextCandidate() => new(ImeKeyKind.NextCandidate);

    public static ImeKey PreviousCandidatePage() => new(ImeKeyKind.PreviousCandidatePage);

    public static ImeKey NextCandidatePage() => new(ImeKeyKind.NextCandidatePage);

    public static ImeKey FirstCandidate() => new(ImeKeyKind.FirstCandidate);

    public static ImeKey LastCandidate() => new(ImeKeyKind.LastCandidate);

    public static ImeKey MoveCompositionCaretLeft() => new(ImeKeyKind.MoveCompositionCaretLeft);

    public static ImeKey MoveCompositionCaretRight() => new(ImeKeyKind.MoveCompositionCaretRight);

    public static ImeKey SelectCandidate(int candidateIndex) => new(ImeKeyKind.CandidateSelection, CandidateIndex: candidateIndex);
}