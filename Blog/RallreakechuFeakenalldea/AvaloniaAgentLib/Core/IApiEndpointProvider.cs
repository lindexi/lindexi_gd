namespace AvaloniaAgentLib.Core;

/// <summary>
/// 提供当前可用的 API 终结点配置。
/// </summary>
public interface IApiEndpointProvider
{
    /// <summary>
    /// 获取当前 API 终结点。
    /// </summary>
    ApiEndpoint GetApiEndpoint();
}
