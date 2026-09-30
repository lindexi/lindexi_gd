using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>
/// Reports which session the host dropped. <see cref="SessionId"/> is unspecified when every session was reset.
/// </summary>
public sealed record ImeResetSessionResponse(ImeSessionId SessionId, bool Reset);
