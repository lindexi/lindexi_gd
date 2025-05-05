
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using Newtonsoft.Json;

var response = new JsonIpcDirectRoutedHandleRequestExceptionResponse()
{
    ExceptionInfo = new JsonIpcDirectRoutedHandleRequestExceptionInfo()
    {
        ExceptionType = "Foo"
    }
};

string jsonText = JsonConvert.SerializeObject(response);

Console.WriteLine(jsonText); // {"__$Exception":{"ExceptionType":"Foo"}}

JsonIpcDirectRoutedHandleRequestExceptionResponse? jsonIpcDirectRoutedHandleRequestExceptionResponse = JsonConvert.DeserializeObject<JsonIpcDirectRoutedHandleRequestExceptionResponse>(jsonText);

Console.WriteLine(jsonIpcDirectRoutedHandleRequestExceptionResponse?.ExceptionInfo?.ExceptionType); // Foo