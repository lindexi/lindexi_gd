using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

public sealed record ImeProcessKeyResponse(
    ImeProcessResult Result,
    ImeSessionId SessionId = default,
    long Generation = 0,
    long ProcessedThroughSequence = 0);
