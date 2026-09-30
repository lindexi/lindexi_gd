using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>
/// Requests that the host drop a session context. An empty session id selects the last active session unless <see cref="ResetAll"/> is true.
/// </summary>
public sealed record ImeResetSessionRequest(ImeSessionId SessionId = default, bool ResetAll = false);
