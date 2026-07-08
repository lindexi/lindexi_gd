namespace XiaoXiIme.ImeUi.Avalonia;

public sealed record CandidateWindowCandidateViewModel(
    int DisplayIndex,
    int CandidateIndex,
    string Text,
    string Reading,
    bool IsSelected);
