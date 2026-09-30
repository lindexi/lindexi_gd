using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

public sealed record ImeSetCompositionResponse(
    ImeProcessResult Result,
    ImeSessionId SessionId = default);
