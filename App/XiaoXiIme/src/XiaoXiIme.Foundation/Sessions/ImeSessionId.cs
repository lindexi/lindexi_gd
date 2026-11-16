using System.Text.Json.Serialization;

namespace XiaoXiIme.Foundation;

[JsonConverter(typeof(ImeSessionIdJsonConverter))]
public readonly record struct ImeSessionId(string Value)
{
    public static ImeSessionId Default { get; } = new("default");

    /// <summary>
    /// Requests the host's last active session instead of creating or targeting the default session.
    /// </summary>
    public static ImeSessionId Unspecified => default;

    public static ImeSessionId Create() => new(Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Maps a process-local HIMC handle to a stable IPC session id without sending a dereferenceable pointer.
    /// </summary>
    public static ImeSessionId FromHimc(nint handle) =>
        handle == 0 ? Default : new($"himc:{Environment.ProcessId:x}:{unchecked((ulong)handle):x}");

    /// <summary>
    /// True when the id was omitted and the host should fall back to its last active session.
    /// </summary>
    public bool IsUnspecified => string.IsNullOrWhiteSpace(Value);

    public ImeSessionId Effective => IsUnspecified ? Default : this;

    public override string ToString() => Effective.Value;
}