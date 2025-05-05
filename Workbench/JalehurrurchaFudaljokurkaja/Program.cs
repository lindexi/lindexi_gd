using System.Text.Json;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;

var response = new JsonIpcDirectRoutedHandleRequestExceptionResponse()
{
    ExceptionInfo = new JsonIpcDirectRoutedHandleRequestExceptionInfo()
    {
        ExceptionType = "Foo"
    }
};

var jsonText = JsonSerializer.Serialize(response);
Console.WriteLine(jsonText); // {"__$Exception":{"ExceptionType":"Foo"}}

var jsonIpcDirectRoutedHandleRequestExceptionResponse = JsonSerializer.Deserialize<JsonIpcDirectRoutedHandleRequestExceptionResponse>(jsonText);
Console.WriteLine(jsonIpcDirectRoutedHandleRequestExceptionResponse?.ExceptionInfo?.ExceptionType); // Foo