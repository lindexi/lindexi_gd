using Newtonsoft.Json;

namespace dotnetCampus.Ipc.IpcRouteds.DirectRouteds;

/// <summary>
/// 直接路由的 IPC 通讯的异常信息，即 IPC 服务端的业务层通讯异常。如参数不匹配、执行的业务端逻辑抛出异常等
/// </summary>
internal class JsonIpcDirectRoutedHandleRequestExceptionResponse
{
    [JsonProperty("__$Exception")]
    public JsonIpcDirectRoutedHandleRequestExceptionInfo? ExceptionInfo { get; set; }
}