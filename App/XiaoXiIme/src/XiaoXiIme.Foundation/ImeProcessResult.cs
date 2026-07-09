namespace XiaoXiIme.Foundation;

public sealed record ImeProcessResult(
    ImeSessionSnapshot Snapshot,
    string? CommitText,
    bool Handled);