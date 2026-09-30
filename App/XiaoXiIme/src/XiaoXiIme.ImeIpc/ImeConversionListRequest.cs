using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>
/// Queries conversion or reverse-conversion candidates for a source string without changing the session composition.
/// </summary>
public sealed record ImeConversionListRequest(
    string Source = "",
    int MaxCount = 0,
    ImeSessionId SessionId = default,
    ImeConversionListKind Kind = ImeConversionListKind.Conversion)
{
    public ImeSessionId EffectiveSessionId => SessionId.Effective;
}
