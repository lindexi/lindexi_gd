namespace XiaoXiIme.ImeUi.Avalonia;

public sealed record CandidateWindowCandidateViewModel(
    int DisplayIndex,
    int CandidateIndex,
    string Text,
    string Reading,
    bool IsSelected)
{
    public string Background => IsSelected ? "#DCEAFF" : "#00FFFFFF";

    public string Foreground => IsSelected ? "#1557B0" : "#202124";

    public string IndexForeground => IsSelected ? "#1557B0" : "#666666";
}
