using System.Text.Json;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;

var response = new JsonIpcDirectRoutedHandleRequestExceptionResponse()
{
    ExceptionInfo = new JsonIpcDirectRoutedHandleRequestExceptionResponse.JsonIpcDirectRoutedHandleRequestExceptionInfo()
    {
        ExceptionType = "Foo"
    }
};

var jsonText = JsonSerializer.Serialize(response, IpcInternalJsonSerializerContext.Default.Options);
Console.WriteLine(jsonText); // {"__$Exception":{"ExceptionType":"Foo"}}

var jsonIpcDirectRoutedHandleRequestExceptionResponse = JsonSerializer.Deserialize<JsonIpcDirectRoutedHandleRequestExceptionResponse>(jsonText, IpcInternalJsonSerializerContext.Default.Options);
Console.WriteLine(jsonIpcDirectRoutedHandleRequestExceptionResponse?.ExceptionInfo?.ExceptionType); // Foo