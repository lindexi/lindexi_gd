namespace XiaoXiIme.Foundation;

/// <summary>Immutable target snapshot; screen coordinates are physical pixels, client coordinates are target-window units.</summary>
public sealed record ImeCandidateAnchor(
    ImeSessionId SessionId,
    long WindowHandle,
    uint ProcessId,
    uint ThreadId,
    int ClientX,
    int ClientY,
    int ScreenX,
    int ScreenY,
    int ExcludeTop,
    uint Dpi,
    string Source,
    bool IsValid,
    string? Failure = null,
    int? ClientExcludeTop = null);
