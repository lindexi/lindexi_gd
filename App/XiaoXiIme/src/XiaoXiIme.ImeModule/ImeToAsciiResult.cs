using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeModule;

public sealed record ImeToAsciiResult(
    bool Handled,
    string? CommitText,
    ImeSessionSnapshot Snapshot)
{
    public static ImeToAsciiResult NotHandled { get; } = new(false, null, ImeSessionSnapshot.Empty);

    public static ImeToAsciiResult FromProcessResult(ImeProcessResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ImeToAsciiResult(result.Handled, result.CommitText, result.Snapshot);
    }
}
