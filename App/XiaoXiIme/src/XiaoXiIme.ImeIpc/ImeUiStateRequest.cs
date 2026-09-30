using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>
/// Requests candidate-window state. An empty session id selects the host's last active session.
/// </summary>
public sealed record ImeUiStateRequest(ImeSessionId SessionId = default);
