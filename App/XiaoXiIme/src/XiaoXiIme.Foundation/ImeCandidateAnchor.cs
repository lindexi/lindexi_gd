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
    int? ClientExcludeTop = null,
    string? WindowIdentityProperty = null,
    long WindowIdentity = 0,
    long ObservationSequence = 0,
    ImeAnchorRectangle? ClientExclusion = null);

/// <summary>Ordered rectangle in target client coordinates, including zero and negative origins.</summary>
public sealed record ImeAnchorRectangle(int Left, int Top, int Right, int Bottom);
