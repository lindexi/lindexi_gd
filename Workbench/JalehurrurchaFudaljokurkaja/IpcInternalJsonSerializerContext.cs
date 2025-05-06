using System.Text.Json.Serialization;

namespace dotnetCampus.Ipc.IpcRouteds.DirectRouteds;

[JsonSerializable(typeof(JsonIpcDirectRoutedHandleRequestExceptionResponse))]
[JsonSerializable(typeof(JsonIpcDirectRoutedHandleRequestExceptionResponse.JsonIpcDirectRoutedHandleRequestExceptionInfo))]
internal partial class IpcInternalJsonSerializerContext : JsonSerializerContext
{
}