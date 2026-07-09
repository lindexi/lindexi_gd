namespace XiaoXiIme.Foundation;

public sealed record ImeCandidateWindowState(
    int Selection,
    int PageStart,
    int PageSize)
{
    public static ImeCandidateWindowState Empty { get; } = new(0, 0, 0);
}
