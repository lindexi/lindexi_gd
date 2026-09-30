using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

public sealed record ImeConversionListResponse(
    ImeCandidate[] Candidates,
    ImeSessionId SessionId = default);
