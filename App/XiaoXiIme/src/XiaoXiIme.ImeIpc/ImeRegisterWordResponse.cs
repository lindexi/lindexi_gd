using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

public sealed record ImeRegisterWordResponse(
    bool Succeeded,
    ImeRegisterWordEntry[] Entries,
    ImeSessionId SessionId = default);