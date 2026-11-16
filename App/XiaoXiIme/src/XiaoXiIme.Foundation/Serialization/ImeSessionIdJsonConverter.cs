using System.Text.Json;
using System.Text.Json.Serialization;

namespace XiaoXiIme.Foundation;

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
            JsonTokenType.String => new ImeSessionId(reader.GetString() ?? string.Empty),
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