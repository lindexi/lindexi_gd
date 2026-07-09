using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies.Models;
using dotnetCampus.Ipc.Serialization;

namespace XiaoXiIme.ImeIpc;

public sealed class XiaoXiImeAotJsonIpcObjectSerializer : IIpcObjectSerializer
{
    private readonly JsonSerializerContext _jsonSerializerContext;

    public XiaoXiImeAotJsonIpcObjectSerializer(JsonSerializerContext jsonSerializerContext)
    {
        ArgumentNullException.ThrowIfNull(jsonSerializerContext);

        _jsonSerializerContext = jsonSerializerContext;
    }

    public byte[] Serialize(object? value)
    {
        if (value is null)
        {
            return JsonSerializer.SerializeToUtf8Bytes<object?>(null, GetJsonTypeInfo<object?>());
        }

        return JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), _jsonSerializerContext);
    }

    public void Serialize(Stream stream, object? value)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (value is null)
        {
            JsonSerializer.Serialize(stream, (object?)null, GetJsonTypeInfo<object?>());
            return;
        }

        JsonSerializer.Serialize(stream, value, value.GetType(), _jsonSerializerContext);
    }

    public IpcJsonElement SerializeToElement(object? value)
    {
        if (value is null)
        {
            return new IpcJsonElement
            {
                RawValueOnSystemTextJson = null,
            };
        }

        return new IpcJsonElement
        {
            RawValueOnSystemTextJson = JsonSerializer.SerializeToElement(value, value.GetType(), _jsonSerializerContext),
        };
    }

    public T Deserialize<T>(byte[] byteList, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(byteList);

        return TryGetJsonTypeInfo<T>(out var jsonTypeInfo)
            ? JsonSerializer.Deserialize(new ReadOnlySpan<byte>(byteList, offset, count), jsonTypeInfo)! 
            : default!;
    }

    public T Deserialize<T>(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return TryGetJsonTypeInfo<T>(out var jsonTypeInfo)
            ? JsonSerializer.Deserialize(stream, jsonTypeInfo)! 
            : default!;
    }

    public T Deserialize<T>(IpcJsonElement jsonElement)
    {
        if (jsonElement.RawValueOnSystemTextJson is not { } rawValue)
        {
            return default!;
        }

        return TryGetJsonTypeInfo<T>(out var jsonTypeInfo)
            ? JsonSerializer.Deserialize(rawValue, jsonTypeInfo)! 
            : default!;
    }

    private JsonTypeInfo<T> GetJsonTypeInfo<T>()
    {
        var jsonTypeInfo = _jsonSerializerContext.GetTypeInfo(typeof(T));
        if (jsonTypeInfo is not JsonTypeInfo<T> typedJsonTypeInfo)
        {
            throw new InvalidOperationException($"JSON type info for {typeof(T)} is not available in {nameof(XiaoXiImeIpcJsonSerializerContext)}.");
        }

        return typedJsonTypeInfo;
    }

    private bool TryGetJsonTypeInfo<T>(out JsonTypeInfo<T> jsonTypeInfo)
    {
        if (_jsonSerializerContext.GetTypeInfo(typeof(T)) is JsonTypeInfo<T> typedJsonTypeInfo)
        {
            jsonTypeInfo = typedJsonTypeInfo;
            return true;
        }

        jsonTypeInfo = null!;
        return false;
    }
}
