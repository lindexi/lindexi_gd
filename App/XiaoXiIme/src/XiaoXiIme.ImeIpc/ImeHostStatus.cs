namespace XiaoXiIme.ImeIpc;

public sealed record ImeHostStatus(
    bool IsRunning,
    string? LastError = null)
{
    public static ImeHostStatus Stopped { get; } = new(false);

    public static ImeHostStatus Running { get; } = new(true);
}
