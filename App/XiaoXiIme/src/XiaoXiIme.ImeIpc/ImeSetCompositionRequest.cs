using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>
/// Replaces the session composition string. An empty composition cancels the current composition.
/// </summary>
public sealed record ImeSetCompositionRequest(
    string Composition = "",
    ImeSessionId SessionId = default)
{
    public ImeSessionId EffectiveSessionId => SessionId.Effective;
}
