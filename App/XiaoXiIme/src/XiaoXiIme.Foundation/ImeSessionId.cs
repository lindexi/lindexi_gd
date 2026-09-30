using System.Text.Json;
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
        handle == 0 ? Default : new($"himc:{unchecked((ulong)handle):x}");

    /// <summary>
    /// True when the id was omitted and the host should fall back to its last active session.
    /// </summary>
    public bool IsUnspecified => string.IsNullOrWhiteSpace(Value);

    public ImeSessionId Effective => IsUnspecified ? Default : this;

    public override string ToString() => Effective.Value;
}

/// <summary>
/// Serializes <see cref="ImeSessionId"/> as a JSON string, or as null when the session is unspecified.
/// The converter is public so AOT source generation in other assemblies can emit metadata for it.
/// </summary>
public sealed class ImeSessionIdJsonConverter : JsonConverter<ImeSessionId>
{
    public override ImeSessionId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => ImeSessionId.Unspecified,
            JsonTokenType.String => new ImeSessionId(reader.GetString()),
            _ => throw new JsonException($"Unexpected token {reader.TokenType} when reading {nameof(ImeSessionId)}."),
        };
    }

    public override void Write(Utf8JsonWriter writer, ImeSessionId value, JsonSerializerOptions options)
    {
        if (value.IsUnspecified)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}