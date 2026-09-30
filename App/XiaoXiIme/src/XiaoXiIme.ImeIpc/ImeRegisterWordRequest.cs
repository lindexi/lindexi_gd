using System.Text.Json.Serialization;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>
/// Registers, unregisters, or enumerates user dictionary words without changing the session composition.
/// </summary>
public sealed record ImeRegisterWordRequest(
    ImeRegisterWordAction Action = ImeRegisterWordAction.Register,
    string Reading = "",
    string Text = "",
    uint Style = 0,
    ImeSessionId SessionId = default)
{
    public ImeSessionId EffectiveSessionId => SessionId.Effective;
}

[JsonConverter(typeof(JsonCamelCaseStringEnumConverter))]
public enum ImeRegisterWordAction
{
    Register,
    Unregister,
    Enumerate,
}
