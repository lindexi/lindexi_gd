namespace XiaoXiIme.ImeUi.Avalonia;

public sealed record CandidateWindowCandidateViewModel(
    int DisplayIndex,
    int CandidateIndex,
    string Text,
    string Reading,
    bool IsSelected)
{
    public string Background => IsSelected ? "#2563EB" : "#00FFFFFF";

    public string Foreground => IsSelected ? "#FFFFFFFF" : "#FF111827";

    public string SecondaryForeground => IsSelected ? "#FFDCE7FF" : "#FF6B7280";
}
