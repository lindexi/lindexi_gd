using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>
/// Requests a session snapshot. An empty session id selects the host's last active session.
/// </summary>
public sealed record ImeSnapshotRequest(ImeSessionId SessionId = default);
