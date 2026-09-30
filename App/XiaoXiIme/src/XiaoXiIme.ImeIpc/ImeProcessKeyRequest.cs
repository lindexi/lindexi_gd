using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

public sealed record ImeProcessKeyRequest(
    ImeKey Key,
    ImeSessionId SessionId = default,
    long Generation = 0,
    long SequenceNumber = 0)
{
    public ImeSessionId EffectiveSessionId => SessionId.Effective;
}
